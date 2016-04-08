using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BruteForceClient
{
    class Program
    {
        static void Main(string[] args)
        {
            Cracking cracking = new Cracking("localhost", 7777);
            cracking.RunCracking();

            foreach (var userInfoClearText in cracking.CrackingResults)
            {
                Console.WriteLine(userInfoClearText);
            }

            Console.WriteLine("The server has stopped working or is not responding, please try again later...");
            Console.ReadKey(true);
        }
    }
}
