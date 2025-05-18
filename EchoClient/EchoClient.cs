using System.Net.Sockets;
using System.Text;

class EchoClient
{
    static void Main(string[] args)
    {
        try
        {
            TcpClient client = new TcpClient("127.0.0.1", 5000);
            NetworkStream stream = client.GetStream();

            Console.WriteLine("에코 서버에 연결되었습니다.");
            Console.WriteLine("보낼 메시지를 입력하세요. (exit 입력 시 종료)");

            while (true)
            {
                Console.WriteLine("> ");
                string message = Console.ReadLine();

                if (message.ToLower() == "exit")
                {
                    break;
                }

                byte[] sendData = Encoding.UTF8.GetBytes(message);
                stream.Write(sendData, 0, sendData.Length);

                byte[] receiveBuffer = new byte[1024];
                int bytesRead = stream.Read(receiveBuffer, 0, receiveBuffer.Length);
                string received = Encoding.UTF8.GetString(receiveBuffer, 0, receiveBuffer.Length);

                Console.WriteLine($"[서버응답] {received}");
            }

            stream.Close();
            client.Close();
            Console.WriteLine("서버와 연결을 종료했습니다.");
        }
        catch (Exception ex)
        {
            Console.WriteLine("오류 발생 : " + ex.Message);
        }
    }
}