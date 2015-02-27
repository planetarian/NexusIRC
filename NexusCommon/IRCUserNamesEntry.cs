using System;

namespace Nexus
{
    [Serializable]
    public struct IRCUserNamesEntry
    {
        public string Nick { get; private set; }
        public string Channel { get; private set; }
        public string Flags { get; private set; }

        public IRCUserNamesEntry(string nick, string channel, string flags)
            : this()
        {
            Nick = nick;
            Channel = channel;
            Flags = flags;
        }
    }
}
