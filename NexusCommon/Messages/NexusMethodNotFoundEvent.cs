using System;
using System.Collections.Generic;

namespace Nexus.Messages
{
    [Serializable]
    public class NexusMethodNotFoundEvent : NexusEvent
    {
        private const string componentKey = "component";
        private const string methodKey = "method";
        public string ComponentName { get; private set; }
        public string MethodName { get; private set; }
        private const string messageName = "Nexus.MethodNotFoundEvent";

        public NexusMethodNotFoundEvent(string componentName, string methodName)
            : base (messageName, new Dictionary<string, object>{{componentKey,componentName},{methodKey,methodName}})
        {
            ComponentName = componentName;
            MethodName = methodName;
        }
    }
}
