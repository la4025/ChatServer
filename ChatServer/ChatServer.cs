using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

class ChatServer
{
    static List<TcpClient> clients = new List<TcpClient>();
    static object lockObj = new object();

    static async Task Main()
    {
        TcpListener listener = new TcpListener(IPAddress.Any, 5000);
        listener.Start();
        Console.WriteLine("[서버 시작] 클라이언트 접속 대기 중...");

        while (true)
        {
            TcpClient client = await listener.AcceptTcpClientAsync();
            lock (lockObj)
            {
                clients.Add(client);
            }
            _ = HandleClientAsync(client);            
        }
    }

    static async Task HandleClientAsync(TcpClient client)
    {
        Console.WriteLine("[클라이언트 접속]");
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

                    BroadCast(message, client);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[에러] {ex.Message}");
            }
            finally
            {
                lock (lockObj)
                {
                    clients.Remove(client);
                }
                Console.WriteLine("[클라이언트 연결 종료]");
            }
        }
    }

    static void BroadCast(string message, TcpClient sender)
    {
        byte[] data = Encoding.UTF8.GetBytes(message);

        lock (lockObj)
        {
            foreach (var client in clients)
            {
                if (client != sender)
                {
                    try
                    {
                        NetworkStream stream = client.GetStream();
                        stream.WriteAsync(data, 0, data.Length);
                    }
                    catch
                    {
                        // 오류 무시 (끊긴 클라이언트 등)
                    }
                }
            }
        }
    }
}
