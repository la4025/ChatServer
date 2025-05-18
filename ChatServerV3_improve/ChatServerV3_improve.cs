using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

class ChatServerV3_improve
{
    static TcpListener listener;
    static Dictionary<string, List<TcpClient>> rooms = new(); // 방이름 -> 클라이언트 리스트
    static Dictionary<TcpClient, string> clientNames = new(); // 클라이언트 -> 닉네임
    static Dictionary<TcpClient, string> clientRooms = new(); // 클라이언트 -> 방 이름
    static object lockObj = new();

    static async Task Main()
    {
        listener = new TcpListener(IPAddress.Any, 5000);
        listener.Start();
        Console.WriteLine("[서버 시작 됨]");

        while (true)
        {
            TcpClient client = await listener.AcceptTcpClientAsync();
            _ = HandleClientAsync(client); // 비동기 처리
        }
    }

    static async Task HandleClientAsync(TcpClient client)
    {
        var stream = client.GetStream();
        var reader = new StreamReader(stream, Encoding.UTF8);
        var writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true };

        try
        {
            // 닉네임 수신
            string nickname = await reader.ReadLineAsync();

            // 방 이름 수신
            string roomName = await reader.ReadLineAsync();

            lock (lockObj)
            {
                if (!rooms.ContainsKey(roomName))
                {
                    rooms[roomName] = new List<TcpClient>();
                }

                rooms[roomName].Add(client);
                clientNames[client] = nickname;
                clientRooms[client] = roomName;
            }

            Console.WriteLine($"[입장] {nickname} -> {roomName}");
            await BroadCastToRoom(roomName, $"● [{nickname}]님이 채팅창 [{roomName}]에 입장했습니다.", client);

            // 메시지 수신 루프
            while (true)
            {
                string message = await reader.ReadLineAsync();

                if (message == null)
                {
                    break;
                }

                Console.WriteLine($"[수신] [{roomName}] {nickname} : {message}");

                await BroadCastToRoom(roomName, $"[{nickname}] : {message}", client);
            }
        }
        catch (Exception ex)
        {

            Console.WriteLine($"[예외 발생] {ex.Message}");
        }
        finally
        {
            // 5. 연결 종료 처리
            string nickname = clientNames.GetValueOrDefault(client, "알 수 없음");
            string roomName = clientNames.GetValueOrDefault(client, "Unknown");

            lock (lockObj)
            {
                if (rooms.ContainsKey(roomName))
                {
                    rooms[roomName].Remove(client);
                }

                clientNames.Remove(client);
                clientRooms.Remove(client);
            }

            Console.WriteLine($"[퇴장] {nickname} <- {roomName}");
            await BroadCastToRoom(roomName, $"○ [{nickname}]님이 채팅창 [{roomName}]에서 퇴장했습니다.", client);

            client.Close();
        }
    }

    static async Task BroadCastToRoom(string roomName, string message, TcpClient sender)
    {
        lock (lockObj)
        {
            if (!rooms.ContainsKey(roomName))
            {
                return;
            }

            foreach (var client in rooms[roomName])
            {
                if (client != sender)
                {
                    try
                    {
                        var writer = new StreamWriter(client.GetStream(), Encoding.UTF8) { AutoFlush = true };
                        writer.WriteLine(message);
                    }
                    catch
                    {
                        // 전송 실패 무시
                    }
                }
            }
        }
    }
}