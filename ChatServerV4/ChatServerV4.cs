using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

class ChatServerV4
{
    static TcpListener listener;
    static Dictionary<string, List<TcpClient>> rooms = new();
    static Dictionary<TcpClient, string> clientNames = new();
    static Dictionary<TcpClient, string> clientRooms = new();
    static HashSet<String> nicknameSet = new();
    static object lockObj = new();

    static async Task Main()
    {
        listener = new TcpListener(IPAddress.Any, 5000);
        listener.Start();
        Console.WriteLine("[서버 시작 됨]");

        while (true)
        {
            TcpClient client = await listener.AcceptTcpClientAsync();
            _ = HandleClientAsync(client);
        }
    }

    static async Task HandleClientAsync(TcpClient client)
    {
        var stream = client.GetStream();
        var reader = new StreamReader(stream, Encoding.UTF8);
        var writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true };

        string nickname = "";
        string roomName = "";

        try
        {
            // 닉네임 입력 (중복 허용 안 함)
            while (true)
            {
                nickname = await reader.ReadLineAsync();

                if (nickname == null)
                {
                    return;
                }

                lock (lockObj)
                {
                    if (!nicknameSet.Contains(nickname))
                    {
                        nicknameSet.Add(nickname);
                        break;
                    }
                }

                await writer.WriteLineAsync("[이미 사용 중인 닉네임 입니다. 다시 입력해주세요.]");
            }

            // 방 이름 입력
            roomName = await reader.ReadLineAsync();

            if (roomName == null)
            {
                return;
            }

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

                if (message.StartsWith("/exit"))
                {
                    await writer.WriteLineAsync("[시스템] 퇴장 처리되었습니다.");
                    break;
                }

                if (message.StartsWith("/whoami"))
                {
                    await writer.WriteLineAsync($"당신의 닉네임은 {nickname} 입니다.");
                    continue;
                }

                if (message.StartsWith("/users"))
                {
                    List<string> users = new();

                    lock (lockObj)
                    {
                        foreach (var c in rooms[roomName])
                        {
                            // clientNames의 키값으로 c가 있으면, 그 키에 대응하는 name을 반환해라.
                            if (clientNames.TryGetValue(c, out var name))
                            {
                                users.Add(name);
                            }
                        }
                    }

                    // users 리스트에 ", " 구분자를 넣어서 이어붙여라.
                    string list = string.Join(", ", users);
                    await writer.WriteLineAsync($"현재 방 유저 : {list}");
                    continue;
                }

                if (message.StartsWith("/w "))
                {
                    string[] tokens = message.Split(' ', 3);

                    if (tokens.Length < 3)
                    {
                        await writer.WriteLineAsync("[시스템] 사용법 : /w 닉네임 메시지");
                        continue;
                    }

                    string targetName = tokens[1];
                    string whisperMsg = tokens[2];

                    // nullable reference types
                    // null이 될 수 있는 targetClient를 만들고, 초기값은 null로 지정한다.
                    TcpClient? targetClient = null;

                    lock (lockObj)
                    {
                        foreach (var kvp in clientNames)
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
                    continue;
                }

                // 일반 채팅 메시지
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