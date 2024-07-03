using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;

class UDPClient
{
    private const int ServerPort = 12345; // Porta do servidor
    private const string ServerAddress = "127.0.0.1"; // Endereço IP do servidor
    private const int PacketSize = 1024; // Tamanho do pacote em bytes

    public static void Main()
    {
        UdpClient udpClient = new UdpClient(); // Cria um cliente UDP
        IPEndPoint serverEP = new IPEndPoint(IPAddress.Parse(ServerAddress), ServerPort); // Define o endereço do servidor

        Console.WriteLine("Cliente UDP iniciado.");

        while (true)
        {
            Console.WriteLine("Digite um comando (UPLOAD, LIST, DOWNLOAD, EXIT):"); // Solicita o comando ao usuário
            string command = Console.ReadLine(); // Lê o comando do usuário
            string[] commandParts = command.Split(' '); // Divide o comando em partes

            switch (commandParts[0])
            {
                case "UPLOAD":
                    UploadFile(udpClient, serverEP, commandParts[1]); // Chama o método para enviar um arquivo
                    break;
                case "LIST":
                    RequestFileList(udpClient, serverEP); // Chama o método para solicitar a lista de arquivos
                    break;
                case "DOWNLOAD":
                    DownloadFile(udpClient, serverEP, commandParts[1]); // Chama o método para baixar um arquivo
                    break;
                case "EXIT":
                    return; // Sai do loop e encerra o cliente
                default:
                    Console.WriteLine("Comando desconhecido."); // Comando inválido
                    break;
            }
        }
    }

    private static void UploadFile(UdpClient udpClient, IPEndPoint serverEP, string filePath)
    {
        if (File.Exists(filePath))
        {
            byte[] fileData = File.ReadAllBytes(filePath); // Lê os dados do arquivo local
            string fileName = Path.GetFileName(filePath); // Obtém o nome do arquivo

            // Envia os dados em pacotes
            int packetNumber = 0;
            for (int i = 0; i < fileData.Length; i += PacketSize)
            {
                int size = Math.Min(PacketSize, fileData.Length - i);
                byte[] packetData = new byte[size];
                Array.Copy(fileData, i, packetData, 0, size);

                byte[] commandData = Encoding.UTF8.GetBytes($"UPLOAD {fileName} {packetNumber}");
                udpClient.Send(commandData, commandData.Length, serverEP); // Envia o comando para o servidor
                udpClient.Send(packetData, packetData.Length, serverEP); // Envia os dados do pacote para o servidor

                // Aguarda ACK do servidor
                byte[] ackData = udpClient.Receive(ref serverEP);
                string ack = Encoding.UTF8.GetString(ackData);
                if (ack == $"ACK {packetNumber}")
                {
                    Console.WriteLine($"Pacote {packetNumber} enviado e confirmado.");
                }
                else
                {
                    Console.WriteLine($"Falha ao enviar o pacote {packetNumber}. Retentando...");
                    i -= PacketSize; // Retorna para retransmitir o pacote
                }

                packetNumber++;
            }

            Console.WriteLine($"Arquivo {fileName} enviado.");
        }
        else
        {
            Console.WriteLine("Arquivo não encontrado."); // Mensagem de erro se o arquivo não for encontrado
        }
    }

    private static void RequestFileList(UdpClient udpClient, IPEndPoint serverEP)
    {
        byte[] commandData = Encoding.UTF8.GetBytes("LIST"); // Prepara o comando para solicitar a lista de arquivos
        udpClient.Send(commandData, commandData.Length, serverEP); // Envia o comando para o servidor

        byte[] buffer = udpClient.Receive(ref serverEP); // Recebe a lista de arquivos do servidor
        string fileList = Encoding.UTF8.GetString(buffer); // Converte os dados recebidos em string
        Console.WriteLine($"Arquivos no servidor: {fileList}"); // Exibe a lista de arquivos
    }

    private static void DownloadFile(UdpClient udpClient, IPEndPoint serverEP, string fileName)
    {
        byte[] commandData = Encoding.UTF8.GetBytes($"DOWNLOAD {fileName}"); // Prepara o comando de download
        udpClient.Send(commandData, commandData.Length, serverEP); // Envia o comando para o servidor

        byte[] fileData = udpClient.Receive(ref serverEP); // Recebe os dados do arquivo do servidor

        if (Encoding.UTF8.GetString(fileData) == "Arquivo não encontrado.")
        {
            Console.WriteLine("Arquivo não encontrado no servidor."); // Mensagem de erro se o arquivo não for encontrado no servidor
        }
        else
        {
            File.WriteAllBytes(fileName, fileData); // Salva os dados do arquivo localmente
            Console.WriteLine($"Arquivo {fileName} baixado.");
        }
    }
}
