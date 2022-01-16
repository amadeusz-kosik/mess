using System;
using System.Diagnostics;

namespace WorkerMonitoringService.ToolProviders
{
    abstract class ToolProvider
    {
        protected EventLog AppEventLog;
        protected String ResultsDirectory;
        protected String ToolDirectory;
        
        public ToolProvider(EventLog appEventLog, String resultsDirectory, String toolDirectory)
        {
            ResultsDirectory = resultsDirectory;
            ToolDirectory = toolDirectory;
            AppEventLog = appEventLog;
        }

        abstract public void Start();

        abstract public void Stop();

        protected Process CreateProcess(String executablePath, String workingDirectory, String arguments)
        {
            Process process = new Process();
            process.StartInfo.Arguments = arguments;
            process.StartInfo.FileName = executablePath;
            process.StartInfo.WorkingDirectory = workingDirectory;
            return process;

        }
    }
}
