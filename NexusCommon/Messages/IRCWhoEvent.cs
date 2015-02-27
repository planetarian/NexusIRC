using System;
using System.Collections.Generic;

namespace Nexus.Messages
{
    [Serializable]
    public class IRCWhoEvent : IRCEvent
    {
        public string Channel { get; private set; }
        public List<IRCUserWhoEntry> Entries { get; private set; }

        public IRCWhoEvent(string channel, List<IRCUserWhoEntry> entries, IRCEventInfo eventInfo)
            : base("IRC.Who", new Dictionary<string, object>
                                  {
                                      {"channel", channel},
                                      {"entries", entries}
                                  }, eventInfo)
        {
            Channel = channel;
            Entries = entries;
        }
    }
}
