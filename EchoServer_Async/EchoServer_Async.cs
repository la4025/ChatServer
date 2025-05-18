using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

class EchoServer_Async
{
    static async Task Main(string[] args)
    {
        int port = 5000;
        TcpListener server = new TcpListener(IPAddress.Any, port);
        server.Start();
        Console.WriteLine($"[서버시작] 포트 {port} 에서 대기중...");
        while (true)
        {
            TcpClient client = await server.AcceptTcpClientAsync();
            _ = HandleClientAsync(client);
        }
    }

    static async Task HandleClientAsync(TcpClient client)
    {
        Console.WriteLine($"[클라이언트 접속] {client.Client.RemoteEndPoint}");
        using (client)
        using (NetworkStream stream = client.GetStream())
        {
            byte[] buffer = new byte[1024];
            int bytesRead;
            
            try
            {
                while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) != 0)
                {
                    string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    Console.WriteLine($"[수신] {message}");
                    
                    byte[] response = Encoding.UTF8.GetBytes(message);
                    await stream.WriteAsync(response, 0, response.Length);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[에러] : {ex.Message}");
            }
        }

        Console.WriteLine($"[클라이언트 종료] {client.Client.RemoteEndPoint}");
    }
}
