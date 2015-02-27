using System;
using System.Collections.Generic;

namespace Nexus.Messages
{
    [Serializable]
    public class IRCChannelModeEvent : IRCEvent
    {
        public string Sender { get; private set; }
        public string Channel { get; private set; }
        public string Modes { get; private set; }
        public string ModeParameters { get; private set; }

        public IRCChannelModeEvent(string sender, string channel, string modes, string modeParameters, IRCEventInfo eventInfo)
            : base("IRC.ChannelMode",
            new Dictionary<string, object>
                {
                    {"sender", sender},
                    {"channel", channel},
                    {"modes", modes},
                    {"modeparameters", modeParameters}
                }, eventInfo)
        {
            Sender = sender;
            Channel = channel;
            Modes = modes;
        }
    }
}
