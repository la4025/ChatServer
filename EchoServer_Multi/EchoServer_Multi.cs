using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

class EchoServer_Multi
{
    static void Main(string[] args)
    {
        TcpListener server = null;
        try
        {
            int port = 5000;
            server = new TcpListener(IPAddress.Any, port);
            server.Start();
            Console.WriteLine($"멀티 클라이언트 에코 서버 시작 (포트 : {port})");

            while (true)
            {
                TcpClient client = server.AcceptTcpClient();
                Console.WriteLine("클라이언트 접속됨");

                // 클라이언트 처리 스레드 생성 및 시작
                Thread clientThread = new Thread(HandleClient);
                clientThread.Start(client);
            }
        }
        catch (SocketException ex)
        {
            Console.WriteLine($"[서버 소켓 예외] {ex.Message}");
        }
        finally
        {
            server?.Stop();
        }
    }

    static void HandleClient(object clientObj)
    {
        TcpClient client = clientObj as TcpClient;

        try
        {
            using (NetworkStream stream = client.GetStream())
            {
                byte[] buffer = new byte[1024];
                int bytesRead;

                while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) != 0)
                {
                    string msg = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    Console.WriteLine($"수신 : {msg}");

                    byte[] response = Encoding.UTF8.GetBytes(msg);
                    stream.Write(response, 0, response.Length);
                }
            }
            Console.WriteLine("클라이언트 연결 종료");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[클라이언트 예외] {ex.Message}");
        }
        finally
        {
            client?.Close();
        }
    }
}
