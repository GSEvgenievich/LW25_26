using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

class UdpBroadcastClient
{
    class PlayerInfo
    {
        public string Name { get; set; }
        public DateTime Time { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
    }

    static async Task Main()
    {
        Console.Write("Введите имя игрока: ");
        string playerName = Console.ReadLine();
        int broadcastIntervalMs = 2000;
        Random rand = new Random();
        UdpClient receiverClient = null;
        int sendToPort = 0;

        try
        {
            int listenPort = 6000;
            sendToPort = 6001;
            receiverClient = new UdpClient(listenPort);
        }
        catch
        {
            int listenPort = 6001;
            sendToPort = 6000;
            receiverClient = new UdpClient(listenPort);
        }

        using UdpClient senderClient = new UdpClient();
        senderClient.EnableBroadcast = true;

        CancellationTokenSource cts = new CancellationTokenSource();

        // Задача приёма сообщений
        Task receiveTask = Task.Run(async () =>
        {
            while (!cts.Token.IsCancellationRequested)
            {
                try
                {
                    UdpReceiveResult result = await receiverClient.ReceiveAsync();
                    string message = Encoding.UTF8.GetString(result.Buffer);
                    PlayerInfo? receivedInfo = JsonSerializer.Deserialize<PlayerInfo>(message);

                    // Игнорируем свои же сообщения
                    if (receivedInfo?.Name != playerName)
                    {
                        Console.WriteLine($"[Получено] {message}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка приёма: {ex.Message}");
                    break;
                }
            }
        }, cts.Token);

        // Завершение по Ctrl+C
        Console.CancelKeyPress += (s, e) =>
        {
            Console.WriteLine("Завершение работы...");
            cts.Cancel();
            e.Cancel = true; // Предотвращаем завершение процесса
        };

        // Отправка сообщений
        try
        {
            while (!cts.Token.IsCancellationRequested)
            {
                var playerInfo = new PlayerInfo
                {
                    Name = playerName,
                    Time = DateTime.Now,
                    X = rand.Next(0, 100),
                    Y = rand.Next(0, 100)
                };

                string json = JsonSerializer.Serialize(playerInfo);
                byte[] data = Encoding.UTF8.GetBytes(json);

                IPEndPoint broadcastEP = new IPEndPoint(IPAddress.Parse("127.0.0.1"), sendToPort);
                await senderClient.SendAsync(data, data.Length, broadcastEP);

                Console.WriteLine($"[Отправлено] {json}");
                await Task.Delay(broadcastIntervalMs, cts.Token);
            }
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("Отправка сообщений остановлена.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка отправки: {ex.Message}");
        }

        await receiveTask; // Дожидаемся завершения задачи приёма
        receiverClient.Dispose();
    }
}
