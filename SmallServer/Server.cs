using System.Net;
using System.Net.Sockets;

namespace SmallServer
{
    class Server
    {
        private TcpListener Listener;

        public Server(int port)
        {
            Listener = new TcpListener(IPAddress.Any, port);
            Listener.Start();
            while (true)
            {
                new Client(Listener.AcceptTcpClientAsync().Result);
            }
        }

        static void Main(string[] args)
        {
            new Server(81);
        }
    }
}
