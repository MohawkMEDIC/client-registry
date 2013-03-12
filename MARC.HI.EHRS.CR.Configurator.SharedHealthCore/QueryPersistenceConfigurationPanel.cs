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

namespace MARC.HI.EHRS.CR.Configurator.SharedHealthCore
{
    /// <summary>
    /// Query persistence configuration panel
    /// </summary>
    public class QueryPersistenceConfigurationPanel : IDataboundConfigurationPanel
    {
        #region IConfigurationPanel Members

        /// <summary>
        /// Message persistence
        /// </summary>
        public string Name
        {
            get { return "Query Persistence"; }
        }

        /// <summary>
        /// Enable configuration
        /// </summary>
        public bool EnableConfiguration { get; set; }

        /// <summary>
        /// Gets the configuration panel
        /// </summary>
        public System.Windows.Forms.Control Panel
        {
            get { return new Label() { Text = "No Configuration Yet Supported", TextAlign = System.Drawing.ContentAlignment.MiddleCenter, AutoSize = false }; }
        }

        /// <summary>
        /// Configure the option
        /// </summary>
        public void Configure(System.Xml.XmlDocument configurationDom)
        {
            //throw new NotImplementedException();
        }

        /// <summary>
        /// Unconfigure the option
        /// </summary>
        public void UnConfigure(System.Xml.XmlDocument configurationDom)
        {
            //throw new NotImplementedException();
        }

        /// <summary>
        /// Determine if the option is configured
        /// </summary>
        public bool IsConfigured(System.Xml.XmlDocument configurationDom)
        {
            return false;
        }

        /// <summary>
        /// Validate the configuration options
        /// </summary>
        public bool Validate(System.Xml.XmlDocument configurationDom)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region IDataboundConfigurationPanel Members

        /// <summary>
        /// Gets or sets the connection string for the message persistence
        /// </summary>
        public string ConnectionString { get; set; }

        /// <summary>
        /// Gets or sets the database configurator
        /// </summary>
        public IDatabaseConfigurator DatabaseConfigurator { get; set; }

        #endregion
    }
}
