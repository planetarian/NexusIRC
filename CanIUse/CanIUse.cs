using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nexus;
using Nexus.Messages;

namespace CanIUse
{
    public class CanIUse : NexusComponent
    {
        public CanIUse()
        {
            RegisterListener<IRCCommandEvent>(Reply);
        }

        public override void Startup()
        {
            base.Startup();
        }

        public override void Shutdown()
        {
            base.Shutdown();
        }

        private void Reply(IRCCommandEvent ev)
        {
            if (ev.Command != "caniuse") return;


        }
    }
}
