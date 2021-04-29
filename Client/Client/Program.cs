﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
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

                foreach (var item in serverFiles)
                {

                    if (item.Size == 0)
                    {
                        continue;
                    }

                    FileM selectedItem = files.FirstOrDefault(x => x.Path == item.Path);
                    
                    if (selectedItem == null)
                    {
                        PacketFile.GetFile(item.Path);
                        continue;
                    }
                    else
                    {
                        Console.WriteLine(selectedItem.Path);
                    }

                    if (item.Size != selectedItem.Size)
                    {
                        File.Delete(mainDir + selectedItem.Path);
                        PacketFile.GetFile(item.Path);
                    }
                    else
                    {
                        string hash;

                        try
                        {
                            hash = CreateFileHash.CreateMD5(mainDir + selectedItem.Path);
                        }
                        catch (System.IO.IOException)
                        {
                            continue;
                        }

                        if (hash != item.Hash)
                        {
                            File.Delete(mainDir + selectedItem.Path);
                            PacketFile.GetFile(item.Path);
                        }
                    }
                    
                    foreach (var i in files)
                    {
                        if (!serverFiles.Exists(x=> x.Path==i.Path))
                        {
                            File.Delete(mainDir+i.Path);
                            
                        }
                        
                    }

                    Dirs.ClearEmptyDirs(mainDir);
                }
                Thread.Sleep(5000);
            }
        }
    }
}