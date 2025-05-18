using System;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

/// <summary>
/// ChatClientV3의 개선버전
/// 닉네임과 채팅방 이름 사이에 구분자('\n')를 삽입하여 전송.
/// 버퍼에 쌓인 문자열을 누적해서 구분자 기준으로 잘라서 출력.
/// </summary>
class ChatClientV3_improve
{
    static async Task Main()
    {
        // 닉네임 입력
        Console.Write("닉네임을 입력하세요 : ");
        string nickname = Console.ReadLine();

        // 방 이름 입력
        Console.Write("입장할 채팅방 이름을 입력하세요 : ");
        string roomName = Console.ReadLine();

        // 서버 연결
        TcpClient client = new TcpClient();
        await client.ConnectAsync("127.0.0.1", 5000);
        Console.WriteLine("[서버에 연결 됨]");

        NetworkStream stream = client.GetStream();

        // 닉네임 전송
        byte[] nickData = Encoding.UTF8.GetBytes(nickname + "\n");
        await stream.WriteAsync(nickData, 0, nickData.Length);

        // 방 이름 전송
        byte[] roomData = Encoding.UTF8.GetBytes(roomName + "\n");
        await stream.WriteAsync(roomData, 0, roomData.Length);

        // 메시지 수신 Task 실행
        _ = Task.Run(() => ReceiveMessagesAsync(stream));

        // 메시지 입력 루프
        while (true)
        {
            string message = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(message))
            {
                continue;
            }

            byte[] data = Encoding.UTF8.GetBytes(message + "\n");
            await stream.WriteAsync(data, 0, data.Length);
        }
    }

    // 메시지 수신 처리
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
                    Console.WriteLine("[서버와의 연결이 종료되었습니다.]");
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
    }
}