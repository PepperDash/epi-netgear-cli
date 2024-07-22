namespace Essentials.Plugin.Netgear.Cli
{
    public interface ISwitchCommands
    { 
        void ChangeVlan(string port, int vlanID);
    }
}