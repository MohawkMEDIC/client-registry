/**
 * Copyright 2012-2013 Mohawk College of Applied Arts and Technology
 * 
 * Licensed under the Apache License, Version 2.0 (the "License"); you 
 * may not use this file except in compliance with the License. You may 
 * obtain a copy of the License at 
 * 
 * http://www.apache.org/licenses/LICENSE-2.0 
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS, WITHOUT
 * WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the 
 * License for the specific language governing permissions and limitations under 
 * the License.
 * 
 * User: fyfej
 * Date: 5-12-2012
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MARC.HI.EHRS.SVC.Core.Configuration;
using System.Windows.Forms;
using System.Security;
using System.Threading;
using System.IO;
using System.Reflection;
using MARC.HI.EHRS.CR.Core.Configuration;
using System.DirectoryServices.AccountManagement;

namespace MARC.HI.EHRS.SVC.Presentation.Configuration
{
    /// <summary>
    /// Service configuration panel
    /// </summary>
    public class ServiceConfigurationPanel : IConfigurationPanel
    {
        #region IConfigurationPanel Members

        // Control panel
        private ucServiceSettings m_controlPanel = new ucServiceSettings();

        /// <summary>
        /// Setup defaults
        /// </summary>
        public ServiceConfigurationPanel()
        {
            this.Mode = ServiceTools.ServiceBootFlag.AutoStart;
            
        }

        /// <summary>
        /// Gets the service mode
        /// </summary>
        public ServiceTools.ServiceBootFlag Mode { get; set; }

        /// <summary>
        /// Account password
        /// </summary>
        public String AccountPassword { get; set; }

        /// <summary>
        /// Account name
        /// </summary>
        public String AccountName { get; set; }

        /// <summary>
        /// Configure this option
        /// </summary>
        public void Configure(System.Xml.XmlDocument configurationDom)
        {
            if (!this.EnableConfiguration)
                return;

            try
            {
                ServiceTools.ServiceInstaller.Install("Client Registry", "Client Registry", Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), "ClientRegistry.exe"), this.AccountName, this.AccountPassword == null ? null : this.AccountPassword.ToString(), this.Mode);
                
            }
            catch (Exception e)
            {
                MessageBox.Show(String.Format("Could not install service, perhaps you are not Administrator? Error was: {0}", e.Message), "Install Error");
            }
        }

        /// <summary>
        /// Enabled configuration
        /// </summary>
        public bool EnableConfiguration { get; set; }

        /// <summary>
        /// Determine if this service is configured
        /// </summary>
        public bool IsConfigured(System.Xml.XmlDocument configurationDom)
        {
            bool isInstalled = ServiceTools.ServiceInstaller.ServiceIsInstalled("Client Registry");
            if (isInstalled)
            {
                var svcInfo = ServiceTools.ServiceInstaller.GetServiceConfig("Client Registry");
                this.Mode = (ServiceTools.ServiceBootFlag)svcInfo.dwStartType;
                this.AccountName = svcInfo.lpServiceStartName;
                this.m_controlPanel.UserAccount = this.AccountName;
                this.m_controlPanel.ServiceStart = this.Mode;
                this.EnableConfiguration = isInstalled;
            }
            
           
            return isInstalled;
        }

        /// <summary>
        /// Name of the service
        /// </summary>
        public string Name
        {
            get { return "Client Registry/Service"; }
        }

        /// <summary>
        /// The control panel
        /// </summary>
        public System.Windows.Forms.Control Panel
        {
            get { return this.m_controlPanel; }
        }

        /// <summary>
        /// Un-configure this option
        /// </summary>
        public void UnConfigure(System.Xml.XmlDocument configurationDom)
        {
            try
            {
                if (ServiceTools.ServiceInstaller.GetServiceStatus("Client Registry") != ServiceTools.ServiceState.Stop)
                    ServiceTools.ServiceInstaller.StopService("Client Registry");
                while (ServiceTools.ServiceInstaller.GetServiceStatus("Client Registry") != ServiceTools.ServiceState.Stop)
                    Thread.Sleep(400);
                
                ServiceTools.ServiceInstaller.Uninstall("Client Registry");
            }
            catch (Exception e)
            {
                MessageBox.Show(String.Format("Could not un-install service, perhaps you are not Administrator? Error was: {0}", e.Message), "Install Error");
            }

        }

        /// <summary>
        /// Validate install
        /// </summary>
        public bool Validate(System.Xml.XmlDocument configurationDom)
        {
            this.AccountName = this.m_controlPanel.UserAccount;
            this.AccountPassword = this.m_controlPanel.Password;
            this.Mode = this.m_controlPanel.ServiceStart;

            if (this.AccountName != null)
            {
                bool valid = false;
                string domainName = null,
                    userName = this.AccountName;
                if (this.AccountName.Contains("\\"))
                {
                    string[] arrT = this.AccountName.Split('\\');
                    domainName = arrT[0];
                    userName = arrT[1];
                }
                if (String.IsNullOrEmpty(domainName))
                {
                    domainName = System.Environment.MachineName;
                }


                // Machine store
                try
                {
                    using (PrincipalContext context = new PrincipalContext(ContextType.Machine, domainName))
                        return context.ValidateCredentials(userName, this.AccountPassword);
                }
                catch
                {
                }


                // Domain store
                try
                {
                    using (PrincipalContext context = new PrincipalContext(ContextType.Domain, domainName))
                        return context.ValidateCredentials(userName, this.AccountPassword);
                }
                catch
                {
                }

            }
            return true;
        }

        /// <summary>
        /// String representation
        /// </summary>
        public override string ToString()
        {
            return this.Name;
        }
        #endregion
    }
}
