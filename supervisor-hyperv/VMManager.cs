using System;
using System.Linq;
using System.Management;

namespace Supervisor.Server
{
    public enum VmState : ushort
    {
        Other = 1,
        Running = 2,
        Off = 3,
        Saved = 6,
        Paused = 9,
        Starting = 10,
        Reset = 11,
        Saving = 32773,
        Pausing = 32776,
        Resuming = 32777,
        FastSaved = 32779,
        FastSaving = 32780,
    }

    public enum VmOperationResult: uint
    {
        Ok = 0,
        Error = 1
    }
        static class ReturnCode
    {
        public const uint Completed = 0;
        public const uint Started = 4096;
        public const uint Failed = 32768;
        public const uint AccessDenied = 32769;
        public const uint NotSupported = 32770;
        public const uint Unknown = 32771;
        public const uint Timeout = 32772;
        public const uint InvalidParameter = 32773;
        public const uint SystemInUse = 32774;
        public const uint InvalidState = 32775;
        public const uint IncorrectDataType = 32776;
        public const uint SystemNotAvailable = 32777;
        public const uint OutofMemory = 32778;
    }

    class VMManager
    {
        private readonly ManagementScope managementScope = new ManagementScope(@"\\.\root\virtualization\v2");

        public ManagementObject GetSnapshotByName(ManagementObject virtualMachine, string snapshotName)
        {
            ManagementObject snapshot = new ManagementClass(virtualMachine.Scope, new ManagementPath("Msvm_VirtualSystemSettingData"), null)
                .GetInstances()
                .OfType<ManagementObject>()
                .Where(v => v["ElementName"] != null && v["ElementName"].ToString() == snapshotName)
                .FirstOrDefault();

            if (snapshot == null)
                throw new VMManagerException("SNAPSHOT_NOT_FOUND");

            return snapshot;
        }

       public ManagementObject GetLastSnapshot(ManagementObject virtualMachine)
        {
            return virtualMachine.GetRelated(
                "Msvm_VirtualSystemSettingData",
                "Msvm_MostCurrentSnapshotInBranch",
                null,
                null,
                "Dependent",
                "Antecedent",
                false,
                null).OfType<ManagementObject>().FirstOrDefault();
        }

        public ManagementObject GetVMByName(string vmName)
        {
            ManagementObject virtualMachine = new ManagementClass(managementScope, new ManagementPath("Msvm_ComputerSystem"), null)
                .GetInstances()
                .OfType<ManagementObject>()
                .Where(v => v["ElementName"].ToString() == vmName)
                .FirstOrDefault();

            if (virtualMachine == null)
                throw new VMManagerException("VM_NOT_FOUND");

            return virtualMachine;
        }

        public VmOperationResult ApplySnapshot(ManagementObject virtualMachine, ManagementObject snapshot)
        {
            var snapshotService = new ManagementClass(virtualMachine.Scope, new ManagementPath("Msvm_VirtualSystemSnapshotService"), null)
                .GetInstances()
                .OfType<ManagementObject>()
                .FirstOrDefault();

            var inParameters = snapshotService.GetMethodParameters("ApplySnapshot");
            inParameters["Snapshot"] = snapshot.Path.Path;
            var outParameters = snapshotService.InvokeMethod("ApplySnapshot", inParameters, null);

            return JobResult(outParameters, this.managementScope);      
        }
                
        public VmOperationResult CreateSnapshot(ManagementObject vm)
        {
            var snapshotService = new ManagementClass(vm.Scope, new ManagementPath("Msvm_VirtualSystemSnapshotService"), null)
                .GetInstances()
                .OfType<ManagementObject>()
                .FirstOrDefault();

            ManagementObject snapshotSettings = GetServiceObject(this.managementScope, "CIM_SettingData");

            var inParameters = snapshotService.GetMethodParameters("CreateSnapshot");
            inParameters["AffectedSystem"] = vm.Path.Path;
            inParameters["SnapshotSettings"] = "";
            inParameters["SnapshotType"] = 2; // Full snapshot

            var outParameters = snapshotService.InvokeMethod("CreateSnapshot", inParameters, null);
            return JobResult(outParameters, this.managementScope);
        }

        public VmOperationResult CreateSnapshot(ManagementObject virtualMachine, string snapshotName)
        {
            var createSnapshotResult = this.CreateSnapshot(virtualMachine);

            if (createSnapshotResult == VmOperationResult.Error)
                return createSnapshotResult;

            var renameResult = this.RenameLatestSnapshot(virtualMachine, snapshotName);
            return renameResult;
        }

        public VmOperationResult RenameLatestSnapshot(ManagementObject virtualMachine, string snapshotName)
        {
            var vmSettings = GetFirstObjectFromCollection(virtualMachine.GetRelated("Msvm_VirtualSystemSettingData", "Msvm_MostCurrentSnapshotInBranch", null, null, "Dependent", "Antecedent", false, null));
            var service = new ManagementClass(this.managementScope, new ManagementPath("Msvm_VirtualSystemManagementService"), null)
                .GetInstances()
                .OfType<ManagementObject>()
                .FirstOrDefault();

            ManagementBaseObject inParams = service.GetMethodParameters("ModifySystemSettings");
            vmSettings["ElementName"] = snapshotName;
            inParams["SystemSettings"] = vmSettings.GetText(TextFormat.CimDtd20);
            ManagementBaseObject outParameters = service.InvokeMethod("ModifySystemSettings", inParams, null);

            return JobResult(outParameters, this.managementScope);
        }

        public VmOperationResult RequestStateChange(ManagementObject virtualMachine, VmState targetState)
        {
            var managementService = new ManagementClass(virtualMachine.Scope, new ManagementPath("Msvm_VirtualSystemManagementService"), null)
                .GetInstances()
                .OfType<ManagementObject>()
                .FirstOrDefault();

            var inParameters = managementService.GetMethodParameters("RequestStateChange");
            inParameters["RequestedState"] = (object) targetState;

            var outParameters = virtualMachine.InvokeMethod("RequestStateChange", inParameters, null);
            return JobResult(outParameters, this.managementScope);
        }



        private static ManagementObject GetServiceObject(ManagementScope scope, string serviceName)
        {
            scope.Connect();
            ManagementPath wmiPath = new ManagementPath(serviceName);
            ManagementClass serviceClass = new ManagementClass(scope, wmiPath, null);
            ManagementObjectCollection services = serviceClass.GetInstances();

            ManagementObject serviceObject = null;

            foreach (ManagementObject service in services)
            {
                serviceObject = service;
            }

            return serviceObject;
        }

        private static VmOperationResult JobResult(ManagementBaseObject outParameters, ManagementScope scope)
        {
            if ((uint)outParameters["ReturnValue"] == ReturnCode.Started)
            {
                if (JobCompleted(outParameters, scope))
                {
                    return VmOperationResult.Ok;
                }
                else
                {
                    Console.Out.WriteLine("Job failed: " + outParameters["ReturnValue"]);
                    return VmOperationResult.Error;
                }
            }
            else if ((uint)outParameters["ReturnValue"] == ReturnCode.Completed)
            {
                return VmOperationResult.Ok;
            }
            else
            {
                Console.Out.WriteLine("Job failed: " + outParameters["ReturnValue"]);
                return VmOperationResult.Error;
            }
        }

        private static bool JobCompleted(ManagementBaseObject outParams, ManagementScope scope)
        {
            bool jobCompleted = true;

            // Retrieve msvc_StorageJob path. This is a full wmi path
            string jobPath = (string)outParams["Job"];

            ManagementObject job = new ManagementObject(scope, new ManagementPath(jobPath), null);

            // Try to get storage job information
            job.Get();
            while ((ushort)job["JobState"] == JobState.Starting
                || (ushort)job["JobState"] == JobState.Running)
            {
                Console.WriteLine("In progress... {0}% completed.", job["PercentComplete"]);
                System.Threading.Thread.Sleep(1000);
                job.Get();
            }

            // Figure out if job failed
            ushort jobState = (ushort)job["JobState"];
            if (jobState != JobState.Completed)
            {
                ushort jobErrorCode = (ushort)job["ErrorCode"];
                Console.WriteLine("Error Code:{0}", jobErrorCode);
                Console.WriteLine("ErrorDescription: {0}", (string)job["ErrorDescription"]);
                jobCompleted = false;
            }

            return jobCompleted;
        }

        private static class JobState
        {
            public const ushort New = 2;
            public const ushort Starting = 3;
            public const ushort Running = 4;
            public const ushort Suspended = 5;
            public const ushort ShuttingDown = 6;
            public const ushort Completed = 7;
            public const ushort Terminated = 8;
            public const ushort Killed = 9;
            public const ushort Exception = 10;
            public const ushort Service = 11;
        }

        public static ManagementObject GetFirstObjectFromCollection(ManagementObjectCollection collection)
        {
            if (collection.Count == 0)
            {
                throw new ArgumentException("The collection contains no objects", "collection");
            }

            foreach (ManagementObject managementObject in collection)
            {
                return managementObject;
            }

            return null;
        }

    }

    class VMManagerException : Exception
    {
        public VMManagerException(string message) : base(message)
        { }
    }
}
