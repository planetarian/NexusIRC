using System;
using System.Collections.Generic;

namespace Nexus.Messages
{
    [Serializable]
    public class IRCEvent : NexusEvent
    {
        private const string valueKey = "value";
        private const string eventInfoKey = "ircEventInfo";

        public IRCEventInfo EventInfo
        {
            get
            {
                if (!Data.ContainsKey(eventInfoKey))
                    throw new InvalidOperationException("IRC Event Info has not been set in this message!");
                if (!(Data[eventInfoKey] is IRCEventInfo))
                    throw new InvalidOperationException("Value using the IRC Event Info key is of the wrong type!");
                return (IRCEventInfo) Data[eventInfoKey];
            }
            private set
            {
                
                if (_data.ContainsKey(eventInfoKey))
                    throw new ArgumentException("Message already contains IRC Event Info!");
                _data[eventInfoKey] = value;
            }
        }

        /// <summary> Creates a new instance of the IRCMessage struct with the provided IRC context information. </summary>
        /// <param name="messageName"> Name of the message. </param>
        /// <param name="data"> Object containing message data. </param>
        /// <param name="eventInfo"> Additional event information. </param>
        public IRCEvent(string messageName, object data, IRCEventInfo eventInfo)
            : base (messageName, data)
        {
            EventInfo = eventInfo;
        }

        /// <summary> Creates a new instance of the IRCMessage struct with the provided IRC context information. </summary>
        /// <param name="messageName"> Name of the message. </param>
        /// <param name="data"> Message data in Dictionary form. </param>
        /// <param name="eventInfo"> Additional event information. </param>
        public IRCEvent(string messageName, Dictionary<string, object> data, IRCEventInfo eventInfo)
            : base (messageName, data)
        {
            EventInfo = eventInfo;
        }

        public IRCEvent(string messageName, IRCEventInfo eventInfo)
            : this(messageName, null, eventInfo)
        {
        }
    }
}
