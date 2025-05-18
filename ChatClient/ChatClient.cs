 using System;
 using System.Net.Sockets;
 using System.Text;
 using System.Threading.Tasks;

 class ChatClient
 {
    static async Task Main(string[] args)
    {
        TcpClient client = new TcpClient();
        await client.ConnectAsync("127.0.0.1", 5000);
        Console.WriteLine("[서버 연결 성공]");
        NetworkStream stream = client.GetStream();

        // 수신 전용 Task 실행
        _ = Task.Run(() => ReceiveMessagesAsync(stream));

        // 입력 송신 루프
        while (true)
        {
            string message = Console.ReadLine();
            if (string.IsNullOrEmpty(message))
            {
                continue;
            }

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
                    Console.WriteLine("[서버 연결 끊김]");
                    break;
                }

                string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                Console.WriteLine($"\n[수신] {message}");
                Console.Write("> "); // 입력줄 표시
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[수신 오류] {ex.Message}");
        }
    }
 }