using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

/// <summary>
/// ChatServerV5
/// ChatServerV4를 객체지향 프로그래밍으로 변경
/// </summary>
class ChatServerV5
{
    public static Dictionary<string, List<TcpClient>> Rooms = new();
    public static Dictionary<TcpClient, string> clientNames = new();
    public static Dictionary<TcpClient, string> clientRooms = new();
    public static object lockObj = new();

    static async Task Main()
    {
        TcpListener listener = new TcpListener(IPAddress.Any, 5000);
        listener.Start();
        Console.WriteLine("[서버 시작 됨]");

        while (true)
        {
            TcpClient client = await listener.AcceptTcpClientAsync();
            var handler = new ClientHandler(client);
            _ = handler.HandleAsync();
        }
    }
}

class ClientHandler
{
    private TcpClient client;
    private NetworkStream stream;
    private StreamReader reader;
    private StreamWriter writer;

    private string nickname = "";
    private string roomName = "";

    // 생성자
    public ClientHandler(TcpClient client)
    {
        this.client = client;
        this.stream = client.GetStream();
        this.reader = new StreamReader(stream, Encoding.UTF8);
        this.writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true };
    }

    public async Task HandleAsync()
    {
        try
        {
            // 1. 닉네임 받기
            nickname = await reader.ReadLineAsync();

            if (nickname == null)
            {
                return;
            }

            // 2. 방 이름 받기
            roomName = await reader.ReadLineAsync();

            if (roomName == null)
            {
                return;
            }

            // 3. 서버 자료구조에 등록
            lock (ChatServerV5.lockObj)
            {
                if (!ChatServerV5.Rooms.ContainsKey(roomName))
                {
                    ChatServerV5.Rooms[roomName] = new List<TcpClient>();
                }

                ChatServerV5.Rooms[roomName].Add(client);
                ChatServerV5.clientNames[client] = nickname;
                ChatServerV5.clientRooms[client] = roomName;
            }

            // 4. 방에 메시지 전파
            Console.WriteLine($"[입장] {nickname} -> {roomName}");
            await BroadCastToRoom($"● [{nickname}]님이 채팅창 [{roomName}]에 입장했습니다.");

            // 5. 메시지 수신 루프
            while (true)
            {
                string message = await reader.ReadLineAsync();

                if (message == null)
                {
                    break;
                }

                if (message.StartsWith("/exit"))
                {
                    await writer.WriteLineAsync("[시스템] 퇴장 처리되었습니다.");
                    break;
                }

                if (message.StartsWith("/"))
                {
                    bool handled = await HandleCommand(message);

                    if (handled)
                    {
                        continue;
                    }
                }

                // 일반 채팅 메시지
                Console.WriteLine($"[수신] [{roomName}] {nickname} : {message}");
                await BroadCastToRoom($"[{nickname}] : {message}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[예외 발생] {ex.Message}");
        }
        finally
        {
            lock (ChatServerV5.lockObj)
            {
                ChatServerV5.Rooms[roomName].Remove(client);
                ChatServerV5.clientNames.Remove(client);
                ChatServerV5.clientRooms.Remove(client);
            }

            Console.WriteLine($"[퇴장] {nickname} <- {roomName}");
            await BroadCastToRoom($"○ [{nickname}]님이 채팅창 [{roomName}]에서 퇴장했습니다.");
            client.Close();
        }
    }

    private async Task<bool> HandleCommand(string message)
    {
        if (message.StartsWith("/whoami"))
        {
            await writer.WriteLineAsync($"당신의 닉네임은 {nickname} 입니다.");
            return true;
        }

        if (message.StartsWith("/users"))
        {
            List<string> users = new();

            lock (ChatServerV5.lockObj)
            {
                foreach (var c in ChatServerV5.Rooms[roomName])
                {
                    // clientNames의 키값으로 c가 있으면, 그 키에 대응하는 name을 반환해라.
                    if (ChatServerV5.clientNames.TryGetValue(c, out var name))
                    {
                        users.Add(name);
                    }
                }
            }

            await writer.WriteLineAsync($"현재 방 유저 : {string.Join(", ", users)}");
            return true;
        }

        if (message.StartsWith("/w "))
        {
            string[] tokens = message.Split(' ', 3);

            if (tokens.Length < 3)
            {
                await writer.WriteLineAsync("[시스템] 사용법 : /w 닉네임 메시지");
                return true;
            }

            string targetName = tokens[1];
            string whisperMsg = tokens[2];

            // nullable reference types
            // null이 될 수 있는 targetClient를 만들고, 초기값은 null로 지정한다.
            TcpClient? targetClient = null;

            lock (ChatServerV5.lockObj)
            {
                foreach (var kvp in ChatServerV5.clientNames)
                {
                    if (kvp.Value == targetName)
                    {
                        targetClient = kvp.Key;
                        break;
                    }
                }
            }

            if (targetClient != null)
            {
                var targetWriter = new StreamWriter(targetClient.GetStream(), Encoding.UTF8) { AutoFlush = true };
                await targetWriter.WriteLineAsync($"[귓속말] {nickname} : {whisperMsg}");
                await writer.WriteLineAsync($"[귓속말 -> {targetName}] : {whisperMsg}");
            }
            else
            {
                await writer.WriteLineAsync("[시스템] 대상 닉네임을 찾을 수 없습니다.");
            }
            return true;
        }

        return false;
    }

    private async Task BroadCastToRoom(string message)
    {
        lock (ChatServerV5.lockObj)
        {
            foreach (var c in ChatServerV5.Rooms[roomName])
            {
                if (c == client)
                {
                    continue;
                }

                try
                {
                    var sw = new StreamWriter(c.GetStream(), Encoding.UTF8) { AutoFlush = true };
                    sw.WriteLine(message);
                }
                catch
                {
                    // 전송 실패 무시
                }
            }
        }
    }
}




