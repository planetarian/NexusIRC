using System.Collections.Generic;

namespace IRCCommon.Messages
{
    public class IRCKickEvent : IRCEvent
    {
        public string Channel { get; private set; }
        public IRCUser User { get; private set; }
        public string[] Kicked { get; private set; }
        public string Reason { get; private set; }

        public IRCKickEvent(IRCUser user, string channel, string[] kicked, string reason, IRCEventInfo eventInfo)
            : base("IRC.Kick", new Dictionary<string, object>
                                   {
                                       {"channel", channel},
                                       {"user", user},
                                       {"kicked", kicked},
                                       {"reason", reason}
                                   }, eventInfo)
        {
            Channel = channel;
            User = user;
            Kicked = kicked;
            Reason = reason;
        }
    }
}
