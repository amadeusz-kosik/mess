using System;
using System.Diagnostics;
using System.IO;

namespace WorkerMonitoringService.ToolProviders
{
    class ProcmonProvider : ToolProvider
    {
        public ProcmonProvider(EventLog appEventLog, String resultsDirectory, String toolkitDirectory) :
            base(appEventLog, resultsDirectory, toolkitDirectory)
        { }

        public override void Start()
        {
            String executablePath = Path.Combine(ToolDirectory, "Procmon.exe");
            String timestamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");

            Directory.CreateDirectory(Path.Combine(ResultsDirectory, "Procmon"));
            Directory.CreateDirectory(Path.Combine(ResultsDirectory, "Procmon", timestamp));
            String logFileName = Path.Combine(ResultsDirectory, "Procmon", timestamp, "ProcmonLog.pml");

            Process procmonProcess = CreateProcess(executablePath, @"C:\", @"/Quiet /AcceptEula /BackingFile " + logFileName);
            procmonProcess.Start();

            AppEventLog.WriteEntry("Procmon started", EventLogEntryType.Information);
        }

        public override void Stop()
        {
            String procmonPath = Path.Combine(ToolDirectory, "Procmon.exe");
            Process terminateProcmonProcess = CreateProcess(procmonPath, @"C:\", @"/Terminate");
            terminateProcmonProcess.Start();
            terminateProcmonProcess.WaitForExit();

            AppEventLog.WriteEntry("Procmon terminated", EventLogEntryType.Information);
        }
    }
}
