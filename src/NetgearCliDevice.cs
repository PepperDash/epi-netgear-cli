// For Basic SIMPL# Classes
// For Basic SIMPL#Pro classes

using PepperDash.Core;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Core.Bridges;
using PepperDash.Essentials.Core.Queues;
using System.Threading;
using System.Threading.Tasks;
using Essentials.Plugin.Netgear.Cli;

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
    public class NetgearCliDevice : EssentialsDevice, ISwitchCommands
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
                if(_config.Password != null)
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

        // _comms gather for ASCII based API's
        // TODO [ ] If not using an ASCII based API, delete the properties below
        private readonly CommunicationGather _commsGather;

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
            Debug.Console(0, this, "Constructing new {0} instance", name);

            _config = config;
            
            _comms = comms;

            TransmitQueue = new GenericQueue($"{key}-txQueue", WAITTIMEMS, Crestron.SimplSharpPro.CrestronThread.Thread.eThreadPriority.MediumPriority, 100);

            var socket = _comms as ISocketStatus;
            if (socket != null)
            {
                // device comms is IP **ELSE** device comms is RS232
                socket.ConnectionChange += socket_ConnectionChange;
            }

            _comms.TextReceived += _comms_TextReceived;
        }

        public override void Initialize()
        {
            Connect = true;
        }



        private void _comms_TextReceived(object sender, GenericCommMethodReceiveTextArgs e)
        {
            //placeholder, add response to password prompt here
        }

        private void socket_ConnectionChange(object sender, GenericSocketStatusChageEventArgs args)
        {
            Debug.LogMessage(Serilog.Events.LogEventLevel.Information, "Socket Status Change: {0}",
                args.Client.ClientStatus.ToString());
        }
        
        public void ChangeVlan(string port, int vlanID)
        {
            if(_password == null)
            {
                Debug.LogMessage(Serilog.Events.LogEventLevel.Error, "Password is null. Please make sure to define the password property at the root properties level for RS232 or in the control.tcpSshProperties object for SSH");
                return;
            }
            //TransmitQueue.Enqueue(new TransmitMessage(_comms, _password));
            /*TransmitQueue.Enqueue(new TransmitMessage(_comms, "enable"));
            TransmitQueue.Enqueue(new TransmitMessage(_comms, "config"));*/
            TransmitQueue.Enqueue(new TransmitMessage(_comms, $"interface {port}"));
            TransmitQueue.Enqueue(new TransmitMessage(_comms, $"vlan participation exclude 1-{MAX_VLANS}"));
            TransmitQueue.Enqueue(new TransmitMessage(_comms, $"vlan acceptframe all"));
            TransmitQueue.Enqueue(new TransmitMessage(_comms, $"vlan pvid {vlanID}"));
            TransmitQueue.Enqueue(new TransmitMessage(_comms, $"vlan participation include {vlanID}"));
            /*TransmitQueue.Enqueue(new TransmitMessage(_comms, "exit"));
            TransmitQueue.Enqueue(new TransmitMessage(_comms, "exit"));
            TransmitQueue.Enqueue(new TransmitMessage(_comms, "exit"));*/
        }

        #endregion


        #region Overrides of EssentialsBridgeableDevice

        #endregion
    }
}