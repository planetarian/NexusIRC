using System;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using Nexus;
using Nexus.Messages;

namespace Roll
{
    public class Roll : NexusComponent
    {
        Random rand = new Random();

        public Roll()
        {
            RegisterListener<IRCCommandEvent>(HandleRollDice);
        }

        private void HandleRollDice(IRCCommandEvent ev)
        {
            bool isCoin = ev.Command == "coin" || ev.Command == "flip";
            bool isDice = ev.Command == "roll" || ev.Command == "dice";
            if (!isCoin && !isDice) return;


            int defaultNumDice = 1;
            int defaultNumSides = isCoin ? 2 : 6;
            bool hasNumSides = false;

            bool empty = ev.Parameters.Length == 0;
            if (!empty)
            {
                var matches = Regex.Matches(" " + String.Join(" ", ev.Parameters),
                    @" ((?<repeat>[~.])|(?<numdice>\d+)?(d(?<numsides>\d+))?)", RegexOptions.Compiled);
                if (matches.Count == 0)
                {
                    CallMethod("IRC.Reply", ev,
                        "Invalid parameters. Must be <numdice> + d<numsides> (ex: 1d8, 3, d10)");
                    return;
                }


                int numDice = defaultNumDice;
                int numSides = defaultNumSides;
                foreach (Match match in matches)
                {
                    bool repeat = match.Groups["repeat"].Success;
                    if (!repeat)
                    {
                        numDice = match.Groups["numdice"].Success
                            ? Int32.Parse(match.Groups["numdice"].Value)
                            : defaultNumDice;

                        hasNumSides = match.Groups["numsides"].Success;
                        numSides = hasNumSides
                            ? Int32.Parse(match.Groups["numsides"].Value)
                            : defaultNumSides;
                    }

                    if (isCoin && hasNumSides)
                    {
                        CallMethod("IRC.Reply", ev,
                               "There's no point in specifying a d-value when flipping a coin...");
                    }
                    RollDice(ev, numDice, numSides);
                }
            }
            else
            {
                RollDice(ev, defaultNumDice, defaultNumSides);
            }

        }


        private void RollDice(IRCMessageEvent ev, int numDice = 1, int numSides = 6)
        {
            if (numDice < 1 || numSides < 2)
            {
                CallMethod("IRC.Reply", ev, "must be at least 1 die and at least 2 sides.");
                return;
            }

            bool coin = numSides == 2;

            string output;

            if (coin)
                output = "Flipped " + numDice + " coin" + (numDice == 1 ? "" : "s") + ". Result" + (numDice == 1 ? "" : "s") + ": ";
            else
                output = "Rolled " + numDice + (numDice==1?" die":" dice") + " with " + numSides + " sides. Result" + (numDice == 1 ? "" : "s") +": ";


            int heads = 0;
            int sum = 0;
            for (int d = 0; d < numDice; d++)
            {
                if (d > 0)
                    output += ", ";

                int val = rand.Next(0, numSides) + 1;
                heads += (val == 2 ? 0 : 1);
                sum += val;

                if (coin)
                    output += (val == 2 ? "tails" : "heads");
                else
                    output += val;
            }

            int avg = sum/numDice;

            if (numDice > 1)
            {
                if (coin)
                    output += "; Heads: " + heads;
                else
                    output += "; Sum: " + sum + "; Average: " + avg;
            }

            CallMethod("IRC.Reply", ev, output);

        }

    }

}
