using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using Chat.Packets;
using Client.Files;
using Client.Models;
using Client.Workers;

namespace Client
{
    internal class Program
    {
        public static TcpClient getClient()
        {
            return new TcpClient("127.0.0.1", 1024);
        }

        public static void Main(string[] args)
        {
            var mainDir = ConfigurationManager.AppSettings.Get("MainDirectory");

            while (true)
            {
                var serverFiles = new List<FileM>();
                string data;


                using (var client = getClient())
                {
                    var stream = client.GetStream();
                    PacketSender.SendJsonString(stream, "GET:FILES:LIST");

                    while (true)
                    {
                        data = PacketRecipient.GetJsonData(stream);

                        if (data != null) break;

                        Thread.Sleep(100);
                    }
                }

                serverFiles = JsonWorker.JsonToFiles(data);
                var files = new List<FileM>();

                ScanFiles.ProcessDirectory(mainDir, files);
                
                /*Console.WriteLine("---------------files-----------------");
                foreach (var item in files)
                {
                    Console.WriteLine(item.Path);
                }
                Console.WriteLine("--------------server------------------");
                foreach (var item in serverFiles)
                {
                    Console.WriteLine(item.Path);
                }*/
                

                for (var i = 0; i < serverFiles.Count; i++)
                {
                    if (i >= files.Count)
                    {
                        PacketFile.GetFile(serverFiles[i].Path, getClient());
                        continue;
                    }

                    if (serverFiles[i].Path == files[i].Path)
                    {
                        string hash = CreateFileHash.CreateMD5(mainDir+files[i].Path);

                        if (hash!=serverFiles[i].Hash)
                        {
                            File.Delete(mainDir+files[i].Path);
                            PacketFile.GetFile(serverFiles[i].Path, getClient());
                        }
                    }
                    else
                    {
                        File.Delete(mainDir+files[i].Path);
                        PacketFile.GetFile(serverFiles[i].Path, getClient());
                    }
                }

                Dirs.ClearEmptyDirs(mainDir);
                
                Thread.Sleep(60000);
            }
        }
    }
}