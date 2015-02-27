using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Nexus.Messages
{
    [Serializable]
    public class UserCommandEvent : NexusEvent
    {
        public string Command { get; private set; }
        public string ParameterString { get; private set; }
        public string[] Parameters { get; private set; }

        public UserCommandEvent(string command, string trailing)
            : base("Nexus.UserCommand", new Dictionary<string, object>
            {
                {"command", command},
                {"parameterstring", trailing},
                {"parameters", trailing != null ? trailing.Split(' ') : new string[0]}
            })
        {
            Command = command;
            ParameterString = trailing;
            Parameters = (string[])Data["parameters"];
        }
    }
}
