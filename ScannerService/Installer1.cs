using System.ComponentModel;
using System.ServiceProcess;

namespace ScannerService
{
    [RunInstaller(true)]
    public partial class Installer1: System.Configuration.Install.Installer
    {
        ServiceInstaller serviceInstaller;
        ServiceProcessInstaller processInstaller;

        public Installer1()
        {
            InitializeComponent();
            serviceInstaller = new ServiceInstaller();
            processInstaller = new ServiceProcessInstaller();

            processInstaller.Account = ServiceAccount.LocalSystem;
            serviceInstaller.StartType = ServiceStartMode.Automatic;
            serviceInstaller.ServiceName = "VekasService";
            serviceInstaller.DisplayName = "VekasService";
            Installers.Add(processInstaller);
            Installers.Add(serviceInstaller);
        }
    }
}
