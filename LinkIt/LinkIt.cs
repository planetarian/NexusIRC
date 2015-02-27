using System;
using System.Collections.Generic;
using Nexus;
using Nexus.Messages;

namespace LinkIt
{
    public class LinkIt : NexusComponent
    {
        private readonly List<string> wpCommands =
            new List<string> {"wp", "wikipedia"};

        private readonly List<string> gtfyCommands =
            new List<string> {"gtfy", "lmgtfy", "google", "goog"};

        private const string wpBaseUri = "http://en.wikipedia.org/w/index.php?search=";
        private const string gtfyBaseUri = "http://lmgtfy.com/?q=";

        public LinkIt()
        {
            RegisterListener<IRCCommandEvent>(GoLinkIt);
        }

        private void GoLinkIt(IRCCommandEvent ev)
        {
            if (wpCommands.Contains(ev.Command))
            {
                CallMethod("IRC.Reply", ev, wpBaseUri + String.Join("+", ev.Parameters));
            }
            else if (gtfyCommands.Contains(ev.Command))
            {
                CallMethod("IRC.Reply", ev, gtfyBaseUri + String.Join("+", ev.Parameters));
            }
        }
    }
}
