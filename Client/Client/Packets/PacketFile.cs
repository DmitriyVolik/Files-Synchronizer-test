using System;
using System.Configuration;
using System.IO;
using System.Net.Sockets;

namespace Chat.Packets
{
    public static class PacketFile
    {

        public static void GetFile(string FileName, TcpClient mainClient)
        {
            using (var client = mainClient)
            {
                var stream = client.GetStream();
                PacketSender.SendJsonString(stream, "GET:FILE:" + FileName);
                            
                using (var output = File.Create(ConfigurationManager.AppSettings.Get("MainDirectory")+FileName))
                {
                                
                    Console.WriteLine("Client connected. Starting to receive the file");

                    while (true)
                    {

                        byte[] buffer = new byte[1024];
                        int bytes=0;
                        do
                        {
                            Console.WriteLine("start write");
                            bytes = stream.Read(buffer, 0, buffer.Length);
                            output.Write(buffer, 0, bytes);
                        } while (stream.DataAvailable);

                        if (bytes>0)
                        {
                            break;
                        }
                    }
                }
            }
        }
        
    }
}