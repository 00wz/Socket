using Microsoft.Extensions.Hosting;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;

class ServerClass : IHostedService
{
    const string SERVER_IP = "10.129.0.14";
    const int SERVER_PORT = 55555;

    //Lists For Clients and Their Nicknames
    private ConcurrentDictionary<Socket, string> Clients;
    private CancellationTokenSource exitApplicationTokenSource;
    Socket listenSocket;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        IPAddress source = IPAddress.Parse(SERVER_IP);
        IPEndPoint ipPoint = new IPEndPoint(source, SERVER_PORT);
        exitApplicationTokenSource = new CancellationTokenSource();
        Clients = new ConcurrentDictionary<Socket, string>();

        //Starting Server
        listenSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        listenSocket.Bind(ipPoint);
        listenSocket.Listen();

        Accept(listenSocket, exitApplicationTokenSource.Token);

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        //Server stop
        exitApplicationTokenSource.Cancel();
        exitApplicationTokenSource.Dispose();
        listenSocket.Close();
        Clients.Clear();
        return Task.CompletedTask;
    }


    //Receiving / Listening Function
    async void Accept(Socket socket, CancellationToken cancellationToken)
    {
        Socket client = null;

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                //Accept Connection
                client = await socket.AcceptAsync(cancellationToken);
                //Console.WriteLine($"Connected with {client.RemoteEndPoint?.ToString()}");

                try
                {
                    //Request And Store Nickname
                    byte[] responseBytes = new byte[256];
                    await client.SendAsync(Encoding.ASCII.GetBytes("NICK"), cancellationToken);
                    int byteCount = await client.ReceiveAsync(responseBytes, SocketFlags.None, cancellationToken);
                    string nickname = Encoding.ASCII.GetString(responseBytes, 0, byteCount);
                    Clients[client] = nickname;

                    //Print And Broadcast Nickname
                    //Console.WriteLine($"{client.RemoteEndPoint?.ToString()}" +
                       // $" successfully logged in as {nickname}");
                    Broadcast($"{nickname} joined!", cancellationToken);
                    client.SendAsync(Encoding.ASCII.GetBytes("Connected to server!"), cancellationToken);
                }
                catch (System.Net.Sockets.SocketException)
                {
                    Clients.Remove(client, out _);
                    //Console.WriteLine($"Connection failure {client.RemoteEndPoint?.ToString()}");
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
    async void ReceiveClient(Socket socket, CancellationToken cancellationToken)
    {
        string message;

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                byte[] responseBytes = new byte[256];
                int byteCount = await socket.ReceiveAsync(responseBytes, SocketFlags.None, cancellationToken);
                message = Encoding.ASCII.GetString(responseBytes, 0, byteCount);
                Broadcast(message, cancellationToken);
            }
        }
        catch (System.Net.Sockets.SocketException)
        {
            string nickname = Clients[socket];
            //Console.WriteLine($"{socket.RemoteEndPoint?.ToString()} {nickname} disconnected");
            Clients.Remove(socket, out _);
            Broadcast($"{nickname} disconnected", cancellationToken);
        }
        catch (System.OperationCanceledException)
        {
            return;
        }

    }

    //Sending Messages To All Connected Clients
    void Broadcast(string message, CancellationToken cancellationToken)
    {
        byte[] messageBytes = Encoding.ASCII.GetBytes(message);
        foreach (var client in Clients.Keys)
        {
            try
            {
                client.SendAsync(messageBytes, cancellationToken);
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

