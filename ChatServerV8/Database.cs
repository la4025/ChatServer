using MySql.Data.MySqlClient;
using System;

public static class Database
{
    private static string connectionString = "Server=localhost;Database=ChatServer;Uid=root;Pwd=Skehdgns4025!";

    public static void InsertLoginLog(string nickname, string room)
    {
        try
        {
            using var conn = new MySqlConnection(connectionString);
            conn.Open();

            string query =
            @"INSERT INTO login_log (nickname, room, event_time)
            VALUES (@nickname, @room, @time)";

            using var cmd = new MySqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@nickname", nickname);
            cmd.Parameters.AddWithValue("@room", room);
            cmd.Parameters.AddWithValue("@time", DateTime.Now);

            cmd.ExecuteNonQuery();
        }
        catch (Exception ex)
        {
            Console.WriteLine("[DB 오류] " + ex.Message);
        }
    }

    public static void InsertChatLog(string nickname, string room, string message)
    {
        try
        {
            using var conn = new MySqlConnection(connectionString);
            conn.Open();

            string query =
           @"INSERT INTO chat_log (nickname, room, message, event_time)
            VALUES (@nickname, @room, @message, @time)";

            using var cmd = new MySqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@nickname", nickname);
            cmd.Parameters.AddWithValue("@room", room);
            cmd.Parameters.AddWithValue("@message", message);
            cmd.Parameters.AddWithValue("@time", DateTime.Now);

            cmd.ExecuteNonQuery();
        }
        catch (Exception ex)
        {
            Console.WriteLine("[DB 오류] " + ex.Message);
        }
    }

    public static List<string> GetRecentChagLogs(string room, int count = 10)
    {
        List<string> logs = new();

        try
        {
            using var conn = new MySqlConnection(connectionString);
            conn.Open();

            string query =
            @"SELECT nickname, message, event_time
            FROM chat_log
            WHERE room = @room
            ORDER BY event_time DESC, id DESC
            LIMIT @count";

            using var cmd = new MySqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@room", room);
            cmd.Parameters.AddWithValue("@count", count);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                string nick = reader.GetString(0);
                string msg = reader.GetString(1);
                DateTime time = reader.GetDateTime(2);

                logs.Add($"[{time:HH:mm}] {nick} : {msg}");
            }
        }
        catch (Exception ex)
        {

            Console.WriteLine("[DB 조회 오류] " + ex.Message);
        }

        return logs;
    }
}