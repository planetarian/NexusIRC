﻿using System;
using System.Collections.Generic;

namespace Nexus.Messages
{
        [Serializable]
        public class IRCMessageEvent : IRCEvent
        {
            public string Sender { get; private set; }
            public string Target { get; private set; }
            public string Message { get; private set; }
            public bool IsChannelMessage { get; private set; }
            public bool IsServerMessage { get; private set; }
            public string ReturnTarget { get; private set; }

            public IRCMessageEvent(string messageName, Dictionary<string, object> data, string sender, string target, string message, IRCEventInfo eventInfo)
                : base(messageName, data, eventInfo)
            {
                Sender = sender;
                Target = target;
                Message = message;
                IsChannelMessage = Target.Length > 1 && Target[0] == '#';
                // TODO: Make this more sophisticated?
                // TODO: Can nicks contain periods?
                IsServerMessage = String.IsNullOrWhiteSpace(sender) || (!Sender.Contains("@") && Sender.Contains("."));

                if (!IsServerMessage)
                    ReturnTarget = IsChannelMessage
                                       ? target
                                       : new IRCUser(Sender, null, null).Nick;
            }
        }
}