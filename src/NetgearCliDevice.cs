using PepperDash.Core;
using PepperDash.Core.Logging;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Core.DeviceTypeInterfaces;
using PepperDash.Essentials.Core.Queues;

namespace Essentials.Plugin.Netgear.Cli
{
    /// <summary>
    /// Plugin device template for third party devices that use IBasicCommunication
    /// </summary>
    /// <remarks>
    /// Rename the class to match the device plugin being developed.
    /// </remarks>
    /// <example>
    /// "EssentialsPluginDeviceTemplate" renamed to "SamsungMdcDevice"
    /// </example>
    public class NetgearCliDevice : EssentialsDevice, INetworkSwitchPoeVlanManager
    {
        /// <summary>
        /// It is often desirable to store the config
        /// </summary>
        private NetgearCliConfigObject _config;


        /// <summary>
        /// Returns the password from the config object or the control object
        /// </summary>
        private string _password
        {
            get
            {
                if (_config.Password != null)
                {
                    return _config.Password;
                }
                else
                {
                    return _config.Control.TcpSshProperties.Password;
                }
            }
        }


        /// <summary>
        /// Provides a queue and dedicated worker thread for processing feedback messages from a device.
        /// </summary>
        private GenericQueue TransmitQueue;

        #region IBasicCommunication Properties and Constructor.  Remove if not needed.

        // TODO [ ] Add, modify, remove properties and fields as needed for the plugin being developed
        private readonly IBasicCommunication _comms;
        //private readonly GenericCommunicationMonitor _commsMonitor;

        /// <summary>
        /// Set this value to that of the delimiter used by the API (if applicable)
        /// </summary>
        public const string DELIMITER = "\r";

        private const int WAITTIMEMS = 2000;
        public const int MAX_VLANS = 4093;


        /// <summary>
        /// Connects/disconnects the comms of the plugin device
        /// </summary>
        /// <remarks>
        /// triggers the _comms.Connect/Disconnect as well as thee comms monitor start/stop
        /// </remarks>
        public bool Connect
        {
            get { return _comms.IsConnected; }
            set
            {
                if (value)
                {
                    _comms.Connect();
                }
                else
                {
                    _comms.Disconnect();
                }
            }
        }

        //public method to set the vlan of a port
        //use existing Netgear plugin to login(SSH)


        /// <summary>
        /// Reports connect feedback through the bridge
        /// </summary>
        //public BoolFeedback ConnectFeedback { get; private set; }

        /// <summary>
        /// Reports online feedback through the bridge
        /// </summary>
        //public BoolFeedback OnlineFeedback { get; private set; }

        /// <summary>
        /// Reports socket status feedback through the bridge
        /// </summary>
        //public IntFeedback StatusFeedback { get; private set; }

        /// <summary>
        /// Plugin device constructor for devices that need IBasicCommunication
        /// </summary>
        /// <param name="key"></param>
        /// <param name="name"></param>
        /// <param name="config"></param>
        /// <param name="comms"></param>
        public NetgearCliDevice(string key, string name, NetgearCliConfigObject config, IBasicCommunication comms)
            : base(key, name)
        {
            this.LogInformation("Constructing new {0} instance", name);

            _config = config;

            _comms = comms;

            TransmitQueue = new GenericQueue($"{key}-txQueue", WAITTIMEMS,
                Crestron.SimplSharpPro.CrestronThread.Thread.eThreadPriority.MediumPriority, 100);

            var socket = _comms as ISocketStatus;
            if (socket != null)
            {
                // device comms is IP **ELSE** device comms is RS232
                socket.ConnectionChange += Socket_ConnectionChange;
            }

            _comms.TextReceived += _comms_TextReceived;

            switch (_comms)
            {
                case PepperDash.Core.GenericSshClient sshClient:
                    sshClient.AutoReconnect = true; //default interval is 5000ms
                    break;
            }
        }

        public override bool CustomActivate()
        {
            // wouldn't normally do this, but there are situations where commands are being sent to the switch as part of the post activation sequence. The SSH connection needs to be connected in those situations.
            Connect = true;
            return base.CustomActivate();
        }


        private void _comms_TextReceived(object sender, GenericCommMethodReceiveTextArgs e)
        {
            if (e.Text.Contains("Password:"))
            {
                TransmitQueue.Enqueue(new TransmitMessage(_comms, _password));
            }

            if (e.Text.Contains("Access denied"))
            {
                Debug.LogMessage(Serilog.Events.LogEventLevel.Error, "Access Denied. Please check the password");
            }
        }

        private void Socket_ConnectionChange(object sender, GenericSocketStatusChageEventArgs args)
        {
            Debug.LogMessage(Serilog.Events.LogEventLevel.Information, "Socket Status Change: {status}", this,
                args.Client.ClientStatus.ToString());
        }

        private void EnableConfigMode()
        {
            TransmitQueue.Enqueue(new TransmitMessage(_comms, "enable"));
            TransmitQueue.Enqueue(new TransmitMessage(_comms, "config"));
        }

        private void BackOut(int numExits)
        {
            for (int i = 0; i < numExits; i++)
            {
                TransmitQueue.Enqueue(new TransmitMessage(_comms, "exit"));
            }
        }

        public void ChangeVlan(string port, int vlanID)
        {
            SetPortVlan(port, (uint)vlanID);
        }

        public int GetPortCurrentVlan(string port)
        {
            throw new System.NotImplementedException();
        }

        public void SetPortVlan(string port, uint vlanId)
        {
            if (_password == null)
            {
                this.LogError("Password is null. Please make sure to define the password property at the root properties level for RS232 or in the control.tcpSshProperties object for SSH");
                return;
            }

            if (_comms is ISocketStatus && !_comms.IsConnected)
            {
                this.LogError("Device is not connected. Please check the connection");
                return;
            }

            EnableConfigMode();
            TransmitQueue.Enqueue(new TransmitMessage(_comms, $"interface {port}"));
            TransmitQueue.Enqueue(new TransmitMessage(_comms, $"vlan participation exclude 1-{MAX_VLANS}"));
            TransmitQueue.Enqueue(new TransmitMessage(_comms, $"vlan acceptframe all"));
            TransmitQueue.Enqueue(new TransmitMessage(_comms, $"vlan pvid {vlanId}"));
            TransmitQueue.Enqueue(new TransmitMessage(_comms, $"vlan participation include {vlanId}"));
            BackOut(3);
        }

        public void SetPortPoeState(string port, bool enabled)
        {
            if (_password == null)
            {
                this.LogError("Password is null. Please make sure to define the password property at the root properties level for RS232 or in the control.tcpSshProperties object for SSH");
                return;
            }

            if (_comms is ISocketStatus && !_comms.IsConnected)
            {
                this.LogWarning("Device is not connected. Please check the connection");
                return;
            }

            if (enabled)
            {
                EnableConfigMode();
                TransmitQueue.Enqueue(new TransmitMessage(_comms, $"interface {port}"));
                TransmitQueue.Enqueue(new TransmitMessage(_comms, $"poe"));
                BackOut(2);
                return;
            }
            else
            {
                EnableConfigMode();
                TransmitQueue.Enqueue(new TransmitMessage(_comms, $"interface {port}"));
                TransmitQueue.Enqueue(new TransmitMessage(_comms, $"no poe"));
                BackOut(2);
                return;
            }
        }

        #endregion


        #region Overrides of EssentialsBridgeableDevice

        #endregion
    }
}