using System;
using System.Diagnostics;
using Microsoft.Win32;

namespace WorkerMonitoringService
{
    public class WindowsRegistryProvider
    {
        private const String MESS_KEY = @"SOFTWARE\MESS";

        private const String KEY_IS_ANALYSIS_ENABLED = "IsAnalysisEnabled";
        private const String KEY_SUFFIX_DIR = "Dir";

        private RegistryView registryView = RegistryView.Registry64;
        private RegistryHive registryHive = RegistryHive.LocalMachine;

        private EventLog eventLog;

        public WindowsRegistryProvider(EventLog eventLog)
        {
            this.eventLog = eventLog;
        }

        public String GetDir(String dirName)
        {
            RegistryKey baseKey = null;
            RegistryKey messSubkey = null;

            try
            {
                baseKey = RegistryKey.OpenBaseKey(registryHive, registryView);
                messSubkey = baseKey.OpenSubKey(MESS_KEY);
                return messSubkey.GetValue(dirName + KEY_SUFFIX_DIR).ToString();
            }
            finally
            {
                if (messSubkey != null)
                    messSubkey.Close();

                if (baseKey != null)
                    baseKey.Close();
            }
        }
        
        public bool IsAnalysisEnabled()
        {
            RegistryKey baseKey = null;
            RegistryKey messSubkey = null;

            try
            {
                baseKey = RegistryKey.OpenBaseKey(registryHive, registryView);
                messSubkey = baseKey.OpenSubKey(MESS_KEY);
                return Convert.ToBoolean((String)messSubkey.GetValue(KEY_IS_ANALYSIS_ENABLED));
            }
            finally
            {
                if (messSubkey != null)
                    messSubkey.Close();

                if (baseKey != null)
                    baseKey.Close();
            }
        }

        public void SetAnalysisEnabled(bool isEnabled)
        {
            RegistryKey baseKey = null;
            RegistryKey messSubkey = null;

            try
            {
                baseKey = RegistryKey.OpenBaseKey(registryHive, registryView);
                messSubkey = baseKey.OpenSubKey(MESS_KEY, true);
                messSubkey.SetValue(KEY_IS_ANALYSIS_ENABLED, isEnabled.ToString());
            }
            finally
            {
                if (messSubkey != null)
                    messSubkey.Close();

                if (baseKey != null)
                    baseKey.Close();
            }
        }
    }
}
