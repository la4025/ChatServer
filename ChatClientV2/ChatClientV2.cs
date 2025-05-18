using System;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

class ChatClient
{
    static async Task Main()
    {
        Console.Write("닉네임을 입력하세요: ");
        string nickname = Console.ReadLine();

        TcpClient client = new TcpClient();
        await client.ConnectAsync("127.0.0.1", 5000);
        Console.WriteLine("[서버에 연결됨]");

        NetworkStream stream = client.GetStream();

        // 닉네임 서버로 전송
        byte[] nickData = Encoding.UTF8.GetBytes(nickname);
        await stream.WriteAsync(nickData, 0, nickData.Length);

        // 수신 Task 시작
        _ = Task.Run(() => ReceiveMessagesAsync(stream));

        // 메시지 입력 루프
        while (true)
        {
            string message = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(message))
                continue;

            byte[] data = Encoding.UTF8.GetBytes(message);
            await stream.WriteAsync(data, 0, data.Length);
        }
    }

    static async Task ReceiveMessagesAsync(NetworkStream stream)
    {
        byte[] buffer = new byte[1024];

        try
        {
            while (true)
            {
                int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                if (bytesRead == 0)
                {
                    Console.WriteLine("[서버 연결 종료]");
                    break;
                }

                string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                Console.WriteLine($"\n{message}");
                Console.Write("> ");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[수신 오류] {ex.Message}");
        }
    }
}
