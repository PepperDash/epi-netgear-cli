using System.Collections.Generic;
using Newtonsoft.Json;
using PepperDash.Essentials.Core;

namespace Essentials.Plugin.Netgear.Cli
{
    /// <summary>
    /// Plugin device configuration object
    /// </summary>
    /// <remarks>
    /// Rename the class to match the device plugin being created
    /// </remarks>
    /// <example>
    /// "EssentialsPluginConfigObjectTemplate" renamed to "SamsungMdcConfig"
    /// </example>
    [ConfigSnippet("\"properties\":{\"control\":{}")]
    public class NetgearCliConfigObject
    {
        /// <summary>
        /// JSON control object
        /// </summary>
        /// <remarks>
        /// Typically this object is not required, but in some instances it may be needed.  For example, when building a 
        /// plugin that is using Telnet (TCP/IP) communications and requires login, the device will need to handle the login.
        /// In order to do so, you will need the username and password in the "tcpSshProperties" object.
        /// </remarks>
        /// <example>
        /// <code>
        /// "control": {
        ///		"method": "tcpIp",
        ///		"controlPortDevKey": "processor",
        ///		"controlPortNumber": 1,
        ///		"comParams": {
        ///			"baudRate": 9600,
        ///			"dataBits": 8,
        ///			"stopBits": 1,
        ///			"parity": "None",
        ///			"protocol": "RS232",
        ///			"hardwareHandshake": "None",
        ///			"softwareHandshake": "None"
        ///		},
        ///		"tcpSshProperties": {
        ///			"address": "172.22.0.101",
        ///			"port": 23,
        ///			"username": "admin",
        ///			"password": "password",
        ///			"autoReconnect": true,
        ///			"autoReconnectIntervalMs": 10000
        ///		}
        ///	}
        /// </code>
        /// </example>
        [JsonProperty("control")]
        public EssentialsControlPropertiesConfig Control { get; set; }

        /// <summary>
        /// Password for the device login if NOT using SSH.  If using SSH, the password property in the "tcpSshProperties" object is used.
        /// </summary>
        [JsonProperty("password")]
        public string Password { get; set; }
    }

}