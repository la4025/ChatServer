﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

// JSON 구조조
public class ChatMessage
{
    public string Type { get; set; }         // "chat" 또는 "command"
    public string? Nickname { get; set; }
    public string? Room { get; set; }
    public string? Message { get; set; }
    public string? Command { get; set; }
}

/// <summary>
/// ChatServerV8
/// DB 연동
/// 닉네임, 방 이름, 메시지, 시간 저장
/// /history 명령어 추가. 해당 방의 최근 10개 메시지 불러오기 가능.
/// </summary>
class ChatServerV8
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
            // 클라이언트가 첫 메시지로 채팅 메시지 JSON을 보내야 함.
            // json! -> null-forgiving operator : null이 아님을 강제한다.
            string? json = await reader.ReadLineAsync();
            ChatMessage? helloMsg = JsonSerializer.Deserialize<ChatMessage>(json!);

            if (helloMsg == null || helloMsg.Type != "join" || helloMsg.Nickname == null || helloMsg.Room == null)
            {
                await writer.WriteLineAsync("[시스템] 닉네임 또는 방 정보가 잘못되었습니다.");
                return;
            }

            nickname = helloMsg.Nickname;
            roomName = helloMsg.Room;

            // 닉네임, 방이름 DB 저장
            Database.InsertLoginLog(nickname, roomName);

            // 딕셔너리에 등록
            lock (ChatServerV8.lockObj)
            {
                if (!ChatServerV8.Rooms.ContainsKey(roomName))
                {
                    ChatServerV8.Rooms[roomName] = new List<TcpClient>();
                }

                ChatServerV8.Rooms[roomName].Add(client);
                ChatServerV8.clientNames[client] = nickname;
                ChatServerV8.clientRooms[client] = roomName;
            }

            Console.WriteLine($"[입장] {nickname} -> {roomName}");
            await BroadCastToRoom($"● [{nickname}]님이 채팅창 [{roomName}]에 입장했습니다.");

            // 메시지 수신 루프
            while (true)
            {
                string? message = await reader.ReadLineAsync();

                if (message == null)
                {
                    break;
                }

                ChatMessage? msg = JsonSerializer.Deserialize<ChatMessage>(message);

                if (msg == null || msg.Type == null)
                {
                    continue;
                }

                if (msg.Type == "command")
                {
                    bool handled = await HandleCommand(msg);

                    if (handled)
                    {
                        break;
                    }
                }
                else if (msg.Type == "chat" && msg.Message != null)
                {
                    // 채팅 DB 저장
                    Database.InsertChatLog(nickname, roomName, msg.Message);

                    // 일반 채팅 메시지
                    Console.WriteLine($"[수신] [{roomName}] {nickname} : {msg.Message}");
                    await BroadCastToRoom($"[{nickname}] : {msg.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[예외 발생] {ex.Message}");
        }
        finally
        {
            lock (ChatServerV8.lockObj)
            {
                ChatServerV8.Rooms[roomName].Remove(client);
                ChatServerV8.clientNames.Remove(client);
                ChatServerV8.clientRooms.Remove(client);
            }

            Console.WriteLine($"[퇴장] {nickname} <- {roomName}");
            await BroadCastToRoom($"○ [{nickname}]님이 채팅창 [{roomName}]에서 퇴장했습니다.");
            client.Close();
        }
    }

    private async Task<bool> HandleCommand(ChatMessage msg)
    {
        switch (msg.Command)
        {
            case "/exit":
                {
                    await writer.WriteLineAsync("[시스템] 퇴장 처리 되었습니다.");
                    return true;
                }
            case "/whoami":
                {
                    await writer.WriteLineAsync($"당신의 닉네임은 {nickname} 입니다.");
                    return false;
                }
            case "/users":
                {
                    List<string> users = new();
                    lock (ChatServerV8.lockObj)
                    {
                        foreach (var c in ChatServerV8.Rooms[roomName])
                        {
                            if (ChatServerV8.clientNames.TryGetValue(c, out var name))
                            {
                                users.Add(name);
                            }
                        }
                    }

                    await writer.WriteLineAsync($"현재 방 유저 : {string.Join(", ", users)}");
                    return false;
                }
            case "/leave":
                {
                    await LeaveRoom();
                    Console.WriteLine($"[퇴장] {nickname} <- {roomName}");
                    await writer.WriteLineAsync("[시스템] 방에서 퇴장했습니다.");
                    return false;
                }
            case "/list":
                {
                    List<string> roomNames;
                    lock (ChatServerV8.lockObj)
                    {
                        roomNames = new List<string>(ChatServerV8.Rooms.Keys);
                    }

                    if (roomNames.Count == 0)
                    {
                        await writer.WriteLineAsync("[시스템] 현재 생성된 채팅방이 없습니다.");
                    }
                    else
                    {
                        await writer.WriteLineAsync("[시스템] 현재 채팅방 목록 : ");
                        foreach (var room in roomName)
                        {
                            await writer.WriteLineAsync(" - " + room);
                        }
                    }

                    return false;
                }
            case "/history":
                {
                    var logs = Database.GetRecentChagLogs(roomName);
                    await writer.WriteLineAsync("[시스템] 최근 채팅 로그 : ");

                    // 역순으로 출력하기 위해 LINQ 사용
                    foreach (var log in logs.AsEnumerable().Reverse())
                    {
                        await writer.WriteLineAsync(" - " + log);
                    }

                    return false;
                }
            default:
                {
                    if (msg.Command!.StartsWith("/w "))
                    {
                        string[] tokens = msg.Command.Split(' ', 3);

                        if (tokens.Length < 3)
                        {
                            await writer.WriteLineAsync("[시스템] 사용법 : /w 닉네임 메시지");
                            return false;
                        }

                        string targetName = tokens[1];
                        string whisperMsg = tokens[2];
                        TcpClient? targetClient = null;

                        lock (ChatServerV8.lockObj)
                        {
                            foreach (var kvp in ChatServerV8.clientNames)
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
                        return false;
                    }
                    else if (msg.Command!.StartsWith("/create "))
                    {
                        string[] tokens = msg.Command.Split(' ', 2);
                        bool isExist;

                        if (tokens.Length < 2)
                        {
                            await writer.WriteLineAsync("[시스템] 사용법 : /create 방제목");
                            return false;
                        }

                        string newRoom = tokens[1];

                        lock (ChatServerV8.lockObj)
                        {
                            isExist = ChatServerV8.Rooms.ContainsKey(newRoom);
                        }

                        if (isExist)
                        {
                            await writer.WriteLineAsync("[시스템] 이미 존재하는 방입니다.");
                            return false;
                        }

                        ChatServerV8.Rooms[newRoom] = new List<TcpClient>();
                    }
                    else if (msg.Command!.StartsWith("/join "))
                    {
                        string[] tokens = msg.Command.Split(' ', 2);
                        bool isExist;

                        if (tokens.Length < 2)
                        {
                            await writer.WriteLineAsync("[시스템] 사용법 : /join 방제목");
                            return false;
                        }

                        string targetRoom = tokens[1];

                        lock (ChatServerV8.lockObj)
                        {
                            isExist = ChatServerV8.Rooms.ContainsKey(targetRoom);
                        }

                        if (isExist)
                        {
                            await writer.WriteLineAsync("[시스템] 존재하지 않는 방입니다.");
                            return false;
                        }

                        await MoveToRoom(targetRoom);
                        await writer.WriteLineAsync($"[시스템] '{targetRoom}' 방에 입장했습니다.");
                        return false;
                    }
                    return false;
                }
        }
    }

    private async Task MoveToRoom(string newRoom)
    {
        await LeaveRoom();

        lock (ChatServerV8.lockObj)
        {
            ChatServerV8.Rooms[newRoom].Add(client);
            ChatServerV8.clientRooms[client] = newRoom;
            roomName = newRoom;
        }

        Console.WriteLine($"[입장] {nickname} -> {roomName}");
        await BroadCastToRoom($"● [{nickname}]님이 채팅창 [{roomName}]에 입장했습니다.");
    }

    private async Task LeaveRoom()
    {
        lock (ChatServerV8.lockObj)
        {
            if (!string.IsNullOrEmpty(roomName) && ChatServerV8.Rooms.ContainsKey(roomName))
            {
                ChatServerV8.Rooms[roomName].Remove(client);

                if (ChatServerV8.Rooms[roomName].Count == 0)
                {
                    ChatServerV8.Rooms.Remove(roomName);
                }
                ChatServerV8.clientRooms.Remove(client);
            }
        }
    }


    private async Task BroadCastToRoom(string message)
    {
        lock (ChatServerV8.lockObj)
        {
            foreach (var c in ChatServerV8.Rooms[roomName])
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
