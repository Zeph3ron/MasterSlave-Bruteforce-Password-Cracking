using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BruteForceMaster
{
    class Program
    {
        static void Main(string[] args)
        {
            MasterServer masterServer = new MasterServer();
            masterServer.StartServer(50);
        }
    }
}
