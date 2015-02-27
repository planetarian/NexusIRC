using System;
using System.Collections.Generic;
using System.Linq;

namespace IRC
{
    internal class Network
    {
        /// <summary> Friendly name for the network. </summary>
        internal string Name { get; set; }

        /// <summary> Default nickname to use when connecting to this network. </summary>
        internal string DefaultNick { get; set; }

        /// <summary> Alternate nickname to use when connecting to this network. </summary>
        internal string AltNick { get; set; }

        /// <summary> Returns a list of the servers on this network. </summary>
        /// <returns> List of Server objects registered for this network. </returns>
        internal List<Server> GetServers()
        {
            lock (_serversLock)
            {
                return _servers.ToList();
            }
        }

        /// <summary> Returns the number of servers on this network. </summary>
        internal int ServerCount
        {
            get
            {
                lock (_serversLock)
                {
                    return _servers.Count;
                }
            }
        }

        private readonly List<Server> _servers = new List<Server>();
        private readonly object _serversLock = new object();

        internal Network(ICollection<Server> servers, string name = null,
                       string defaultNick = null)
        {
            if (servers == null)
                throw new ArgumentNullException("servers");
            if (servers.Count == 0)
                throw new ArgumentException(
                    Strings.NetworkNoServers, "servers");


            Name = name ?? Strings.NetworkUnnamed;
            DefaultNick = defaultNick;

            // Copy the contents of servers
            lock (_serversLock)
            {
                _servers = servers.ToList();
            }
        }

        internal Network(string address, ushort port,
                       string name = null, string defaultNick = null)
            : this(new[] {new Server(address, port)}, name, defaultNick)
        {
        }

        internal Network(string address, ICollection<ushort> ports,
                       string name = null, string defaultNick = null)
            : this(new[] {new Server(address, ports)}, name, defaultNick)
        {
        }
        
    }
}
