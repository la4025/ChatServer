using System;
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
/// ChatClientV6
/// JSON 기반 메시지 시스템 적용
/// 메시지를 구조화해서 서버에 전송
/// </summary>
class ChatClientV6
{
    static string nickname = "";
    static string roomName = "";

    static async Task Main()
    {
        // 닉네임 입력
        Console.Write("닉네임을 입력하세요: ");
        nickname = Console.ReadLine();

        // 방 이름 입력
        Console.Write("입장할 채팅방 이름을 입력하세요: ");
        roomName = Console.ReadLine();

        // 서버 연결
        TcpClient client = new TcpClient();
        await client.ConnectAsync("127.0.0.1", 5000);
        Console.WriteLine("[서버에 연결됨]");

        NetworkStream stream = client.GetStream();

        ChatMessage helloMsg;

        helloMsg = new ChatMessage
        {
            Type = "join",
            Nickname = nickname,
            Room = roomName
        };

        string userJson = JsonSerializer.Serialize(helloMsg);
        byte[] userData = Encoding.UTF8.GetBytes(userJson + "\n");
        await stream.WriteAsync(userData, 0, userData.Length);

        // 수신 루프 실행
        _ = Task.Run(() => ReceiveMessagesAsync(stream));

        // 메시지 입력 루프
        while (true)
        {
            string input = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(input))
            {
                continue;
            }

            ChatMessage msg;

            if (input.StartsWith("/")) // 명령어
            {
                msg = new ChatMessage
                {
                    Type = "command",
                    Command = input
                };
            }
            else // 일반 채팅
            {
                msg = new ChatMessage
                {
                    Type = "chat",
                    Nickname = nickname,
                    Room = roomName,
                    Message = input
                };
            }

            string json = JsonSerializer.Serialize(msg);
            byte[] data = Encoding.UTF8.GetBytes(json + "\n");
            await stream.WriteAsync(data, 0, data.Length);
        }
    }

    static async Task ReceiveMessagesAsync(NetworkStream stream)
    {
        byte[] buffer = new byte[1024];
        StringBuilder messageBuffer = new();

        try
        {
            while (true)
            {
                int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                if (bytesRead == 0)
                {
                    Console.WriteLine("[서버와의 연결이 종료되었습니다]");
                    break;
                }

                string chunk = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                messageBuffer.Append(chunk);

                string all = messageBuffer.ToString();
                int newLineIndex;

                while ((newLineIndex = all.IndexOf('\n')) >= 0)
                {
                    string completeMessage = all[..newLineIndex];
                    Console.WriteLine($"\n{completeMessage}");
                    Console.Write("> ");

                    all = all[(newLineIndex + 1)..];
                }

                messageBuffer.Clear();
                messageBuffer.Append(all);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[수신 오류] {ex.Message}");
        }
        finally
        {
            Environment.Exit(0);
        }
    }
}


