using OpenSCM.ClientService.Application;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenSCM.ClientService.CSConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            CSApplication application = new CSApplication();
            application.Start();
            Console.ReadLine();
        }
    }
}
