using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Client
{
    class Program
    {
        static void Main(string[] args)
        {
            IPAddress destination = IPAddress.Parse("192.168.0.168");
            int port = 55555;
            Socket socket = null;
            CancellationTokenSource exitApplicationTokenSource = null;

            try
            {
                socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                socket.Connect(destination, port);
                exitApplicationTokenSource = new CancellationTokenSource();

                Receive(socket, exitApplicationTokenSource.Token);

                Console.Write("Choose your nickname: ");
                string message = Console.ReadLine();

                //write 
                byte[]? messageBytes;
                while (message != ":q") //enter ":q" to exit 
                {
                    messageBytes = Encoding.UTF8.GetBytes(message);
                    socket.SendAsync(messageBytes);
                    message = Console.ReadLine();
                }
            }
            finally
            {
                socket?.Close();
                exitApplicationTokenSource?.Cancel();
                exitApplicationTokenSource?.Dispose();
            }
        }

        //receive
        static async void Receive(Socket socket, CancellationToken cancellationToken)
        {
            byte[] responseBytes = new byte[256];
            char[] responseChars = new char[256];

            while (!cancellationToken.IsCancellationRequested)
            {
                int bytesReceived = await socket.ReceiveAsync(responseBytes, SocketFlags.None, cancellationToken);

                int charCount = Encoding.ASCII.GetChars(responseBytes, 0, bytesReceived, responseChars, 0);

                Console.Out.WriteLine(responseChars, 0, charCount);
            }
        }
    }
}
