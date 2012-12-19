/**
 * Copyright 2012-2012 Mohawk College of Applied Arts and Technology
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

namespace MARC.HI.EHRS.SVC.Presentation.Configuration
{
    /// <summary>
    /// Service configuration panel
    /// </summary>
    public class ServiceConfigurationPanel : IConfigurationPanel
    {
        #region IConfigurationPanel Members

        /// <summary>
        /// Account password
        /// </summary>
        public SecureString AccountPassword { get; set; }

        /// <summary>
        /// Account name
        /// </summary>
        public String AccountName { get; set; }

        /// <summary>
        /// Configure this option
        /// </summary>
        public void Configure(System.Xml.XmlDocument configurationDom)
        {
            try
            {
                ServiceTools.ServiceInstaller.InstallAndStart("Client Registry", "Client Registry", Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), "ClientRegistry.exe"), this.AccountName, this.AccountPassword == null ? null : this.AccountPassword.ToString());
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
            return ServiceTools.ServiceInstaller.ServiceIsInstalled("Client Registry");
        }

        /// <summary>
        /// Name of the service
        /// </summary>
        public string Name
        {
            get { return "Client Registry Windows Service"; }
        }

        /// <summary>
        /// The control panel
        /// </summary>
        public System.Windows.Forms.Control Panel
        {
            get { return new Label() { Text = "No Configuration Yet Supported", TextAlign = System.Drawing.ContentAlignment.MiddleCenter, AutoSize = false }; }
        }

        /// <summary>
        /// Un-configure this option
        /// </summary>
        public void UnConfigure(System.Xml.XmlDocument configurationDom)
        {
            try
            {
                if (ServiceTools.ServiceInstaller.GetServiceStatus("Client Registry") == ServiceTools.ServiceState.Run)
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
            return true; // todo:
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
