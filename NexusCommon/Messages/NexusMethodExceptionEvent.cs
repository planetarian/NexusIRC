using System;
using System.Collections.Generic;

namespace Nexus.Messages
{
    [Serializable]
    public class NexusMethodExceptionEvent : NexusEvent
    {
        private const string componentKey = "component";
        private const string methodKey = "method";
        private const string exceptionKey = "exception";
        private const string messageName = "Nexus.MethodExceptionEvent";

        public string ComponentName { get; private set; }
        public string MethodName { get; private set; }
        public Exception Exception { get; private set; }

        public NexusMethodExceptionEvent(string componentName, string methodName, Exception exception)
            : base(messageName, new Dictionary<string, object>
            {
                { componentKey, componentName },
                { methodKey, methodName },
                { exceptionKey, exception }
            })
        {
            ComponentName = componentName;
            MethodName = methodName;
            Exception = exception;
        }
    }
}
