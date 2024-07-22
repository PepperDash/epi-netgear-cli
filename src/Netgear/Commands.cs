using System;
using System.Collections.Generic;
using System.Text;

namespace Essentials.Plugin.Netgear.Cli
{
    public class Commands
    {
        public static string DisablePort(string port, string delimiter)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append($"interface {port}" + delimiter);
            sb.Append(" shutdown" + delimiter);
            return sb.ToString();
        }

        public static string EnablePort(string port, string delimiter)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append($"interface {port}" + delimiter);
            sb.Append(" no shutdown" + delimiter);
            return sb.ToString();
        }

        public static string AssignVlan(string port, int vlan, string delimiter)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append($"interface {port}" + delimiter);
            sb.Append($"switchport access vlan {vlan}" + delimiter);
            return sb.ToString();
        }
    }
}