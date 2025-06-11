using System;
using System.Net.Sockets;
using System.Text;
class ConsoleClient
{
    static void Main()
    {
        TcpClient client = new TcpClient();
        try
        {
            client.Connect("127.0.0.1", 5000);
            NetworkStream stream = client.GetStream();
            byte[] buffer = new byte[1024];

            Console.WriteLine("Подключено к серверу.");

            while (true)
            {
                int bytesRead = stream.Read(buffer, 0, buffer.Length);
                string serverMessage = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                Console.WriteLine(serverMessage.Trim());

                if (serverMessage.Contains("Введите ваш ход"))
                {
                    string move = Console.ReadLine();
                    byte[] data = Encoding.UTF8.GetBytes(move);
                    stream.Write(data, 0, data.Length);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Ошибка: " + ex.Message);
        }
    }
}