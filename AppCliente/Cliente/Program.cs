using System;
using System.IO;
using System.Net.Sockets;
using System.Text;

public class FileClient
{
    // Endereço do servidor e porta de conexão
    private const string serverAddress = "192.168.0.117";
    private const int port = 12345;
    // Diretório onde os arquivos serão salvos localmente no cliente
    private const string clientFilesDirectory = @"C:\Users\masig\Desktop\Projects\ChatArchive\FileClient";
    // Tamanho do buffer de envio
    private const int bufferSize = 1024;

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
            Console.WriteLine("2. Listar arquivos do Servidor");
            Console.WriteLine("3. Download de arquivo");
            Console.WriteLine("4. Sair");
            Console.Write("Selecione: ");
            // Lê a opção selecionada pelo usuário
            string option = Console.ReadLine();

            // Executa a operação correspondente com base na opção selecionada
            switch (option)
            {
                case "1":
                    // Solicita o caminho do arquivo para upload
                    Console.Write("Envie o caminho completo do arquivo: ");
                    string filePath = Console.ReadLine();
                    // Chama o método para upload do arquivo
                    UploadFile(filePath);
                    break;
                case "2":
                    // Chama o método para listar arquivos do servidor
                    ListFiles();
                    break;
                case "3":
                    // Solicita o nome do arquivo para download
                    Console.Write("Envie o nome do arquivo para Download: ");
                    string fileName = Console.ReadLine();
                    // Chama o método para download do arquivo
                    DownloadFile(fileName);
                    break;
                case "4":
                    // Encerra o programa
                    Console.WriteLine("Saindo...");
                    return;
                default:
                    // Informa opção inválida
                    Console.WriteLine("Opção Inválida.");
                    break;
            }
        }
    }

    private static void UploadFile(string filePath)
    {
        try
        {
            // Obtém o nome do arquivo a partir do caminho completo
            string fileName = Path.GetFileName(filePath);
            // Cria um cliente TCP conectando ao servidor na porta especificada
            TcpClient client = new TcpClient(serverAddress, port);
            NetworkStream stream = client.GetStream();
            StreamWriter writer = new StreamWriter(stream);
            StreamReader reader = new StreamReader(stream);

            // Envia a requisição UPLOAD seguida do nome do arquivo
            writer.WriteLine("UPLOAD " + fileName);
            writer.Flush();

            // Lê os bytes do arquivo
            byte[] fileBytes = File.ReadAllBytes(filePath);
            int totalBlocks = (int)Math.Ceiling((double)fileBytes.Length / bufferSize);

            // Envia o arquivo em blocos numerados
            for (int i = 0; i < totalBlocks; i++)
            {
                int start = i * bufferSize;
                int length = Math.Min(bufferSize, fileBytes.Length - start);
                byte[] block = new byte[length];
                Array.Copy(fileBytes, start, block, 0, length);

                // Envia o número do bloco
                writer.WriteLine("BLOCK " + i);
                writer.Flush();
                // Envia o bloco de dados
                stream.Write(block, 0, length);
                stream.Flush();

                // Lê a confirmação de recebimento (ACK) do servidor
                string ack = reader.ReadLine();
                if (ack != "ACK " + i)
                {
                    throw new Exception("Erro na confirmação do bloco " + i);
                }
            }

            // Informa o fim do arquivo
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
            // Cria um cliente TCP conectando ao servidor na porta especificada
            TcpClient client = new TcpClient(serverAddress, port);
            NetworkStream stream = client.GetStream();
            StreamWriter writer = new StreamWriter(stream);
            StreamReader reader = new StreamReader(stream);

            // Envia a requisição LIST para obter a lista de arquivos no servidor
            writer.WriteLine("LIST");
            writer.Flush();

            // Lê a resposta do servidor que contém a lista de arquivos
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
            // Cria um cliente TCP conectando ao servidor na porta especificada
            TcpClient client = new TcpClient(serverAddress, port);
            NetworkStream stream = client.GetStream();
            StreamWriter writer = new StreamWriter(stream);

            // Envia a requisição DOWNLOAD seguida do nome do arquivo desejado
            writer.WriteLine("DOWNLOAD " + fileName);
            writer.Flush();

            // Caminho onde o arquivo será salvo localmente
            string filePath = Path.Combine(clientFilesDirectory, fileName);
            byte[] buffer = new byte[bufferSize];
            int bytesRead;

            // Cria um novo arquivo e escreve os dados recebidos do servidor
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
