using System;
using System.IO;
using System.Net.Sockets;

class TcpImageClient
{
    static void Main()
    {
        Console.Write("Введите имя файла изображения для отправки: ");
        string fileName = Console.ReadLine();

        if (!File.Exists(fileName))
        {
            Console.WriteLine("Файл не найден.");
            return;
        }

        byte[] fileBytes = File.ReadAllBytes(fileName);
        long fileSize = fileBytes.LongLength;

        try
        {
            using TcpClient client = new TcpClient("127.0.0.1", 5000);
            using NetworkStream stream = client.GetStream();

            // Отправляем размер файла (8 байт, long)
            byte[] sizeBytes = BitConverter.GetBytes(fileSize);
            stream.Write(sizeBytes, 0, sizeBytes.Length);

            // Отправляем файл
            stream.Write(fileBytes, 0, fileBytes.Length);
            Console.WriteLine($"Отправлено {fileSize} байт.");

            // Получаем размер файла ответа
            byte[] responseSizeBytes = new byte[8];
            ReadExact(stream, responseSizeBytes, 8);
            long responseSize = BitConverter.ToInt64(responseSizeBytes, 0);

            // Получаем файл ответа
            byte[] responseBytes = new byte[responseSize];
            ReadExact(stream, responseBytes, (int)responseSize);

            // Сохраняем файл
            string outputFileName = "resized_" + Path.GetFileName(fileName);
            File.WriteAllBytes(outputFileName, responseBytes);
            Console.WriteLine($"Получен и сохранён файл: {outputFileName}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка: {ex.Message}");
        }
    }

    static void ReadExact(NetworkStream stream, byte[] buffer, int size)
    {
        int totalRead = 0;
        while (totalRead < size)
        {
            int read = stream.Read(buffer, totalRead, size - totalRead);
            if (read == 0)
                throw new IOException("Соединение закрыто преждевременно");
            totalRead += read;
        }
    }
}