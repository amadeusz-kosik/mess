using System;
using System.Collections.Generic;
using System.Diagnostics;
using WorkerMonitoringService.ToolProviders;

namespace WorkerMonitoringService
{
    public class WorkerMonitorLogicService
    {
        private EventLog eventLog;
        private WindowsRegistryProvider windowsRegistryProvider;
        private List<ToolProvider> toolProviders;

        private Boolean isActive;
        private Boolean isEnabled;

        public class AlreadyInRequestedState : Exception
        { }

        public WorkerMonitorLogicService(EventLog eventLog, WindowsRegistryProvider windowsRegistryProvider)
        {
            this.eventLog = eventLog;
            this.windowsRegistryProvider = windowsRegistryProvider;

            try
            {
                String resultsDir = windowsRegistryProvider.GetDir("Results");
                toolProviders = new List<ToolProvider>();
                toolProviders.Add(new ProcmonProvider(this.eventLog, resultsDir, windowsRegistryProvider.GetDir("Procmon")));
                toolProviders.Add(new WiresharkProvider(this.eventLog, resultsDir, windowsRegistryProvider.GetDir("Wireshark")));

                isEnabled = windowsRegistryProvider.IsAnalysisEnabled();

                if (isEnabled)
                    StartAnalysis();
                else
                    isActive = false;
                
            }
            catch (Exception exc)
            {
                this.eventLog.WriteEntry("Failed to init WorkerSvc: \r\n" + exc.ToString(), EventLogEntryType.Error);
            }
        }

        public void DisableAnalysis()
        {
            isEnabled = false;
            windowsRegistryProvider.SetAnalysisEnabled(false);
        }

        public Boolean IsAnalysisActive()
        {
            return isActive;
        }

        public Boolean IsAnalysisEnabled()
        {
            return isEnabled;
        }
        
        public void StartAnalysis()
        {
            if (isActive)
                throw new AlreadyInRequestedState();

            foreach (var toolProvider in toolProviders)
                toolProvider.Start();

            isActive = true;
            isEnabled = true;

            windowsRegistryProvider.SetAnalysisEnabled(true);
        }

        public void StopAnalysis()
        {
            if (!isActive)
                throw new AlreadyInRequestedState();

            isActive = false;

            foreach (var toolProvider in toolProviders)
                toolProvider.Stop();
        }
    }
}
