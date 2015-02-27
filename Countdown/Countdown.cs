using System;
using System.Globalization;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using Nexus;
using Nexus.Messages;

namespace Countdown
{
    internal class CountdownTracker
    {
        public int ConnectionId;
        public string Target;
        public int StartValue;
        public int Interval;

        public bool Restart = true;
        public int CurrentValue;

        public CountdownTracker(int connectionId, string target, int startValue = 5, int interval = 2)
        {
            ConnectionId = connectionId;
            Target = target;
            StartValue = startValue;
            Interval = interval;
        }
    }

    public class Countdown : NexusComponent
    {
        private const int maxCount = 10;
        private const int maxInterval = 10;
        private const int startDelay = 3;

        private readonly List<string> countCommands =
            new List<string> { "count", "countdown" };

        private readonly List<string> stopCountCommands =
                    new List<string> { "stopcount", "stopcountdown" };



        private readonly List<CountdownTracker> counts = new List<CountdownTracker>();
        private readonly object _countsLock = new object();
        private bool running = true;

        public Countdown()
        {
            RegisterListener<IRCCommandEvent>(CommandReceived);
        }

        public override void Shutdown()
        {
            running = false;
        }

        private void CommandReceived(IRCCommandEvent ev)
        {
            if (countCommands.Contains(ev.Command))
                HandleStartCount(ev);
            else if (stopCountCommands.Contains(ev.Command))
                HandleStopCount(ev);
        }

        private void HandleStopCount(IRCCommandEvent ev)
        {
            lock (_countsLock)
            {
                if (counts.Any(c => c.Target == ev.ReturnTarget))
                {
                    CallMethod("IRC.Reply", ev, "Countdown stopped!");
                    CountdownTracker count = counts.Find(c => c.Target == ev.ReturnTarget);
                    counts.Remove(count);
                }
                else
                    CallMethod("IRC.Reply", ev, "No countdown in progress!");

            }
        }

        private void HandleStartCount(IRCCommandEvent ev)
        {
            lock (_countsLock)
            {
                CountdownTracker myCount;
                bool started = false;
                if (counts.Any(c => c.Target == ev.ReturnTarget))
                {
                    myCount = counts.Find(c => c.Target == ev.ReturnTarget);
                    started = true;
                }
                else
                {
                    myCount = new CountdownTracker(ev.EventInfo.ConnectionId, ev.ReturnTarget);
                }

                myCount.Restart = true;

                // Check start parameter
                if (ev.Parameters.Length > 0)
                {
                    uint start;
                    if (!UInt32.TryParse(ev.Parameters[0], out start))
                    {
                        CallMethod("IRC.Reply", ev, "Invalid parameters! First parameter should be a number for me to start on.");
                        return;
                    }
                    if (myCount.StartValue > maxCount)
                    {
                        CallMethod("IRC.Reply", ev, 
                            "Start count can't be more than " + maxCount + ". " +
                            myCount.StartValue + " is too much!");
                        return;
                    }
                    myCount.StartValue = (int)start;
                }

                // Check Interval parameter
                if (ev.Parameters.Length > 1)
                {
                    uint interval;
                    if (!UInt32.TryParse(ev.Parameters[1], out interval))
                    {
                        CallMethod("IRC.Reply", ev,
                                   "Invalid parameters! Second parameter should be the number of seconds between counts.");
                        return;
                    }
                    if (myCount.Interval > maxCount)
                    {
                        CallMethod("IRC.Reply", ev,
                                   "Interval can't be more than " + maxInterval + " seconds. " +
                                   myCount.Interval + " is too much!");
                        return;
                    }
                    myCount.Interval = (int)interval;
                }

                if (started)
                    return;

                counts.Add(myCount);
                ThreadPool.QueueUserWorkItem(CountThreadExecute, myCount.Target);
            }
        }

        private void CountThreadExecute(object targetObj)
        {
            var target = targetObj as string;
            if (target == null) return;
            
            while (running)
            {
                lock (_countsLock)
                {
                    CountdownTracker count;
                    if (counts.Any(c => c.Target == target))
                        count = counts.Find(c => c.Target == target);
                    else
                        return;

                    if (count.Restart)
                    {
                        CallMethod("IRC.Say", count.ConnectionId, count.Target, "Starting countdown!");
                        count.Restart = false;
                        count.CurrentValue = count.StartValue;
                        Thread.Sleep(startDelay * 1000 - 100);
                    }
                    else if (count.CurrentValue > 0)
                    {
                        CallMethod("IRC.Say", count.ConnectionId, count.Target,
                            count.CurrentValue.ToString(CultureInfo.InvariantCulture), true);
                        count.CurrentValue--;
                        Thread.Sleep(count.Interval * 1000 - 100); // We sleep for one second below to avoid keeping a lock
                    }
                    else
                    {
                        CallMethod("IRC.Say", count.ConnectionId, count.Target, "Uguu!", true);
                        counts.Remove(count);
                        return;
                    }
                }
                Thread.Sleep(100);
            }
        }
    }
}
