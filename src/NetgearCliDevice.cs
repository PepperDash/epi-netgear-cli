using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text.RegularExpressions;
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

        private ConcurrentQueue<Session> sessions = new ConcurrentQueue<Session>();

        /// <summary>
        /// Synchronizes access to the session queue so the "start when first enqueued" decision and
        /// the completion/advance logic cannot race across the caller thread and the comms thread.
        /// </summary>
        private readonly object _sessionLock = new object();

        /// <summary>
        /// Last-known access VLAN per port, updated when a SetPortVlan session completes.
        /// </summary>
        private readonly ConcurrentDictionary<string, int> _portVlanCache = new ConcurrentDictionary<string, int>();

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

            TransmitQueue = new GenericQueue($"{key}-txQueue", Crestron.SimplSharpPro.CrestronThread.Thread.eThreadPriority.MediumPriority, 100);

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

        public event EventHandler<NetworkSwitchPortEventArgs> PortStateChanged;

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
                return;
            }

            if (e.Text.Contains("Access denied"))
            {
                this.LogError("Access Denied. Please check the password");
                return;
            }

            // Netgear paginates long output with "--More-- or (q)uit", which contains no command prompt
            // and would otherwise stall the session. Send a space to page through until the prompt returns.
            if (e.Text.Contains("--More--"))
            {
                _comms.SendText(" ");
                return;
            }

            // Surface command-level errors so a command that was accepted by the parser but rejected by
            // the switch is not silently reported as a successful state change. The session is still
            // allowed to advance on the following prompt so the pipeline does not stall.
            if (ContainsCommandError(e.Text))
            {
                this.LogWarning("Switch reported a command error: {response}", e.Text.Trim());
            }

            // A device prompt indicates the switch is ready for the next command. Netgear managed
            // switches render the prompt as the system name in parentheses followed by the mode symbol,
            // e.g. "(M4250-...)#", "(name)(Config)#", or "(name)>". Anchor on ")" + optional space +
            // "#"/">" so a stray '#'/'>' inside output is not mistaken for a prompt, while still
            // tolerating firmware variants that put whitespace before the symbol (e.g. "(name) #").
            if (!Regex.IsMatch(e.Text, @"\)\s*[#>]"))
            {
                return;
            }

            Session completed = null;
            Session sessionToStart = null;

            // Only queue mutations are performed under the lock. Events are raised outside the lock
            // because PortStateChanged handlers (e.g. CameraManager) call back into SetPortPoeState /
            // SetPortVlan, which re-enter this lock and would otherwise deadlock.
            lock (_sessionLock)
            {
                if (!sessions.TryPeek(out var currentSession))
                {
                    // No active session - this prompt is from login/idle, ignore it.
                    return;
                }

                if (currentSession.Messages.Count > 0)
                {
                    var message = currentSession.Messages[0];
                    currentSession.Messages.RemoveAt(0);
                    TransmitQueue.Enqueue(message);
                    return;
                }

                // All messages for the current session have been sent - the session is complete.
                completed = currentSession;
                sessions.TryDequeue(out _);
                sessions.TryPeek(out sessionToStart);
            }

            OnSessionCompleted(completed);

            if (sessionToStart != null)
            {
                StartSession(sessionToStart);
            }
        }

        private void Socket_ConnectionChange(object sender, GenericSocketStatusChageEventArgs args)
        {
            Debug.LogMessage(Serilog.Events.LogEventLevel.Information, "Socket Status Change: {status}", this,
                args.Client.ClientStatus.ToString());
        }

        private List<IQueueMessage> EnableConfigMode()
        {
            return new List<IQueueMessage>() {new TransmitMessage(_comms, "enable"),
            new TransmitMessage(_comms, "config")};
        }

        private List<IQueueMessage> BackOut(int numExits)
        {
            var messages = new List<IQueueMessage>();

            for (int i = 0; i < numExits; i++)
            {
                messages.Add(new TransmitMessage(_comms, "exit"));
            }

            return messages;
        }

        private void StartSession(Session session)
        {
            NetworkSwitchPortEventType changeType;

            if (session.MethodName == "SetPortVlan")
            {
                changeType = NetworkSwitchPortEventType.VlanChangeInProgress;
            }
            else if (session.MethodName == "SetPortPoeState")
            {
                changeType = session.PortPoeState == true
                    ? NetworkSwitchPortEventType.PoeEnableInProgress
                    : NetworkSwitchPortEventType.PoeDisableInProgress;
            }
            else
            {
                this.LogWarning("Unknown session method name: {methodName}", session.MethodName);
                return;
            }

            PortStateChanged?.Invoke(this, new NetworkSwitchPortEventArgs(session.Port, changeType));

            if (session.Messages.Count > 0)
            {
                var message = session.Messages[0];
                session.Messages.RemoveAt(0);
                TransmitQueue.Enqueue(message);
            }
        }

        /// <summary>
        /// Enqueues a session and starts it if it is the only one queued. The enqueue and the
        /// "is this the only session" decision are performed atomically so concurrent callers cannot
        /// both skip starting the pipeline (which would stall the queue).
        /// </summary>
        private void EnqueueSession(Session session)
        {
            bool startNow;

            lock (_sessionLock)
            {
                sessions.Enqueue(session);
                startNow = sessions.Count == 1;
            }

            if (startNow)
            {
                StartSession(session);
            }
        }

        /// <summary>
        /// Raises the completion PortStateChanged event for a drained session and updates the cached
        /// port VLAN when a SetPortVlan session completes. Must be called outside <see cref="_sessionLock"/>.
        /// </summary>
        private void OnSessionCompleted(Session session)
        {
            if (session == null)
            {
                return;
            }

            NetworkSwitchPortEventType changeType;

            if (session.MethodName == "SetPortVlan")
            {
                if (session.TargetVlan.HasValue)
                {
                    _portVlanCache[session.Port] = (int)session.TargetVlan.Value;
                }

                changeType = NetworkSwitchPortEventType.VlanChanged;
            }
            else if (session.MethodName == "SetPortPoeState")
            {
                changeType = session.PortPoeState == true
                    ? NetworkSwitchPortEventType.PoEEnabled
                    : NetworkSwitchPortEventType.PoEDisabled;
            }
            else
            {
                this.LogWarning("Unknown session method name: {methodName}", session.MethodName);
                return;
            }

            PortStateChanged?.Invoke(this, new NetworkSwitchPortEventArgs(session.Port, changeType));
        }

        /// <summary>
        /// Best-effort detection of common Netgear CLI error responses so a rejected command can be logged.
        /// </summary>
        private static bool ContainsCommandError(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return false;
            }

            // Match phrase-specific error responses rather than loose substrings. A bare "Invalid"
            // check falsely tripped on valid output such as "Invalid Signature Counter" in
            // `show poe port info`, so use the actual error phrases the switch emits.
            return text.IndexOf("% ", StringComparison.OrdinalIgnoreCase) >= 0
                || text.IndexOf("Command not found", StringComparison.OrdinalIgnoreCase) >= 0
                || text.IndexOf("Incomplete command", StringComparison.OrdinalIgnoreCase) >= 0
                || text.IndexOf("invalid interface", StringComparison.OrdinalIgnoreCase) >= 0
                || text.IndexOf("Unrecognized command", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        public void ChangeVlan(string port, int vlanID)
        {
            SetPortVlan(port, (uint)vlanID);
        }

        /// <inheritdoc />
        public int GetPortCurrentVlan(string port)
        {
            // Returns the last VLAN this device applied to the port, or -1 when the port has not been
            // set yet. Per the interface contract, -1 signals "unavailable".
            if (!string.IsNullOrEmpty(port) && _portVlanCache.TryGetValue(port, out var vlan))
            {
                return vlan;
            }

            return -1;
        }

        /// <inheritdoc />
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

            var messages = new List<IQueueMessage>();

            messages.AddRange(EnableConfigMode());
            messages.Add(new TransmitMessage(_comms, $"interface {port}"));
            messages.Add(new TransmitMessage(_comms, $"vlan participation exclude 1-{MAX_VLANS}"));
            messages.Add(new TransmitMessage(_comms, $"vlan acceptframe all"));
            messages.Add(new TransmitMessage(_comms, $"vlan pvid {vlanId}"));
            messages.Add(new TransmitMessage(_comms, $"vlan participation include {vlanId}"));
            messages.AddRange(BackOut(3));

            var session = new Session(port, "SetPortVlan", messages: messages)
            {
                TargetVlan = vlanId
            };

            EnqueueSession(session);
        }


        /// <inheritdoc />
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
                var messages = new List<IQueueMessage>();
                messages.AddRange(EnableConfigMode());
                messages.Add(new TransmitMessage(_comms, $"interface {port}"));
                messages.Add(new TransmitMessage(_comms, $"poe"));
                messages.AddRange(BackOut(2));
                var session = new Session(port, "SetPortPoeState", portPoeState: enabled, messages: messages);
                EnqueueSession(session);

                return;
            }
            else
            {
                var messages = new List<IQueueMessage>();
                messages.AddRange(EnableConfigMode());
                messages.Add(new TransmitMessage(_comms, $"interface {port}"));
                messages.Add(new TransmitMessage(_comms, $"no poe"));
                messages.AddRange(BackOut(2));
                var session = new Session(port, "SetPortPoeState", portPoeState: enabled, messages: messages);
                EnqueueSession(session);

                return;
            }
        }

        #endregion


        #region Overrides of EssentialsBridgeableDevice

        #endregion
    }
}