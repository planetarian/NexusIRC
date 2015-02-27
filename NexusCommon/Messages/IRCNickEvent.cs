using System;
using System.Collections.Generic;

namespace Nexus.Messages
{
    [Serializable]
    public class IRCNickEvent : IRCEvent
    {
        public IRCUser User { get; private set; }
        public string OldNick { get; private set; }
        public string NewNick { get; private set; }

        public IRCNickEvent(IRCUser user, string newNick, IRCEventInfo eventInfo)
            : base("IRC.Nick",
            new Dictionary<string, object>
                {
                    {"user", user},
                    {"oldnick", user.Nick},
                    {"newnick", newNick}
                }, eventInfo)
        {
            User = user;
            OldNick = user.Nick;
            NewNick = newNick;
        }
    }
}
