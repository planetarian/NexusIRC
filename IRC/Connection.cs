using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading;
using Nexus;
using Nexus.Messages;

namespace IRC
{
    internal class Connection
    {
        internal string CurrentNetworkName { get; private set; }
        internal string CurrentAddress { get; private set; }
        internal ushort CurrentPort { get; private set; }
        internal string CurrentServerPass { get; private set; }
        internal string CurrentNick { get; private set; }

        internal bool Connected
        {
            get
            {
                lock (_connectedLock)
                {
                    return _connected;
                }
            }
            private set
            {
                lock (_connectedLock)
                {
                    if (_connected == value) return;
                    _connected = value;
                }
            }
        }

        private bool _connected;


        private const int sendDelayMs = 500;
        private const int readIntervalMs = 2;
        private const int readIdleTimeoutMinutes = 5;
        private const int idleTimeoutMinutes = 5;
        private const int idleTimeoutMs = idleTimeoutMinutes*60*1000;
        private const int sendTimeoutSeconds = 15;
        private const int retryConnectSeconds = 10;
        private const int retryConnectAttempts = 100;
        private int retryConnectAttempt = 0;

        private bool closeConnection;
        private bool retryConnection;

        private readonly IRC irc;
        private readonly int id;

        private readonly Queue<string> messageQueue = new Queue<string>(); 
        private readonly Queue<string> messageQueueHighPriority = new Queue<string>();

        private readonly ManualResetEvent messageHighPriorityEvent = new ManualResetEvent(false);

        private Network currentNetwork;
        private ushort currentServerIndex;
        private ushort currentPortIndex;

        private Socket socket;
        private readonly Thread timeoutThread;

        private ulong nextMessageId;
        private ulong lastConnectMessageId;

        private readonly object _connectedLock = new object();
        private readonly object _networkLock = new object();
        private readonly object _messageQueueLock = new object();
        private readonly object _socketLock = new object();

        public DateTime LastMessageReceivedDate { get; private set; }
        public DateTime LastMessageDate { get; private set; }


        internal Connection(IRC irc, int id)
        {
            irc.SendMessage(new IRCConnectionCreatedEvent(new IRCEventInfo(DateTime.Now, id)));

            this.irc = irc;
            this.id = id;

            LastMessageDate = DateTime.Now;
            LastMessageReceivedDate = DateTime.Now;
            
            var readThread = new Thread(DoRead);
            var sendThread = new Thread(DoSend);
            timeoutThread = new Thread(DoCheckTimeout);

            readThread.Start();
            sendThread.Start();
            timeoutThread.Start();
        }

        private void DoCheckTimeout()
        {
            if ((DateTime.Now - LastMessageDate).Milliseconds > idleTimeoutMs)
            {
                Disconnect("Connection idle for " + idleTimeoutMinutes +
                           " minutes, assuming connection lost", true);
            }
            Thread.Sleep(1000);
        }

        internal void SetNetwork(Network network)
        {
            lock (_networkLock)
            {
                if (network == null)
                    throw new ArgumentNullException("network", Strings.NoNetwork);

                if (network.ServerCount == 0)
                    throw new ArgumentException(Strings.ConnectNoServers);

                currentNetwork = network;
                currentServerIndex = 0;
                currentPortIndex = 0;
            }
        }

        /// <summary> Initiates a connection to an IRC server, using a provided network
        /// or reconnecting to a previous network. </summary>
        /// <param name="network"> Optional network to connect to.
        /// Without this, it will connect to the previously-connected network. </param>
        internal void Connect(Network network = null)
        {
            lock (_socketLock)
            {
                if (Connected)
                    throw new InvalidOperationException(Strings.ConnectAlreadyConnected);

                lock (_networkLock)
                {
                    if (network != null)
                        SetNetwork(network);
                    else if (currentNetwork == null)
                        // No network provided, no network already set, nothing to connect to.
                        throw new InvalidOperationException(Strings.ConnectNoNetwork);

                    // currentNetwork can be modified afterwards with no effect on the connection.
                    CurrentNetworkName = currentNetwork.Name;
                    CurrentAddress = currentNetwork.GetServers()[currentServerIndex].Address;
                    CurrentPort = currentNetwork.GetServers()[currentServerIndex].GetPorts()[currentPortIndex];
                    CurrentServerPass = currentNetwork.GetServers()[currentServerIndex].Password;
                }


                irc.SendMessage(new IRCConnectingEvent(
                    new IRCEventInfo(id, CurrentNetworkName, CurrentAddress)));

                int tries = 5;
                do
                {

                    // Create the socket.
                    socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)
                    {
                        // Will assume disconnection if this many minutes pass without receiving any data.
                        //ReceiveTimeout = readIdleTimeoutMinutes*60*1000,
                        // Will assume disconnection if this many seonds pass without successful Send.
                        SendTimeout = sendTimeoutSeconds*1000,
                    };

                    try
                    {
                        // NOTE: DnsEndPoint does not work in Mono 2.10
                        socket.Connect(CurrentAddress, CurrentPort);
                    }
                    catch (SocketException ex)
                    {
                        // Send a message notifying that connect failed.
                        irc.SendMessage(new IRCConnectFailedEvent(
                            ex.Message, new IRCEventInfo(id, CurrentNetworkName, CurrentAddress)));
                        Thread.Sleep(500);
                    }
                } while (!socket.Connected && tries-- > 0);

                if (!socket.Connected) return;

                Connected = true;
                lastConnectMessageId = nextMessageId;
                retryConnectAttempt = 0;

                // Send a message notifying that we successfully connected.
                irc.SendMessage(new IRCConnectedEvent(
                    new IRCEventInfo(id, CurrentNetworkName, CurrentAddress)));

                // Send pass/nick/user info.
                Login();
            }
        }

        /// <summary> Initiates a connection to an IRC server
        /// using the specified address and port. </summary>
        /// <param name="address"> Server address to connect to. </param>
        /// <param name="port"> Port to connect to. </param>
        internal void Connect(string address, ushort port)
        {
            var network = new Network(new[] { new Server(address, port) });
            Connect(network);
        }

        /// <summary> Initiates a connection to an IRC server
        /// using the specified address and ports. </summary>
        /// <param name="address"> Server address to connect to. </param>
        /// <param name="ports"> Ports to connect to. </param>
        internal void Connect(string address, ICollection<ushort> ports)
        {
            var network = new Network(new[] { new Server(address, ports) });
            Connect(network);
        }

        /// <summary> Sends PASS/NICK/USER messages to the server. </summary>
        internal void Login()
        {
            string message;
            if (!String.IsNullOrWhiteSpace(CurrentServerPass))
            {
                message = String.Format("PASS {0}", CurrentServerPass);
                QueueSend(message, true);
            }

            message = String.Format("NICK {0}", currentNetwork.DefaultNick ?? irc.DefaultNick);
            QueueSend(message, true);

            message = String.Format("USER {0} \"{1}\" \"{2}\" :{3}",
                                    irc.DefaultUserName, socket.LocalEndPoint,
                                    CurrentAddress, irc.DefaultRealName);
            QueueSend(message, true);
        }

        /// <summary> Forcefully disconnects the connection and sends a notification
        /// of the disconnect with a specified message. </summary>
        /// <param name="message"> Message to send to components about the disconnect. </param>
        /// <param name="retry"> Whether to reconnect after the disconnect completes. </param>
        internal void Disconnect(string message, bool retry = false)
        {
            lock (_socketLock)
            {
                // Cancel any connections currently waiting to reconnect.
                retryConnection = retry;
                
                if (!Connected)
                    return;

                if (socket.Connected)
                {
                    QueueSend("QUIT :" + message, true);
                    try
                    {
                        socket.Disconnect(false);
                    }
                    catch (SocketException ex)
                    {
                        // Doesn't matter, we're trying to close it anyway.
                        Console.WriteLine("{0}Exception while closing socket:{0}{1}{0}",
                            Environment.NewLine, ex);
                    }

                }

                Connected = false;

                irc.SendMessage(new IRCDisconnectedEvent(
                                    message, new IRCEventInfo(id, CurrentNetworkName, CurrentAddress)));
            }
        }

        /// <summary> Signals for worker threads to stop working </summary>
        internal void CloseConnection(string message)
        {
            lock (_connectedLock)
            {
                if (Connected)
                    Disconnect(message);
            }
            closeConnection = true;
            irc.SendMessage(new IRCConnectionClosedEvent(new IRCEventInfo(DateTime.Now, id)));
        }

        /// <summary> Add data to the send queue with the specified priority. </summary>
        /// <param name="data"> String message data to be added to the queue. </param>
        /// <param name="highPriority"> Whether to use high priority for this message. </param>
        internal void QueueSend(string data, bool highPriority = false)
        {
            if (String.IsNullOrWhiteSpace(data))
                throw new InvalidOperationException(Strings.SendNoData);

            // Postpone disconnection until we've queued the message.
            lock (_connectedLock)
            {
                // If we try to send after disconnecting,
                // cancel and notify about the failed message.
                if (!Connected)
                    irc.SendMessage(new IRCInfoMessage(Strings.DataSendCancelledMessage, data, new IRCEventInfo(id)));

                lock (_messageQueueLock)
                {
                    (highPriority ? messageQueueHighPriority : messageQueue).Enqueue(data);
                    if (highPriority)
                        messageHighPriorityEvent.Set();
                }
            }
        }

        /// <summary> Send the next message in the given queue. </summary>
        /// <param name="queue">Queue containing messages to be sent.</param>
        /// <returns>true if the send was successful.</returns>
        private bool SendDataFromQueue(Queue<string> queue)
        {
            // Skip if there's nothing to send.
            if (queue.Count == 0) return false;

            // TODO: More robust codeset functionality.
            string data = queue.Dequeue();
            byte[] dataBytes = Encoding.UTF8.GetBytes(data + IRC.NewLine);

            int bytesSent;
            try
            {
                bytesSent = socket.Send(dataBytes);
                LastMessageDate = DateTime.Now;
            }
            catch (SocketException ex)
            {
                // Notify components that the message send failed.
                var messageData = new Dictionary<string, object>
                                      {
                                          {"data", data},
                                          {"reason", ex.Message},
                                          {"exception", ex}
                                      };

                irc.SendMessage(new IRCInfoMessage(Strings.DataSendFailedMessage, messageData,
                    new IRCEventInfo(id, CurrentNetworkName, CurrentAddress)));
                return false;
            }

#if DEBUG
            // TODO: remove this later.
            if (bytesSent != dataBytes.Length)
                throw new Exception("Sent data doesn't match buffer?");
#endif
            // Notify components that the message has been sent
            irc.SendMessage(new IRCInfoMessage(Strings.DataSentMessage, data,
                    new IRCEventInfo(id, CurrentNetworkName, CurrentAddress)));

            return true;
        }

        private void DoSend()
        {
            while (!closeConnection)
            {
                // Don't peg the CPU with constant checking of send queue
                // Ensure we sleep on every run even when something breaks/continues.
                //messageHighPriorityEvent.WaitOne(TimeSpan.FromMilliseconds(sendDelayMs));
                messageHighPriorityEvent.WaitOne(TimeSpan.FromMilliseconds(sendDelayMs));
                //Thread.Sleep(TimeSpan.FromMilliseconds(sendDelayMs));

                // If we've lost connection for whatever reason, clear the message queues.
                if (!Connected)
                {
                    CancelMessages();
                    continue;
                }

                // Wait some time between each send to prevent flooding.
                // Stop waiting if a high-priority message has been queued.
                lock (_messageQueueLock)
                {
                    // Send high-priority messages first.
                    if (!SendDataFromQueue(messageQueueHighPriority))
                        SendDataFromQueue(messageQueue);
                    if (messageQueueHighPriority.Count < 1)
                        messageHighPriorityEvent.Reset();
                }

            }
        }

        private void DoRead()
        {

            const int bufferLength = 1024;
            string remainder = String.Empty;
            var buffer = new byte[bufferLength];

            while (!closeConnection)
            {
                //if (nextMessageId > 10)
                //    throw new Exception("ex");

                // Don't peg the CPU with constant reading.
                // Ensure we sleep on every run even when something breaks/continues.
                Thread.Sleep(TimeSpan.FromMilliseconds(readIntervalMs));

                if (!Connected)
                {
                    remainder = String.Empty;
                    buffer.Initialize();
                    continue;
                }

                int readBytes;
                try
                {
                    readBytes = socket.Receive(buffer);
                }
                catch (SocketException ex)
                {
                    Disconnect(ex.Message, true);
                    continue;
                }

                if (readBytes == 0)
                    Disconnect(Strings.ServerClosedConnection);

                // TODO: support other encodings
                string receivedString = remainder + Encoding.UTF8.GetString(buffer, 0, readBytes);

                int endIndex;
                while ((endIndex = receivedString.IndexOf(IRC.NewLine, StringComparison.InvariantCulture)) >= 0)
                {
                    string message = receivedString.Substring(0, endIndex);
                    int nextStartIndex = endIndex + 2;
                    receivedString = (receivedString.Length > nextStartIndex)
                                         ? receivedString.Substring(nextStartIndex,
                                                                    receivedString.Length - nextStartIndex)
                                         : String.Empty;

                    irc.SendMessage(new IRCDataReceivedEvent(
                                        nextMessageId, message,
                                        new IRCEventInfo(id, CurrentNetworkName, CurrentAddress)));

                    // Pong
                    if (message.Substring(0, 6) == "PING :")
                        QueueSend("PONG :" + message.Substring(6, message.Length - 6));


                    // Handle on-connect nick collisions
                    // This should only happen very early in a connection's lifetime, for obvious reasons
                    if (nextMessageId < lastConnectMessageId + 20)
                    {
                        string subMessage = message;
                        if (message[0] == ':') // Prefixed with server
                            subMessage = message.Substring(message.IndexOf(' ') + 1);

                        const string connectBadNick = "433 * ";

                        if (subMessage.Substring(0, connectBadNick.Length) == connectBadNick)
                        {
                            string defaultNick = currentNetwork.DefaultNick ?? irc.DefaultNick;
                            string altNick = currentNetwork.AltNick ?? irc.DefaultAltNick;
                            if (subMessage.Substring(connectBadNick.Length, defaultNick.Length) == defaultNick)
                                QueueSend("NICK " + altNick, true);
                            else if (subMessage.Substring(connectBadNick.Length, altNick.Length) == altNick)
                                QueueSend("NICK " + defaultNick + nextMessageId);
                        }
                    }


                    nextMessageId++;
                }
                remainder = receivedString;

            }
        }

        /// <summary> Cancels all pending outgoing messages. </summary>
        private void CancelMessages()
        {
            lock (_messageQueueLock)
            {
                CancelMessages(messageQueueHighPriority);
                CancelMessages(messageQueue);
                messageHighPriorityEvent.Reset();
            }
        }

        /// <summary> Clears the specified message queue and sends a notification of its cancellation. </summary>
        /// <param name="messages"> Message queue to cancel. </param>
        private void CancelMessages(Queue<string> messages)
        {
            if (messages == null)
                throw new ArgumentNullException("messages");

            // Dequeue each message and send a notification of its cancellation.
            while (messages.Count > 0)
            {
                string nextMessage = messages.Dequeue();
                irc.SendMessage(new IRCEvent(Strings.DataSendCancelledMessage, nextMessage, new IRCEventInfo(id)));
            }
        }

    }
}