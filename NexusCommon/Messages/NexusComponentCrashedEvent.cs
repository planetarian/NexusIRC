using System;

namespace Nexus.Messages
{
    [Serializable]
    public class NexusComponentCrashedEvent : NexusEvent
    {
        public string ComponentName { get; private set; }
        public string Reason { get; private set; }
        public string Details { get; private set; }
        private const string messageName = "Nexus.ComponentCrashedEvent";

        public NexusComponentCrashedEvent(string componentName, string reason, string details = null)
            : base(messageName, componentName)
        {
            ComponentName = componentName;
            Reason = reason;
            Details = details;
        }
    }
}
