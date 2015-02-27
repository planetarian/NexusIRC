using System;
using System.Collections.Generic;

namespace Nexus.Messages
{
    [Serializable]
    public class IRCInfoMessage : IRCEvent
    {
        public string Message { get; private set; }
        private const string messageName = "Nexus.IRCInfo";
        private const string messageKey = "message";
        private const string valueKey = "value";

        public IRCInfoMessage(string message, IRCEventInfo eventInfo)
            : base(messageName, new Dictionary<string, object> { { messageKey, message } }, eventInfo)
        {
        }
        public IRCInfoMessage(string message, object value, IRCEventInfo eventInfo)
            : base(messageName, new Dictionary<string, object>{{messageKey,message},{valueKey,value}}, eventInfo)
        {
        }
    }
}
