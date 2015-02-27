
using System;

namespace Nexus.Messages
{
    [Serializable]
    public class IRCConnectingEvent : IRCEvent
    {
        public IRCConnectingEvent(IRCEventInfo eventInfo)
            : base("IRC.Connecting", eventInfo)
        {
        }
    }
}
