using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.ServiceModel;
using System.ServiceModel.Web;

namespace WorkerMonitoringService
{
    [ServiceContract]
    interface IWorkerRESTAPI
    {
        [WebGet(UriTemplate = "analysis/disable")]
        Stream Disable();

        [WebGet(UriTemplate = "analysis/start")]
        Stream Start();

        [WebGet(UriTemplate = "analysis/stop")]
        Stream Stop();

        [WebGet(UriTemplate = "analysis/isEnabled")]
        Stream IsEnabled();
    }

    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single)]
    public class WorkerRESTAPI : IWorkerRESTAPI
    {
        private EventLog eventLog;
        private WorkerMonitorLogicService workerMonitorLogicService;

        public WorkerRESTAPI(EventLog eventLog, WorkerMonitorLogicService workerMonitorLogicService)
        {
            this.eventLog = eventLog;
            this.workerMonitorLogicService = workerMonitorLogicService;
        }

        public Stream Disable()
        {
            try
            {
                workerMonitorLogicService.DisableAnalysis();
                return RespondAsText("OK");
            }
            catch (Exception exc)
            {
                eventLog.WriteEntry("Stop operation failed: " + exc.ToString(), EventLogEntryType.Warning);
                return RespondAsText("ERROR");
            }
        }

        public Stream Start()
        {
            try
            {
                workerMonitorLogicService.StartAnalysis();
                return RespondAsText("OK");
            }
            catch (WorkerMonitorLogicService.AlreadyInRequestedState)
            {
                return RespondAsText("ALREADY_STARTED");
            }
            catch (Exception exc)
            {
                eventLog.WriteEntry("Start operation failed: " + exc.ToString(), EventLogEntryType.Warning);
                return RespondAsText("ERROR");
            }            
        }

        public Stream Stop()
        {
            try
            {
                workerMonitorLogicService.StopAnalysis();
                return RespondAsText("OK");
            }
            catch(WorkerMonitorLogicService.AlreadyInRequestedState)
            {
                return RespondAsText("ALREADY_STOPPED");
            }
            catch(Exception exc)
            {
                eventLog.WriteEntry("Stop operation failed: " + exc.ToString(), EventLogEntryType.Warning);
                return RespondAsText("ERROR");
            }
        }

        public Stream IsEnabled()
        {
            return RespondAsText(workerMonitorLogicService.IsAnalysisEnabled().ToString());
        }
        
        private Stream RespondAsText(string input)
        {
            WebOperationContext.Current.OutgoingResponse.ContentType = "text/plain";
            byte[] rawInput = Encoding.UTF8.GetBytes(input);
            return new MemoryStream(rawInput);
        }        
    }   
}
