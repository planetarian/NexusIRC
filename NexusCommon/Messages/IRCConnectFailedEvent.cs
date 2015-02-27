using System;
using System.Collections.Generic;

namespace Nexus.Messages
{
    [Serializable]
    public class IRCConnectFailedEvent : IRCEvent
    {
        public string Message { get; private set; }

        public IRCConnectFailedEvent(string message, IRCEventInfo eventInfo)
            : base("IRC.ConnectFailed", new Dictionary<string, object> { { "message", message } }, eventInfo)
        {
            Message = message;
        }

        public IRCConnectFailedEvent(IRCEventInfo eventInfo)
            : base("IRC.ConnectFailed", new Dictionary<string, object> { { "message", null } }, eventInfo)
        {
        }
    }
}