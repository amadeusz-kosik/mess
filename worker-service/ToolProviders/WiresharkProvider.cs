using System;
using System.Diagnostics;
using System.IO;

namespace WorkerMonitoringService.ToolProviders
{
    class WiresharkProvider: ToolProvider
    {
        public WiresharkProvider(EventLog appEventLog, String resultsDirectory, String toolkitDirectory) :
            base(appEventLog, resultsDirectory, toolkitDirectory)
        { }

        private Process WiresharkProcess;

        public override void Start()
        {
            String executablePath = Path.Combine(ToolDirectory, "dumpcap.exe");
            String logFileName = "DumpcapLog-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".pcap";
            WiresharkProcess = CreateProcess(executablePath, @"C:\", @"-N 100 -w " + ResultsDirectory + @"/" + logFileName);
            WiresharkProcess.Start();

            AppEventLog.WriteEntry("Wireshark started", EventLogEntryType.Information);
        }

        public override void Stop()
        {
            WiresharkProcess.Kill();
            AppEventLog.WriteEntry("Wireshark killed", EventLogEntryType.Information);
        }
    }
}
