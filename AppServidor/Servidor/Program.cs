using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

public class FileServer
{
    // Porta de conexão do servidor
    private const int port = 12345;
    // Diretório onde os arquivos serão salvos no servidor
    private const string filesDirectory = @"C:\Users\masig\Desktop\Projects\ChatArchive\FileServer";
    // Tamanho do buffer de recebimento
    private const int bufferSize = 1024;

    public static void Main()
    {
        // Verifica se o diretório de arquivos existe; se não, cria.
        if (!Directory.Exists(filesDirectory))
        {
            Directory.CreateDirectory(filesDirectory);
        }

        Console.WriteLine("Servidor está rodando...");
        Console.WriteLine($"Listado na porta {port}");

        // Cria um listener TCP na porta especificada
        TcpListener listener = new TcpListener(IPAddress.Any, port);
        listener.Start();

        try
        {
            while (true)
            {
                // Aceita conexões dos clientes
                TcpClient client = listener.AcceptTcpClient();
                Console.WriteLine("Cliente conectado");

                // Cria uma tarefa para manipular o cliente em paralelo
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
            // Obtém o stream de rede para comunicação com o cliente
            NetworkStream stream = client.GetStream();
            StreamReader reader = new StreamReader(stream);
            StreamWriter writer = new StreamWriter(stream);

            // Lê a requisição do cliente
            string request = reader.ReadLine();
            Console.WriteLine("Request received: " + request);

            if (request.StartsWith("UPLOAD"))
            {
                // Extrai o nome do arquivo da requisição
                string fileName = request.Substring(7); // Remove "UPLOAD "
                // Recebe o arquivo enviado pelo cliente
                ReceiveFile(fileName, stream, reader, writer);
            }
            else if (request == "LIST")
            {
                // Envia a lista de arquivos disponíveis para o cliente
                SendFileList(writer);
            }
            else if (request.StartsWith("DOWNLOAD"))
            {
                // Extrai o nome do arquivo da requisição
                string fileName = request.Substring(9); // Remove "DOWNLOAD "
                // Envia o arquivo solicitado para o cliente
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
            // Caminho completo do arquivo no diretório de arquivos
            string filePath = Path.Combine(filesDirectory, fileName);

            // Cria um novo arquivo e recebe os dados do cliente
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

                        // Envia a confirmação de recebimento (ACK)
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
            // Obtém a lista de arquivos no diretório de arquivos
            string[] files = Directory.GetFiles(filesDirectory);
            foreach (string file in files)
            {
                // Escreve o nome de cada arquivo na stream para o cliente
                writer.WriteLine(Path.GetFileName(file));
            }
            writer.Flush(); // Garante que todos os dados sejam enviados
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
            // Caminho completo do arquivo no diretório de arquivos
            string filePath = Path.Combine(filesDirectory, fileName);

            if (File.Exists(filePath))
            {
                // Lê os bytes do arquivo e os envia para o cliente
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
