using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IRCCommon.Messages
{
    [Serializable]
    public class IRCCommandEvent : IRCMessageEvent
    {
        public string Command { get; private set; }
        public string[] Parameters { get; private set; }
        
        public IRCCommandEvent (string sender, string target, string command, string[] parameters, string message, IRCEventInfo eventInfo)
            : base("IRC.Command",
            new Dictionary<string, object>
                {
                    {"sender", sender},
                    {"target", target},
                    {"command", command},
                    {"parameters", parameters},
                    {"message", message}
                }, sender, target, message, eventInfo)
        {
            Command = command;
            Parameters = parameters;
        }
    }
}
