using System;
using System.Collections.Generic;

namespace Memphis.Client.Core
{
    public class MemphisMessageHandlerEventArgs : EventArgs
    {
        public MemphisMessageHandlerEventArgs(List<MemphisMessage> messageList, System.Exception ex)
        {
            MessageList = messageList;
            Exception = ex;
        }


        /// <summary>
        /// Retrieves the message.
        /// </summary>
        public List<MemphisMessage> MessageList { get; }

        public System.Exception Exception { get; }
    }
}