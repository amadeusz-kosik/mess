using System;
using System.Diagnostics;
using System.ServiceModel;
using System.ServiceProcess;
using System.Threading;

namespace WorkerMonitoringService.WindowsService
{    
    public partial class WorkerServerSvc : ServiceBase
    {
        ServiceHost RESTServiceHost;

        private WorkerMonitorLogicService workerMonitorLogicService;

        public WorkerServerSvc()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            EventLog.Log = "Application";

            try
            {
                WindowsRegistryProvider windowsRegistryProvider = new WindowsRegistryProvider(EventLog);
                workerMonitorLogicService = new WorkerMonitorLogicService(EventLog, windowsRegistryProvider);

                WorkerRESTAPI workerMonitorService = new WorkerRESTAPI(EventLog, workerMonitorLogicService);
                RESTServiceHost = new ServiceHost(workerMonitorService);

                RESTServiceHost.Open();
            }
            catch(Exception exc)
            {
                EventLog.WriteEntry("Failed to start WorkerServiceSvc: " + exc.ToString(), EventLogEntryType.Error);
            }            
        }

        protected override void OnStop()
        {
            try
            {
                if (workerMonitorLogicService.IsAnalysisActive())
                    workerMonitorLogicService.StopAnalysis();

                RESTServiceHost.Close();
            }
            catch(Exception exc)
            {
                EventLog.WriteEntry("Error during stopping service: " + exc.ToString(), EventLogEntryType.Warning);
            }
        }  
    }
}
