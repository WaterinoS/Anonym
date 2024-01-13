using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Diagnostics;
using System.Web;
using System.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Net.Sockets;
using System.Net.Security;
using System.Net.WebSockets;
using System.Threading;
using System.IO;
using FiddlerCore;
using Fiddler;
using Newtonsoft.Json;
using System.Runtime.InteropServices;

namespace Anonym
{
    class Program
    {
        static bool tcp_started = false;
        static List<string> blacklist = new List<string>();
        //static List<string> proxylist = new List<string>();
        static StreamWriter w;
        static bool Loadblacklist(string file)
        {
            if (!File.Exists(file)) return false;

            StreamReader weblist = new StreamReader(file);
            while (!weblist.EndOfStream)
               blacklist.Add(weblist.ReadLine());

            return true;
        }
        //static bool LoadProxyList(string file)
        //{
        //    if (!File.Exists(file)) return false;

        //    StreamReader _proxylist = new StreamReader(file);
        //    while (!_proxylist.EndOfStream)
        //        proxylist.Add(_proxylist.ReadLine());

        //    return true;
        //}

        static void FiddlerApplication_BeforeResponse(Session sess)
        {
            var output = "[Proceed] > " + sess.fullUrl;
            output += " [" + sess.m_hostIP + "]";
            output += Environment.NewLine;

            bool found = false;
            foreach (var item in blacklist)
            {
                if (sess.fullUrl.Contains(item))
                {
                    found = true;
                    Console.Title = "HTTP Requests Blocker | Coded by WaterSmoke | [" + sess.m_clientIP + ":" + sess.m_clientPort + "] " + "-> " + sess.fullUrl;
                    string headers = sess.oRequest.headers.ToString();

                    if (output.Length > 3 && output.Length < 64)
                    {
                        Console.Write(output);
                    }
                    w.Write(output);
                }
            }

            if (found)
            {
                output = "[Blocked] > " + sess.fullUrl;
                output += " [" + sess.m_hostIP + "]";
                output += Environment.NewLine;
                if (output.Length > 3 && output.Length < 256)
                {
                    Console.Write(output);
                }
                w.Write(output);
                sess.oRequest.headers.RemoveAll();
                return;
            }
        }

        static void FiddlerApplication_BeforeRequest(Session sess)
        {
            //if (sess == null || sess.oRequest == null || sess.oRequest.headers == null)
            //     return;

           // if (!(sess.RequestMethod == "CONNECT"))
            {
                var output = "[Proceed] > " + sess.fullUrl;
                output += " [" + sess.m_hostIP + "]";
                output += Environment.NewLine;

                bool found = false;
                foreach (var item in blacklist)
                {
                    if (sess.fullUrl.Contains(item))
                    {
                        found = true;
                        Console.Title = "HTTP Requests Blocker | Coded by WaterSmoke | [" + sess.m_clientIP + ":" + sess.m_clientPort + "] " + "-> " + sess.fullUrl;
                        string headers = sess.oRequest.headers.ToString();

                        if (output.Length > 3 && output.Length < 64)
                        {
                            Console.Write(output);
                        }
                        w.Write(output);
                    }
                }

                if (found)
                {
                    output = "[Blocked] > " + sess.fullUrl;
                    output += " [" + sess.m_hostIP + "]";
                    output += Environment.NewLine;
                    if (output.Length > 3 && output.Length < 256)
                    {
                        Console.Write(output);
                    }
                    w.Write(output);
                    sess.oRequest.headers.RemoveAll();
                    return;
                }
            }
        }

        public static bool InstallCertificate()
        {
            if (!CertMaker.rootCertExists())
            {
                if (!CertMaker.createRootCert())
                    return false;

                if (!CertMaker.trustRootCert())
                    return false;
            }

            System.Net.ServicePointManager.ServerCertificateValidationCallback += delegate (
            object sender,
            X509Certificate cert,
            X509Chain chain,
            SslPolicyErrors sslPolicyErrors)
            {
                if (sslPolicyErrors == SslPolicyErrors.None)
                {
                    return true;   //Is valid
                }

                if (cert.GetCertHashString() == "99E92D8447AEF30483B1D7527812C9B7B3A915A7")
                {
                    return true;
                }

                return false;
            };

            return true;
        }


        static void Console_CancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            if (tcp_started)
            {
                tcp_started = false;
                w.Close();
                FiddlerApplication.oProxy.Detach();
                FiddlerApplication.Shutdown();
                Console.WriteLine("> Started.");
            }
        }

        static bool ConsoleEventCallback(int eventType)
        {
            if (eventType == 2)
            {
                if (tcp_started)
                {
                    tcp_started = false;
                    w.Close();
                    FiddlerApplication.oProxy.Detach();
                    FiddlerApplication.Shutdown();
                    Console.WriteLine("> Started.");
                }
            }
            return false;
        }

        static void Main(string[] args)
        {
            handler = new ConsoleEventDelegate(ConsoleEventCallback);
            SetConsoleCtrlHandler(handler, true);

            Console.Title = "HTTP Requests Blocker | Coded by WaterSmoke | [?]";
            Console.ForegroundColor = ConsoleColor.Green;
            Console.CancelKeyPress += new ConsoleCancelEventHandler(Console_CancelKeyPress);

            if (Loadblacklist("Blacklist.txt")/* && LoadProxyList("ProxyList.txt")*/)
            {
                InstallCertificate();

                FiddlerApplication.BeforeRequest += FiddlerApplication_BeforeRequest;
                FiddlerApplication.BeforeResponse += FiddlerApplication_BeforeResponse;

                Console.Write("Commands:\nstart\nclear\nreload\nstop\n_________________\n\n");
                while (true)
                {
                    try
                    {
                        var line = Console.ReadLine();
                        if (line.Contains("stop"))
                        {
                            if (tcp_started)
                            {
                                tcp_started = false;
                                w.Close();
                                FiddlerApplication.oProxy.Detach();
                                FiddlerApplication.Shutdown();
                                Console.WriteLine("> Stopped.");
                            }
                        }
                        else if (line.Contains("start"))
                        {
                            if (!tcp_started)
                            {
                                tcp_started = true;
                                w = new StreamWriter("output.log");
                                // FiddlerApplication.Startup(0, FiddlerCoreStartupFlags.Default | FiddlerCoreStartupFlags.HookUsingPACFile | FiddlerCoreStartupFlags.CaptureFTP | FiddlerCoreStartupFlags.AllowRemoteClients);
                                FiddlerApplication.Startup(0, true, true, true);
                                Console.WriteLine("> Started.");
                            }
                        }
                        else if (line.Contains("clear"))
                        {
                            if (tcp_started)
                            {
                                Console.Clear();
                                Console.WriteLine("> Console clean.");
                            }
                        }
                        else if (line.Contains("reload"))
                        {
                            if (Loadblacklist("Blacklist.txt"))
                            {
                                Console.WriteLine("> Blacklist reloaded.");
                            }
                            //if(Loadblacklist("blacklist.txt") && LoadProxyList("ProxyList.txt"))
                            //{
                            //    Console.WriteLine("> blacklist & ProxyList reloaded.");
                            //}
                        }
                    }
                    catch (Exception e)
                    {
                        Console.Title = "HTTP Requests Blocker | Coded by WaterSmoke | [?] -> " + e.Message;
                    }
                }
            }
        }

        static ConsoleEventDelegate handler;   // Keeps it from getting garbage collected
                                               // Pinvoke
        private delegate bool ConsoleEventDelegate(int eventType);
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool SetConsoleCtrlHandler(ConsoleEventDelegate callback, bool add);
    }
}
