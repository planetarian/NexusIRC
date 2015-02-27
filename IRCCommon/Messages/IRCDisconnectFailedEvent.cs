using System;
using System.Collections.Generic;

namespace IRCCommon.Messages
{
    [Serializable]
    public class IRCDisconnectFailedEvent : IRCEvent
    {
        public string Message { get; private set; }

        public IRCDisconnectFailedEvent(string message, IRCEventInfo eventInfo)
            : base("IRC.DisconnectFailed", new Dictionary<string, object> { { "message", message } }, eventInfo)
        {
            Message = message;
        }

        public IRCDisconnectFailedEvent(IRCEventInfo eventInfo)
            : base("IRC.DisconnectFailed", new Dictionary<string, object> { { "message", null } }, eventInfo)
        {
        }
    }
}