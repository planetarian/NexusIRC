using System;
using System.Collections.Generic;

namespace IRCCommon.Messages
{
    [Serializable]
    public class IRCInfoEvent : IRCEvent
    {
        public string Message { get; private set; }
        private const string messageName = "Nexus.IRCInfo";
        private const string messageKey = "message";
        private const string valueKey = "value";

        public IRCInfoEvent(string message, IRCEventInfo eventInfo)
            : base(messageName, new Dictionary<string, object> { { messageKey, message } }, eventInfo)
        {
        }
        public IRCInfoEvent(string message, object value, IRCEventInfo eventInfo)
            : base(messageName, new Dictionary<string, object>{{messageKey,message},{valueKey,value}}, eventInfo)
        {
        }
    }
}
