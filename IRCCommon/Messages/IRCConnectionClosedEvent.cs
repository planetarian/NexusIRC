using System;

namespace IRCCommon.Messages
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