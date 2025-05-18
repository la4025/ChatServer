using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

class ChatServerV2
{
    static TcpListener listener;
    static List<TcpClient> clients = new List<TcpClient>();
    static Dictionary<TcpClient, string> clientNames = new Dictionary<TcpClient, string>();
    static object lockObj = new object();

    static async Task Main()
    {
        listener = new TcpListener(IPAddress.Any, 5000);
        listener.Start();
        Console.WriteLine("[서버 시작 됨]");

        while (true)
        {
            TcpClient client = await listener.AcceptTcpClientAsync();

            lock(lockObj)
            {
                clients.Add(client);
            }

            _ = HandleClientAsync(client);
        }
    }

    static async Task HandleClientAsync(TcpClient client)
    {
        NetworkStream stream = client.GetStream();
        byte[] buffer = new byte[1024];

        // 닉네임 받기
        int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
        string nickname = Encoding.UTF8.GetString(buffer, 0, bytesRead).Trim();

        lock(lockObj)
        {
            clientNames[client] = nickname;
        }

        Console.WriteLine($"[접속] {nickname}");
        await BroadcastAsync($"🟢 [{nickname}]님이 입장했습니다.", client);

        try
        {
            while (true)
            {
                bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);

                if (bytesRead == 0)
                {
                    break;
                }

                string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                Console.WriteLine($"[수신] {nickname} : {message}");

                await BroadcastAsync($"[{nickname}] {message}", client);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[예외 발생] {ex.Message}");
        }
        finally
        {
            lock (lockObj)
            {
                clients.Remove(client);
                clientNames.Remove(client);
            }

            Console.WriteLine($"[퇴장] {nickname}");
            await BroadcastAsync($"🔴 [{nickname}]님이 퇴장했습니다.", client);

            client.Close();
        }
    }

    static async Task BroadcastAsync(string message, TcpClient sender)
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
                        // 끊어진 클라이언트는 무시
                    }
                }
            }
        }
    }
}