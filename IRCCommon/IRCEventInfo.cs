using System;

namespace IRCCommon
{
    [Serializable]
    public struct IRCEventInfo
    {
        /// <summary> Time of the event's occurrance. </summary>
        public DateTime Time { get; private set; }

        /// <summary> Connection ID the message applies to. </summary>
        public int ConnectionId { get; private set; }

        /// <summary> Name of the network the message applies to. </summary>
        public string NetworkName { get; private set; }

        /// <summary> Address of the server the message applies to. </summary>
        public string ServerAddress { get; private set; }

        public IRCEventInfo(DateTime time, int connectionId, string networkName = null, string serverAddress = null)
            : this()
        {
            Time = time;
            ConnectionId = connectionId;
            NetworkName = networkName;
            ServerAddress = serverAddress;
        }

        public IRCEventInfo(int connectionId, string networkName = null, string serverAddress = null)
            : this(DateTime.Now, connectionId, networkName, serverAddress)
        {
        }
    }
}
