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
using MARC.HI.EHRS.CR.Configurator.SharedHealthCore.Panels;
using System.Xml;

namespace MARC.HI.EHRS.CR.Configurator.SharedHealthCore
{
    /// <summary>
    /// Terminology configuration panel
    /// </summary>
    public class TerminologyConfigurationPanel : IDataboundConfigurationPanel
    {

        private pnlConfigureTerminology m_configPanel = new pnlConfigureTerminology();
        private string ctsServiceName = typeof(MARC.HI.EHRS.SVC.Terminology.CTS12.CtsTerminologyResolver).AssemblyQualifiedName,
            dbServiceName = typeof(MARC.HI.EHRS.SVC.Terminology.QuickAndDirty.QuickAndDirtyTerminologyResolver).AssemblyQualifiedName;

        /// <summary>
        /// Setup default parameters
        /// </summary>
        public TerminologyConfigurationPanel()
        {
            this.MaxCacheSize = 0;
            this.EnableLocal = true;
        }

        /// <summary>
        /// Enable CTS
        /// </summary>
        public bool EnableCts { get; set; }
        /// <summary>
        /// Enable local
        /// </summary>
        public bool EnableLocal { get; set; }
        /// <summary>
        /// Maximum cache size
        /// </summary>
        public decimal MaxCacheSize { get; set; }
        /// <summary>
        /// CTS URL
        /// </summary>
        public string CtsUrl { get; set; }


        #region IConfigurationPanel Members

        /// <summary>
        /// Message persistence
        /// </summary>
        public string Name
        {
            get { return "Code Validation"; }
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
            get { return this.m_configPanel; }

        }

        /// <summary>
        /// Configure the option
        /// </summary>
        public void Configure(System.Xml.XmlDocument configurationDom)
        {
            if (this.ConnectionString == null || this.DatabaseConfigurator == null)
                throw new ArgumentNullException("Unable to connect to the database");
            else if (!this.EnableConfiguration)
                return;

            XmlElement configSectionsNode = configurationDom.SelectSingleNode("//*[local-name() = 'configSections']") as XmlElement,
                terminology = configurationDom.SelectSingleNode("//*[local-name() = 'marc.hi.ehrs.svc.terminology']") as XmlElement,
                coreNode = configurationDom.SelectSingleNode("//*[local-name() = 'marc.hi.ehrs.svc.core']") as XmlElement;

            // Config sections node
            if (configSectionsNode == null)
            {
                configSectionsNode = configurationDom.CreateElement("configSections");
                configurationDom.DocumentElement.PrependChild(configSectionsNode);
            }
            XmlElement configSectionNode = configSectionsNode.SelectSingleNode("./*[local-name() = 'section'][@name = 'marc.hi.ehrs.svc.terminology']") as XmlElement;
            if (configSectionNode == null)
            {
                configSectionNode = configurationDom.CreateElement("section");
                configSectionNode.Attributes.Append(configurationDom.CreateAttribute("name"));
                configSectionNode.Attributes.Append(configurationDom.CreateAttribute("type"));
                configSectionNode.Attributes["name"].Value = "marc.hi.ehrs.svc.terminology";
                configSectionNode.Attributes["type"].Value = "MARC.HI.EHRS.SVC.Terminology.Configuration.ConfigurationSectionHandler, MARC.HI.EHRS.SVC.Terminology, Version=1.0.0.0";
                configSectionsNode.AppendChild(configSectionNode);
            }

            // Persistence section node
            if (terminology == null)
            {
                terminology = configurationDom.CreateElement("marc.hi.ehrs.svc.terminology");
                configurationDom.DocumentElement.AppendChild(terminology);
            }

            // QDCDB Configuration
            XmlElement qdcdbConnectionManager = terminology.SelectSingleNode("./*[local-name() = 'qdcdb']") as XmlElement;
            if (this.EnableLocal)
            {
                if (qdcdbConnectionManager == null)
                {
                    qdcdbConnectionManager = configurationDom.CreateElement("qdcdb");
                    terminology.AppendChild(qdcdbConnectionManager);
                }
                if (qdcdbConnectionManager.Attributes["connection"] == null)
                    qdcdbConnectionManager.Attributes.Append(configurationDom.CreateAttribute("connection"));
                qdcdbConnectionManager.Attributes["connection"].Value = this.ConnectionString;
                if(qdcdbConnectionManager.Attributes["enableCtsFallback"] == null)
                    qdcdbConnectionManager.Attributes.Append(configurationDom.CreateAttribute("enableCtsFallback"));
                qdcdbConnectionManager.Attributes["enableCtsFallback"].Value = this.EnableCts.ToString();
            }
            else
            {
                if (qdcdbConnectionManager != null) // remove
                    terminology.RemoveChild(qdcdbConnectionManager);
            }

            // CTS Configuration
            XmlElement ctsConnectionManager = terminology.SelectSingleNode("./*[local-name() = 'cts']") as XmlElement;
            if (this.EnableCts)
            {
                if (ctsConnectionManager == null)
                {
                    ctsConnectionManager = configurationDom.CreateElement("cts");
                    terminology.AppendChild(ctsConnectionManager);
                }

                // Clear the sub nodes and add the code systems for CTS
                ctsConnectionManager.RemoveAll();

                if (ctsConnectionManager.Attributes["messageRuntimeUrl"] == null)
                    ctsConnectionManager.Attributes.Append(configurationDom.CreateAttribute("messageRuntimeUrl"));
                ctsConnectionManager.Attributes["messageRuntimeUrl"].Value = this.CtsUrl;
                
               
                foreach (string cs in new string[] { "2.16.840.1.113883.6.96", "2.16.840.1.113883.6.1" })
                {
                    var fillInDetails = configurationDom.CreateElement("fillInDetails");
                    fillInDetails.Attributes.Append(configurationDom.CreateAttribute("codeSystem"));
                    fillInDetails.Attributes["codeSystem"].Value = cs;
                    ctsConnectionManager.AppendChild(fillInDetails);
                }

            }
            else
                if (ctsConnectionManager != null)
                    terminology.RemoveChild(ctsConnectionManager);

            // Cache size
            if (terminology.Attributes["maxMemoryCacheSize"] == null)
                terminology.Attributes.Append(configurationDom.CreateAttribute("maxMemoryCacheSize"));
            terminology.Attributes["maxMemoryCacheSize"].Value = this.MaxCacheSize.ToString();


            // Ensure the assembly is loaded and the provider registered
            if (coreNode == null)
            {
                coreNode = configurationDom.CreateElement("marc.hi.ehrs.svc.core");
                configurationDom.DocumentElement.AppendChild(coreNode);
            }
            XmlElement serviceAssemblyNode = coreNode.SelectSingleNode("./*[local-name() = 'serviceAssemblies']") as XmlElement,
                serviceProviderNode = coreNode.SelectSingleNode("./*[local-name() = 'serviceProviders']") as XmlElement;
            if (serviceAssemblyNode == null)
            {
                serviceAssemblyNode = configurationDom.CreateElement("serviceAssemblies");
                coreNode.AppendChild(serviceAssemblyNode);
            }
            if (serviceProviderNode == null)
            {
                serviceProviderNode = configurationDom.CreateElement("serviceProviders");
                coreNode.AppendChild(serviceProviderNode);
            }


            XmlElement addServiceAsmNode = serviceAssemblyNode.SelectSingleNode("./*[local-name() = 'add'][@assembly = 'MARC.HI.EHRS.SVC.Terminology.dll']") as XmlElement,
                addServiceProvNode = serviceProviderNode.SelectSingleNode(String.Format("./*[local-name() = 'add'][@type = '{0}']", dbServiceName)) as XmlElement;
            // Try the cts one
            if(addServiceProvNode == null)
                addServiceProvNode = serviceProviderNode.SelectSingleNode(String.Format("./*[local-name() = 'add'][@type = '{0}']", ctsServiceName)) as XmlElement;
            
            if (addServiceAsmNode == null)
            {
                addServiceAsmNode = configurationDom.CreateElement("add");
                addServiceAsmNode.Attributes.Append(configurationDom.CreateAttribute("assembly"));
                addServiceAsmNode.Attributes["assembly"].Value = "MARC.HI.EHRS.SVC.Terminology.dll";
                serviceAssemblyNode.AppendChild(addServiceAsmNode);
            }
            if (addServiceProvNode == null)
            {
                addServiceProvNode = configurationDom.CreateElement("add");
                addServiceProvNode.Attributes.Append(configurationDom.CreateAttribute("type"));
                addServiceProvNode.Attributes["type"].Value = this.EnableLocal ? dbServiceName : this.EnableCts ? ctsServiceName : String.Empty;
                if (String.IsNullOrEmpty(addServiceProvNode.Attributes["type"].Value))
                    throw new InvalidOperationException("Invalid configuration for code validation");
                serviceProviderNode.AppendChild(addServiceProvNode);
            }

            // Instruct the database to create the feature for core
            bool shouldQuit = false;
            while (!shouldQuit)
                try
                {
                    if(this.EnableLocal)
                    {
                        this.DatabaseConfigurator.DeployFeature("QDCDB", this.ConnectionString, configurationDom);
                        this.DatabaseConfigurator.DeployFeature("LOINC", this.ConnectionString, configurationDom);
                    }
                    shouldQuit = true;
                }
                catch (Exception)
                {
                    switch (MessageBox.Show("There was an error deploying the code database schema to the database. This commonly occurs when an older version of the schema exists in the selected database. Would you like to try removing the old schema and re-deploying? (Selecting No will ignore this error)", "Error during deploy", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question))
                    {
                        case DialogResult.Yes:
                            this.DatabaseConfigurator.UnDeployFeature("LOINC", this.ConnectionString, configurationDom);
                            this.DatabaseConfigurator.UnDeployFeature("QDCDB", this.ConnectionString, configurationDom);
                            break;
                        case DialogResult.Cancel:
                            throw;
                        case DialogResult.No:
                            shouldQuit = true;
                            break;
                    }
                }
        }

        /// <summary>
        /// Unconfigure the option
        /// </summary>
        public void UnConfigure(System.Xml.XmlDocument configurationDom)
        {
            this.DatabaseConfigurator.UnDeployFeature("LOINC", this.ConnectionString, configurationDom);
            this.DatabaseConfigurator.UnDeployFeature("QDCDB", this.ConnectionString, configurationDom);

            // Select the relevant items and un-configure
            XmlNode configSection = configurationDom.SelectSingleNode("//*[local-name() = 'section'][@name = 'marc.hi.ehrs.svc.terminology']"),
                persistenceSection = configurationDom.SelectSingleNode("//*[local-name() = 'marc.hi.ehrs.svc.terminology']"),
                addAssemblyNode = configurationDom.SelectSingleNode("//*[local-name() = 'marc.hi.ehrs.svc.core']/*[local-name() = 'serviceAssemblies']/*[local-name() = 'add'][@assembly = 'MARC.HI.EHRS.SVC.Terminology.dll']"),
                addProviderNodeCts = configurationDom.SelectSingleNode(String.Format("//*[local-name() = 'marc.hi.ehrs.svc.core']/*[local-name() = 'serviceProviders']/*[local-name() = 'add'][@type = '{0}']", ctsServiceName)),
                addProviderNodeDb = configurationDom.SelectSingleNode(String.Format("//*[local-name() = 'marc.hi.ehrs.svc.core']/*[local-name() = 'serviceProviders']/*[local-name() = 'add'][@type = '{0}']", dbServiceName));

            if (configSection != null)
                configSection.ParentNode.RemoveChild(configSection);
            if (persistenceSection != null)
                persistenceSection.ParentNode.RemoveChild(persistenceSection);
            if (addAssemblyNode != null)
                addAssemblyNode.ParentNode.RemoveChild(addAssemblyNode);
            if (addProviderNodeCts != null)
                addProviderNodeCts.ParentNode.RemoveChild(addProviderNodeCts);
            if (addProviderNodeDb != null)
                addProviderNodeDb.ParentNode.RemoveChild(addProviderNodeDb);
        }

        /// <summary>
        /// Determine if the option is configured
        /// </summary>
        public bool IsConfigured(System.Xml.XmlDocument configurationDom)
        {
            XmlNode configSection = configurationDom.SelectSingleNode("//*[local-name() = 'section'][@name = 'marc.hi.ehrs.svc.terminology']"),
                qdcdbPersistenceSection = configurationDom.SelectSingleNode("//*[local-name() = 'marc.hi.ehrs.svc.terminology']/*[local-name() = 'qdcdb']"),
                ctsSection = configurationDom.SelectSingleNode("//*[local-name() = 'marc.hi.ehrs.svc.terminology']/*[local-name() = 'cts']"),
                addAssemblyNode = configurationDom.SelectSingleNode("//*[local-name() = 'marc.hi.ehrs.svc.core']/*[local-name() = 'serviceAssemblies']/*[local-name() = 'add'][@assembly = 'MARC.HI.EHRS.SVC.Terminology.dll']"),
                addProviderNodeCts = configurationDom.SelectSingleNode(String.Format("//*[local-name() = 'marc.hi.ehrs.svc.core']/*[local-name() = 'serviceProviders']/*[local-name() = 'add'][@type = '{0}']", ctsServiceName)),
                addProviderNodeDb = configurationDom.SelectSingleNode(String.Format("//*[local-name() = 'marc.hi.ehrs.svc.core']/*[local-name() = 'serviceProviders']/*[local-name() = 'add'][@type = '{0}']", dbServiceName)),
                persistenceSection = configurationDom.SelectSingleNode("//*[local-name() = 'marc.hi.ehrs.svc.terminology']");

            // Get connection string
            if (persistenceSection != null)
            {

                if (persistenceSection.Attributes["maxMemoryCacheSize"] != null)
                    this.MaxCacheSize = Decimal.Parse(persistenceSection.Attributes["maxMemoryCacheSize"].Value);

                if (qdcdbPersistenceSection != null)
                {
                    string connString = qdcdbPersistenceSection.SelectSingleNode("./@connection").Value,
                        invariantProvider = configurationDom.SelectSingleNode(String.Format("//*[local-name() = 'connectionStrings']/*[local-name() = 'add'][@name = '{0}']/@providerName", connString)).Value;
                    // First get the provider
                    this.DatabaseConfigurator = DatabaseConfiguratorRegistrar.Configurators.Find(o => o.InvariantName == invariantProvider);
                    this.ConnectionString = connString;
                    this.EnableLocal = true;


                }
                if (ctsSection != null && ctsSection.Attributes["messageRuntimeUrl"] != null)
                {
                    this.CtsUrl = ctsSection.Attributes["messageRuntimeUrl"].Value;
                    this.EnableCts = ctsSection != null &&
                        (qdcdbPersistenceSection != null && qdcdbPersistenceSection.Attributes["enableCtsFallback"] != null && Convert.ToBoolean(qdcdbPersistenceSection.Attributes["enableCtsFallback"].Value) || qdcdbPersistenceSection == null);
                }
            }

            // Set config options
            this.m_configPanel.DatabaseConfigurator = this.DatabaseConfigurator;
            this.m_configPanel.SetConnectionString(configurationDom, this.ConnectionString);
            this.m_configPanel.EnableLocalValidation = this.EnableLocal;
            this.m_configPanel.EnableRemoteValidation = this.EnableCts;
            this.m_configPanel.MaxCacheSize = this.MaxCacheSize;
            this.m_configPanel.CtsUrl = this.CtsUrl;
            
            if (configSection != null && persistenceSection != null && 
                (qdcdbPersistenceSection != null || ctsSection != null) 
                && addAssemblyNode != null &&
                (addProviderNodeDb != null || addProviderNodeCts != null)
                && !EnableConfiguration)
                EnableConfiguration = true;

            // Enable configuration
            return configSection != null && persistenceSection != null &&
                (qdcdbPersistenceSection != null || ctsSection != null)
                && addAssemblyNode != null &&
                (addProviderNodeDb != null || addProviderNodeCts != null);
        }

        /// <summary>
        /// Validate the configuration options
        /// </summary>
        public bool Validate(System.Xml.XmlDocument configurationDom)
        {
            this.DatabaseConfigurator = m_configPanel.DatabaseConfigurator;
            this.ConnectionString = m_configPanel.GetConnectionString(configurationDom);
            this.EnableCts = this.m_configPanel.EnableRemoteValidation;
            this.EnableLocal = this.m_configPanel.EnableLocalValidation;
            this.CtsUrl = this.m_configPanel.CtsUrl;
            this.MaxCacheSize = this.m_configPanel.MaxCacheSize;
            bool isValid = true;
            isValid &= (ConnectionString != null && DatabaseConfigurator != null && this.EnableLocal) || !this.EnableLocal;
            isValid &= (this.EnableCts && !String.IsNullOrEmpty(this.CtsUrl)) || !this.EnableCts;
            isValid &= this.EnableLocal || this.EnableCts;
            return isValid;
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


        /// <summary>
        /// Override of tostring
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return this.Name;
        }
        #endregion
    }


}
