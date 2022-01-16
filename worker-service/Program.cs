using System.ServiceProcess;

namespace WorkerMonitoringService
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main()
        {
            ServiceBase[] ServicesToRun;
            ServicesToRun = new ServiceBase[]
            {
                new WindowsService.WorkerServerSvc()
            };
            ServiceBase.Run(ServicesToRun);
        }
    }
}
