using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Collections.Concurrent;

namespace Server
{
    class Program
    {
        //Connection Data
        const string SERVER_IP = "192.168.0.168";
        const int SERVER_PORT = 55555;

        //Lists For Clients and Their Nicknames
        static ConcurrentDictionary<Socket, string> Clients = new ConcurrentDictionary<Socket, string>();

        static void Main(string[] args)
        {
            IPAddress source = IPAddress.Parse(SERVER_IP);
            IPEndPoint ipPoint = new IPEndPoint(source, SERVER_PORT);
            CancellationTokenSource  exitApplicationTokenSource = new CancellationTokenSource();

            //Starting Server
            Socket listenSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            listenSocket.Bind(ipPoint);
            listenSocket.Listen();

            Console.WriteLine("Server if listening...");
            Accept(listenSocket, exitApplicationTokenSource.Token);

            Console.WriteLine("Press anything to exit");
            Console.ReadKey();

            //Server stop
            exitApplicationTokenSource.Cancel();
            exitApplicationTokenSource.Dispose();
            listenSocket.Close();
        }

        //Receiving / Listening Function
        static async void Accept(Socket socket, CancellationToken cancellationToken)
        {
            Socket client=null;

            while(!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    //Accept Connection
                    client = await socket.AcceptAsync(cancellationToken);
                    Console.WriteLine($"Connected with {client.RemoteEndPoint?.ToString()}");

                    try
                    {   
                        //Request And Store Nickname
                        byte[] responseBytes = new byte[256];
                        await client.SendAsync(Encoding.ASCII.GetBytes("NICK"),cancellationToken);
                        int byteCount=await client.ReceiveAsync(responseBytes, SocketFlags.None, cancellationToken);
                        string nickname = Encoding.ASCII.GetString(responseBytes, 0, byteCount);
                        Clients[client] = nickname;

                        //Print And Broadcast Nickname
                        Console.WriteLine($"{client.RemoteEndPoint?.ToString()}" +
                            $" successfully logged in as {nickname}");
                        Broadcast($"{nickname} joined!", cancellationToken);
                        client.SendAsync(Encoding.ASCII.GetBytes("Connected to server!"), cancellationToken);
                    }
                    catch (System.Net.Sockets.SocketException)
                    {
                        Clients.Remove(client,out _);
                        Console.WriteLine($"Connection failure {client.RemoteEndPoint?.ToString()}");
                        continue;
                    }
                }
                catch (System.OperationCanceledException)
                {
                    return;
                }
                
                //Start Handling Thread For Client
                ReceiveClient(client, cancellationToken);
            }
        }

        //Handling Messages From Clients
        static async void ReceiveClient(Socket socket, CancellationToken cancellationToken)
        {
            string message;

            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    byte[] responseBytes = new byte[256];
                    int byteCount = await socket.ReceiveAsync(responseBytes, SocketFlags.None, cancellationToken);
                    message = Encoding.ASCII.GetString(responseBytes, 0, byteCount);
                    Broadcast($"{Clients[socket]}: {message}", cancellationToken);
                }
            }
            catch (System.Net.Sockets.SocketException) 
            {
                string nickname = Clients[socket];
                Console.WriteLine($"{socket.RemoteEndPoint?.ToString()} {nickname} disconnected");
                Clients.Remove(socket,out _);
                Broadcast($"{nickname} disconnected", cancellationToken);
            }
            catch (System.OperationCanceledException)
            {
                return;
            }
            
        }

        //Sending Messages To All Connected Clients
        static void Broadcast(string message, CancellationToken cancellationToken)
        {
            byte[] messageBytes = Encoding.ASCII.GetBytes(message);
            foreach (var client in Clients.Keys)
            {
                try
                {
                    client.SendAsync(messageBytes,cancellationToken);
                }
                catch (System.Net.Sockets.SocketException)
                {
                    continue;
                }
                catch (System.OperationCanceledException)
                {
                    return;
                }
            }
        }
    }
}
