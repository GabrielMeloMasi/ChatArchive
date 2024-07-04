using System;
using System.IO;
using System.Net.Sockets;
using System.Text;

public class FileClient
{
    private const string serverAddress = "192.168.0.117";
    private const int port = 12345;
    private const string clientFilesDirectory = @"C:\Users\masig\Desktop\Projects\ChatArchive\FileClient";
    private const int bufferSize = 1024; // Tamanho do buffer de envio

    public static void Main()
    {
        if (!Directory.Exists(clientFilesDirectory))
        {
            Directory.CreateDirectory(clientFilesDirectory);
        }

        while (true)
        {
            Console.WriteLine("1. Upload de arquivo");
            Console.WriteLine("2. Listar arquivos do Servidor");
            Console.WriteLine("3. Download de arquivo");
            Console.WriteLine("4. Sair");
            Console.Write("Selecione: ");
            string option = Console.ReadLine();

            switch (option)
            {
                case "1":
                    Console.Write("Envie o caminho completo do arquivo: ");
                    string filePath = Console.ReadLine();
                    UploadFile(filePath);
                    break;
                case "2":
                    ListFiles();
                    break;
                case "3":
                    Console.Write("Envie o nome do arquivo para Download: ");
                    string fileName = Console.ReadLine();
                    DownloadFile(fileName);
                    break;
                case "4":
                    Console.WriteLine("Saindo...");
                    return;
                default:
                    Console.WriteLine("Opção Inválida.");
                    break;
            }
        }
    }

    private static void UploadFile(string filePath)
    {
        try
        {
            string fileName = Path.GetFileName(filePath);
            TcpClient client = new TcpClient(serverAddress, port);
            NetworkStream stream = client.GetStream();
            StreamWriter writer = new StreamWriter(stream);
            StreamReader reader = new StreamReader(stream);

            writer.WriteLine("UPLOAD " + fileName);
            writer.Flush();

            byte[] fileBytes = File.ReadAllBytes(filePath);
            int totalBlocks = (int)Math.Ceiling((double)fileBytes.Length / bufferSize);

            for (int i = 0; i < totalBlocks; i++)
            {
                int start = i * bufferSize;
                int length = Math.Min(bufferSize, fileBytes.Length - start);
                byte[] block = new byte[length];
                Array.Copy(fileBytes, start, block, 0, length);

                writer.WriteLine("BLOCK " + i);
                writer.Flush();
                stream.Write(block, 0, length);
                stream.Flush();

                string ack = reader.ReadLine();
                if (ack != "ACK " + i)
                {
                    throw new Exception("Erro na confirmação do bloco " + i);
                }
            }

            writer.WriteLine("EOF");
            writer.Flush();

            Console.WriteLine($"Arquivo '{fileName}' enviado com sucesso.");
            client.Close();
        }
        catch (Exception ex)
        {
            Console.WriteLine("Erro para upload de arquivo: " + ex.Message);
        }
    }

    private static void ListFiles()
    {
        try
        {
            TcpClient client = new TcpClient(serverAddress, port);
            NetworkStream stream = client.GetStream();
            StreamWriter writer = new StreamWriter(stream);
            StreamReader reader = new StreamReader(stream);

            writer.WriteLine("LIST");
            writer.Flush();

            string fileList = reader.ReadToEnd();
            Console.WriteLine("Arquivos do servidor:");
            Console.WriteLine(fileList);

            client.Close();
        }
        catch (Exception ex)
        {
            Console.WriteLine("Erro para listar arquivos: " + ex.Message);
        }
    }

    private static void DownloadFile(string fileName)
    {
        try
        {
            TcpClient client = new TcpClient(serverAddress, port);
            NetworkStream stream = client.GetStream();
            StreamWriter writer = new StreamWriter(stream);

            writer.WriteLine("DOWNLOAD " + fileName);
            writer.Flush();

            string filePath = Path.Combine(clientFilesDirectory, fileName);
            byte[] buffer = new byte[bufferSize];
            int bytesRead;

            using (FileStream fileStream = File.Create(filePath))
            {
                while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    fileStream.Write(buffer, 0, bytesRead);
                }
            }

            Console.WriteLine($"Arquivo '{fileName}' baixado com sucesso de '{clientFilesDirectory}'.");
            client.Close();
        }
        catch (Exception ex)
        {
            Console.WriteLine("Erro para baixar arquivo: " + ex.Message);
        }
    }
}
