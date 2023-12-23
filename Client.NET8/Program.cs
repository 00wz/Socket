using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Client
{
    class Program
    {
        //Connection Data
        const string SERVER_IP = "192.168.0.168";
        const int SERVER_PORT = 55555;
        static string Nickname;

        static void Main(string[] args)
        {
            IPAddress destination = IPAddress.Parse(SERVER_IP);
            CancellationTokenSource exitApplicationTokenSource = new CancellationTokenSource();

            //Choosing Nickname
            Console.Write("Choose your nickname: ");
            Nickname = Console.ReadLine();

            //Connecting To Server
            Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socket.Connect(destination, SERVER_PORT);

            //Starting Threads For Listening
            Receive(socket, exitApplicationTokenSource.Token);

            //Writing
            byte[]? messageBytes;
            string message = Console.ReadLine();
            while (message != ":q") //enter ":q" to exit 
            {
                messageBytes = Encoding.ASCII.GetBytes($"{Nickname}: {message}"); 
                socket.SendAsync(messageBytes);
                message = Console.ReadLine();
            }

            exitApplicationTokenSource.Cancel();
            exitApplicationTokenSource.Dispose();
            socket.Close(0);
        }

        //Listening to Server and Sending Nickname
        static async void Receive(Socket socket, CancellationToken cancellationToken)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {   byte[] responseBytes = new byte[256];
                    int byteCount=await socket.ReceiveAsync(responseBytes, SocketFlags.None, cancellationToken);
                    string message = Encoding.ASCII.GetString(responseBytes, 0, byteCount);

                    if(string.Equals(message,"NICK"))
                    {
                        var nicknameBytes = Encoding.ASCII.GetBytes(Nickname);
                        socket.SendAsync(nicknameBytes);
                    }
                    else
                    {
                        Console.WriteLine(message);
                    }
                }
            }
            catch (System.OperationCanceledException)
            {
                return;
            }
        }
    }
}
