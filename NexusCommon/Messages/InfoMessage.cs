using System;
using System.Collections.Generic;

namespace Nexus.Messages
{
    [Serializable]
    public class InfoMessage : NexusEvent
    {
        public string Message { get; private set; }
        private const string messageName = "Nexus.Info";

        private const string messageKey = "message";
        private const string valueKey = "value";

        public InfoMessage(string message)
            : base(messageName, new Dictionary<string, object> { { messageKey, message } })
        {
            Message = message;
        }

        public InfoMessage(string message, object value)
            : base(messageName, new Dictionary<string, object>{{messageKey,message},{valueKey,value}})
        {
            Message = message;
        }


    }
}
