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

namespace MARC.HI.EHRS.SHR.Configurator.SharedHealthCore
{
    public class MessagingConfigurationPanel : IConfigurationPanel
    {
        #region IConfigurationPanel Members

        /// <summary>
        /// Configure the option
        /// </summary>
        /// <param name="configurationDom"></param>
        public void Configure(System.Xml.XmlDocument configurationDom)
        {
            //throw new NotImplementedException();
        }

        /// <summary>
        /// True whether configuration is enabled
        /// </summary>
        public bool EnableConfiguration { get; set; }
        
        /// <summary>
        /// True if the option is configured
        /// </summary>
        public bool IsConfigured(System.Xml.XmlDocument configurationDom)
        {
            return false;
        }

        /// <summary>
        /// Gets the name of the configuration
        /// </summary>
        public string Name
        {
            get { return "Messaging"; }
        }

        /// <summary>
        /// Gets the configuration panel
        /// </summary>
        public System.Windows.Forms.Control Panel
        {
            get { return new Label() { Text = "No Configuration Yet Supported", TextAlign = System.Drawing.ContentAlignment.MiddleCenter, AutoSize = false }; }
        }

        /// <summary>
        /// Un-configure the messaging options
        /// </summary>
        /// <param name="configurationDom"></param>
        public void UnConfigure(System.Xml.XmlDocument configurationDom)
        {
            //throw new NotImplementedException();
        }

        /// <summary>
        /// Validate the messaging
        /// </summary>
        public bool Validate(System.Xml.XmlDocument configurationDom)
        {
            //throw new NotImplementedException();
            return true;
        }

        #endregion
    }
}
