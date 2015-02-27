using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Nexus.Messages
{
    [Serializable]
    public class UserMessageEvent : NexusEvent
    {
        public string Message { get; private set; }

        public UserMessageEvent(string message)
            : base("Nexus.UserMessage", message)
        {
            Message = message;
        }
    }
}
