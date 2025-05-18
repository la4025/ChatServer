using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

/// <summary>
/// 닉네임 + 채팅방 이름 입력 후 접속
/// </summary>
class ChatServerV3
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
        NetworkStream stream = client.GetStream();
        byte[] buffer = new byte[1024];

        try
        {
            // 1. 닉네임 받기
            int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
            string nickname = Encoding.UTF8.GetString(buffer, 0, bytesRead).Trim();

            // 2. 방 이름 받기
            bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
            string roomName = Encoding.UTF8.GetString(buffer, 0, bytesRead).Trim();

            // 3. 사용자 정보 등록
            lock (lockObj)
            {
                if(!rooms.ContainsKey(roomName))
                {
                    rooms[roomName] = new List<TcpClient>();
                }

                rooms[roomName].Add(client);
                clientNames[client] = nickname;
                clientRooms[client] = roomName;
            }

            Console.WriteLine($"[입장] {nickname} -> {roomName}");
            await BroadCastToRoom(roomName, $"● [{nickname}]님이 채팅창 [{roomName}]에 입장했습니다.", client);

            // 4. 메시지 수신 루프
            while (true)
            {
                bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                
                if (bytesRead == 0)
                {
                    break;
                }

                string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
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
        byte[] data = Encoding.UTF8.GetBytes(message);

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
                        NetworkStream stream = client.GetStream();
                        stream.WriteAsync(data, 0, data.Length); // await 없이 실행 (fire and forget)
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