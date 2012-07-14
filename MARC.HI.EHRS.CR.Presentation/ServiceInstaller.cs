using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;

namespace MARC.HI.EHRS.SVC.Presentation
{
    [RunInstaller(true)]
    public class ServiceInstaller : System.Configuration.Install.Installer
    {

        private System.ServiceProcess.ServiceProcessInstaller m_serviceProcessInstaller;
        private System.ServiceProcess.ServiceInstaller m_serviceInstaller;

        public ServiceInstaller()
        {
            // This call is required by the Designer.
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.m_serviceProcessInstaller = new System.ServiceProcess.ServiceProcessInstaller();
            this.m_serviceInstaller = new System.ServiceProcess.ServiceInstaller();

            this.m_serviceProcessInstaller.Account = System.ServiceProcess.ServiceAccount.LocalSystem;
            this.m_serviceProcessInstaller.Password = null;
            this.m_serviceProcessInstaller.Username = null;
            this.m_serviceInstaller.ServiceName = "Client Registry";
            this.m_serviceInstaller.StartType = System.ServiceProcess.ServiceStartMode.Automatic;

            this.Installers.AddRange(
                new System.Configuration.Install.Installer[] 
                { 
                    this.m_serviceProcessInstaller, 
                    this.m_serviceInstaller
                });
        }
    }
}

