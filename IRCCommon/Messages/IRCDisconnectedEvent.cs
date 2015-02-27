using System;
using System.Collections.Generic;

namespace IRCCommon.Messages
{
    [Serializable]
    public class IRCDisconnectedEvent : IRCEvent
    {
        public string Message { get; private set; }

        public IRCDisconnectedEvent(string message, IRCEventInfo eventInfo)
            : base("IRC.Disconnected", new Dictionary<string, object> { { "message", message } }, eventInfo)
        {
            Message = message;
        }

        public IRCDisconnectedEvent(IRCEventInfo eventInfo)
            : base("IRC.Disconnected", new Dictionary<string, object> { { "message", null } }, eventInfo)
        {
        }
    }
}