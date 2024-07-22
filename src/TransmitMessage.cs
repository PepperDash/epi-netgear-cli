using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PepperDash.Core;
using PepperDash.Essentials.Core.Queues;

namespace Essentials.Plugin.Netgear.Cli
{
    internal class TransmitMessage: IQueueMessage
    {
        private readonly IBasicCommunication _comms;
        private readonly string _messageToSend;

        public TransmitMessage(IBasicCommunication comms, string messageToSend)
        {
            _comms = comms;
            _messageToSend = messageToSend;
        }   

        public void Dispatch()
        {
            try
            {
                if (_comms == null) return;
                _comms.SendText(_messageToSend + NetgearCliDevice.DELIMITER);
            }
            catch (Exception e)
            {
                Debug.LogMessage(e, "Error dispatching message", _comms);
            }
        }
    }
}
