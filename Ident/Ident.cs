using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Nexus;
using Nexus.Messages;
using System.Net;

namespace Ident
{
    public class Ident : NexusComponent
    {
        private const int identPort = 113;
        private static string userName = "Nexus";
        private Thread socketThread;
        private bool running = true;
        private static bool connectAccepted;

        public Ident()
        {
            RegisterListener<IRCConnectingEvent>(StartListening);
        }

        public override void Startup()
        {
            if (socketThread == null)
            {
                socketThread = new Thread(CreateListener);
                socketThread.Start();
            }
        }

        public override void Shutdown()
        {
            running = false;
        }

        private void StartListening(IRCConnectingEvent ev)
        {
        }

        private void CreateListener()
        {
            var localEP = new IPEndPoint(IPAddress.Any, 113);
            SendMessage(new InfoMessage("Ident.Listening"));

            var listener = new Socket(localEP.Address.AddressFamily,
                SocketType.Stream, ProtocolType.Tcp);

            try
            {
                listener.Bind(localEP);
                listener.Listen(10);

                while (running)
                {
                    connectAccepted = false;

                    listener.BeginAccept(AcceptCallback, listener);

                    while (running && !connectAccepted)
                        Thread.Sleep(100);
                }

                listener.Close(2);
            }
            catch(Exception ex)
            {
                SendMessage(new InfoMessage("Socket thread exception occurred!", ex));
            }
        }

        private static void AcceptCallback(IAsyncResult ar)
        {
            var listener = (Socket) ar.AsyncState;
            Socket handler;
            try
            {
                handler = listener.EndAccept(ar);
            }
            catch (ObjectDisposedException)
            {
                // We're shutting down; just exit here.
                return;
            }
            connectAccepted = true;

            var state = new StateObject();
            state.WorkSocket = handler;
            handler.BeginReceive(state.Buffer, 0, handler.Available, SocketFlags.None, readCallback, state);
        }

        private static void readCallback (IAsyncResult ar)
        {
            var state = (StateObject) ar.AsyncState;
            Socket handler = state.WorkSocket;
            int read = handler.EndReceive(ar);
            if (read > 0)
            {
                string bufString = Encoding.ASCII.GetString(state.Buffer, 0, read).Trim();
                string returnMessage = bufString + " : USERID : UNIX : " + userName + Environment.NewLine;
                byte[] sendBack = Encoding.ASCII.GetBytes(returnMessage);
                handler.Send(sendBack);
            }
            handler.Close();
        }

    }

    internal class StateObject
    {
        public Socket WorkSocket = null;
        public const int BufferSize = 1024;
        public byte[] Buffer = new byte[BufferSize];
        public StringBuilder Sb = new StringBuilder();
    }

}
