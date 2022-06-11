using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace server
{
    internal static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main(string[] args)
        {
            if (args.Length > 0)
            {
                if (args[0] == "-c")
                {
                    return;
                }
            }

            ServiceBase.Run(new[] { new Service1() });
        }
    }
}
