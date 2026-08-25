

using System;
using System.Collections.Generic;
using PepperDash.Essentials.Core.Queues;

namespace PepperDash.Essentials.Plugins
{
    /// <summary>
    /// Represents a session for a specific port and method. This can be used to track active sessions and their creation time.
     /// The Port property can be used to identify the port associated with the session, while the MethodName property can be used to specify the method being executed in the session.
     /// The CreatedAt property can be useful for tracking how long a session has been active or for logging purposes.
     /// </summary>
     /// <remarks>
     /// This class is a simple data structure and does not contain any methods for managing sessions. It is intended to be used in conjunction with a collection (e.g., ConcurrentQueue) to store and manage multiple sessions.
     /// </remarks>
     /// <example>
     /// To create a new session for port "1" and method "SetPoeVlan", you can use the following code:
     /// <code>
     /// var session = new Session("1", "SetPoeVlan");
     /// </code>
     /// This will create a new Session object with the specified port and method, and the CreatedAt property will be set to the current date and time.
     /// </example>
    /// </summary>
    public class Session
    {
        /// <summary>
        /// The port associated with the session.
        /// </summary>
        public string Port { get; private set; }

        /// <summary>
        /// The method being executed in the session.
        /// </summary>
        public string MethodName { get; set; }

        /// <summary>
        /// The PoE state associated with the session, if applicable. This can be used to track the current PoE state of the port during the session.
        /// </summary>
        public bool? PortPoeState { get; set; }

        /// <summary>
        /// The target access VLAN ID for a SetPortVlan session. Used to update the cached port VLAN when the session completes.
        /// </summary>
        public uint? TargetVlan { get; set; }

        /// <summary>
        /// The date and time when the session was created.
        /// </summary>
        public DateTime CreatedAt { get; set; }

        public List<IQueueMessage> Messages { get; set; } = new List<IQueueMessage>();

        /// <summary>
        /// Constructor for the Session class.
        /// </summary>
        /// <param name="port">The port associated with the session.</param>
        /// <param name="methodName">The method being executed in the session.</param>
        /// <param name="portPoeState">The PoE state associated with the session.</param>
        /// <param name="messages">The list of messages associated with the session.</param>
        public Session(string port, string methodName, bool? portPoeState = null, List<IQueueMessage> messages = null)
        {
            Port = port;
            MethodName = methodName;
            PortPoeState = portPoeState;
            CreatedAt = DateTime.Now;
            Messages = messages ?? new List<IQueueMessage>();
        }

        
    }
}