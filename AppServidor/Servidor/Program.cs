using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;

class UDPServer
{
    private const int Port = 12345; // Porta usada pelo servidor
    private const string StorageFolder = "ServerFiles"; // Pasta para armazenar arquivos recebidos
    private const int PacketSize = 1024; // Tamanho do pacote em bytes
    private static Dictionary<int, byte[]> receivedPackets = new Dictionary<int, byte[]>(); // Buffer de pacotes recebidos

    public static void Main()
    {
        if (!Directory.Exists(StorageFolder))
        {
            Directory.CreateDirectory(StorageFolder); // Cria a pasta de armazenamento se não existir
        }

        UdpClient udpServer = new UdpClient(Port); // Cria um servidor UDP na porta especificada
        Console.WriteLine($"Servidor UDP iniciado na porta {Port}");

        while (true)
        {
            IPEndPoint remoteEP = null;
            byte[] data = udpServer.Receive(ref remoteEP); // Recebe dados do cliente
            string receivedData = Encoding.UTF8.GetString(data); // Converte os dados recebidos em string
            Console.WriteLine($"Dados recebidos de {remoteEP}: {receivedData}");

            string[] commandParts = receivedData.Split(' '); // Divide a string em partes usando espaço como delimitador
            string command = commandParts[0]; // Obtém o comando da string

            switch (command)
            {
                case "UPLOAD":
                    int packetNumber = int.Parse(commandParts[2]); // Número do pacote
                    ReceiveFile(udpServer, remoteEP, commandParts[1], packetNumber); // Chama o método para receber o arquivo
                    break;
                case "LIST":
                    SendFileList(udpServer, remoteEP); // Chama o método para enviar a lista de arquivos
                    break;
                case "DOWNLOAD":
                    SendFile(udpServer, remoteEP, commandParts[1]); // Chama o método para enviar o arquivo solicitado
                    break;
                default:
                    Console.WriteLine("Comando desconhecido."); // Comando inválido
                    break;
            }
        }
    }

    private static void ReceiveFile(UdpClient udpServer, IPEndPoint remoteEP, string fileName, int packetNumber)
    {
        byte[] fileData = udpServer.Receive(ref remoteEP); // Recebe os dados do arquivo do cliente
        receivedPackets[packetNumber] = fileData; // Armazena o pacote recebido no buffer

        // Envia ACK de confirmação de recebimento
        byte[] ack = Encoding.UTF8.GetBytes($"ACK {packetNumber}");
        udpServer.Send(ack, ack.Length, remoteEP);

        // Se todos os pacotes forem recebidos, monta o arquivo
        if (AllPacketsReceived())
        {
            using (var fileStream = new FileStream(Path.Combine(StorageFolder, fileName), FileMode.Create, FileAccess.Write))
            {
                foreach (var packet in receivedPackets)
                {
                    fileStream.Write(packet.Value, 0, packet.Value.Length); // Escreve os dados do arquivo na pasta de armazenamento
                }
            }
            receivedPackets.Clear(); // Limpa o buffer
            Console.WriteLine($"Arquivo {fileName} recebido e armazenado.");
        }
    }

    private static bool AllPacketsReceived()
    {
        // Verifica se todos os pacotes foram recebidos (simplesmente verifica se não há buracos na sequência)
        // Para simplificação, assumimos que os pacotes começam em 0 e são consecutivos.
        int expectedPacketCount = receivedPackets.Keys.Count;
        for (int i = 0; i < expectedPacketCount; i++)
        {
            if (!receivedPackets.ContainsKey(i))
            {
                return false;
            }
        }
        return true;
    }

    private static void SendFileList(UdpClient udpServer, IPEndPoint remoteEP)
    {
        string[] files = Directory.GetFiles(StorageFolder); // Obtém a lista de arquivos na pasta de armazenamento
        string fileList = string.Join(",", files); // Concatena os nomes dos arquivos em uma única string
        byte[] data = Encoding.UTF8.GetBytes(fileList); // Converte a lista de arquivos para bytes
        udpServer.Send(data, data.Length, remoteEP); // Envia a lista de arquivos para o cliente
        Console.WriteLine("Lista de arquivos enviada.");
    }

    private static void SendFile(UdpClient udpServer, IPEndPoint remoteEP, string fileName)
    {
        string filePath = Path.Combine(StorageFolder, fileName); // Obtém o caminho completo do arquivo
        if (File.Exists(filePath))
        {
            byte[] fileData = File.ReadAllBytes(filePath); // Lê os dados do arquivo
            udpServer.Send(fileData, fileData.Length, remoteEP); // Envia os dados do arquivo para o cliente
            Console.WriteLine($"Arquivo {fileName} enviado.");
        }
        else
        {
            byte[] data = Encoding.UTF8.GetBytes("Arquivo não encontrado."); // Mensagem de erro se o arquivo não for encontrado
            udpServer.Send(data, data.Length, remoteEP); // Envia a mensagem de erro para o cliente
            Console.WriteLine("Arquivo não encontrado.");
        }
    }
}
