using System;
using System.Collections.Generic;

namespace Nexus.Messages
{
    [Serializable]
    public class IRCDataReceivedEvent : IRCEvent
    {
        public ulong MessageId { get; private set; }
        public IRCMessage Message { get; private set; }

        public IRCDataReceivedEvent(ulong messageId, string data, IRCEventInfo eventInfo)
            : base("IRC.DataReceived",
            new Dictionary<string, object>
                {
                    {"id", messageId},
                    {"data", data}
                }, eventInfo)
        {
            MessageId = messageId;
            Message = new IRCMessage(data);
        }
    }
}
