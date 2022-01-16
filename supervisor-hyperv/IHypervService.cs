using System;
using System.IO;
using System.ServiceModel;
using System.ServiceModel.Web;

namespace Supervisor.Server
{
    [ServiceContract]
    interface IHyperVService
    {
        [WebGet(UriTemplate = "{vmName}/snapshot/{snapshotName}/apply")]
        Stream LoadSnapshot(string vmName, string snapshotName);

        [WebGet(UriTemplate = "{vmName}/snapshot/{snapshotName}/create")]
        Stream CreateSnapshot(string vmName, string snapshotName);

        [WebGet(UriTemplate = "{vmName}/snapshot/latest/rename/{snapshotName}")]
        Stream RenameLatestSnapshot(string vmName, string snapshotName);

        [WebGet(UriTemplate = "{vmName}/start")]
        Stream StartVM(string vmName);

        [WebGet(UriTemplate = "{vmName}/status")]
        Stream StatusVM(string vmName);

        [WebGet(UriTemplate = "{vmName}/stop")]
        Stream StopVM(string vmName);
    }
}
