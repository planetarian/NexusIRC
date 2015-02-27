using System;
using System.Collections.Generic;

namespace Nexus.Messages
{
    [Serializable]
    public class IRCNoticeEvent : IRCMessageEvent
    {

        public IRCNoticeEvent(string sender, string target, string message, IRCEventInfo eventInfo)
            : base("IRC.Notice",
            new Dictionary<string, object>
                {
                    {"sender", sender},
                    {"target", target},
                    {"message", message}
                }, sender, target, message, eventInfo)
        { }
    }
}
