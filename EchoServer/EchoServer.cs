using System;
using System.Net;
using System.Net.Sockets;
using System.Text;


class EchoServer
{
    static void Main(string[] args)
    {
        Console.InputEncoding = Encoding.UTF8;
        Console.OutputEncoding = Encoding.UTF8;

        TcpListener server = null;

        try
        {
            int port = 5000;
            server = new TcpListener(IPAddress.Any, port);
            server.Start();

            Console.WriteLine("에코서버가 시작되었습니다. 클라이언트 접속 대기 중....");

            while (true)
            {
                using TcpClient client = server.AcceptTcpClient();
                Console.WriteLine("클라이언트 접속됨");

                NetworkStream stream = client.GetStream();
                byte[] buffer = new byte[1024];
                int bytesRead;

                while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) != 0)
                {
                    string received = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    Console.WriteLine("수신 : " + received);

                    // 에코 : 다시 전송
                    byte[] sendData = Encoding.UTF8.GetBytes(received);
                    stream.Write(sendData, 0, sendData.Length);
                }

                Console.WriteLine("클라이언트 연결 종료됨");
            }
        }
        catch (SocketException e)
        {
            Console.WriteLine("소켓 예외 : " + e.Message);
        }
        finally
        {
            server?.Stop();
        }
    }
}