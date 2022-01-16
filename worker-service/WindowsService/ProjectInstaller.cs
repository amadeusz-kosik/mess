using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration.Install;
using System.Linq;
using System.ServiceProcess;
using System.Threading.Tasks;


namespace WorkerMonitoringService.WindowsService
{
    [RunInstaller(true)]
    public partial class ProjectInstaller : System.Configuration.Install.Installer
    {
        public ProjectInstaller()
        {
            InitializeComponent();
        }

        private void ServiceInstaller_AfterInstall(object sender, InstallEventArgs e)
        {
            ServiceController serviceController = new ServiceController(this.WorkerServiceInstaller.ServiceName);
            serviceController.Start();
        }
    }
}
