using System;
using System.Collections.Generic;

namespace Nexus.Messages
{
    [Serializable]
    public class IRCPartEvent : IRCEvent
    {
        public IRCUser User { get; private set; }
        public string Channel { get; private set; }
        public string Reason { get; private set; }

        public IRCPartEvent(IRCUser user, string channel, string reason, IRCEventInfo eventInfo)
            : base("IRC.Part",
            new Dictionary<string, object>
                {
                    {"user", user},
                    {"channel", channel},
                    {"reason", reason}
                }, eventInfo)
        {
            User = user;
            Channel = channel;
            Reason = reason;
        }
    }
}
