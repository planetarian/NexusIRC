using System;
using System.Text.RegularExpressions;

namespace Nexus
{
    [Serializable]
    public struct IRCMessage
    {
        public string Raw { get; private set; }
        public string Prefix { get; private set; }
        public string Command { get; private set; }
        public string[] Parameters { get; private set; }
        public string Trailing { get; private set; }

        private const string messageRegexPattern =
            "^(:(?<prefix>[^ ]+) +)?(?<command>[^ ]+)(?<innerparams>( +[^ ]+)*?)?( +:(?<outerparams>.*))?$";

        public IRCMessage(string data)
            : this()
        {
            Match match = Regex.Match(data, messageRegexPattern, RegexOptions.Compiled);
            if (!match.Success) throw new InvalidOperationException("IRCMessage data invalid.");

            Raw = data;
            Prefix = match.Groups["prefix"].Value;
            Command = match.Groups["command"].Value;
            Parameters = match.Groups["innerparams"].Value
                .Split(new[] {' '}, StringSplitOptions.RemoveEmptyEntries);
            Trailing = match.Groups["outerparams"].Value;
        }
    }
}

// // If ya wanna get technical (don't)
        //protected const string HostSegmentRegex = "[a-zA-Z](?:[a-zA-Z0-9-][a-zA-Z0-9]+)*"; // accepts: a-0
        //protected const string HostRegex = HostSegmentRegex + "(?:" + HostSegmentRegex + ")*";
        //protected const string NickRegex = @"[a-zA-Z0-9-`^{}\[\]\\]+";
        //protected const string PrefixRegex = "(?<prefix>(?:" + HostRegex + ")|(?:" + NickRegex + "))";
        //protected const string CommandRegex = @"(?<command>[a-zA-Z]+|\d{3})";
