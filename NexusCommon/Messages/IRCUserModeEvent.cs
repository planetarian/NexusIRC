using System;
using System.Collections.Generic;

namespace Nexus.Messages
{
    [Serializable]
    public class IRCUserModeEvent : IRCEvent
    {
        public string Sender { get; private set; }
        public string Target { get; private set; }
        public string Modes { get; private set; }

        public IRCUserModeEvent(string sender, string target, string modes, IRCEventInfo eventInfo)
            : base("IRC.UserMode",
            new Dictionary<string, object>
                {
                    {"sender", sender},
                    {"target", target},
                    {"modes", modes}
                }, eventInfo)
        {
            Sender = sender;
            Target = target;
            Modes = modes;
        }
    }
}
