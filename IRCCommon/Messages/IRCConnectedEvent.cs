using System;

namespace IRCCommon.Messages
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
