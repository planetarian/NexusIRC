using System;
using System.Collections.Generic;

namespace IRC
{
    internal struct Server
    {
        /// <summary> The name of the server. </summary>
        internal string Name { get; private set; }

        /// <summary> The server IP address or hostname. </summary>
        internal string Address { get; private set; }

        /// <summary> Password to connect to the server. </summary>
        internal string Password { get; private set; }

        private readonly ushort[] _ports;

        /// <summary> The ports the server runs on. </summary>
        internal ushort[] GetPorts()
        {
            return (ushort[])_ports.Clone();
        }

        /// <summary> Initializes a new instance of the pIRC.Server class
        /// with a specific server <paramref name="address"/>
        /// and <paramref name="port"/>. </summary>
        /// <param name="name"> The name of the server. </param>
        /// <param name="address"> The server IP address or hostname to connect to. </param>
        /// <param name="port"> The port the server runs on. </param>
        /// <param name="password"> Password to connect to the server. </param>
        internal Server(string address, ushort port, string name = null, string password = null)
            : this(address, new []{ port }, name, password)
        {
        }

        /// <summary> Initializes a new instance of the pIRC.Server class
        /// with a specific server <paramref name="address"/>
        /// and set of <paramref name="ports"/>. </summary>
        /// <param name="name"> The name of the server. </param>
        /// <param name="address"> The server IP address or hostname to connect to. </param>
        /// <param name="ports"> The ports the server runs on. </param>
        /// <param name="password"> Password to connect to the server. </param>
        internal Server(string address, ICollection<ushort> ports = null, string name = null, string password = null)
            : this()
        {
            if (String.IsNullOrWhiteSpace(address))
                throw new InvalidOperationException(Strings.ServerNoAddress);
            
            Name = name ?? address;
            Address = address;
            Password = password;

            // Clone the provided ports list so it can't be modified from outside
            // TODO: is this necessary?
            if (ports != null && ports.Count != 0)
            {
                _ports = new ushort[ports.Count];
                ports.CopyTo(_ports, 0);
            }
            else _ports = new[] {IRC.DefaultPort};
        }
    }
}
