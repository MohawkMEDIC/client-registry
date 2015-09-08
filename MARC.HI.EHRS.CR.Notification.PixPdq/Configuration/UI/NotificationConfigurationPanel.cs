using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MARC.HI.EHRS.SVC.Core.Configuration;
using System.Xml;
using MARC.HI.EHRS.SVC.Core.Configuration.UI;
using System.Security.Cryptography.X509Certificates;
using MARC.Everest.Connectors;
using MARC.Everest.Connectors.WCF.Core;
using System.Configuration;
using System.IO;
using System.Reflection;
using MARC.Everest.RMIM.UV.NE2008.Interactions;
using MARC.HI.EHRS.SVC.Core.DataTypes;
using System.ComponentModel;
using MARC.Everest.Formatters.XML.ITS1;
using MARC.Everest.Formatters.XML.Datatypes.R1;

namespace MARC.HI.EHRS.CR.Notification.PixPdq.Configuration.UI
{
    /// <summary>
    /// Notification configuration panel
    /// </summary>
    public class NotificationConfigurationPanel : IConfigurationPanel
    {
        /// <summary>
        /// Notification CTOR
        /// </summary>
        public NotificationConfigurationPanel()
        {
            OidRegistrar.ExtendedAttributes.Add("CustodialDeviceId", typeof(String));
            OidRegistrar.ExtendedAttributes.Add("CustodialDeviceName", typeof(String));
            OidRegistrar.ExtendedAttributes.Add("CustodialOrgName", typeof(String));
        }

        // Panel
        private pnlNotification m_panel = new pnlNotification();

        // Configuration
        private NotificationConfiguration m_configuration = new NotificationConfiguration(Environment.ProcessorCount);

        // reload configuration
        private bool m_needSync = true;

        #region IConfigurationPanel Members

        /// <summary>
        /// Configure the feature
        /// </summary>
        public void Configure(System.Xml.XmlDocument configurationDom)
        {
            if (!this.EnableConfiguration || this.m_panel.Targets.Count == 0)
                return;

            XmlElement configSectionsNode = configurationDom.SelectSingleNode("//*[local-name() = 'configSections']") as XmlElement,
                notificationNode = configurationDom.SelectSingleNode("//*[local-name() = 'marc.hi.ehrs.cr.notification.pixpdq']") as XmlElement,
                coreNode = configurationDom.SelectSingleNode("//*[local-name() = 'marc.hi.ehrs.svc.core']") as XmlElement,
                everestNode = configurationDom.SelectSingleNode("//*[local-name() = 'marc.everest.connectors.wcf']") as XmlElement;

            // Config sections node
            if (configSectionsNode == null)
            {
                configSectionsNode = configurationDom.CreateElement("configSections");
                configurationDom.DocumentElement.PrependChild(configSectionsNode);
            }
            XmlElement configSectionNode = configSectionsNode.SelectSingleNode("./*[local-name() = 'section'][@name = 'marc.hi.ehrs.cr.notification.pixpdq']") as XmlElement;
            if (configSectionNode == null)
            {
                configSectionNode = configurationDom.CreateElement("section");
                configSectionNode.Attributes.Append(configurationDom.CreateAttribute("name"));
                configSectionNode.Attributes.Append(configurationDom.CreateAttribute("type"));
                configSectionNode.Attributes["name"].Value = "marc.hi.ehrs.cr.notification.pixpdq";
                configSectionNode.Attributes["type"].Value = typeof(ConfigurationSectionHandler).AssemblyQualifiedName;
                configSectionsNode.AppendChild(configSectionNode);
            }
            configSectionNode = configSectionsNode.SelectSingleNode("./*[local-name() = 'section'][@name = 'marc.everest.connectors.wcf']") as XmlElement;
            if (configSectionNode == null)
            {
                configSectionNode = configurationDom.CreateElement("section");
                configSectionNode.Attributes.Append(configurationDom.CreateAttribute("name"));
                configSectionNode.Attributes.Append(configurationDom.CreateAttribute("type"));
                configSectionNode.Attributes["name"].Value = "marc.everest.connectors.wcf";
                configSectionNode.Attributes["type"].Value = typeof(MARC.Everest.Connectors.WCF.Configuration.ConfigurationSection).AssemblyQualifiedName;
                configSectionsNode.AppendChild(configSectionNode);
            }



            // Persistence section node
            if (notificationNode == null)
            {
                notificationNode = configurationDom.CreateElement("marc.hi.ehrs.cr.notification.pixpdq");
                configurationDom.DocumentElement.AppendChild(notificationNode);
            }
            
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


            XmlElement addServiceAsmNode = serviceAssemblyNode.SelectSingleNode("./*[local-name() = 'add'][@assembly = 'MARC.HI.EHRS.CR.Notification.PixPdq.dll']") as XmlElement,
                addServiceProvNode = serviceProviderNode.SelectSingleNode(String.Format("./*[local-name() = 'add'][@type = '{0}']", typeof(PixNotifier).AssemblyQualifiedName)) as XmlElement;
            if (addServiceAsmNode == null)
            {
                addServiceAsmNode = configurationDom.CreateElement("add");
                addServiceAsmNode.Attributes.Append(configurationDom.CreateAttribute("assembly"));
                addServiceAsmNode.Attributes["assembly"].Value = "MARC.HI.EHRS.CR.Notification.PixPdq.dll";
                serviceAssemblyNode.AppendChild(addServiceAsmNode);
            }
            if (addServiceProvNode == null)
            {
                addServiceProvNode = configurationDom.CreateElement("add");
                addServiceProvNode.Attributes.Append(configurationDom.CreateAttribute("type"));
                addServiceProvNode.Attributes["type"].Value = typeof(PixNotifier).AssemblyQualifiedName;
                serviceProviderNode.AppendChild(addServiceProvNode);
            }

           

            // Write the configuration
            XmlElement targetsNode = notificationNode.SelectSingleNode("./*[local-name() = 'targets']") as XmlElement;
            if (targetsNode == null)
                targetsNode = notificationNode.AppendChild(configurationDom.CreateElement("targets")) as XmlElement;
            
            // Now loop through configuration
            foreach (var targ in this.m_panel.Targets)
            {

                // Find an add with the device id
                XmlElement addNode = targetsNode.SelectSingleNode(string.Format("./*[local-name() = 'add'][@name = '{0}']", targ.Configuration.Name)) as XmlElement;
                if (addNode == null)
                    addNode = targetsNode.AppendChild(configurationDom.CreateElement("add")) as XmlElement;



                // Setup WCF endpoint
                if (targ.Configuration.Notifier.GetType().Name.Contains("HL7v3"))
                    CreateWcfClient(configurationDom, targ);
                else
                    targ.Configuration.ConnectionString = targ.Address.ToString();

                // Clear add node
                addNode.RemoveAll();


                // Certificate info
                XmlElement certificateNode = addNode.SelectSingleNode("./*[local-name() = 'trustedIssuerCertificate']") as XmlElement;
                if (targ.ServerCertificate != null)
                {
                    if (certificateNode == null)
                        certificateNode = addNode.AppendChild(configurationDom.CreateElement("trustedIssuerCertificate")) as XmlElement;

                    certificateNode.RemoveAll();
                    certificateNode.Attributes.Append(configurationDom.CreateAttribute("storeLocation"));
                    certificateNode.Attributes.Append(configurationDom.CreateAttribute("storeName"));
                    certificateNode.Attributes.Append(configurationDom.CreateAttribute("x509FindType"));
                    certificateNode.Attributes.Append(configurationDom.CreateAttribute("findValue"));
                    certificateNode.Attributes["storeLocation"].Value = targ.ServerCertificateLocation.ToString();
                    certificateNode.Attributes["storeName"].Value = targ.ServerCertificateStore.ToString();
                    certificateNode.Attributes["x509FindType"].Value = X509FindType.FindByThumbprint.ToString();
                    certificateNode.Attributes["findValue"].Value = targ.ServerCertificate.Thumbprint;
                }
                else if (certificateNode != null)
                    certificateNode.ParentNode.RemoveChild(certificateNode);
                
                // LLP Certificate
                certificateNode = addNode.SelectSingleNode("./*[local-name() = 'clientLLPCertificate']") as XmlElement;
                if (targ.ClientCertificate != null)
                {
                    if (certificateNode == null)
                        certificateNode = addNode.AppendChild(configurationDom.CreateElement("clientLLPCertificate")) as XmlElement;

                    certificateNode.RemoveAll();
                    certificateNode.Attributes.Append(configurationDom.CreateAttribute("storeLocation"));
                    certificateNode.Attributes.Append(configurationDom.CreateAttribute("storeName"));
                    certificateNode.Attributes.Append(configurationDom.CreateAttribute("x509FindType"));
                    certificateNode.Attributes.Append(configurationDom.CreateAttribute("findValue"));
                    certificateNode.Attributes["storeLocation"].Value = targ.ClientCertificateLocation.ToString();
                    certificateNode.Attributes["storeName"].Value = targ.ClientCertificateStore.ToString();
                    certificateNode.Attributes["x509FindType"].Value = X509FindType.FindByThumbprint.ToString();
                    certificateNode.Attributes["findValue"].Value = targ.ClientCertificate.Thumbprint;
                }
                else if (certificateNode != null)
                    certificateNode.ParentNode.RemoveChild(certificateNode);

                // Setup core attribute
                addNode.Attributes.Append(configurationDom.CreateAttribute("connectionString"));
                addNode.Attributes["connectionString"].Value = targ.Configuration.ConnectionString;
                addNode.Attributes.Append(configurationDom.CreateAttribute("deviceId"));
                addNode.Attributes["deviceId"].Value = targ.Configuration.DeviceIdentifier;
                addNode.Attributes.Append(configurationDom.CreateAttribute("name"));
                addNode.Attributes["name"].Value = targ.Configuration.Name;
                addNode.Attributes.Append(configurationDom.CreateAttribute("myActor"));
                addNode.Attributes["myActor"].Value = targ.Configuration.Notifier.GetType().Name;

                // Now append notification domain
                foreach (var ntfy in targ.Configuration.NotificationDomain)
                {
                    var notifyNode = addNode.AppendChild(configurationDom.CreateElement("notify")) as XmlElement;
                    notifyNode.Attributes.Append(configurationDom.CreateAttribute("domain"));
                    notifyNode.Attributes["domain"].Value = ntfy.Domain;
                    foreach (var act in ntfy.Actions)
                    {
                        var actionNode = notifyNode.AppendChild(configurationDom.CreateElement("action")) as XmlElement;
                        actionNode.Attributes.Append(configurationDom.CreateAttribute("type"));
                        actionNode.Attributes["type"].Value = act.Action.ToString();
                    }
                }


            }

            // Everest configuration
            if (everestNode == null)
                everestNode = configurationDom.DocumentElement.AppendChild(configurationDom.CreateElement("marc.everest.connectors.wcf")) as XmlElement;

            // Load and import
            XmlDocument tEverestConfig = new XmlDocument();
            tEverestConfig.LoadXml("<marc.everest.connectors.wcf><action type=\"MARC.Everest.RMIM.UV.NE2008.Interactions.PRPA_IN201301UV02\" action=\"urn:hl7-org:v3:PRPA_IN201301UV02\"/><action type=\"MARC.Everest.RMIM.UV.NE2008.Interactions.PRPA_IN201302UV02\" action=\"urn:hl7-org:v3:PRPA_IN201301UV02\"/><action type=\"MARC.Everest.RMIM.UV.NE2008.Interactions.PRPA_IN201304UV02\" action=\"urn:hl7-org:v3:PRPA_IN201304UV02\"/></marc.everest.connectors.wcf>");
            var tEverestNode = configurationDom.ImportNode(tEverestConfig.DocumentElement, true) as XmlElement;
            foreach (XmlElement child in tEverestNode.ChildNodes)
                if(everestNode.SelectSingleNode(String.Format("./*[local-name() = 'action'][@type='{0}']", child.Attributes["type"].Value)) == null)
                    everestNode.AppendChild(child);

            if(everestNode.Attributes["formatter"] == null)
            {
                everestNode.Attributes.Append(configurationDom.CreateAttribute("formatter"));
                everestNode.Attributes["formatter"].Value=typeof(XmlIts1Formatter).AssemblyQualifiedName;
            }
            if (everestNode.Attributes["aide"] == null)
            {
                everestNode.Attributes.Append(configurationDom.CreateAttribute("aide"));
                everestNode.Attributes["aide"].Value = typeof(DatatypeFormatter).AssemblyQualifiedName;
            }

            this.m_needSync = true;
        }

        /// <summary>
        /// Create WCF Client
        /// </summary>
        private void CreateWcfClient(XmlDocument configurationDom, pnlNotification.TargetConfigurationInformation targ)
        {
            var wcfRoot = configurationDom.SelectSingleNode("//*[local-name() = 'system.serviceModel']") as XmlElement;

            // Create WCF Root?
            if (wcfRoot == null)
                wcfRoot = configurationDom.DocumentElement.AppendChild(configurationDom.CreateElement("system.serviceModel")) as XmlElement;

            // Client node
            var clientNode = wcfRoot.SelectSingleNode("./*[local-name() = 'client']") as XmlElement;
            if (clientNode == null)
                clientNode = wcfRoot.AppendChild(configurationDom.CreateElement("client")) as XmlElement;

            // Connection string ... 
            List<String> endpointName = null;
            if (!ConnectionStringParser.ParseConnectionString(targ.Configuration.ConnectionString).TryGetValue("endpointname", out endpointName))
            {
                endpointName = new List<string>() { Guid.NewGuid().ToString().Substring(0, 6) };
                targ.Configuration.ConnectionString = string.Format("endpointName={0}", endpointName[0]);
            }

            // Endpoint
            var epNode = clientNode.SelectSingleNode(String.Format("./*[local-name() = 'endpoint'][@name = '{0}']", endpointName[0])) as XmlElement;
            if(epNode == null)
            {
                epNode = clientNode.AppendChild(configurationDom.CreateElement("endpoint")) as XmlElement;;
                epNode.Attributes.Append(configurationDom.CreateAttribute("name"));
                epNode.Attributes["name"].Value= endpointName[0];
            }

            // Setup the ep
            if (epNode.Attributes["address"] == null)
                epNode.Attributes.Append(configurationDom.CreateAttribute("address"));
            epNode.Attributes["address"].Value = targ.Address.ToString();

            // Contract
            if (epNode.Attributes["contract"] == null)
                epNode.Attributes.Append(configurationDom.CreateAttribute("contract"));
            epNode.Attributes["contract"].Value = typeof(IConnectorContract).FullName;

            // Binding
            if (epNode.Attributes["binding"] == null)
                epNode.Attributes.Append(configurationDom.CreateAttribute("binding"));
            epNode.Attributes["binding"].Value = "wsHttpBinding";

            // binding configuration
            string bindingConfigurationName = null;
            if (epNode.Attributes["bindingConfiguration"] != null)
                bindingConfigurationName = epNode.Attributes["bindingConfiguration"].Value;
            else
            {
                epNode.Attributes.Append(configurationDom.CreateAttribute("bindingConfiguration"));
                bindingConfigurationName = String.Format("{0}_binding", endpointName[0]);
                epNode.Attributes["bindingConfiguration"].Value = bindingConfigurationName;
            }
            
            // behavior configuration
            string behaviorConfigurationName = null;
            if (epNode.Attributes["behaviorConfiguration"] != null)
                behaviorConfigurationName = epNode.Attributes["behaviorConfiguration"].Value;
            else 
            {
                epNode.Attributes.Append(configurationDom.CreateAttribute("behaviorConfiguration"));
                behaviorConfigurationName = String.Format("{0}_behavior", endpointName[0]);
                epNode.Attributes["behaviorConfiguration"].Value = behaviorConfigurationName;
            }


            // Binding
            CreateWcfBehaviorConfiguration(behaviorConfigurationName, targ, wcfRoot);
            CreateWcfBindingConfiguration(bindingConfigurationName, targ, wcfRoot);
        }

        /// <summary>
        /// Create behavior
        /// </summary>
        private void CreateWcfBehaviorConfiguration(string behaviorName, MARC.HI.EHRS.CR.Notification.PixPdq.Configuration.UI.pnlNotification.TargetConfigurationInformation configInfo, XmlElement wcfNode)
        {
            XmlDocument configurationDom = wcfNode.OwnerDocument;
            XmlElement wcfBehaviorNode = wcfNode.SelectSingleNode("./*[local-name() = 'behaviors']") as XmlElement;
            if (wcfBehaviorNode == null)
                wcfBehaviorNode = wcfNode.AppendChild(configurationDom.CreateElement("behaviors")) as XmlElement;

            XmlElement wcfServiceBehaviorNode = wcfBehaviorNode.SelectSingleNode("./*[local-name() = 'endpointBehaviors']") as XmlElement;
            if (wcfServiceBehaviorNode == null)
                wcfServiceBehaviorNode = wcfBehaviorNode.AppendChild(configurationDom.CreateElement("endpointBehaviors")) as XmlElement;

            XmlElement wcfRevisionBehaviorNode = wcfServiceBehaviorNode.SelectSingleNode(String.Format("./*[local-name() = 'behavior'][@name = '{0}']", behaviorName)) as XmlElement;
            if (wcfRevisionBehaviorNode == null)
            {
                wcfRevisionBehaviorNode = wcfServiceBehaviorNode.AppendChild(configurationDom.CreateElement("behavior")) as XmlElement;
                wcfRevisionBehaviorNode.Attributes.Append(configurationDom.CreateAttribute("name")).Value = behaviorName;
            }

            // Security?
            XmlElement wcfClientCredentialsNode = wcfRevisionBehaviorNode.SelectSingleNode("./*[local-name() = 'clientCredentials']") as XmlElement;
            if (configInfo.ClientCertificate != null) // Security is enabled
            {
                if (wcfClientCredentialsNode == null)
                    wcfClientCredentialsNode = wcfRevisionBehaviorNode.AppendChild(configurationDom.CreateElement("clientCredentials")) as XmlElement;
                wcfClientCredentialsNode.RemoveAll();

                XmlElement wcfServiceCertificateNode = wcfClientCredentialsNode.AppendChild(configurationDom.CreateElement("clientCertificate")) as XmlElement;
                wcfServiceCertificateNode.Attributes.Append(configurationDom.CreateAttribute("storeLocation")).Value = configInfo.ClientCertificateLocation.ToString();
                wcfServiceCertificateNode.Attributes.Append(configurationDom.CreateAttribute("storeName")).Value = configInfo.ClientCertificateStore.ToString();
                wcfServiceCertificateNode.Attributes.Append(configurationDom.CreateAttribute("x509FindType")).Value = X509FindType.FindByThumbprint.ToString();
                wcfServiceCertificateNode.Attributes.Append(configurationDom.CreateAttribute("findValue")).Value = configInfo.ClientCertificate.Thumbprint;

                // Validate server ?
                if (configInfo.ValidateServerCert)
                {
                    XmlElement clientCertNode = wcfClientCredentialsNode.AppendChild(configurationDom.CreateElement("serviceCertificate")) as XmlElement,
                        authNode = clientCertNode.AppendChild(configurationDom.CreateElement("authentication")) as XmlElement;
                    authNode.Attributes.Append(configurationDom.CreateAttribute("certificateValidationMode"));
                    authNode.Attributes["certificateValidationMode"].Value = "Custom";
                    authNode.Attributes.Append(configurationDom.CreateAttribute("customCertificateValidatorType"));
                    authNode.Attributes["customCertificateValidatorType"].Value = typeof(SecureNodeCertificateValidator).AssemblyQualifiedName;
                }

            }
            else if (wcfClientCredentialsNode != null) // Remove the credentials node
                wcfRevisionBehaviorNode.RemoveChild(wcfClientCredentialsNode);
        }

        /// <summary>
        /// Binding Configuration
        /// </summary>
        private void CreateWcfBindingConfiguration(string bindingName, MARC.HI.EHRS.CR.Notification.PixPdq.Configuration.UI.pnlNotification.TargetConfigurationInformation configInfo, XmlElement wcfNode)
        {
            XmlDocument configurationDom = wcfNode.OwnerDocument;
            XmlElement wcfBindingNode = wcfNode.SelectSingleNode("./*[local-name() = 'bindings']") as XmlElement;
            if (wcfBindingNode == null)
                wcfBindingNode = wcfNode.AppendChild(configurationDom.CreateElement("bindings")) as XmlElement;

            // Get the binding name
            var bindingType = wcfNode.SelectSingleNode(string.Format(".//*[local-name() = 'client']//*[local-name() = 'endpoint'][@bindingConfiguration = '{0}']/@binding", bindingName));
            if (bindingType == null)
                throw new ConfigurationErrorsException("Cannot determine the binding for the specified configuration, does the endpoint have the binding attribute?");

            XmlElement wcfBindingTypeNode = wcfBindingNode.SelectSingleNode(String.Format("./*[local-name() = '{0}']", bindingType.Value)) as XmlElement;
            if (wcfBindingTypeNode == null)
                wcfBindingTypeNode = wcfBindingNode.AppendChild(configurationDom.CreateElement(bindingType.Value)) as XmlElement;

            // Is there a binding with our name on it?
            XmlElement wcfBindingConfigurationNode = wcfBindingTypeNode.SelectSingleNode(string.Format("./*[local-name() = 'binding'][@name = '{0}']", bindingName)) as XmlElement;
            if (wcfBindingConfigurationNode == null)
            {
                XmlDocument defaultBindingInfo = new XmlDocument();
                defaultBindingInfo.Load(Path.Combine(Path.Combine(Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), "config"), "pix"), "NotificationBinding.xml"));
                wcfBindingConfigurationNode = wcfBindingTypeNode.AppendChild(configurationDom.ImportNode(defaultBindingInfo.DocumentElement, true)) as XmlElement;
            }

            if (wcfBindingConfigurationNode.Attributes["name"] == null)
                wcfBindingConfigurationNode.Attributes.Append(configurationDom.CreateAttribute("name"));
            wcfBindingConfigurationNode.Attributes["name"].Value = bindingName;

            // Security?
            XmlElement wcfSecurityModeNode = wcfBindingConfigurationNode.SelectSingleNode("./*[local-name() = 'security']") as XmlElement;
            if (wcfSecurityModeNode == null)
                wcfSecurityModeNode = wcfBindingConfigurationNode.AppendChild(configurationDom.CreateElement("security")) as XmlElement;
            if (wcfSecurityModeNode.Attributes["mode"] == null)
                wcfSecurityModeNode.Attributes.Append(configurationDom.CreateAttribute("mode"));

            if (configInfo.Address.Scheme == "https")
            {
                wcfSecurityModeNode.RemoveAll();
                wcfSecurityModeNode.Attributes.Append(configurationDom.CreateAttribute("mode")).Value = "Transport";
                // Transport options
                var wcfTransportNode = wcfSecurityModeNode.AppendChild(configurationDom.CreateElement("transport")) as XmlElement;
                wcfTransportNode.Attributes.Append(configurationDom.CreateAttribute("clientCredentialType")).Value = configInfo.ClientCertificate != null ? "Certificate" : "None";
            }
            else
                wcfSecurityModeNode.Attributes["mode"].Value = "None";

        }

        /// <summary>
        /// True if configuration is enabled
        /// </summary>
        public bool EnableConfiguration
        {
            get;
            set;
        }

        /// <summary>
        /// Load configuration, populate the UI, etc
        /// </summary>
        /// <param name="configurationDom"></param>
        /// <returns></returns>
        public bool IsConfigured(System.Xml.XmlDocument configurationDom)
        {
            // This is a complex configuration so here we go.
            XmlElement configSectionNode = configurationDom.SelectSingleNode("//*[local-name() = 'configSections']/*[local-name() = 'section'][@name = 'marc.hi.ehrs.cr.notification.pixpdq']") as XmlElement,
                configRoot = configurationDom.SelectSingleNode("//*[local-name() = 'marc.hi.ehrs.cr.notification.pixpdq']") as XmlElement,
                wcfRoot = configurationDom.SelectSingleNode("//*[local-name() = 'system.serviceModel']") as XmlElement;

            // Load the current config if applicable
            if (configRoot != null)
                this.m_configuration = new ConfigurationSectionHandler().Create(null, null, configRoot) as NotificationConfiguration;
            else
                this.m_configuration = new NotificationConfiguration(Environment.ProcessorCount);

            bool isConfigured = configSectionNode != null && configRoot != null &&
                wcfRoot != null && this.m_configuration != null && this.m_configuration.Targets.Count > 0;
            if (!this.m_needSync)
                return isConfigured;
            this.EnableConfiguration = isConfigured;
            this.m_needSync = false;

            if (configRoot == null) // makes the following logic clearer
                configRoot = configurationDom.CreateElement("marc.hi.ehrs.cr.notification.pixpdq");
            this.m_panel.OidRegistrar = OidRegistrarConfigurationPanel.LoadOidRegistrar(configurationDom);

            this.m_panel.SetTargets(this.m_configuration.Targets, wcfRoot);


            // Loop through the configuration templates 
            return isConfigured;
        }

        /// <summary>
        /// Gets the name of the panel
        /// </summary>
        public string Name
        {
            get { return "PIXv3 Notifications"; }
        }

        /// <summary>
        /// Gets the panel for configuration
        /// </summary>
        public System.Windows.Forms.Control Panel
        {
            get { return this.m_panel; }
        }

        /// <summary>
        /// Un-Configure the system
        /// </summary>
        public void UnConfigure(System.Xml.XmlDocument configurationDom)
        {
            // This is a complex configuration so here we go.
            XmlElement configSectionNode = configurationDom.SelectSingleNode("//*[local-name() = 'configSections']/*[local-name() = 'section'][@name = 'marc.hi.ehrs.cr.notification.pixpdq']") as XmlElement,
                configRoot = configurationDom.SelectSingleNode("//*[local-name() = 'marc.hi.ehrs.cr.notification.pixpdq']") as XmlElement,
                wcfRoot = configurationDom.SelectSingleNode("//*[local-name() = 'system.serviceModel']") as XmlElement,
                addAssemblyNode = configurationDom.SelectSingleNode("//*[local-name() = 'marc.hi.ehrs.svc.core']/*[local-name() = 'serviceAssemblies']/*[local-name() = 'add'][@assembly = 'MARC.HI.EHRS.CR.Notification.PixPdq.dll']") as XmlElement,
                addProviderNode = configurationDom.SelectSingleNode(String.Format("//*[local-name() = 'marc.hi.ehrs.svc.core']/*[local-name() = 'serviceProviders']/*[local-name() = 'add'][@type = '{0}']", typeof(PixNotifier).AssemblyQualifiedName)) as XmlElement;

            // Remove the sections
            if (configSectionNode != null)
                configSectionNode.ParentNode.RemoveChild(configSectionNode);
            if (configRoot != null)
                configRoot.ParentNode.RemoveChild(configRoot);
            if (addAssemblyNode != null)
                addAssemblyNode.ParentNode.RemoveChild(addAssemblyNode);
            if (addProviderNode != null)
                addProviderNode.ParentNode.RemoveChild(addProviderNode);

            if (wcfRoot != null && this.m_configuration.Targets != null)
            {
                // Remove each WCF configuration
                foreach (var targ in this.m_configuration.Targets)
                    {
                        var connectionString = ConnectionStringParser.ParseConnectionString(targ.ConnectionString);
                        List<String> serviceColl = null;
                        if(connectionString.TryGetValue("endpointname", out serviceColl))
                        {
                            var serviceName = serviceColl[0];
                            // Lookup the service information
                            XmlElement endpointNode = wcfRoot.SelectSingleNode(String.Format(".//*[local-name() = 'client']/*[local-name() = 'endpoint'][@name = '{0}']", serviceName)) as XmlElement;
                            if (endpointNode == null) continue;
                            if (endpointNode.Attributes["behaviorConfiguration"] != null)
                            {
                                XmlElement behavior = wcfRoot.SelectSingleNode(String.Format(".//*[local-name() = 'behavior'][@name = '{0}']", endpointNode.Attributes["behaviorConfiguration"].Value)) as XmlElement;
                                if (behavior != null)
                                {
                                    XmlElement serviceCertificateNode = behavior.SelectSingleNode(".//*[local-name() = 'serviceCertificate']") as XmlElement;
                                    behavior.ParentNode.RemoveChild(behavior);
                                }
                            }

                            // Remove the bindings
                            if (endpointNode.Attributes["bindingConfiguration"] != null)
                            {
                                var binding = wcfRoot.SelectSingleNode(String.Format(".//*[local-name() = 'binding'][@name = '{0}']", endpointNode.Attributes["bindingConfiguration"].Value)) as XmlElement;

                                if (binding != null)
                                    binding.ParentNode.RemoveChild(binding);
                            }
                            endpointNode.ParentNode.RemoveChild(endpointNode);

                        }

                    }
            }

            this.m_needSync = true;
        }

        /// <summary>
        /// Validate the configuration
        /// </summary>
        public bool Validate(System.Xml.XmlDocument configurationDom)
        {
            return true;
        }

        /// <summary>
        /// Represent as string for UI
        /// </summary>
        public override string ToString()
        {
            return "IHE PIXv3 Notifications";
        }
        #endregion
    }
}
