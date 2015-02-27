using System;

namespace Nexus.Messages
{
    [Serializable]
    public class IRCConnectionCreatedEvent : IRCEvent
    {
        public IRCConnectionCreatedEvent(IRCEventInfo eventInfo)
            : base("IRC.ConnectionCreatedEvent", null, eventInfo)
        {

        }
    }
}