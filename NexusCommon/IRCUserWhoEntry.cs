using System;

namespace Nexus
{
    [Serializable]
    public struct IRCUserWhoEntry
    {
        public IRCUser User { get; private set; }
        public string Channel { get; private set; }
        public string Flags { get; private set; }

        public IRCUserWhoEntry(IRCUser user, string channel, string flags) : this()
        {
            User = user;
            Channel = channel;
            Flags = flags;
        }
    }
}
