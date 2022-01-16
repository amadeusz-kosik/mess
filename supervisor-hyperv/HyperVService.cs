using System;
using System.IO;
using System.Management;
using System.Text;
using System.ServiceModel.Web;
using System.Text.RegularExpressions;

namespace Supervisor.Server
{
    public class HyperVService: IHyperVService
    {

        private static readonly Regex vmNameRegex = new Regex(@"^MW\d$");

        private readonly VMManager vmManager = new VMManager();

        public Stream LoadSnapshot(string vmName, string snapshotName)
        {
            Func<String> requestHandler = delegate()
            {
                ManagementObject virtualMachine = vmManager.GetVMByName(vmName);
                ManagementObject snapshot = vmManager.GetSnapshotByName(virtualMachine, snapshotName);

                VmOperationResult result = vmManager.ApplySnapshot(virtualMachine, snapshot);

                if (result == VmOperationResult.Ok)
                    return "OK";
                else
                    return "ERROR";
            };

            return ProcessRequest(requestHandler, vmName);
        }

        public Stream CreateSnapshot(string vmName, string snapshotName)
        {
            Func<String> requestHandler = delegate ()
            {
                var virtualMachine = vmManager.GetVMByName(vmName);
                var result = vmManager.CreateSnapshot(virtualMachine, snapshotName);

                if (result == VmOperationResult.Ok)
                    return "OK";
                else
                    return "ERROR";
            };

            return ProcessRequest(requestHandler, vmName);
        }

        public Stream RenameLatestSnapshot(string vmName, string snapshotName)
        {
            Func<String> requestHandler = delegate ()
            {
                var virtualMachine = vmManager.GetVMByName(vmName);
                var result = vmManager.RenameLatestSnapshot(virtualMachine, snapshotName);

                if (result == VmOperationResult.Ok)
                    return "OK";
                else
                    return "ERROR";
            };

            return ProcessRequest(requestHandler, vmName);
        }

        public Stream StartVM(string vmName)
        {
            Func<String> requestHandler = delegate()
            {
                ManagementObject virtualMachine = vmManager.GetVMByName(vmName);
                VmOperationResult result = vmManager.RequestStateChange(virtualMachine, VmState.Running);

                if (result == VmOperationResult.Ok)
                    return "OK";
                else
                    return "ERROR";
            };

            return ProcessRequest(requestHandler, vmName);
        }

        public Stream StatusVM(string vmName)
        {
            Func<String> requestHandler = delegate()
            {
                ManagementObject virtualMachine = vmManager.GetVMByName(vmName);
                return (((VmState) virtualMachine["EnabledState"]).ToString());
            };

            return ProcessRequest(requestHandler, vmName);
        }

        public Stream StopVM(string vmName)
        {
            Func<String> requestHandler = delegate()
            {
                ManagementObject virtualMachine = vmManager.GetVMByName(vmName);
                VmOperationResult result = vmManager.RequestStateChange(virtualMachine, VmState.Off);

                if (result == VmOperationResult.Ok)
                    return "OK";
                else
                    return "ERROR";
            };

            return ProcessRequest(requestHandler, vmName);
        }
        
        private System.IO.Stream ProcessRequest(Func<String> requestHandler, String vmName)
        {
            if (!vmNameRegex.IsMatch(vmName))
            {
                return RespondAsText("ILLEGAL_VM_NAME");
            }

            try
            {
                String result = requestHandler();                
                return RespondAsText(result);
            }
            catch(Exception exception)
            {
                return RespondAsText(exception.ToString());
            }
        }

        private System.IO.Stream RespondAsText(string input)
        {
            WebOperationContext.Current.OutgoingResponse.ContentType = "text/plain";
            byte[] rawInput = Encoding.UTF8.GetBytes(input);
            return new System.IO.MemoryStream(rawInput);
        }
    }
}
