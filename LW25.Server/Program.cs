using System.Net;
using System.Net.Sockets;
using System.Text;

class ConsoleServer
{
    static TcpClient client1 = null;
    static TcpClient client2 = null;

    static string move1 = null;
    static string move2 = null;

    static object lockObj = new object();

    static void Main()
    {
        TcpListener listener = new TcpListener(IPAddress.Any, 5000);
        listener.Start();
        Console.WriteLine("Сервер запущен. Ожидание двух игроков...");

        client1 = listener.AcceptTcpClient();
        Console.WriteLine("Игрок 1 подключился.");
        SendMessage(client1, "Ожидание второго игрока...");

        client2 = listener.AcceptTcpClient();
        Console.WriteLine("Игрок 2 подключился.");
        SendMessage(client1, "Игрок 2 подключился. Игра начинается!");
        SendMessage(client2, "Игрок 1 уже подключен. Игра начинается!");

        Thread t1 = new Thread(() => HandleClient(client1, 1));
        Thread t2 = new Thread(() => HandleClient(client2, 2));
        t1.Start();
        t2.Start();
    }

    static void HandleClient(TcpClient client, int playerNumber)
    {
        NetworkStream stream = client.GetStream();
        byte[] buffer = new byte[1024];

        while (true)
        {
            try
            {
                // Ждём, пока можно начать новый раунд
                lock (lockObj)
                {
                    // Если текущий игрок уже сделал ход, ждём
                    while ((playerNumber == 1 && move1 != null) || (playerNumber == 2 && move2 != null))
                    {
                        Monitor.Wait(lockObj);
                    }

                    SendMessage(client, "Введите ваш ход (камень, ножницы, бумага):");
                }

                int bytesRead = stream.Read(buffer, 0, buffer.Length);
                if (bytesRead == 0) break; // клиент отключился

                string move = Encoding.UTF8.GetString(buffer, 0, bytesRead).Trim().ToLower();

                lock (lockObj)
                {
                    if (playerNumber == 1)
                        move1 = move;
                    else
                        move2 = move;

                    // Если оба игрока сделали ход — обработать раунд
                    if (move1 != null && move2 != null)
                    {
                        string result1 = GetResult(move1, move2);
                        string result2 = GetResult(move2, move1);

                        SendMessage(client1, $"Противник сыграл: {move2}. {result1}");
                        SendMessage(client2, $"Противник сыграл: {move1}. {result2}");

                        // Сбросить ходы
                        move1 = null;
                        move2 = null;

                        // Разбудить оба потока для следующего раунда
                        Monitor.PulseAll(lockObj);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Игрок {playerNumber} отключился: {ex.Message}");
                break;
            }
        }

        client.Close();
    }

    static void SendMessage(TcpClient client, string message)
    {
        byte[] data = Encoding.UTF8.GetBytes(message + "\n");
        client.GetStream().Write(data, 0, data.Length);
    }

    static string GetResult(string myMove, string opponentMove)
    {
        if (myMove == opponentMove)
            return "Ничья!";
        if ((myMove == "камень" && opponentMove == "ножницы") ||
            (myMove == "ножницы" && opponentMove == "бумага") ||
            (myMove == "бумага" && opponentMove == "камень"))
            return "Вы победили!";
        return "Вы проиграли!";
    }
}