using System;

namespace Nexus.Messages
{
    [Serializable]
    public class IRCConnectionClosedEvent : IRCEvent
    {
        public IRCConnectionClosedEvent(IRCEventInfo eventInfo)
            : base("IRC.ConnectionClosedEvent", null, eventInfo)
        {

        }
    }
}