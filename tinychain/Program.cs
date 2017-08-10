using System;
using System.Text;
using System.Security.Cryptography;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace tinychain
{
    class MainClass
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("Launching TinyChain");
            TinyChainProgram tcp = new TinyChainProgram();
            tcp.startListener();
            tcp.startFindPOW();

            ConsoleKeyInfo keyinfo;
            do
            {
                keyinfo = Console.ReadKey();
                if(keyinfo.Key == ConsoleKey.A)
                {
                    tcp.connect("127.0.0.1", 2525);
                    //tcp.connect("192.168.80.104", 2525);
                }
                if(keyinfo.Key == ConsoleKey.L)
                    tcp.listblocks();
            }
            while(keyinfo.Key != ConsoleKey.X);
        }
    }
}
