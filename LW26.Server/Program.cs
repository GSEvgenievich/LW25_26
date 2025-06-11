using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;

class TcpImageServer
{
    static void Main()
    {
        TcpListener listener = new TcpListener(IPAddress.Any, 5000);
        listener.Start();
        Console.WriteLine("Сервер запущен, ожидание клиентов...");

        while (true)
        {
            TcpClient client = listener.AcceptTcpClient();
            Console.WriteLine("Клиент подключился.");
            ThreadPool.QueueUserWorkItem(HandleClient, client);
        }
    }

    static void HandleClient(object obj)
    {
        TcpClient client = (TcpClient)obj;
        try
        {
            using NetworkStream stream = client.GetStream();

            // Читаем размер файла (8 байт)
            byte[] sizeBytes = new byte[8];
            ReadExact(stream, sizeBytes, 8);
            long fileSize = BitConverter.ToInt64(sizeBytes, 0);

            // Читаем файл
            byte[] imageBytes = new byte[fileSize];
            ReadExact(stream, imageBytes, (int)fileSize);
            Console.WriteLine($"Получено изображение размером {fileSize} байт.");

            // Обработка изображения: сжатие в 2 раза
            byte[] resizedBytes = ResizeImage(imageBytes);

            // Отправляем размер результата
            byte[] resizedSizeBytes = BitConverter.GetBytes(resizedBytes.LongLength);
            stream.Write(resizedSizeBytes, 0, resizedSizeBytes.Length);
            // Отправляем сжатое изображение
            stream.Write(resizedBytes, 0, resizedBytes.Length);
            Console.WriteLine($"Отправлено сжатое изображение размером {resizedBytes.Length} байт.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка обработки клиента: {ex.Message}");
        }
        finally
        {
            client.Close();
        }
    }

    static byte[] ResizeImage(byte[] imageBuffer)
    {
        using var inputStream = new MemoryStream(imageBuffer);
        using var originalImage = new Bitmap(inputStream);

        int newWidth = originalImage.Width / 2;
        int newHeight = originalImage.Height / 2;

        using var resizedImage = new Bitmap(newWidth, newHeight);
        using (var graphics = Graphics.FromImage(resizedImage))
        {
            graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            graphics.DrawImage(originalImage, 0, 0, newWidth, newHeight);
        }

        using var outputStream = new MemoryStream();
        resizedImage.Save(outputStream, ImageFormat.Jpeg);
        return outputStream.ToArray();
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