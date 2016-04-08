using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace BruteForceMaster
{
    internal class MasterServer
    {
        private const int Port = 7777;
        private DictionaryHandler _dictionaryHandler;
        private bool _running;
        private readonly string _passwordsPath = "passwords1.txt";
        private readonly string _crackedPasswordsPath = "crackedPasswords.txt";
        public Stopwatch Stopwatch { get; set; }

        public void StartServer(int nrOfSubsets)
        {
            _dictionaryHandler = new DictionaryHandler(nrOfSubsets);
            _running = true;

            Console.WriteLine("The master server has been started");

            var serverEndPoint = new IPEndPoint(IPAddress.Any, Port);
            var tcpServer = new TcpListener(serverEndPoint);
            tcpServer.Start();
            Stopwatch = new Stopwatch();
            while (_running)
            {
                if (!_dictionaryHandler.IsWorkDone())
                {
                    Console.WriteLine("The server is now listening for connections");
                    var tcpClient = tcpServer.AcceptTcpClient();
                    Console.WriteLine("A client has connected to the server");

                    var t = Task.Run(() => HandleOneClient(tcpClient));
                    Thread.Sleep(500);
                }
                else
                {
                    Console.WriteLine("Server has finished the subsets and is attempting a graceful shutdown.");
                    Console.WriteLine("The passwords that were successfully cracked can be found in bin foler 'crackedPasswords'");
                    Stopwatch.Stop();
                    Console.WriteLine("Total time: " + Stopwatch.Elapsed);
                    _dictionaryHandler.CleanUp();
                    PasswordFileHandler.StampPasswordFile(_crackedPasswordsPath);
                    _running = false;
                }
            }
        }

        private void HandleOneClient(TcpClient tcpClient)
        {
            if (!Stopwatch.IsRunning)
            {
                Stopwatch.Start();
            }
            using (var networkStream = tcpClient.GetStream())
            {
                using (var reader = new StreamReader(networkStream))
                {
                    var input = reader.ReadLine();
                    if (input == "get.pass")
                    {
                        PasswordFileHandler.SendPasswordFile(_passwordsPath, networkStream);
                    }
                    else if (input == "get.words")
                    {
                        SendDictionarySubset(networkStream);
                    }
                    else if (input.Contains("get.nextset"))
                    {
                        var inputArray = input.Split('#');
                        for (var i = 1; i < inputArray.Length - 1; i++)
                        {
                            var userPassPair = inputArray[i].Split(':');
                            PasswordFileHandler.WriteClearPasswordFile(_crackedPasswordsPath, new[] {userPassPair[0]},
                                new[] {userPassPair[1]});
                        }
                        _dictionaryHandler.SetsReceived++;
                        SendDictionarySubset(networkStream);
                    }
                }
            }
        }

        private void SendDictionarySubset(NetworkStream networkStream)
        {
            if (!_dictionaryHandler.IsWorkDone())
            {
                var dictionaryFileStream = _dictionaryHandler.GetDictionarySubset();
                using (var sr = new StreamReader(dictionaryFileStream))
                {
                    using (var sw = new StreamWriter(networkStream))
                    {
                        while (!sr.EndOfStream)
                        {
                            var line = sr.ReadLine();
                            sw.WriteLine(line);
                        }
                    }
                }
            }
        }
    }
}