using System;
using System.Collections.Generic;

namespace Nexus.Messages
{
    [Serializable]
    public class IRCJoinEvent : IRCEvent
    {
        public IRCUser User { get; private set; }
        public string Channel { get; private set; }

        public IRCJoinEvent(IRCUser user, string channel, IRCEventInfo eventInfo)
            : base("IRC.Join",
            new Dictionary<string, object>
                {
                    {"user", user},
                    {"channel", channel}
                }, eventInfo)
        {
            User = user;
            Channel = channel;
        }
    }
}
