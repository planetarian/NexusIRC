using System;
using System.Collections.Generic;

namespace Nexus.Messages
{
    [Serializable]
    public class IRCCommandEvent : IRCMessageEvent
    {
        public string Command { get; private set; }
        public string Body { get; private set; }
        public string[] Parameters { get; private set; }
        
        public IRCCommandEvent (string sender, string target, string command, string body, string[] parameters, string message, IRCEventInfo eventInfo)
            : base("IRC.Command",
            new Dictionary<string, object>
                {
                    {"sender", sender},
                    {"target", target},
                    {"command", command},
                    {"parameters", parameters},
                    {"body", body},
                    {"message", message}
                }, sender, target, message, eventInfo)
        {
            Command = command;
            Body = body;
            Parameters = parameters;
        }
    }
}
