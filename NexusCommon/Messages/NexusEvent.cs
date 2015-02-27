using System;
using System.Collections.Generic;
using System.Linq;

namespace Nexus.Messages
{
    [Serializable]
    public class NexusEvent
    {
        private const string valueKey = "value";

        /// <summary> Name of the message. </summary>
        public string MessageName { get; private set; }

        /// <summary> Name of the component that sent this message. </summary>
        public string SourceComponent
        {
            get { return _sourceComponent; }
            set
            {
                if (_sourceComponent != null)
                    throw new InvalidOperationException("Source component already set.");
                _sourceComponent = value;
            }
        }

        private string _sourceComponent;

        /// <summary> Keep the message data internal and clone for external access. </summary>
        protected Dictionary<string, object> _data;

        /// <summary> Retrieves the message data. </summary>
        /// <returns> Message data in Dictionary form. </returns>
        public Dictionary<string, object> Data
        {
            get
            {
                return _data == null
                    ? new Dictionary<string, object>()
                    : _data.ToDictionary(d => d.Key, d => d.Value);
            }
            protected set { _data = value; }
        }

        public object Value
        {
            get
            {
                if (_data != null && _data.Count == 1 && _data.ContainsKey(valueKey))
                    return _data[valueKey];
                return null;
            }
            protected set
            {
                if (_data != null)
                    _data[valueKey] = value;
            }
        }

        public DateTime Time { get; private set; }



        public NexusEvent(string messageName, Dictionary<string, object> data)
            : this(messageName)
        {
            if (data != null)
                _data = data;
        }

        public NexusEvent(string messageName, object value)
            : this(messageName)
        {
            Value = value;
        }

        public NexusEvent(string messageName)
        {
            if (String.IsNullOrWhiteSpace(messageName))
                throw new ArgumentException("Must specify a message name.", "messageName");

            _data = new Dictionary<string, object>();
            MessageName = messageName;
            Time = DateTime.Now;
        }
    }
}
