using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace Myrtille.Capture.Services
{
    static class Program
    {
        public static void Main(string[] args)
        {
            if (args.FirstOrDefault()?.ToUpper() == "/CONSOLE")
            {
                RunAsConsole();
            }
            else
            {
                RunAsService();
            }
        }
        private static void RunAsConsole()
        {
            SaveService serv = new SaveService();
            serv.StartService();

            Console.WriteLine("Running service as console. Press any key to stop.");
            Console.ReadKey();

            serv.Stop();
        }
        private static void RunAsService()
        {
            /* Warning: Don't load the object graph or 
             * initialize anything in here. 
             * 
             * Initialize everything in TestService.StartService() instead
             */
            ServiceBase[] ServicesToRun;
            ServicesToRun = new ServiceBase[]
            {
                new SaveService()
            };
            ServiceBase.Run(ServicesToRun);
        }
    }
}
