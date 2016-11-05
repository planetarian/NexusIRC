using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using Nexus;
using Nexus.Messages;

namespace IRCMessageTranslator
{
    public class Translator : NexusComponent
    {
        private string commandPrefix = "~";

        private bool defaultHighPriority = false;

        private readonly Dictionary<int, ulong> nextMessageIds =
            new Dictionary<int, ulong>();

        private readonly Dictionary<int, List<IRCDataReceivedEvent>> queuedEvents =
            new Dictionary<int, List<IRCDataReceivedEvent>>();

        private readonly Dictionary<int, Dictionary<string, List<IRCUserWhoEntry>>> whoQueue =
            new Dictionary<int, Dictionary<string, List<IRCUserWhoEntry>>>();

        private readonly Dictionary<int, Dictionary<string, List<IRCUserNamesEntry>>> namesQueue =
            new Dictionary<int, Dictionary<string, List<IRCUserNamesEntry>>>();

        private readonly object _namesQueueLock = new object();
        private readonly object _whoQueueLock = new object();
        private readonly object _queueLock = new object();

        public Translator()
        {
            ExposeMethodsAs("IRC");
            RegisterListener<IRCDataReceivedEvent>(QueueMessage);
        }

        public void QueueMessage(IRCDataReceivedEvent ev)
        {
            int cid = ev.EventInfo.ConnectionId;

            lock (_queueLock)
            {
                if (!nextMessageIds.ContainsKey(cid))
                    nextMessageIds.Add(cid, 0);

                if (!queuedEvents.ContainsKey(cid))
                    queuedEvents.Add(cid, new List<IRCDataReceivedEvent>());

                queuedEvents[cid].Add(ev);

                IRCDataReceivedEvent queuedEvent;
                while ((queuedEvent = queuedEvents[cid].Find(e => e.MessageId == nextMessageIds[cid])) != null)
                {
                    HandleMessage(queuedEvent);
                    queuedEvents[cid].Remove(queuedEvent);
                    nextMessageIds[cid]++;
                }
            }
        }

        public void HandleMessage(IRCDataReceivedEvent ev) {
            IRCMessage m = ev.Message;
            
            switch (ev.Message.Command.ToLower())
            {
                
                case "mode":
                    HandleMode(ev);
                    break;
                case "privmsg":
                    CheckParameterCount(ev, 1);
                    HandlePrivmsgItem(ev);
                    break;
                case "notice":
                    CheckParameterCount(ev, 1);
                    SendMessage(new IRCNoticeEvent(m.Prefix, m.Parameters[0], m.Trailing, ev.EventInfo));
                    break;
                case "nick":
                    SendMessage(new IRCNickEvent(new IRCUser(m.Prefix, null, null), m.Trailing, ev.EventInfo));
                    break;
                case "join":
                    SendMessage(new IRCJoinEvent(new IRCUser(m.Prefix, null, null), m.Trailing, ev.EventInfo));
                    break;
                case "part":
                    CheckParameterCount(ev, 1);
                    SendMessage(new IRCPartEvent(new IRCUser(m.Prefix, null, null), m.Parameters[0], m.Trailing, ev.EventInfo));
                    break;
                case "kick":
                    CheckParameterCount(ev, 2);
                    SendMessage(new IRCKickEvent(new IRCUser(m.Prefix, null, null), m.Parameters[0], m.Parameters[1].Split(','), m.Trailing, ev.EventInfo));
                    break;
                case "quit":
                    SendMessage(new IRCQuitEvent(new IRCUser(m.Prefix, null, null), m.Trailing, ev.EventInfo));
                    break;
                case "invite":
                    CheckParameterCount(ev, 1);
                    SendMessage(new IRCInviteEvent(new IRCUser(m.Prefix, null, null), m.Parameters[0], m.Trailing, ev.EventInfo));
                    break;

                    // Numerics

                case "352": // WHO list item.
                    HandleWhoItem(ev);
                    break;
                case "315": // End of WHO list.
                    HandleWhoEnd(ev);
                    break;

                case "353":
                    HandleNamesItem(ev);
                    break;
                case "366":
                    HandleNamesEnd(ev);
                    break;
            }
        }

        private void HandlePrivmsgItem(IRCDataReceivedEvent ev)
        {
            IRCMessage m = ev.Message;
            var privmsgEvent = new IRCPrivmsgEvent(m.Prefix, m.Parameters[0], m.Trailing, ev.EventInfo);
            SendMessage(privmsgEvent);

            if (privmsgEvent.IsServerMessage)
                return;

            var regexString = "^" + commandPrefix + @"(?<command>\w+)(?: +(?<parameters>.*))?$";
            var regex = Regex.Match(m.Trailing.Trim(), regexString, RegexOptions.IgnoreCase);
            if (!regex.Success) return;

            string command = regex.Groups["command"].Value.ToLower();
            string body = regex.Groups["parameters"].Value;
            string[] parameters = Regex.Replace(regex.Groups["parameters"].Value, " +", " ").Split(' ');
            if (parameters.Length == 1 && parameters[0] == "")
                parameters = new string[]{};
            SendMessage(new IRCCommandEvent(m.Prefix, m.Parameters[0], command, body, parameters, m.Trailing, ev.EventInfo));
        }

        private void HandleMode(IRCDataReceivedEvent ev)
        {
            IRCMessage m = ev.Message;

            //string target = m.Parameters[0]

            if (ev.Message.Parameters.Length > 1 && ev.Message.Parameters[0].Substring(0,1) == "#")
            {
                string optional = m.Parameters.Length == 3 ? m.Parameters[2] : null;
                SendMessage(new IRCChannelModeEvent(m.Prefix, m.Parameters[0], m.Parameters[1], optional, ev.EventInfo));
            }
            else
                SendMessage(new IRCUserModeEvent(m.Prefix, m.Parameters[0], m.Trailing, ev.EventInfo));
        }

        private void HandleWhoItem(IRCDataReceivedEvent ev)
        {
            CheckParameterCount(ev, 7);

            int id = ev.EventInfo.ConnectionId;
            string channel = ev.Message.Parameters[1].ToLower();
            
            // Format: 0:MyNick 1:Channel 2:UserName 3:Host 4:Server 5:Nick 6:Flags
            string[] p = ev.Message.Parameters;

            // Format: 1 Real Name
            string tr = ev.Message.Trailing;
            string realName = tr.Substring(tr.IndexOf(' '));
            
            var user = new IRCUser(p[5], p[2], p[3], realName, p[4]);
            var whoEntry = new IRCUserWhoEntry(user, channel, p[6]);

            lock (_whoQueueLock)
            {
                if (!whoQueue.ContainsKey(id))
                    whoQueue.Add(id, new Dictionary<string, List<IRCUserWhoEntry>>());
                if (!whoQueue[id].ContainsKey(channel))
                    whoQueue[id].Add(channel, new List<IRCUserWhoEntry>());
                whoQueue[id][channel].Add(whoEntry);
            }
        }

        private void HandleWhoEnd(IRCDataReceivedEvent ev)
        {
            int id = ev.EventInfo.ConnectionId;
            string channel = ev.Message.Parameters[1].ToLower();

            lock (_whoQueueLock)
            {
                if (!whoQueue.ContainsKey(id) || !whoQueue[id].ContainsKey(channel))
                    return;
                List<IRCUserWhoEntry> entries = whoQueue[id][channel];
                
                whoQueue[id].Remove(channel);
                if (whoQueue[id].Count == 0)
                    whoQueue.Remove(id);

                SendMessage(new IRCWhoEvent(channel, entries, ev.EventInfo));
            }
        }

        private void HandleNamesItem(IRCDataReceivedEvent ev)
        {
            CheckParameterCount(ev, 3);

            int cid = ev.EventInfo.ConnectionId;
            // 0:piro 1:= 2:#channel
            string channel = ev.Message.Parameters[2].ToLower();
            lock (_namesQueueLock)
            {
                if (!namesQueue.ContainsKey(cid))
                    namesQueue.Add(cid, new Dictionary<string, List<IRCUserNamesEntry>>());
                if (!namesQueue[cid].ContainsKey(channel))
                    namesQueue[cid].Add(channel, new List<IRCUserNamesEntry>());

                foreach (string name in ev.Message.Trailing.Trim().Split(' '))
                {
                    Match match = Regex.Match(name, @"^(?<prefix>[^a-zA-Z0-9-`^{}\[\]\\])?(?<nick>.+)$", RegexOptions.Compiled);
                    if (!match.Success)
                        throw new InvalidOperationException("Incorrectly-formatted NAMES response.");

                    var entry = new IRCUserNamesEntry(
                        match.Groups["nick"].Value, channel, match.Groups["prefix"].Value);
                    namesQueue[cid][channel].Add(entry);
                }
            }

        }

        private void HandleNamesEnd(IRCDataReceivedEvent ev)
        {
            int cid = ev.EventInfo.ConnectionId;
            string channel = ev.Message.Parameters[1].ToLower();

            lock (_namesQueueLock)
            {
                if (!namesQueue.ContainsKey(cid) || !namesQueue[cid].ContainsKey(channel))
                    return;

                List<IRCUserNamesEntry> entries = namesQueue[cid][channel];

                namesQueue[cid].Remove(channel);
                if (namesQueue[cid].Count == 0)
                    namesQueue.Remove(cid);

                SendMessage(new IRCNamesEvent(channel, entries, ev.EventInfo));
            }
        }

        private static void CheckParameterCount(IRCDataReceivedEvent ev, int count)
        {
            if (ev.Message.Parameters.Length < count)
                throw new InvalidOperationException("Invalid IRC message received.");
        }


        // All public methods are exposed as IRC.*


        public void Say(int connectionId, string target, string message, bool highPriority)
        {
            Exception ex = CallMethod("IRC.SendRaw", connectionId,
                $"PRIVMSG {target} :{message}",
                highPriority);

            if (ex != null)
                throw (ex);
        }

        public void Say(int connectionId, string target, string message)
        {
            Say(connectionId, target, message, defaultHighPriority);
        }

        public void Reply(IRCMessageEvent ev, string message, bool highPriority)
        {
            Say(ev.EventInfo.ConnectionId, ev.ReturnTarget, message, highPriority);
        }

        public void Reply(IRCCommandEvent ev, string message, bool highPriority)
        {
            Say(ev.EventInfo.ConnectionId, ev.ReturnTarget, message, highPriority);
        }

        public void Reply(IRCMessageEvent ev, string message)
        {
            Say(ev.EventInfo.ConnectionId, ev.ReturnTarget, message, defaultHighPriority);
        }

        public void Reply(IRCCommandEvent ev, string message)
        {
            Say(ev.EventInfo.ConnectionId, ev.ReturnTarget, message, defaultHighPriority);
        }


    }
}
