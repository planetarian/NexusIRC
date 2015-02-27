using System;

namespace Nexus.Messages
{
    [Serializable]
    public class IRCConnectedEvent : IRCEvent
    {
        public IRCConnectedEvent(IRCEventInfo eventInfo)
            : base("IRC.Connected", eventInfo)
        {
        }
    }
}
