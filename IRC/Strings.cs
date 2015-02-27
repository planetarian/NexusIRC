namespace IRC
{
    internal static class Strings
    {
        
        //
        // General
        //

        public const string NetworkUnnamed = "Unnamed Network";
        public const string ServerClosedConnection = "Server closed connection.";


        //
        // Errors
        //

        public const string ConnectNoNetwork = "Connect: No network/server provided.";
        public const string ConnectNoServers = "Connect: No servers provided.";
        public const string ConnectAlreadyConnected = "Connect: Already connected.";

        public const string DisconnectNotConnected = "Disconnect: Not connected.";
        public const string NetworkNoServers = "Network: No servers provided.";
        public const string ServerNoAddress = "Server: No address provided.";

        public const string SendNotConnected = "QueueSend: Not connected.";
        public const string SendNoData = "QueueSend: No data provided.";

        public const string ReadIdleTimeout = "Went too long without receiving data.";
        public const string NoNetwork = "No network provided.";

        public const string DisconnectDefault = "Component called Disconnect.";
        public const string CloseConnectionDefault = "Component called CloseConnection.";
        public const string ShutdownDefault = "Shutting down NexusIRC.";

        public const string InvalidConnection = "Invalid Connection";
        public const string InvalidAddress = "Invalid Address";
        public const string InvalidPort = "Invalid Port";

        //
        // IRC Messages
        //

        private const string connectionMessagePrefix = "IRC.";

        public const string ConnectionCreatedMessage = connectionMessagePrefix + "ConnectionCreated";
        public const string ConnectionClosedMessage = connectionMessagePrefix + "ConnectionClosed";

        public const string ConnectFailedMessage = connectionMessagePrefix + "ConnectFailed";
        public const string ConnectedMessage = connectionMessagePrefix + "Connected";
        public const string DisconnectedMessage = connectionMessagePrefix + "Disconnected";
        
        public const string DataReceivedMessage = connectionMessagePrefix + "DataReceived";

        public const string DataSentMessage = connectionMessagePrefix + "DataSent";
        public const string DataSendFailedMessage = connectionMessagePrefix + "DataSendFailed";
        public const string DataSendCancelledMessage = connectionMessagePrefix + "DataSendCancelled";
    }
}
