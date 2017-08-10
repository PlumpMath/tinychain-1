using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using System.Security.Cryptography;

namespace tinychain
{
    class TinyChainProgram
    {
        struct Connection
        {
            public string host;
            public int port;
            public bool isConnected;

            public Connection(string host, int port, bool isConnected)
            {
                this.host = host;
                this.port = port;
                this.isConnected = isConnected;
            }
        }

        private List<Connection> connections = new List<Connection>()
        {
            new Connection("127.0.0.1", 2525, false)
        };

        //Network vars
        private List<TcpClient> clients = new List<TcpClient>();
        private TcpClient syncClient;
        private int syncCount;
        private int syncBlockSize;

        private TcpListener listener;
        private Thread connectionThread;
        private ASCIIEncoding asen = new ASCIIEncoding();

        //Blockchain vars
        private List<TinyBlock> blockchain = new List<TinyBlock>();
        private string newBlockData = "Johan Block";
        private Thread POWsearchThread;

        private static ManualResetEvent hashMre = new ManualResetEvent(true);

        public TinyChainProgram()
        {
            
        }
        public void listblocks()
        {
            for(int i = 0; i < blockchain.Count; i++)
                Console.Write(blockchain[i].Serialize() + Environment.NewLine);
        }
        #region Blockchain POW
        public void startFindPOW()
        {
            Console.WriteLine("Starting POW working thread");
            TinyBlock genesisblock = new TinyBlock();

            string json = JMessage.Serialize(JMessage.FromValue(genesisblock));
            Console.WriteLine(json);
            Console.WriteLine(genesisblock.Serialize());
            blockchain.Add(genesisblock);

            ThreadStart ts = new ThreadStart(FindPOW);
            POWsearchThread = new Thread(ts);
            POWsearchThread.Start();
        }
        public void FindPOW()
        {
            byte[] thisHash = blockchain[blockchain.Count - 1].thisHash;
            SHA256 hash = SHA256.Create();
            byte[] POWcheck;
            int POW = 0;
            int workingIndex = blockchain.Count;
            Random rnd = new Random();

            do
            {
                hashMre.WaitOne();

                if(blockchain.Count != workingIndex)
                {
                    thisHash = blockchain[blockchain.Count - 1].thisHash;
                    workingIndex = blockchain.Count;
                    POW = rnd.Next();
                }

                POWcheck = hash.ComputeHash(Encoding.UTF8.GetBytes(thisHash.ToString() + POW));

                //if(POWcheck[0] == 0 && POWcheck[1] == 0) // Difficuly hardcoded :)
                if(POWcheck[0] == 0 && POWcheck[1] == 0 && POWcheck[2] == 0) // Difficuly hardcoded :)
                {
                    Console.WriteLine("Found block");
                    TinyBlock newBlock = new TinyBlock(workingIndex, thisHash, newBlockData, POW);
                    //Console.WriteLine(json);
                    sendDataToAll(newBlock);
                    blockchain.Add(newBlock);
                }
                else
                {
                    POW++;
                }   
            } while(true);


        }
        #endregion

        #region Networking
        public void startListener()
        {
            Console.WriteLine("Launching P2P Network");

            listener = new TcpListener(IPAddress.Any, 2525);
            listener.Start();

            ThreadStart ts = new ThreadStart(acceptClientsWorker);
            connectionThread = new Thread(ts);
            connectionThread.Start();
        }
        private async void initConnections()
        {
            await Task.Delay(5000);
            foreach(Connection hp in connections)
            {
                connect(hp.host, hp.port);
            }
        }
        public void connect(string host, int port)
        {
            try
            {
                TcpClient client = new TcpClient();
                client.Connect(host, port);

                if(client.Connected)
                {
                    Console.WriteLine("Connected to: " + host);

                    addClient(client);
                    sendDataTo(client, new Command("getBlockHeight"));
                }
                else
                {
                    Console.WriteLine("Connected failed to: " + host);
                }
            }
            catch(SocketException e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine("Connected failed to: " + host);
            }
        }
        private void acceptClientsWorker()
        {
            while(true)
            {
                TcpClient client = listener.AcceptTcpClient();
                Console.WriteLine("Connection accepted from: " + ((IPEndPoint)client.Client.RemoteEndPoint).Address.ToString());
                addClient(client);

                //broadcast("New Connection, Current Connections: " + clients.Count);
            }
        }
        public void broadcast(string stringData)
        {
            byte[] data = asen.GetBytes(stringData);
            foreach(TcpClient c in clients)
            {
                c.Client.Send(data);
            }
        }
        private void addClient(TcpClient client)
        {
            clients.Add(client);
            ThreadPool.QueueUserWorkItem(clientListenThread, client);
        }
        private void clientListenThread(object obj)
        {
            TcpClient client = (TcpClient)obj;
            StringBuilder sb = new StringBuilder();
            int bytesRead = 0;
            int bytesToRead = 0;

            while(true)
            {
                try
                {
                    byte[] data = new byte[1024 * 8];
                    int dataLenght = client.Client.Receive(data);
                    //Console.WriteLine("total size: " + dataLenght);
                    if(dataLenght == 0)
                    {
                        Console.WriteLine("Disconnected");
                        clients.Remove(client);
                        break;
                    }
                    
                    int offset = 0;
                    //Split data
                    while(offset < dataLenght)
                    {
                        if(bytesRead >= bytesToRead)
                        {
                            sb.Clear();
                            bytesRead = 0;

                            //There is a super small chance that the size bytes does not come at the same time, that has not been accounted for
                            byte[] byteSize = { data[offset + 0], data[offset + 1], data[offset + 2], data[offset + 3] };
                            int size = BitConverter.ToInt32(byteSize, 0);
                            bytesToRead = size;
                            //Console.WriteLine("data size: " + size);
                            offset += 4;

                            if(offset >= dataLenght)
                                break;
                        }

                        for(int i = offset; i < offset + bytesToRead; i++)
                        {
                            if(i >= dataLenght)
                            {
                                break;
                            }
                            sb.Append(Convert.ToChar(data[i]));
                            bytesRead++;
                        }

                        if(bytesRead < bytesToRead)
                        {
                            break;
                        }

                        offset += bytesRead;

                        Console.WriteLine("From " + ((IPEndPoint)client.Client.RemoteEndPoint).Address.ToString() + ": " + sb.ToString());

                        //Must be of json data or it will crash
                        JMessage message = JMessage.Deserialize(sb.ToString());

                        if(message.Type == typeof(Command))
                        {
                            Command c = message.Value.ToObject<Command>();
                            if(c.command == "getBlockHeight")
                            {
                                sendDataTo(client, new BlockHeight(blockchain.Count));
                            }
                            else if(c.command == "sync")
                            {
                                /*foreach(TinyBlock tb in blockchain)
                                {
                                    sendDataTo(client, tb);
                                }*/
                                sendSyncList(client);
                            }
                        }
                        else if(message.Type == typeof(TinyBlock))
                        {
                            hashMre.Reset();
                            TinyBlock tb = message.Value.ToObject<TinyBlock>();
                            if(tb.verifyBlock(blockchain.Last()))
                            {
                                blockchain.Add(tb);
                            }
                            hashMre.Set();
                        }
                        else if(message.Type == typeof(SyncList))
                        {
                            SyncList syncList = message.Value.ToObject<SyncList>();
                            startSyncing(syncList);
                        }
                        else if(message.Type == typeof(BlockHeight))
                        {
                            BlockHeight c = message.Value.ToObject<BlockHeight>();
                            syncCount++;

                            if(c.value > blockchain.Count && syncBlockSize < c.value)
                            {
                                syncBlockSize = c.value;
                                syncClient = client;
                            }
                            if(syncCount == clients.Count && syncClient != null)
                            {
                                syncCount = 0;
                                sendDataTo(syncClient, new Command("sync"));
                                //blockchain.Clear();
                            }
                        }
                    }
                }
                catch(SocketException e)
                {
                    Console.WriteLine(e.Message);
                    clients.Remove(client);
                    break;
                }
            }
        }
        private void startSyncing(SyncList syncList)
        {
            hashMre.Reset();

            for(int i = 0; i < syncList.blocks.Count; i++)
            {
                //Do stuff
            }

            hashMre.Set();
        }
        private void sendSyncList(TcpClient client)
        {
            sendDataTo(client, new SyncList(blockchain));
        }
        private void sendDataToAll<T>(T data)
        {
            foreach(TcpClient client in clients)
            {
                sendDataTo(client, data);
            }
        }
        private void sendDataTo<T>(TcpClient client, T data)
        {
            string json = JMessage.Serialize(JMessage.FromValue(data));
            byte[] jsonData = asen.GetBytes(json);

            if(client != null)
            {
                client.Client.Send(BitConverter.GetBytes(jsonData.Length));
                client.Client.Send(jsonData);
            }
        }
        #endregion Networking
    }
}
