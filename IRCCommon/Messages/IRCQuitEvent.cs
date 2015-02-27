using System;
using System.Collections.Generic;

namespace IRCCommon.Messages
{
    [Serializable]
    public class IRCQuitEvent : IRCEvent
    {
        public IRCUser User { get; private set; }
        public string Reason { get; private set; }

        public IRCQuitEvent(IRCUser user, string reason, IRCEventInfo eventInfo)
            : base("IRC.Quit",
            new Dictionary<string, object>
                {
                    {"user", user},
                    {"reason", reason}
                }, eventInfo)
        {
            User = user;
            Reason = reason;
        }
    }
}
