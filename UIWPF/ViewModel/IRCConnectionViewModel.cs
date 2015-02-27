using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UIWPF.Helpers;

namespace UIWPF.ViewModel
{
    public class IRCConnectionViewModel : ViewModelBaseExtended
    {
        public string ServerAddress
        {
            get { return _serverAddress; }
            set { SetProperty(() => ServerAddress, ref _serverAddress, value); }
        }
        private string _serverAddress;

        public int ConnectionId
        {
            get { return _connectionId; }
            set { SetProperty(() => ConnectionId, ref _connectionId, value); }
        }
        private int _connectionId;

        public string ConnectionName
        {
            get { return _connectionName; }
            set { SetProperty(() => ConnectionName, ref _connectionName, value); }
        }
        private string _connectionName;


        public IRCConnectionViewModel(int connectionId, string serverAddress)
        {
            ConnectionId = connectionId;
            ServerAddress = serverAddress;
            ConnectionName = "[" + ConnectionId + "]: " + ServerAddress;
        }
    }
}
