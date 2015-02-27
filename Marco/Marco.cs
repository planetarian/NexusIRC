using System;
using Nexus;
using Nexus.Messages;

namespace Marco
{
    public class Marco : NexusComponent
    {
        public Marco()
        {
            RegisterListener<IRCCommandEvent>(Polo);
        }

        private void Polo(IRCCommandEvent ev)
        {
            if (ev.Command != "marco") return;

            CallMethod("IRC.Reply", ev, "polo~");
        }
    }
}
