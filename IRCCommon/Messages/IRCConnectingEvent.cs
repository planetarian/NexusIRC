
using System;

namespace IRCCommon.Messages
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
