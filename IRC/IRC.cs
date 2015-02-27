using System.Threading;
using Nexus;
using System;
using System.Collections.Generic;

namespace IRC
{
    public class IRC : NexusComponent
    {
        /// <summary> The default port to use if no port is specified. </summary>
        internal const ushort DefaultPort = 6667;
        
        // For fun, let's mimic Environment.Newline =)
        internal static string NewLine = "\r\n";

        internal string DefaultNick { get; private set; }
        internal string DefaultAltNick { get; private set; }
        internal string DefaultRealName { get; private set; }
        internal string DefaultUserName { get; private set; }

        private readonly Dictionary<int, Connection> connections
            = new Dictionary<int, Connection>();

        private int nextId = 1;

        private readonly object _connectionsLock = new object();

        public IRC()
        {
            ExposeMethods();
        }

        public override void Startup()
        {
            //DefaultNick = "TestNick";
            DefaultNick = "NexusTest";
            DefaultAltNick = "NexusIRC";
            DefaultRealName = "Nexus Experimental Modular IRC client";
            DefaultUserName = "nexus";

            //Connect("localhost");
            Thread.Sleep(5000);
            Connect("irc.irchighway.net");
        }

        public override void Shutdown()
        {
            CloseAllConnections(Strings.ShutdownDefault);
        }

        public bool HasConnection(int connectionId)
        {
            lock (_connectionsLock)
            {
                return connections.ContainsKey(connectionId);
            }
        }

        public int CreateConnection()
        {
            var id = nextId++;
            connections[id] = new Connection(this, id);
            return id;
        }

        public void Connect(int connectionId)
        {
            lock (_connectionsLock)
            {
                // Verify the connection exists.
                if (!HasConnection(connectionId))
                    throw new InvalidOperationException(Strings.InvalidConnection);

                // Verify the connection exists.
                if (!HasConnection(connectionId))
                    throw new InvalidOperationException(Strings.InvalidConnection);

                // Disconnect the Connection if it's connected already.
                if (connections[connectionId].Connected)
                    connections[connectionId].Disconnect("Reconnecting");

                // Connect to the server via an ad-hoc network.
                connections[connectionId].Connect();
            }
        }

        /// <summary> Creates a new Connection using the given address and port. </summary>
        /// <param name="address"> Address to connect to. </param>
        /// <param name="port"> Port to connect to. </param>
        /// <param name="connectionId"> </param>
        /// <returns> String if an error occurred,
        /// True if the creation of the connection was successful. </returns>
        public void Connect(int connectionId, string address, int port)
        {
            // No funny addresses.
            if (String.IsNullOrWhiteSpace(address))
                throw new InvalidOperationException(Strings.InvalidAddress);

            // Convert the int port into a ushort.
            // Parameter is left as int for simplicity.
            ushort properPort;
            try
            {
                properPort = Convert.ToUInt16(port);
            }
            catch (OverflowException)
            {
                throw new InvalidOperationException(Strings.InvalidPort);
            }

            lock (_connectionsLock)
            {
                // Verify the connection exists.
                if (!HasConnection(connectionId))
                    throw new InvalidOperationException(Strings.InvalidConnection);

                // Disconnect the Connection if it's connected already.
                if (connections[connectionId].Connected)
                    connections[connectionId].Disconnect("Reconnecting");

                // Connect to the server via an ad-hoc network.
                var network = new Network(new List<Server> { new Server(address, properPort) });
                connections[connectionId].Connect(network);
            }
        }

        public void Connect(int connectionId, string address)
        {
            Connect(connectionId, address, DefaultPort);
        }

        /// <summary> Creates a new Connection using the given address and port. </summary>
        /// <param name="address"> Address to connect to. </param>
        /// <param name="port"> Port to connect to. </param>
        /// <returns> String if an error occurred,
        /// True if the creation of the connection was successful. </returns>
        public void Connect(string address, int port)
        {
            lock (_connectionsLock)
            {
                // Create a new connection and connect.
                int id = CreateConnection();
                Connect(id, address, port);
            }
        }

        /// <summary> Creates a new Connection using the given address on the default port. </summary>
        /// <param name="address"> Address to connect to. </param>
        /// <returns> String if an error occurred,
        /// True if the creation of the connection was successful. </returns>
        public void Connect(string address)
        {
            Connect(address, DefaultPort);
        }


        /// <summary> Forcefully disconnects the connection with the specified ID
        /// and sends a notification of the disconnect with a specified message. </summary>
        /// <param name="connectionId"> ID of the connection to disconnect. </param>
        /// <param name="message"> Notification message to send to components. </param>
        /// <returns> String if an error occurred,
        /// True if the creation of the connection was successful. </returns>
        public void Disconnect(int connectionId, string message)
        {
            lock (_connectionsLock)
            {
                // Verify the connection exists.
                if (!HasConnection(connectionId))
                    throw new InvalidOperationException(Strings.InvalidConnection);

                connections[connectionId].Disconnect(
                    message ?? Strings.DisconnectDefault);
            }
        }

        /// <summary> Closes a connection with the given ID and sends a notification
        /// with the given message. </summary>
        /// <param name="connectionId"> ID of the connection to be closed. </param>
        /// <param name="message"> Message to send to components notifying of the closure. </param>
        /// <returns> String if an error occurred,
        /// True if the creation of the connection was successful. </returns>
        public void CloseConnection(int connectionId, string message = null)
        {
            lock (_connectionsLock)
            {
                // Verify the connection exists.
                if (!HasConnection(connectionId))
                    throw new InvalidOperationException(Strings.InvalidConnection);

                connections[connectionId].CloseConnection(
                    message ?? Strings.CloseConnectionDefault);
            }
        }

        /// <summary> Disconnects and closes all connections and sends notifications of their closure. </summary>
        /// <param name="message"> Message to send to components notifying of the closure. </param>
        public void CloseAllConnections(string message = null)
        {
            foreach (var connection in connections)
                connection.Value.CloseConnection(message);
            connections.Clear();
        }


        /// <summary> Add data to the send queue at normal priority. </summary>
        /// <param name="connectionId"> ID of the connection sending the data. </param>
        /// <param name="data"> String message data to be added to the queue. </param>
        public void SendRaw(int connectionId, string data)
        {
            SendRaw(connectionId, data, false);
        }

        /// <summary> Add data to the send queue with the specified priority. </summary>
        /// <param name="connectionId"> ID of the connection sending the data. </param>
        /// <param name="data"> String message data to be added to the queue. </param>
        /// <param name="highPriority"> Whether to use high priority for this message. </param>
        public void SendRaw(int connectionId, string data, bool highPriority)
        {
            lock (_connectionsLock)
            {
                // Verify the connection exists.
                if (!HasConnection(connectionId))
                    throw new InvalidOperationException(Strings.InvalidConnection);

                connections[connectionId].QueueSend(data, highPriority);
            }
        }

        public string GetCurrentNick(int connectionId)
        {
            throw new NotImplementedException();
        }
    }
}
