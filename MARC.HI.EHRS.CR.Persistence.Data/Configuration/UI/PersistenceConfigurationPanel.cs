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
using System.Xml;
using System.Windows.Forms;
using MARC.HI.EHRS.CR.Configurator.SharedHealthCore.Panels;
using MARC.HI.EHRS.CR.Core.ComponentModel;
using System.Reflection;
using MARC.HI.EHRS.CR.Persistence.Data;
using MARC.HI.EHRS.CR.Persistence.Data.Configuration;
using MARC.HI.EHRS.SVC.Core.DataTypes;
using MARC.HI.EHRS.SVC.Core;
using MARC.HI.EHRS.CR.Core.Configuration;
using MARC.HI.EHRS.CR.Core;
using System.Collections.Specialized;
using System.Diagnostics;
using MARC.HI.EHRS.CR.Persistence.Data.Configuration.UI;
using MARC.HI.EHRS.CR.Core.Data;

namespace MARC.HI.EHRS.CR.Configurator.SharedHealthCore
{
    public class PersistenceConfigurationPanel : IDataboundConfigurationPanel, IAutoDeployConfigurationPanel
    {
        private string serviceName = null;
        private pnlConfigureDatabase m_configPanel = null;
        private bool m_needSync = true;

        /// <summary>
        /// Allow duplicate records
        /// </summary>
        public bool AllowDuplicateRecords { get; set; }
        /// <summary>
        /// True if auto-merge shoudl be enabled
        /// </summary>
        public bool AutoMerge { get; set; }
        /// <summary>
        /// True if update when existing records are registered
        /// </summary>
        public bool UpdateIfExists { get; set; }
        /// <summary>
        /// True if minimum auto-merge criteria
        /// </summary>
        public int MinAutoMergeCriteria { get; set; }
        /// <summary>
        /// True if merge criteria
        /// </summary>
        public List<String> MergeCriteria { get; set; }
        /// <summary>
        /// Gets or sets the match algorithms
        /// </summary>
        public List<String> MatchAlgorithms { get; set; }
        /// <summary>
        /// GEts or sets the default matching strength
        /// </summary>
        public string DefaultMatchStrength { get; set; }
        /// <summary>
        /// Persistence configuration panel constructor
        /// </summary>
        public PersistenceConfigurationPanel()
        {
            this.m_configPanel = new pnlConfigureDatabase();
            serviceName = typeof(DatabasePersistenceService).AssemblyQualifiedName;
            // OID Extensions used
            OidRegistrar.ExtendedAttributes.Add("IsUniqueIdentifier", typeof(bool));
            OidRegistrar.ExtendedAttributes.Add("GloballyAssignable", typeof(bool));
            OidRegistrar.ExtendedAttributes.Add("IsMergeSurvivor", typeof(bool));
            this.UpdateIfExists = true;
            this.AutoMerge = false;
            this.DefaultMatchStrength = "Strong";
            this.MatchAlgorithms = new List<string>() {
                "Soundex",
                "Variant",
                "Exact"
            };
            this.MergeCriteria = new List<string>() {
                "Names",
                "GenderCode",
                "BirthTime",
                "OtherIdentifiers",
                "Addresses"
            };
            this.MinAutoMergeCriteria = 4;
            
        }

        #region IConfigurationPanel Members

        /// <summary>
        /// Enable configuration
        /// </summary>
        public bool EnableConfiguration { get; set; }

        /// <summary>
        /// Gets the name of the panel
        /// </summary>
        public string Name
        {
            get { return "Client Registry/Persistence"; }
        }

        /// <summary>
        /// Gets the windows panel to display
        /// </summary>
        public System.Windows.Forms.Control Panel
        {
            get 
            {
                
                return this.m_configPanel;
            }
        }

        /// <summary>
        /// Configure the option to the specified configurationDom
        /// </summary>
        public void Configure(System.Xml.XmlDocument configurationDom)
        {
            if (this.ConnectionString == null || this.DatabaseConfigurator == null)
                throw new ArgumentNullException("Unable to connect to the database");
            else if (!this.EnableConfiguration)
                return;

            XmlElement configSectionsNode = configurationDom.SelectSingleNode("//*[local-name() = 'configSections']") as XmlElement,
                persistenceNode = configurationDom.SelectSingleNode("//*[local-name() = 'marc.hi.ehrs.cr.persistence.data']") as XmlElement,
                crNode = configurationDom.SelectSingleNode("//*[local-name() = 'marc.hi.ehrs.cr']") as XmlElement,
                coreNode = configurationDom.SelectSingleNode("//*[local-name() = 'marc.hi.ehrs.svc.core']") as XmlElement;

            // Config sections node
            if (configSectionsNode == null)
            {
                configSectionsNode = configurationDom.CreateElement("configSections");
                configurationDom.DocumentElement.PrependChild(configSectionsNode);
            }
            XmlElement configSectionNode = configSectionsNode.SelectSingleNode("./*[local-name() = 'section'][@name = 'marc.hi.ehrs.cr.persistence.data']") as XmlElement;
            if (configSectionNode == null)
            {
                configSectionNode = configurationDom.CreateElement("section");
                configSectionNode.Attributes.Append(configurationDom.CreateAttribute("name"));
                configSectionNode.Attributes.Append(configurationDom.CreateAttribute("type"));
                configSectionNode.Attributes["name"].Value = "marc.hi.ehrs.cr.persistence.data";
                configSectionNode.Attributes["type"].Value = typeof(ConfigurationSectionHandler).AssemblyQualifiedName;
                configSectionsNode.AppendChild(configSectionNode);
            }
            configSectionNode = configSectionsNode.SelectSingleNode("./*[local-name() = 'section'][@name = 'marc.hi.ehrs.cr']") as XmlElement;
            if (configSectionNode == null)
            {
                configSectionNode = configurationDom.CreateElement("section");
                configSectionNode.Attributes.Append(configurationDom.CreateAttribute("name"));
                configSectionNode.Attributes.Append(configurationDom.CreateAttribute("type"));
                configSectionNode.Attributes["name"].Value = "marc.hi.ehrs.cr";
                configSectionNode.Attributes["type"].Value = typeof(ClientRegistryConfigurationSectionHandler).AssemblyQualifiedName;
                configSectionsNode.AppendChild(configSectionNode);
            }

            // Persistence section node
            if (persistenceNode == null)
            {
                persistenceNode = configurationDom.CreateElement("marc.hi.ehrs.cr.persistence.data");
                configurationDom.DocumentElement.AppendChild(persistenceNode);
            }
            XmlElement validationNode = persistenceNode.SelectSingleNode("./*[local-name() = 'validation']") as XmlElement,
                connectionManager = persistenceNode.SelectSingleNode("./*[local-name() = 'connectionManager']") as XmlElement,
                nameMatchNode = persistenceNode.SelectSingleNode("./*[local-name() = 'nameMatching']") as XmlElement;
            if (validationNode == null)
            {
                validationNode = configurationDom.CreateElement("validation");
                persistenceNode.AppendChild(validationNode);
            }

            if(validationNode.Attributes["minPersonNameMatch"] == null)
                validationNode.Attributes.Append(configurationDom.CreateAttribute("minPersonNameMatch"));
            if(validationNode.Attributes["personMustExist"] == null)
                validationNode.Attributes.Append(configurationDom.CreateAttribute("personMustExist"));
            if(validationNode.Attributes["allowDuplicates"] == null)
                validationNode.Attributes.Append(configurationDom.CreateAttribute("allowDuplicates"));

            validationNode.Attributes["minPersonNameMatch"].Value = "1.0";
            validationNode.Attributes["personMustExist"].Value = "false";
            validationNode.Attributes["allowDuplicates"].Value = this.AllowDuplicateRecords.ToString();

            if (nameMatchNode == null)
                nameMatchNode = persistenceNode.AppendChild(configurationDom.CreateElement("nameMatching")) as XmlElement;
            nameMatchNode.RemoveAll();
            if (nameMatchNode.Attributes["defaultMatchStr"] == null)
                nameMatchNode.Attributes.Append(configurationDom.CreateAttribute("defaultMatchStr"));
            nameMatchNode.Attributes["defaultMatchStr"].Value = this.DefaultMatchStrength ?? "Exact";
            foreach (var match in this.MatchAlgorithms ?? new List<String>() { "Exact" } )
            {
                XmlElement matchNode = nameMatchNode.AppendChild(configurationDom.CreateElement("algorithm")) as XmlElement;
                XmlAttribute nameNode = matchNode.Attributes.Append(configurationDom.CreateAttribute("name"));
                nameNode.Value = match;
            }
            

            if (connectionManager == null)
            {
                connectionManager = configurationDom.CreateElement("connectionManager");
                persistenceNode.AppendChild(connectionManager);
            }

            if(connectionManager.Attributes["connection"] == null)
                connectionManager.Attributes.Append(configurationDom.CreateAttribute("connection"));
            connectionManager.Attributes["connection"].Value = this.ConnectionString;


            // Ensure the assembly is loaded and the provider registered
            if (coreNode == null)
            {
                coreNode = configurationDom.CreateElement("marc.hi.ehrs.svc.core");
                configurationDom.DocumentElement.AppendChild(coreNode);

                // Add the config section
                configSectionNode = configurationDom.CreateElement("section");
                configSectionNode.Attributes.Append(configurationDom.CreateAttribute("name"));
                configSectionNode.Attributes.Append(configurationDom.CreateAttribute("type"));
                configSectionNode.Attributes["name"].Value = "marc.hi.ehrs.svc.core";
                configSectionNode.Attributes["type"].Value = typeof(HostConfigurationSectionHandler).AssemblyQualifiedName;
                configSectionsNode.AppendChild(configSectionNode);
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
            XmlElement addServiceAsmNode = serviceAssemblyNode.SelectSingleNode("./*[local-name() = 'add'][@assembly = 'MARC.HI.EHRS.CR.Persistence.Data.dll']") as XmlElement,
                addServiceProvNode = serviceProviderNode.SelectSingleNode(String.Format("./*[local-name() = 'add'][@type = '{0}']", serviceName)) as XmlElement;
            if (addServiceAsmNode == null)
            {
                addServiceAsmNode = configurationDom.CreateElement("add");
                addServiceAsmNode.Attributes.Append(configurationDom.CreateAttribute("assembly"));
                addServiceAsmNode.Attributes["assembly"].Value = "MARC.HI.EHRS.CR.Persistence.Data.dll";
                serviceAssemblyNode.AppendChild(addServiceAsmNode);
            }
            if (addServiceProvNode == null)
            {
                addServiceProvNode = configurationDom.CreateElement("add");
                addServiceProvNode.Attributes.Append(configurationDom.CreateAttribute("type"));
                addServiceProvNode.Attributes["type"].Value = serviceName;
                serviceProviderNode.AppendChild(addServiceProvNode);
            }

            // Service node for configuration
            addServiceProvNode = serviceProviderNode.SelectSingleNode(String.Format("./*[local-name() = 'add'][@type = '{0}']", typeof(ClientRegistryConfigurationProvider).AssemblyQualifiedName)) as XmlElement;
            if (addServiceProvNode == null)
            {
                addServiceProvNode = configurationDom.CreateElement("add");
                addServiceProvNode.Attributes.Append(configurationDom.CreateAttribute("type"));
                addServiceProvNode.Attributes["type"].Value = typeof(ClientRegistryConfigurationProvider).AssemblyQualifiedName;
                serviceProviderNode.AppendChild(addServiceProvNode);
            }

            // Service node for merging
            addServiceProvNode = serviceProviderNode.SelectSingleNode(String.Format("./*[local-name() = 'add'][@type = '{0}']", typeof(DatabaseMergeService).AssemblyQualifiedName)) as XmlElement;
            if (addServiceProvNode == null)
            {
                addServiceProvNode = configurationDom.CreateElement("add");
                addServiceProvNode.Attributes.Append(configurationDom.CreateAttribute("type"));
                addServiceProvNode.Attributes["type"].Value = typeof(DatabaseMergeService).AssemblyQualifiedName;
                serviceProviderNode.AppendChild(addServiceProvNode);
            }

            // Core data service
            addServiceProvNode = serviceProviderNode.SelectSingleNode(String.Format("./*[local-name() = 'add'][@type = '{0}']", typeof(ClientRegistryDataService).AssemblyQualifiedName)) as XmlElement;
            if (addServiceProvNode == null)
            {
                addServiceProvNode = configurationDom.CreateElement("add");
                addServiceProvNode.Attributes.Append(configurationDom.CreateAttribute("type"));
                addServiceProvNode.Attributes["type"].Value = typeof(ClientRegistryDataService).AssemblyQualifiedName;
                serviceProviderNode.AppendChild(addServiceProvNode);
            }

            // Add the core config section
            if (crNode == null)
            {
                crNode = configurationDom.CreateElement("marc.hi.ehrs.cr");
                configurationDom.DocumentElement.AppendChild(crNode);
            }
            // Registration node
            XmlElement registrationCriteria = crNode.SelectSingleNode("./*[local-name() = 'registration']") as XmlElement;
            if (registrationCriteria == null)
            {
                registrationCriteria = configurationDom.CreateElement("registration");
                crNode.AppendChild(registrationCriteria);
            }
            // Set auto merge
            registrationCriteria.RemoveAll();
            if (registrationCriteria.Attributes["autoMerge"] == null)
                registrationCriteria.Attributes.Append(configurationDom.CreateAttribute("autoMerge"));
            registrationCriteria.Attributes["autoMerge"].Value = this.AutoMerge.ToString();
            if(registrationCriteria.Attributes["updateIfExists"] == null)
                registrationCriteria.Attributes.Append(configurationDom.CreateAttribute("updateIfExists"));
            registrationCriteria.Attributes["updateIfExists"].Value = this.UpdateIfExists.ToString();
            if (registrationCriteria.Attributes["minimumAutoMergeMatchCriteria"] == null)
                registrationCriteria.Attributes.Append(configurationDom.CreateAttribute("minimumAutoMergeMatchCriteria"));
            registrationCriteria.Attributes["minimumAutoMergeMatchCriteria"].Value = this.MinAutoMergeCriteria.ToString();
            
            
            // Next do criteria
            
            foreach (var prop in this.MergeCriteria)
            {
                XmlElement mergeCrit = registrationCriteria.AppendChild(configurationDom.CreateElement("mergeCriterion")) as XmlElement;
                XmlAttribute fieldName = mergeCrit.Attributes.Append(configurationDom.CreateAttribute("field"));
                fieldName.Value = prop;
            }

            // Add registration node
            // Instruct the database to create the feature for core
            bool shouldQuit = false;
            while(!shouldQuit)
                try
                {
                    this.DatabaseConfigurator.DeployFeature("CR-DDL", this.ConnectionString, configurationDom);
                    this.DatabaseConfigurator.DeployFeature("CR-FX", this.ConnectionString, configurationDom);
                    this.DatabaseConfigurator.DeployFeature("CR-MRG", this.ConnectionString, configurationDom);
                    this.DatabaseConfigurator.DeployFeature("CR-NAME", this.ConnectionString, configurationDom);
                    this.DatabaseConfigurator.DeployFeature("CR-SRCH", this.ConnectionString, configurationDom);
                    shouldQuit = true;
                }
                catch (Exception)
                {
                    switch (MessageBox.Show("There was an error deploying the CR schema to the database. This commonly occurs when an older version of the schema exists in the selected database. Would you like to try removing the old schema and re-deploying? (Selecting No will ignore this error)", "Error during deploy", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question))
                    {
                        case DialogResult.Yes:
                            this.DatabaseConfigurator.UnDeployFeature("CR-DDL", this.ConnectionString, configurationDom);
                            this.DatabaseConfigurator.UnDeployFeature("CR-FX", this.ConnectionString, configurationDom);
                            this.DatabaseConfigurator.UnDeployFeature("CR-MRG", this.ConnectionString, configurationDom);
                            this.DatabaseConfigurator.UnDeployFeature("CR-NAME", this.ConnectionString, configurationDom);
                            this.DatabaseConfigurator.UnDeployFeature("CR-SRCH", this.ConnectionString, configurationDom);
                            break;
                        case DialogResult.Cancel:
                            throw;
                        case DialogResult.No:
                            shouldQuit = true;
                            break;
                    }
                }
            this.m_needSync = true;
        }

        /// <summary>
        /// UnConfigure the option from the specified dom
        /// </summary>
        public void UnConfigure(System.Xml.XmlDocument configurationDom)
        {
            try
            {
                if (MessageBox.Show("Would you like to uninstall the Client Registry database schema?", "Confirm DROP", MessageBoxButtons.YesNo) == DialogResult.Yes)
                {
                    this.DatabaseConfigurator.UnDeployFeature("CR-DDL", this.ConnectionString, configurationDom);
                    this.DatabaseConfigurator.UnDeployFeature("CR-FX", this.ConnectionString, configurationDom);
                    this.DatabaseConfigurator.UnDeployFeature("CR-MRG", this.ConnectionString, configurationDom);
                    this.DatabaseConfigurator.UnDeployFeature("CR-NAME", this.ConnectionString, configurationDom);
                    this.DatabaseConfigurator.UnDeployFeature("CR-SRCH", this.ConnectionString, configurationDom);
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(String.Format("Could not un-deploy database schema: {0}", e.Message), "Error");
            }
            // Select the relevant items and un-configure
            XmlNode configSection = configurationDom.SelectSingleNode("//*[local-name() = 'section'][@name = 'marc.hi.ehrs.cr.persistence.data']"),
                crConfigSection = configurationDom.SelectSingleNode("//*[local-name() = 'section'][@name = 'marc.hi.ehrs.cr']"),
                persistenceSection = configurationDom.SelectSingleNode("//*[local-name() = 'marc.hi.ehrs.cr.persistence.data']"),
                crSection = configurationDom.SelectSingleNode("//*[local-name() = 'marc.hi.ehrs.cr']"),
                addAssemblyNode = configurationDom.SelectSingleNode("//*[local-name() = 'marc.hi.ehrs.svc.core']/*[local-name() = 'serviceAssemblies']/*[local-name() = 'add'][@assembly = 'MARC.HI.EHRS.CR.Persistence.Data.dll']"),
                addProviderNode = configurationDom.SelectSingleNode(String.Format("//*[local-name() = 'marc.hi.ehrs.svc.core']/*[local-name() = 'serviceProviders']/*[local-name() = 'add'][@type = '{0}']", serviceName));

            if (configSection != null)
                configSection.ParentNode.RemoveChild(configSection);
            if (persistenceSection != null)
                persistenceSection.ParentNode.RemoveChild(persistenceSection);
            if (addAssemblyNode != null)
                addAssemblyNode.ParentNode.RemoveChild(addAssemblyNode);
            if (addProviderNode != null)
                addProviderNode.ParentNode.RemoveChild(addProviderNode);
            if (crConfigSection != null)
                crConfigSection.ParentNode.RemoveChild(crConfigSection);
            if (crSection != null)
                crSection.ParentNode.RemoveChild(crSection);
        }

        /// <summary>
        /// Determine if the option is configured
        /// </summary>
        public bool IsConfigured(System.Xml.XmlDocument configurationDom)
        {
            // Select the relevant items and un-configure
            XmlNode configSection = configurationDom.SelectSingleNode("//*[local-name() = 'section'][@name = 'marc.hi.ehrs.cr.persistence.data']"),
                crConfigSection = configurationDom.SelectSingleNode("//*[local-name() = 'section'][@name = 'marc.hi.ehrs.cr']"),
                persistenceSection = configurationDom.SelectSingleNode("//*[local-name() = 'marc.hi.ehrs.cr.persistence.data']"),
                crSection = configurationDom.SelectSingleNode("//*[local-name() = 'marc.hi.ehrs.cr']"),
                addAssemblyNode = configurationDom.SelectSingleNode("//*[local-name() = 'marc.hi.ehrs.svc.core']/*[local-name() = 'serviceAssemblies']/*[local-name() = 'add'][@assembly = 'MARC.HI.EHRS.CR.Persistence.Data.dll']"),
                addProviderNode = configurationDom.SelectSingleNode(String.Format("//*[local-name() = 'marc.hi.ehrs.svc.core']/*[local-name() = 'serviceProviders']/*[local-name() = 'add'][@type = '{0}']", serviceName));

            // Get connection string
            if (persistenceSection != null)
            {
                string connString = persistenceSection.SelectSingleNode("./*[local-name() = 'connectionManager']/@connection").Value,
                    invariantProvider = configurationDom.SelectSingleNode(String.Format("//*[local-name() = 'connectionStrings']/*[local-name() = 'add'][@name = '{0}']/@providerName", connString)).Value;
                // First get the provider
                this.DatabaseConfigurator = DatabaseConfiguratorRegistrar.Configurators.Find(o => o.InvariantName == invariantProvider);
                this.ConnectionString = connString;

                ConfigurationSectionHandler config = new ConfigurationSectionHandler();
                try
                {
                    config.Create(null, null, persistenceSection);
                }
                catch { }

                this.AllowDuplicateRecords = config.Validation.AllowDuplicateRecords;
                this.DefaultMatchStrength = config.Validation.DefaultMatchStrength.ToString();
                this.MatchAlgorithms = new List<string>();
                foreach (var fi in typeof(MatchAlgorithm).GetFields(BindingFlags.Static | BindingFlags.Public))
                    if ((config.Validation.DefaultMatchAlgorithms & (MatchAlgorithm)fi.GetValue(null)) != 0)
                        this.MatchAlgorithms.Add(fi.Name);

                // Updates available?
                if(this.DatabaseConfigurator is IDatabaseUpdater)
                {
                    var updater = this.DatabaseConfigurator as IDatabaseUpdater;
                    var updates = updater.GetUpdates(this.ConnectionString, configurationDom).Where(o=>o.Id.StartsWith("CR")).ToList();
                    //updates.Sort((a, b) => a.Id.CompareTo(b.Id));
                    while(updates.Count > 0)
                    {
                        using(frmDatabaseUpdateConfirmation dialog = new frmDatabaseUpdateConfirmation())
                        {
                            dialog.Updates = updates;
                            if(dialog.ShowDialog() == DialogResult.OK)
                            {
                                foreach (var upd in updates)
                                    updater.DeployUpdate(upd, this.ConnectionString, configurationDom);
                                
                            }
                        }
                        updates = updater.GetUpdates(this.ConnectionString, configurationDom).Where(o => o.Id.StartsWith("CR")).ToList();
                       // updates.Sort((a, b) => a.Id.CompareTo(b.Id));

            }
                }
            }

            // Get the cr config
            if (crSection != null)
            {
                MARC.HI.EHRS.CR.Core.Configuration.ClientRegistryConfigurationSectionHandler config = new Core.Configuration.ClientRegistryConfigurationSectionHandler();
                try
                {
                    var crConfig = config.Create(null, null, crSection) as MARC.HI.EHRS.CR.Core.Configuration.ClientRegistryConfiguration;
                    this.AutoMerge = crConfig.Registration.AutoMerge;
                    this.MinAutoMergeCriteria = crConfig.Registration.MinimumMergeMatchCriteria;
                    this.UpdateIfExists = crConfig.Registration.UpdateIfExists;
                    this.MergeCriteria = new List<string>();
                    foreach (var crm in crConfig.Registration.MergeCriteria)
                        this.MergeCriteria.Add(crm.FieldName);
                }
                catch { }

            }


            // Set config options
            bool isConfigured = configSection != null && persistenceSection != null && addAssemblyNode != null &&
                addProviderNode != null && crSection != null && crConfigSection != null;
            if (!this.m_needSync)
                return isConfigured;
            this.m_needSync = false;
            if (isConfigured)
            {
                this.m_configPanel.AllowDuplicates = this.AllowDuplicateRecords;
                this.m_configPanel.DefaultMatchStrength = this.DefaultMatchStrength;
                this.m_configPanel.MatchAlgorithms = this.MatchAlgorithms;
                this.m_configPanel.MatchFields = this.MergeCriteria;
                this.m_configPanel.AutoMerge = this.AutoMerge;
                this.m_configPanel.MinMatch = this.MinAutoMergeCriteria;
                this.m_configPanel.UpdateIfExists = this.UpdateIfExists;
                if (configSection != null && persistenceSection != null && addAssemblyNode != null &&
                    addProviderNode != null && !EnableConfiguration && crConfigSection != null && crSection != null)
                    EnableConfiguration = true;
            }
            this.m_configPanel.DatabaseConfigurator = this.DatabaseConfigurator;
            this.m_configPanel.SetConnectionString(configurationDom, this.ConnectionString);

            // Enable configuration
            return isConfigured;
        }

        /// <summary>
        /// Validate the configuration
        /// </summary>
        public bool Validate(XmlDocument configurationDom)
        {
            this.DatabaseConfigurator = m_configPanel.DatabaseConfigurator;
            this.ConnectionString = m_configPanel.GetConnectionString(configurationDom);
            this.AllowDuplicateRecords = this.m_configPanel.AllowDuplicates;
            this.MergeCriteria = this.m_configPanel.MatchFields;
            this.UpdateIfExists = this.m_configPanel.UpdateIfExists;
            this.MinAutoMergeCriteria = (int)this.m_configPanel.MinMatch;
            this.DefaultMatchStrength = this.m_configPanel.DefaultMatchStrength;
            this.MatchAlgorithms = this.m_configPanel.MatchAlgorithms;
            this.AutoMerge = this.m_configPanel.AutoMerge;
            return ConnectionString != null && DatabaseConfigurator != null;
        }
        #endregion

        #region IDataboundConfigurationPanel Members

        /// <summary>
        /// Gets or sets the connection string
        /// </summary>
        public string ConnectionString { get; set; }

        /// <summary>
        /// Gets or sets the configurator
        /// </summary>
        public IDatabaseConfigurator DatabaseConfigurator { get; set; }

        /// <summary>
        /// Represent the panel as a string
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return "Client Registry Persistence Services";
        }

        #endregion

        #region IAutoDeployConfigurationPanel Members

        /// <summary>
        /// Prepare configuration
        /// </summary>
        public void PrepareConfigure(XmlDocument configurationDom, Dictionary<string, System.Collections.Specialized.StringCollection> deploymentOptions)
        {
            StringCollection dbServer = null,
                   dbProvider = null,
                   dbUser = null,
                   dbPassword = null,
                   dbDb = null;

            if (!deploymentOptions.TryGetValue("dbprovider", out dbProvider) ||
                !deploymentOptions.TryGetValue("dbserver", out dbServer) ||
                !deploymentOptions.TryGetValue("dbuser", out dbUser) ||
                !deploymentOptions.TryGetValue("dbpassword", out dbPassword) ||
                !deploymentOptions.TryGetValue("dbdb", out dbDb))
            {
                Trace.TraceError("Insufficient application configuration options");
            }


            // Now, try to create the dbprovider
            this.DatabaseConfigurator = Activator.CreateInstance(Type.GetType(dbProvider[0])) as IDatabaseConfigurator;
            try // to create the "dbname"
            {
                this.DatabaseConfigurator.CreateDatabase(dbServer[0], dbUser[0], dbPassword[0], dbDb[0], dbUser[0]);
            }
            catch { }

            // Setup the connection string
            this.ConnectionString = this.DatabaseConfigurator.CreateConnectionStringElement(configurationDom, dbServer[0], dbUser[0], dbPassword[0], dbDb[0]);
            this.EnableConfiguration = true;
        }

        #endregion
    }
}
