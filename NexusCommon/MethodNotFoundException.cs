using System;
using System.Runtime.Serialization;

namespace Nexus
{
    [Serializable]
    public class MethodNotFoundException : Exception
    {
        public string MethodName { get; private set; }

        public MethodNotFoundException(string methodName)
            : base("Method not found: " + methodName)
        {
            MethodName = methodName;
        }

        public MethodNotFoundException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
