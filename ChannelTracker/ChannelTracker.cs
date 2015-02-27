using Nexus;
using System;
using System.Collections.Generic;
using System.Linq;
using Nexus.Messages;

namespace ChannelTracker
{
    public class ChannelTracker : NexusComponent
    {
        private readonly Dictionary<int, Dictionary<string, List<IRCUser>>> users =
            new Dictionary<int, Dictionary<string, List<IRCUser>>>();

        private readonly object _usersLock = new object();

        public ChannelTracker()
        {
            ExposeMethods();
            RegisterListener<IRCNickEvent>(HandleNickEvent);
            RegisterListener<IRCJoinEvent>(HandleJoinEvent);
            RegisterListener<IRCPartEvent>(HandlePartEvent);
            RegisterListener<IRCKickEvent>(HandleKickEvent);
            RegisterListener<IRCQuitEvent>(HandleQuitEvent);
            RegisterListener<IRCWhoEvent>(HandleWhoEvent);
            RegisterListener<IRCNamesEvent>(HandleNamesEvent);

            RegisterListener<IRCDisconnectedEvent>(HandleDisconnectEvent);
            RegisterListener<IRCDisconnectFailedEvent>(HandleDisconnectEvent);
            RegisterListener<IRCConnectionClosedEvent>(HandleDisconnectEvent);
        }

        private void HandleDisconnectEvent(IRCEvent ev)
        {
            lock (_usersLock)
            {
                if (users.ContainsKey(ev.EventInfo.ConnectionId))
                {
                    users.Remove(ev.EventInfo.ConnectionId);
                }
            }
        }

        public bool NickIsInChannel(int connectionId, string nick, string channel)
        {
            channel = channel.ToLower();
            lock (_usersLock)
            {
                if (!users.ContainsKey(connectionId))
                    return false;
                if (!users[connectionId].ContainsKey(channel))
                    return false;
                return users[connectionId][channel].Count(ircUser => ircUser.Nick == nick) != 0;
            }
        }

        public string[] GetNicksInChannel(int connectionId, string channel)
        {
            channel = channel.ToLower();
            lock (_usersLock)
            {
                if (!users.ContainsKey(connectionId))
                    return null;
                if (!users[connectionId].ContainsKey(channel))
                    return null;

                return users[connectionId][channel].Select(u => u.Nick).ToArray();
            }
        }

        private void HandleNickEvent(IRCNickEvent ev)
        {
            lock (_usersLock)
            {
                int cid = ev.EventInfo.ConnectionId;
                InitList(cid);
                // Don't care what the channel name is.
                foreach (List<IRCUser> channelUsers in users[cid].Values)
                {
                    // If the user isn't on the channel, skip this channel.
                    if (!channelUsers.Any(u => u.Nick == ev.OldNick)) continue;

                    // Remove the old nick entry.
                    channelUsers.RemoveAll(u => u.Nick == ev.OldNick);
                    // Add the new nick entry.
                    channelUsers.Add(new IRCUser(ev.NewNick, ev.User.UserName, ev.User.Host,
                                               ev.User.RealName, ev.User.Server));
                }
            }
        }

        private void HandleJoinEvent(IRCJoinEvent ev)
        {
            lock (_usersLock)
            {
                int cid = ev.EventInfo.ConnectionId;
                string channel = ev.Channel.ToLower();
                InitList(cid, channel);
                List<IRCUser> channelUsers = users[cid][channel];

                if (channelUsers.Any(u => u.Nick == ev.User.Nick))
                    throw new InvalidOperationException(
                        String.Format("User {0} is already in channel {1}",
                                      ev.User.Nick, channel));

                channelUsers.Add(ev.User);
            }
        }

        private void HandleNamesEvent(IRCNamesEvent ev)
        {
            lock (_usersLock)
            {

                CallMethod("IRC.SendRaw", ev.EventInfo.ConnectionId, "who " + ev.Channel);
            }
        }

        private void HandlePartEvent(IRCPartEvent ev)
        {
            lock (_usersLock)
            {
                int cid = ev.EventInfo.ConnectionId;
                string channel = ev.Channel.ToLower();
                InitList(cid, channel);
                List<IRCUser> channelUsers = users[cid][channel];

                if (!channelUsers.Any(u => u.Nick == ev.User.Nick))
                    SendMessage(new IRCInfoMessage("User parted not in list, has WHO been ran?", ev.EventInfo));
                    /*
                    throw new InvalidOperationException(
                        String.Format("User {0} is not in channel {1}",
                                      ev.User.Nick, channel));*/

                channelUsers.RemoveAll(u => u.Nick == ev.User.Nick);
            }
        }

        private void HandleKickEvent(IRCKickEvent ev)
        {
            lock (_usersLock)
            {
                int cid = ev.EventInfo.ConnectionId;
                string channel = ev.Channel.ToLower();
                InitList(cid, channel);
                List<IRCUser> channelUsers = users[cid][channel];

                foreach (string kicked in ev.Kicked)
                {
                    if (!channelUsers.Any(u => u.Nick == kicked))
                        throw new InvalidOperationException(
                            String.Format("User {0} is not in channel {1}",
                                          kicked, channel));

                    channelUsers.RemoveAll(u => u.Nick == kicked);
                }
            }
        }

        private void HandleQuitEvent(IRCQuitEvent ev)
        {
            lock (_usersLock)
            {
                int cid = ev.EventInfo.ConnectionId;
                InitList(cid);
                // Don't care what the channel name is.
                foreach (List<IRCUser> channelUsers in users[cid].Values)
                {
                    // If the user isn't on the channel, skip this channel.
                    if (!channelUsers.Any(u => u.Nick == ev.User.Nick)) continue;

                    // Remove the user.
                    channelUsers.RemoveAll(u => u.Nick == ev.User.Nick);
                }
            }
        }

        private void HandleWhoEvent(IRCWhoEvent ev)
        {
            lock (_usersLock)
            {
                int cid = ev.EventInfo.ConnectionId;
                string channel = ev.Channel.ToLower();
                InitList(cid, channel);
                users[cid][channel].Clear();

                foreach (IRCUserWhoEntry entry in ev.Entries)
                    users[cid][channel].Add(entry.User);
            }

        }

        private void InitList(int connectionId, string channel)
        {
            channel = channel.ToLower();
            lock (_usersLock)
            {
                InitList(connectionId);

                if (!users[connectionId].ContainsKey(channel))
                    users[connectionId].Add(channel, new List<IRCUser>());
            }
        }

        private void InitList(int connectionId)
        {
            lock (_usersLock)
            {
                if (!users.ContainsKey(connectionId))
                    users.Add(connectionId, new Dictionary<string, List<IRCUser>>());
            }
        }


    }
}
