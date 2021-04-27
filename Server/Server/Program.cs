﻿/*using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Server
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            Socket s = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP);
            IPAddress ip = IPAddress.Parse("127.0.0.1");
            IPEndPoint ep = new IPEndPoint(ip, 1024);
            
            try
            {
                s.Bind(ep);
                s.Listen(10);
                Console.WriteLine("Server Start");
                
                while (true)
                {
                    Socket ns = s.Accept();
                    Console.WriteLine(ns.RemoteEndPoint.ToString());
                    

                    //Прием
                    byte[] buffer = new byte[1024];
                    int l; //длина ответа

                    string result = "";
                    int bytes = 0;
                    
                    do
                    {
                        Thread.Sleep(1000);

                        if (ns.Available > 0)
                        {
                            l = ns.Receive(buffer);
                            result += System.Text.Encoding.Unicode.GetString(buffer, 0, l);
                        }
                        if (result=="GET:FILES")
                        {
                            Console.WriteLine(result);
                            result = "";
                            ns.Close();
                        }
                    } while (ns.Connected);
                }
            }
            catch (SocketException ex)
            {
                Console.WriteLine(ex.Message);
            }
            
        }
    }
}*/


using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Net;  
using System.Net.Sockets;  
using System.Text;  
using System.Threading;
using System.Threading.Tasks;
using Server.Files;
using Server.Models;
using Server.Workers;

// State object for reading client data asynchronously  
public class StateObject
{
    // Size of receive buffer.  
    public const int BufferSize = 1024;

    // Receive buffer.  
    public byte[] buffer = new byte[BufferSize];

    // Received data string.
    public StringBuilder sb = new StringBuilder();

    // Client socket.
    public Socket workSocket = null;
}  
  
public class AsynchronousSocketListener
{
    // Thread signal.  
    public static ManualResetEvent allDone = new ManualResetEvent(false);

    public static List<FileM> Files = new List<FileM>();
    
    public AsynchronousSocketListener()
    {
    }

    public static void RefreshFiles()
    {
        while (true)
        {
            var temp = new List<FileM>();
            ScanFiles.ProcessDirectory(MainDirectory, temp);
            Files = temp;
            Thread.Sleep(60000);
        }
    }

    public static async void AsyncRefreshFiles()
    {
        await Task.Run(RefreshFiles);
    }

    public static string MainDirectory= ConfigurationManager.AppSettings.Get("MainDirectory");
    public static void StartListening()
    {
        // Establish the local endpoint for the socket.  
        // The DNS name of the computer  
        // running the listener is "host.contoso.com".  
        IPHostEntry ipHostInfo = Dns.GetHostEntry(Dns.GetHostName());  
        IPAddress ipAddress = IPAddress.Parse("127.0.0.1");
        IPEndPoint localEndPoint = new IPEndPoint(ipAddress, 1024);

        // Create a TCP/IP socket.  
        Socket listener = new Socket(ipAddress.AddressFamily,  
            SocketType.Stream, ProtocolType.Tcp );

        int count = 0;
        
        AsyncRefreshFiles();

        // Bind the socket to the local endpoint and listen for incoming connections.  
        try {  
            listener.Bind(localEndPoint);  
            listener.Listen(100);  
  
            while (true) {
                // Set the event to nonsignaled state.  
                allDone.Reset();  
  
                // Start an asynchronous socket to listen for connections.  
                Console.WriteLine("Waiting for a connection...");  
                listener.BeginAccept(
                    new AsyncCallback(AcceptCallback),  
                    listener );  
  
                // Wait until a connection is made before continuing.  
                allDone.WaitOne(); 
                
            }  
  
        } catch (Exception e) {  
            Console.WriteLine(e.ToString());  
        }  
  
        Console.WriteLine("\nPress ENTER to continue...");  
        Console.Read();  
  
    }

    public static void AcceptCallback(IAsyncResult ar)
    {
        Console.WriteLine("Accept");
        // Signal the main thread to continue.  
        allDone.Set();  
  
        // Get the socket that handles the client request.  
        Socket listener = (Socket) ar.AsyncState;  
        Socket handler = listener.EndAccept(ar);  
  
        // Create the state object.  
        StateObject state = new StateObject();  
        state.workSocket = handler;  
        handler.BeginReceive( state.buffer, 0, StateObject.BufferSize, 0,  
            new AsyncCallback(ReadCallback), state);  
    }

    public static void ReadCallback(IAsyncResult ar)
    {
        String content = String.Empty;  
  
        // Retrieve the state object and the handler socket  
        // from the asynchronous state object.  
        StateObject state = (StateObject) ar.AsyncState;  
        Socket handler = state.workSocket;  
  
        // Read data from the client socket.
        int bytesRead = handler.EndReceive(ar);  
  
        if (bytesRead > 0) {  
            // There  might be more data, so store the data received so far.  
            state.sb.Append(Encoding.Unicode.GetString(  
                state.buffer, 0, bytesRead));  
  
            // Check for end-of-file tag. If it is not there, read
            // more data.  
            content = state.sb.ToString(); 
            
            Console.WriteLine(content);
            
            if (content=="GET:FILES:LIST")
            {
                Console.WriteLine(JsonWorker.FilesToJson(Files));
                Send(handler, JsonWorker.FilesToJson(Files));
                
            } 
            else if (content.Contains("GET:FILE:"))
            {
                string path = content.Replace("GET:FILE:", "");
                Console.WriteLine(MainDirectory+path);

                /*Send(handler, "ddd");*/
                
                handler.SendFile(MainDirectory+path);
                Console.WriteLine("file send!!");
            }
            else {  
                // Not all data received. Get more.  
                handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,  
                    new AsyncCallback(ReadCallback), state);  
            }  
            
            /*if (content.IndexOf("<EOF>") > -1) {  
                // All the data has been read from the
                // client. Display it on the console.  
                Console.WriteLine("Read {0} bytes from socket. \n Data : {1}",  
                    content.Length, content );  
                // Echo the data back to the client.  
                Send(handler, content);  
            } */
        }  
    }

    private static void Send(Socket handler, String data)
    {
        // Convert the string data to byte data using ASCII encoding.  
        byte[] byteData = Encoding.Unicode.GetBytes(data);  
  
        // Begin sending the data to the remote device.  
        handler.BeginSend(byteData, 0, byteData.Length, 0,  
            new AsyncCallback(SendCallback), handler);  
    }
    
    // .BeginSend(bytes, 0, bytes.Length, SocketFlags.None, endSendCallback, clientSocket);
    

    private static void SendCallback(IAsyncResult ar)
    {
        try
        {
            // Retrieve the socket from the state object.  
            Socket handler = (Socket) ar.AsyncState;  
  
            // Complete sending the data to the remote device.  
            int bytesSent = handler.EndSend(ar);  
            Console.WriteLine("Sent {0} bytes to client.", bytesSent);  
  
            handler.Shutdown(SocketShutdown.Both);  
            handler.Close();  
  
        }
        catch (Exception e)
        {
            Console.WriteLine(e.ToString());  
        }  
    }

    public static int Main(String[] args)
    {
        StartListening();  
        return 0;  
    }
}