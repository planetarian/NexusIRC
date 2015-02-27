using System;
using System.Collections.Generic;

namespace Nexus.Messages
{
    [Serializable]
    public class IRCPrivmsgEvent : IRCMessageEvent
    {

        public IRCPrivmsgEvent (string sender, string target, string message, IRCEventInfo eventInfo)
            : base("IRC.Privmsg",
            new Dictionary<string, object>
                {
                    {"sender", sender},
                    {"target", target},
                    {"message", message}
                }, sender, target, message, eventInfo)
        { }
    }
}
