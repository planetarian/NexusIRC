using System;
using System.Text.RegularExpressions;

namespace Nexus
{
    [Serializable]
    public struct IRCUser
    {
        public string Nick { get; private set; }
        public string UserName { get; private set; }
        public string Host { get; private set; }
        public string RealName { get; private set; }
        public string Server { get; private set; }


        public IRCUser(string nick, string userName, string host, string realName, string server) : this()
        {
            if (String.IsNullOrWhiteSpace(nick))
                throw new InvalidOperationException("No nick provided.");

            Nick = nick;
            UserName = userName;
            Host = host;
            RealName = realName;
            Server = server;
        }

        public IRCUser(string userFormatted, string realName, string server) : this()
        {
            if (String.IsNullOrWhiteSpace(userFormatted))
                throw new InvalidOperationException("No user provided.");

            Match match = Regex.Match(userFormatted, @"^(?<nick>[^!]+)!(?<user>[^@]+)@(?<host>.+)$", RegexOptions.Compiled);
            if (!match.Success)
                throw new InvalidOperationException("Invalid user format.");

            Nick = match.Groups["nick"].Value;
            UserName = match.Groups["user"].Value;
            Host = match.Groups["host"].Value;
            RealName = realName;
            Server = server;
        }

    }
}
