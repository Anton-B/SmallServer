using System;
using System.Net;
using System.Net.Sockets;

namespace SmallServer
{
    class Server : IDisposable
    {
        private TcpListener listener;

        public Server(int port)
        {
            listener = new TcpListener(IPAddress.Any, port);
            Start();
        }

        private void Start()
        {            
            listener.Start();
            while (true)
                Client.Request(listener.AcceptTcpClientAsync().Result);
        }

        public void Dispose()
        {
            listener.Stop();
        }

        static void Main(string[] args)
        {
            new Server(81);
        }
    }
}
