﻿// ****************************************************************************
// <copyright file="SomeComponent.cs" company="ALICERAIN">
// Copyright © ALICERAIN 2012
// </copyright>
// ****************************************************************************
// <author>Ash Wolford</author>
// <email>piro@live.com</email>
// <date>2012-03-26</date>
// <project>NexusIRC.NexusComponents</project>
// <web>http://pirocast.net/</web>
// <license>
// All rights reserved, until I decide on an appropriate license.
// </license>
// ****************************************************************************

using System;
using System.Linq;
using System.Text.RegularExpressions;
using Nexus;
using Nexus.Messages;

namespace NexusComponents
{
    public class SomeComponent : NexusComponent
    {
        private readonly object _printLock = new object();

        private string currentTarget;
        private int currentCid = 1;

        public SomeComponent()
        {
            RegisterListener<IRCEvent>(Print, true);

            RegisterListener<UserMessageEvent>(IRCSendMessage);
            RegisterListener<UserCommandEvent>(HandleCommand);

            RegisterListener<NexusMethodNotFoundEvent>(HandleMethodNotFound);
            RegisterListener<NexusMethodExceptionEvent>(HandleMethodException);
        }

        private void HandleMethodException(NexusMethodExceptionEvent ev)
        {
            lock (_printLock)
            {
                SetConsoleColor(ConsoleColor.Red, ConsoleColor.DarkRed);
                Console.WriteLine("Method {0} called by {1} threw {2}.",
                    ev.MethodName, ev.ComponentName, ev.Exception.GetType());
                Console.WriteLine("Exception details: {0}", ev.Exception.Message);
                SetConsoleColor();
            }
        }

        private void HandleMethodNotFound(NexusMethodNotFoundEvent ev)
        {
            lock (_printLock)
            {
                SetConsoleColor(ConsoleColor.Red, ConsoleColor.DarkRed);
                Console.WriteLine("Method {0} not registered.", ev.MethodName);
                SetConsoleColor();
            }
        }

        private void HandleCommand(UserCommandEvent ev)
        {
            lock (_printLock)
            {
                switch (ev.Command.ToLower())
                {
                    case "t":
                    case "targ":
                    case "target":
                        SetTarget(ev);
                        break;
                    case "raw":
                    case "r":
                        CallMethod("IRC.SendRaw", currentCid, ev.ParameterString);
                        break;
                    case "dc":
                    case "disconnect":
                        HandleDisconnectCommand(ev);
                        break;
                    case "connect":
                    case "server":
                        HandleConnectCommand(ev);
                        break;
                    case "nc":
                    case "new":
                    case "newconnection":
                        int newCid;
                        CallMethod("IRC.CreateConnection", out newCid);
                        break;
                    default:
                        SetConsoleColor(ConsoleColor.Red, ConsoleColor.DarkRed);
                        Console.WriteLine("Invalid command.");
                        break;
                }


                SetConsoleColor();
            }
        }

        private void HandleConnectCommand(UserCommandEvent ev)
        {
            lock (_printLock)
            {

                Match match = Regex.Match(ev.ParameterString ?? String.Empty,
                    @"(?<cid>\d+)? *((?<address>[\w.-]+)([ :](?<port>\d+))?)?");

                if (!match.Success)
                {
                    SetConsoleColor(ConsoleColor.Red, ConsoleColor.DarkRed);
                    Console.WriteLine("Invalid parameters."); // TODO: better message
                    return;
                }

                int cid = currentCid;

                if (match.Groups["cid"].Success)
                {
                    Int32.TryParse(match.Groups["cid"].Value, out cid);
                    // no need to test, we know it was a success already

                    bool hasCid;
                    CallMethod("IRC.HasConnection", out hasCid, cid);

                    if (!hasCid)
                    {
                        SetConsoleColor(ConsoleColor.Red, ConsoleColor.DarkRed);
                        Console.WriteLine("Connection ID {0} does not exist.", cid);
                        return;
                    }
                }

                if (match.Groups["address"].Success)
                {
                    string address = match.Groups["address"].Value;

                    int port;
                    if (match.Groups["port"].Success && Int32.TryParse(match.Groups["port"].Value, out port))
                        CallMethod("IRC.Connect", cid, address, port);
                    else
                        CallMethod("IRC.Connect", cid, address);
                }
                else
                    CallMethod("IRC.Connect", cid);


                /*
                if (ev.Parameters.Length == 0)
                {
                    CallMethod("IRC.Connect", currentCid);
                }
                else if (ev.Parameters.Length > 0)
                {
                    string address = ev.Parameters[0];
                    bool hasPort = ev.Parameters.Length > 1;
                    if (hasPort)
                    {
                        int port = 0;
                        if (!Int32.TryParse(ev.Parameters[1], out port))
                        {
                            SetConsoleColor(ConsoleColor.Red, ConsoleColor.DarkRed);
                            Console.WriteLine("Second parameter (port) invalid.");
                            return;
                        }
                        else
                        {
                        }
                    }
                    else
                    {
                        CallMethod("IRC.Connect", currentCid, address);
                    }
                }*/
                SetConsoleColor();
            }
        }

        private void HandleDisconnectCommand(UserCommandEvent ev)
        {
            int cid = 1;
            string message = "User disconnected.";

            if (ev.Parameters.Length == 0)
            {
                cid = currentCid;
            }
            else if (ev.Parameters.Length > 0)
            {
                if (!Int32.TryParse(ev.Parameters[0], out cid))
                {
                    SetConsoleColor(ConsoleColor.Red, ConsoleColor.DarkRed);
                    Console.WriteLine("Invalid parameter. First parameter must be a connection ID.");
                    return;
                }
                if (ev.Parameters.Length > 1)
                    message = ev.ParameterString.Substring(ev.ParameterString.IndexOf(' ') + 1);
            }

            bool hasCid;
            CallMethod("IRC.HasConnection", out hasCid, cid);

            if (!hasCid)
            {
                SetConsoleColor(ConsoleColor.Red, ConsoleColor.DarkRed);
                Console.WriteLine("Connection ID {0} does not exist.", cid);
            }
            else
                CallMethod("IRC.Disconnect", cid, message);
        }

        private void SetTarget(UserCommandEvent ev)
        {
            if (ev.Parameters.Length != 1)
            {
                SetConsoleColor(ConsoleColor.Red, ConsoleColor.DarkRed);
                Console.WriteLine("Must provide a single parameter specifying the target nick/channel.");
            }
            else
            {
                string target = ev.Parameters[0];
                int targetCid;
                if (Int32.TryParse(target, out targetCid))
                {
                    bool hasCid;
                    CallMethod("IRC.HasConnection", out hasCid, targetCid);
                    if (hasCid)
                    {
                        currentCid = targetCid;
                        SetConsoleColor(ConsoleColor.Green, ConsoleColor.DarkGreen);
                        Console.WriteLine("Target connection ID set to {0}", targetCid);
                    }
                    else
                    {
                        SetConsoleColor(ConsoleColor.Red, ConsoleColor.DarkRed);
                        Console.WriteLine("Connection ID {0} does not exist.", targetCid);
                    }
                }

                else
                {
                    currentTarget = target;
                    SetConsoleColor(ConsoleColor.Green, ConsoleColor.DarkGreen);
                    Console.WriteLine("Chat target set to {0}", currentTarget);
                }

                
            }
        }

        private void IRCSendMessage(UserMessageEvent ev)
        {
            lock (_printLock)
            {
                if (currentTarget == null)
                {
                    SetConsoleColor(ConsoleColor.Red,ConsoleColor.DarkRed);
                    Console.WriteLine("You must first set a target (channel/user) with /t <target>");
                }
                else
                {
                    CallMethod("IRC.Say", currentCid, currentTarget, ev.Message);
                }

                SetConsoleColor();
            }
        }




        public void Print(IRCEvent ev)
        {
            lock (_printLock)
            {
                string evType = ev.GetType().Name;
                switch (evType)
                {
                    case "IRCConnectionCreatedEvent":
                        SetConsoleColor(ConsoleColor.Green, ConsoleColor.DarkGreen);
                        Print((IRCConnectionCreatedEvent) ev);
                        break;
                    case "IRCConnectionClosedEvent":
                        SetConsoleColor(ConsoleColor.Blue, ConsoleColor.DarkBlue);
                        Print((IRCConnectionClosedEvent) ev);
                        break;

                    case "IRCConnectingEvent":
                        SetConsoleColor(ConsoleColor.Blue);
                        Print((IRCConnectingEvent)ev);
                        break;
                    case "IRCConnectedEvent":
                        SetConsoleColor(ConsoleColor.Green);
                        Print((IRCConnectedEvent)ev);
                        break;
                    case "IRCConnectFailedEvent":
                        SetConsoleColor(ConsoleColor.Red);
                        Print((IRCConnectFailedEvent)ev);
                        break;
                    case "IRCDisconnectedEvent":
                        SetConsoleColor(ConsoleColor.Red);
                        Print((IRCDisconnectedEvent)ev);
                        break;
                    case "IRCDisconnectFailedEvent":
                        SetConsoleColor(ConsoleColor.Red, ConsoleColor.DarkRed);
                        Print((IRCDisconnectFailedEvent)ev);
                        break;

                    case "IRCJoinEvent":
                        SetConsoleColor(ConsoleColor.DarkCyan);
                        Print((IRCJoinEvent)ev);
                        break;
                    case "IRCPartEvent":
                        SetConsoleColor(ConsoleColor.DarkRed);
                        Print((IRCPartEvent)ev);
                        break;
                    case "IRCQuitEvent":
                        SetConsoleColor(ConsoleColor.DarkRed);
                        Print((IRCQuitEvent)ev);
                        break;
                    case "IRCInviteEvent":
                        SetConsoleColor(ConsoleColor.DarkGreen);
                        Print((IRCInviteEvent)ev);
                        break;
                    case "IRCKickEvent":
                        SetConsoleColor(ConsoleColor.Red);
                        Print((IRCKickEvent)ev);
                        break;

                    case "IRCInfoMessage":
                        SetConsoleColor(ConsoleColor.DarkGray);
                        PrintData(ev, "Info");
                        break;
                    case "IRCDataReceivedEvent":
                        SetConsoleColor(ConsoleColor.DarkMagenta);
                        //PrintData(ev, "Data");
                        break;

                    case "IRCPrivmsgEvent":
                        SetConsoleColor();
                        Print((IRCPrivmsgEvent) ev);
                        break;
                    case "IRCCommandEvent":
                        SetConsoleColor(ConsoleColor.Yellow, ConsoleColor.DarkYellow);
                        Print((IRCCommandEvent) ev);
                        break;
                    case "IRCNoticeEvent":
                        SetConsoleColor(ConsoleColor.DarkGreen);
                        Print((IRCNoticeEvent) ev);
                        break;

                    case "IRCNickEvent":
                        SetConsoleColor(ConsoleColor.DarkGray);
                        Print((IRCNickEvent) ev);
                        break;

                    case "IRCWhoEvent":
                        SetConsoleColor(ConsoleColor.DarkGray);
                        Print((IRCWhoEvent) ev);
                        break;
                    case "IRCNamesEvent":
                        SetConsoleColor(ConsoleColor.DarkGray);
                        Print((IRCNamesEvent) ev);
                        break;

                    case "IRCUserModeEvent":
                        SetConsoleColor(ConsoleColor.DarkGray);
                        Print((IRCUserModeEvent)ev);
                        break;
                    case "IRCChannelModeEvent":
                        SetConsoleColor(ConsoleColor.DarkGray);
                        Print((IRCChannelModeEvent)ev);
                        break;

                    default:
                        SetConsoleColor(ConsoleColor.DarkRed);
                        Print((object) ev);
                        break;
                }

                SetConsoleColor();
            }
        }


        private static void Print(IRCConnectionCreatedEvent ev)
        {
            Console.WriteLine("[{0}] Connection {1} created", EvTime(ev), ev.EventInfo.ConnectionId);
        }

        private static void Print(IRCConnectionClosedEvent ev)
        {
            Console.WriteLine("[{0}] Connection {1} closed", EvTime(ev), ev.EventInfo.ConnectionId);
        }

        private static void Print(IRCConnectingEvent ev)
        {
            Console.WriteLine("[{0}][{1}] Connecting to {2}", EvTime(ev),
                ev.EventInfo.ConnectionId, ev.EventInfo.ServerAddress);
        }

        private static void Print(IRCConnectedEvent ev)
        {
            Console.WriteLine("[{0}][{1}] Connected to {2}", EvTime(ev),
                ev.EventInfo.ConnectionId, ev.EventInfo.ServerAddress);
        }

        private static void Print(IRCConnectFailedEvent ev)
        {
            Console.WriteLine("[{0}][{1}] Connection to {2} failed: {3}", EvTime(ev),
                ev.EventInfo.ConnectionId, ev.EventInfo.ServerAddress, ev.Message);
        }

        private static void Print(IRCDisconnectedEvent ev)
        {
            Console.WriteLine("[{0}][{1}] Disconnected from {2} ({3})", EvTime(ev),
                ev.EventInfo.ConnectionId, ev.EventInfo.ServerAddress, ev.Message);
        }

        private static void Print(IRCDisconnectFailedEvent ev)
        {
            Console.WriteLine("[{0}][{1}] Disconnection from {2} failed: {3}", EvTime(ev),
                ev.EventInfo.ConnectionId, ev.EventInfo.ServerAddress, ev.Message);
        }


        private static void Print(IRCJoinEvent ev)
        {
            Console.WriteLine("[{0}][{1}][{2}] Joins: {3} ({4}!{5})", EvTime(ev),
                ev.EventInfo.ConnectionId, ev.Channel, ev.User.Nick, ev.User.UserName, ev.User.Host);
        }

        private static void Print(IRCPartEvent ev)
        {
            Console.WriteLine("[{0}][{1}][{2}] Parts: {3} ({4}!{5})", EvTime(ev),
                ev.EventInfo.ConnectionId, ev.Channel, ev.User.Nick, ev.User.UserName, ev.User.Host);
        }

        private static void Print(IRCQuitEvent ev)
        {
            Console.WriteLine("[{0}][{1}] Quits: {2} ({3}!{4}) ({5})", EvTime(ev),
                ev.EventInfo.ConnectionId, ev.User.Nick, ev.User.UserName, ev.User.Host, ev.Reason);
        }

        private static void Print(IRCInviteEvent ev)
        {
            Console.WriteLine("[{0}][{1}] {2} ({3}!{4}) invites you to join {5}", EvTime(ev),
                ev.EventInfo.ConnectionId, ev.Sender.Nick, ev.Sender.UserName, ev.Sender.Host, ev.Channel);
        }

        private static void Print(IRCKickEvent ev)
        {
            Console.WriteLine("[{0}][{1}][{2}] {3} was kicked by {4} ({5})", EvTime(ev),
                ev.EventInfo.ConnectionId, ev.Channel, ev.Kicked[0], ev.User.Nick, ev.Reason);
        }


        private static void Print(IRCMessageEvent ev)
        {
            Console.WriteLine("[{0}][{1}][{2}] <{3}> {4}", EvTime(ev),
                ev.EventInfo.ConnectionId, ev.Target, ev.Sender.Split('!')[0], ev.Message);
        }


        private static void Print(IRCNickEvent ev)
        {
            Console.WriteLine("[{0}][{1}] {2} is now known as {3}", EvTime(ev),
                ev.EventInfo.ConnectionId, ev.OldNick, ev.NewNick);
        }


        private static void Print(IRCWhoEvent ev)
        {
            foreach (var entry in ev.Entries)
                Console.WriteLine("[{0}][{1}][{2}] {3}", EvTime(ev),
                    ev.EventInfo.ConnectionId, ev.Channel, entry.User.Nick);
            Console.WriteLine("[{0}][{1}][{2}] End of WHO list.", EvTime(ev),
                ev.EventInfo.ConnectionId, ev.Channel);
        }

        private static void Print(IRCNamesEvent ev)
        {
            Console.WriteLine("[{0}][{1}][{2}] {3}", EvTime(ev),
                ev.EventInfo.ConnectionId, ev.Channel, String.Join(" ", ev.Entries.Select(e => e.Nick)));
            Console.WriteLine("[{0}][{1}][{2}] End of NAMES list.", EvTime(ev),
                ev.EventInfo.ConnectionId, ev.Channel);
        }

        private static void Print(IRCUserModeEvent ev)
        {
            Console.WriteLine("[{0}][{1}] {2} sets mode: {3}", EvTime(ev),
                ev.EventInfo.ConnectionId, ev.Target, ev.Modes);
        }

        private static void Print(IRCChannelModeEvent ev)
        {
            Console.WriteLine("[{0}][{1}][{2}] {3} sets mode: {4}", EvTime(ev),
                ev.EventInfo.ConnectionId, ev.Channel, ev.Sender.Split('!')[0], ev.Modes);
        }


        private static void PrintData(IRCEvent ev, string pretext)
        {
            var value = ev.Value as String;
            if (value == null || value.StartsWith("PONG "))
                return;

            Console.WriteLine("[{0}][{1}] {2}: {3}", EvTime(ev), ev.EventInfo.ConnectionId, pretext,
                String.Join(",", ev.Data.Select(d => "{" + d.Key + ":" + d.Value + "}")));
        }


        private static void Print(object ev)
        {
            var nexEvent = ev as NexusEvent;
            if (nexEvent != null)
                Console.WriteLine("[{0}] {1} {2}", FormattedTime(nexEvent.Time), nexEvent.GetType().Name, nexEvent.MessageName);
            else
                Console.WriteLine("[{0}] {1}", FormattedTime(DateTime.Now), ev);
        }

        /*private void Print(IRCDataReceivedEvent ev)
        {
            lock (_printLock)
            {
                Console.WriteLine(ev.EventInfo.Time + ": ");
                Console.WriteLine("- Prefix: " + ev.Message.Prefix);
                Console.WriteLine("- Command: " + ev.Message.Command);
                Console.WriteLine("- Parameters: " + String.Join(" ", ev.Message.Parameters));
                Console.WriteLine("- Trailing: " + ev.Message.Trailing);
            }
        }
        */

        /*private void PrintRaw(IRCDataReceivedEvent ev)
        {
            Console.WriteLine(ev.Message.Raw);
        }*/

        private static void SetConsoleColor(
            ConsoleColor foreground = ConsoleColor.White,
            ConsoleColor background = ConsoleColor.Black)
        {
            Console.BackgroundColor = background;
            Console.ForegroundColor = foreground;
        }

        private static string EvTime(IRCEvent ev)
        {
            return FormattedTime(ev.EventInfo.Time);
        }

        private static string FormattedTime(DateTime dt)
        {
            return dt.ToString("HH:mm:ss");
        }
    }
}