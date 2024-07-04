using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

public class FileServer
{
    private const int port = 12345;
    private const string filesDirectory = @"C:\Users\masig\Desktop\Projects\ChatArchive\FileServer";
    private const int bufferSize = 1024; // Tamanho do buffer de recebimento

    public static void Main()
    {
        if (!Directory.Exists(filesDirectory))
        {
            Directory.CreateDirectory(filesDirectory);
        }

        Console.WriteLine("Servidor está rodando...");
        Console.WriteLine($"Listado na porta {port}");

        TcpListener listener = new TcpListener(IPAddress.Any, port);
        listener.Start();

        try
        {
            while (true)
            {
                TcpClient client = listener.AcceptTcpClient();
                Console.WriteLine("Cliente conectado");

                Task.Run(() => HandleClient(client));
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error: " + ex.Message);
        }
    }

    private static void HandleClient(TcpClient client)
    {
        try
        {
            NetworkStream stream = client.GetStream();
            StreamReader reader = new StreamReader(stream);
            StreamWriter writer = new StreamWriter(stream);

            string request = reader.ReadLine();
            Console.WriteLine("Request received: " + request);

            if (request.StartsWith("UPLOAD"))
            {
                string fileName = request.Substring(7); // Remove "UPLOAD "
                ReceiveFile(fileName, stream, reader, writer);
            }
            else if (request == "LIST")
            {
                SendFileList(writer);
            }
            else if (request.StartsWith("DOWNLOAD"))
            {
                string fileName = request.Substring(9); // Remove "DOWNLOAD "
                SendFile(fileName, stream);
            }

            client.Close();
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error handling client: " + ex.Message);
        }
    }

    private static void ReceiveFile(string fileName, NetworkStream stream, StreamReader reader, StreamWriter writer)
    {
        try
        {
            string filePath = Path.Combine(filesDirectory, fileName);
            using (FileStream fileStream = File.Create(filePath))
            {
                string line;
                while ((line = reader.ReadLine()) != "EOF")
                {
                    if (line.StartsWith("BLOCK"))
                    {
                        int blockNumber = int.Parse(line.Substring(6)); // Remove "BLOCK "
                        byte[] buffer = new byte[bufferSize];
                        int bytesRead = stream.Read(buffer, 0, buffer.Length);
                        fileStream.Write(buffer, 0, bytesRead);

                        writer.WriteLine("ACK " + blockNumber);
                        writer.Flush();
                    }
                }
            }

            Console.WriteLine($"Arquivo '{fileName}' recebido e salvo.");
        }
        catch (Exception ex)
        {
            Console.WriteLine("Erro para receber arquivo: " + ex.Message);
        }
    }

    private static void SendFileList(StreamWriter writer)
    {
        try
        {
            string[] files = Directory.GetFiles(filesDirectory);
            foreach (string file in files)
            {
                writer.WriteLine(Path.GetFileName(file));
            }
            writer.Flush();
        }
        catch (Exception ex)
        {
            Console.WriteLine("Erro para enviar lista de arquivos: " + ex.Message);
        }
    }

    private static void SendFile(string fileName, NetworkStream stream)
    {
        try
        {
            string filePath = Path.Combine(filesDirectory, fileName);

            if (File.Exists(filePath))
            {
                byte[] fileBytes = File.ReadAllBytes(filePath);
                stream.Write(fileBytes, 0, fileBytes.Length);
                Console.WriteLine($"Arquivo '{fileName}' enviado para o cliente.");
            }
            else
            {
                Console.WriteLine($"Arquivo '{fileName}' não encontrado.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Erro para enviar arquivo: " + ex.Message);
        }
    }
}
