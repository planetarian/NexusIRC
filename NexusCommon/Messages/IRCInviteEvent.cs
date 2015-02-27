using System;
using System.Collections.Generic;

namespace Nexus.Messages
{
    [Serializable]
    public class IRCInviteEvent : IRCEvent
    {
        public IRCUser Sender { get; private set; }
        public string Invitee { get; private set; }
        public string Channel { get; private set; }

        public IRCInviteEvent(IRCUser sender, string invitee, string channel, IRCEventInfo eventInfo)
            : base("IRC.Invite",
            new Dictionary<string, object>
                {
                    {"sender", sender},
                    {"channel", channel},
                    {"invitee", invitee}
                }, eventInfo)
        {
            Sender = sender;
            Channel = channel;
            Invitee = invitee;
        }
    }
}
