using System;
using System.Collections.Generic;

namespace IRCCommon.Messages
{
    [Serializable]
    public class IRCNamesEvent : IRCEvent
    {
        public string Channel { get; private set; }
        public List<IRCUserNamesEntry> Entries { get; private set; }

        public IRCNamesEvent(string channel, List<IRCUserNamesEntry> entries, IRCEventInfo eventInfo)
            : base("IRC.Names", new Dictionary<string, object>
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
