using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ESRollbackScheduler
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main()
        {
#if DEBUG
            ESRollbackSchedulerService eSRollbackSchedulerService = new ESRollbackSchedulerService();
            eSRollbackSchedulerService.OnDebug();
            Thread.Sleep(Timeout.Infinite);
#else
            ServiceBase[] ServicesToRun;
            ServicesToRun = new ServiceBase[]
            {
                new ESRollbackSchedulerService()
            };
            ServiceBase.Run(ServicesToRun);
#endif

        }
    }
}
