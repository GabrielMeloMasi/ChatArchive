using System;
using System.IO;
using System.Net.Sockets;
using System.Text;

public class FileClient
{
    private const string serverAddress = "10.199.65.65";
    private const int port = 12345;
    private const string clientFilesDirectory = @"C:\Users\2568455\Desktop\Projeto Vale\projects\projetos-pessoais\ChatArchive\FileClient";

    public static void Main()
    {
        // Verifica se o diretório do cliente existe; se não, cria.
        if (!Directory.Exists(clientFilesDirectory))
        {
            Directory.CreateDirectory(clientFilesDirectory);
        }

        while (true)
        {
            // Menu de opções para o cliente.
            Console.WriteLine("1. Upload de arquivo");
            Console.WriteLine("2. Listas de arquivos do Servirdor");
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
            // Obtém o nome do arquivo a partir do caminho completo.
            string fileName = Path.GetFileName(filePath);
            // Cria um cliente TCP conectando ao servidor na porta especificada.
            TcpClient client = new TcpClient(serverAddress, port);
            NetworkStream stream = client.GetStream();

            // Envia a requisição UPLOAD seguida do nome do arquivo.
            StreamWriter writer = new StreamWriter(stream);
            writer.WriteLine("UPLOAD " + fileName);
            writer.Flush();

            // Lê os bytes do arquivo e os envia para o servidor.
            byte[] fileBytes = File.ReadAllBytes(filePath);
            stream.Write(fileBytes, 0, fileBytes.Length);

            Console.WriteLine($"Arquivo '{fileName}' enviado com sucesso.");

            // Fecha a conexão com o servidor.
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
            // Cria um cliente TCP conectando ao servidor na porta especificada.
            TcpClient client = new TcpClient(serverAddress, port);
            NetworkStream stream = client.GetStream();

            // Envia a requisição LIST para obter a lista de arquivos no servidor.
            StreamWriter writer = new StreamWriter(stream);
            writer.WriteLine("LIST");
            writer.Flush();

            // Lê a resposta do servidor que contém a lista de arquivos.
            StreamReader reader = new StreamReader(stream);
            string fileList = reader.ReadToEnd();

            Console.WriteLine("Arquivos do servidor:");
            Console.WriteLine(fileList);

            // Fecha a conexão com o servidor.
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
            // Cria um cliente TCP conectando ao servidor na porta especificada.
            TcpClient client = new TcpClient(serverAddress, port);
            NetworkStream stream = client.GetStream();

            // Envia a requisição DOWNLOAD seguida do nome do arquivo desejado.
            StreamWriter writer = new StreamWriter(stream);
            writer.WriteLine("DOWNLOAD " + fileName);
            writer.Flush();

            // Caminho onde o arquivo será salvo localmente.
            string filePath = Path.Combine(clientFilesDirectory, fileName);
            byte[] buffer = new byte[1024];
            int bytesRead;

            // Cria um novo arquivo e escreve os dados recebidos do servidor.
            using (FileStream fileStream = File.Create(filePath))
            {
                while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    fileStream.Write(buffer, 0, bytesRead);
                }
            }

            Console.WriteLine($"Arquivo '{fileName}' baixado com sucesso de '{clientFilesDirectory}'.");

            // Fecha a conexão com o servidor.
            client.Close();
        }
        catch (Exception ex)
        {
            Console.WriteLine("Erro para baixar arquivo: " + ex.Message);
        }
    }
}
