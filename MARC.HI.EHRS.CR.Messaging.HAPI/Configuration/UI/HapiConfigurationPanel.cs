using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MARC.HI.EHRS.SVC.Core.Configuration;
using System.Xml.Serialization;
using System.IO;
using System.Reflection;
using System.Xml;

namespace MARC.HI.EHRS.CR.Messaging.HL7.Configuration.UI
{
    /// <summary>
    /// Represents the HAPI configuration panel
    /// </summary>
    public class HapiConfigurationPanel : IConfigurationPanel
    {

        // Panel
        private pnlHapiConfiguration m_panel = new pnlHapiConfiguration();
        // True if the panel needs a sync with configuration
        private bool m_needsSync = true;
        // Revision template
        private List<HandlerConfigTemplate> m_templates = new List<HandlerConfigTemplate>();
        // configuration
        private HL7ConfigurationSection m_configuration = new HL7ConfigurationSection();

        /// <summary>
        /// todo: default configuration
        /// </summary>
        public HapiConfigurationPanel()
        {
            String etplPath = Path.Combine(Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), "Config"), "HAPI");
            XmlSerializer xsz = new XmlSerializer(typeof(HandlerConfigTemplate));
            foreach (var etpFileName in Directory.GetFiles(etplPath))
            {
                try
                {
                    this.m_templates.Add(xsz.Deserialize(File.OpenRead(etpFileName)) as HandlerConfigTemplate);
                }
                catch { }
            }
        }

        #region IConfigurationPanel Members

        /// <summary>
        /// Configure the panel
        /// </summary>
        /// <param name="configurationDom"></param>
        public void Configure(System.Xml.XmlDocument configurationDom)
        {

            if (this.m_configuration.Services.Count == 0)
                return; // No active configurations

            XmlElement configSectionsNode = configurationDom.SelectSingleNode("//*[local-name() = 'configSections']") as XmlElement,
                hapiNode = configurationDom.SelectSingleNode("//*[local-name() = 'marc.hi.ehrs.cr.messaging.hl7']") as XmlElement,
                multiNode = configurationDom.SelectSingleNode("//*[local-name() = 'marc.hi.ehrs.svc.messaging.multi']") as XmlElement,
                coreNode = configurationDom.SelectSingleNode("//*[local-name() = 'marc.hi.ehrs.svc.core']") as XmlElement;

            Type mmhType = Type.GetType("MARC.HI.EHRS.SVC.Messaging.Multi.MultiMessageHandler, MARC.HI.EHRS.SVC.Messaging.Multi"),
                mmhConfigType = Type.GetType("MARC.HI.EHRS.SVC.Messaging.Multi.Configuration.ConfigurationSectionHandler, MARC.HI.EHRS.SVC.Messaging.Multi");

            // Ensure the assembly is loaded and the provider registered
            if (coreNode == null)
            {
                coreNode = configurationDom.CreateElement("marc.hi.ehrs.svc.core");
                configurationDom.DocumentElement.AppendChild(coreNode);
            }

            // Config sections node
            if (configSectionsNode == null)
            {
                configSectionsNode = configurationDom.CreateElement("configSections");
                configurationDom.DocumentElement.PrependChild(configSectionsNode);
            }
            XmlElement configSectionNode = configSectionsNode.SelectSingleNode("./*[local-name() = 'section'][@name = 'marc.hi.ehrs.cr.messaging.hl7']") as XmlElement;
            if (configSectionNode == null)
            {
                configSectionNode = configurationDom.CreateElement("section");
                configSectionNode.Attributes.Append(configurationDom.CreateAttribute("name"));
                configSectionNode.Attributes.Append(configurationDom.CreateAttribute("type"));
                configSectionNode.Attributes["name"].Value = "marc.hi.ehrs.cr.messaging.hl7";
                configSectionNode.Attributes["type"].Value = typeof(ConfigurationSectionHandler).AssemblyQualifiedName;
                configSectionsNode.AppendChild(configSectionNode);
            }

            configSectionNode = configSectionsNode.SelectSingleNode("./*[local-name() = 'section'][@name = 'marc.hi.ehrs.svc.messaging.multi']") as XmlElement;
            if (configSectionNode == null && mmhConfigType != null)
            {
                configSectionNode = configurationDom.CreateElement("section");
                configSectionNode.Attributes.Append(configurationDom.CreateAttribute("name"));
                configSectionNode.Attributes.Append(configurationDom.CreateAttribute("type"));
                configSectionNode.Attributes["name"].Value = "marc.hi.ehrs.svc.messaging.multi";
                configSectionNode.Attributes["type"].Value = mmhConfigType.AssemblyQualifiedName;
                configSectionsNode.AppendChild(configSectionNode);
            }

            // Persistence section node
            if (hapiNode == null)
            {
                hapiNode = configurationDom.CreateElement("marc.hi.ehrs.cr.messaging.hl7");
                configurationDom.DocumentElement.AppendChild(hapiNode);
            }
            XmlElement servicesNode = hapiNode.SelectSingleNode("./*[local-name() = 'services']") as XmlElement;
            if (servicesNode == null)
                servicesNode = hapiNode.AppendChild(configurationDom.CreateElement("services")) as XmlElement;

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

            // Add service provider (Multi if available, Everest if otherwise)
            XmlElement addServiceAsmNode = serviceAssemblyNode.SelectSingleNode("./*[local-name() = 'add'][@assembly = 'MARC.HI.EHRS.CR.Messaging.HL7.dll']") as XmlElement,
                addServiceProvNode = serviceProviderNode.SelectSingleNode(String.Format("./*[local-name() = 'add'][@type = '{0}']", (mmhType ?? typeof(HL7MessageHandler)).AssemblyQualifiedName)) as XmlElement;
            if (addServiceAsmNode == null)
            {
                addServiceAsmNode = configurationDom.CreateElement("add");
                addServiceAsmNode.Attributes.Append(configurationDom.CreateAttribute("assembly"));
                addServiceAsmNode.Attributes["assembly"].Value = "MARC.HI.EHRS.CR.Messaging.HL7.dll";
                serviceAssemblyNode.AppendChild(addServiceAsmNode);
            }
            if (addServiceProvNode == null)
            {
                addServiceProvNode = configurationDom.CreateElement("add");
                addServiceProvNode.Attributes.Append(configurationDom.CreateAttribute("type"));
                addServiceProvNode.Attributes["type"].Value = (mmhType ?? typeof(HL7MessageHandler)).AssemblyQualifiedName;
                serviceProviderNode.AppendChild(addServiceProvNode);
            }

            // Multi-message handler registration?
            if (mmhType != null)
            {
                XmlElement mmhNode = configurationDom.SelectSingleNode(".//*[local-name() = 'marc.hi.ehrs.svc.messaging.multi']") as XmlElement;
                if (mmhNode == null)
                    mmhNode = configurationDom.DocumentElement.AppendChild(configurationDom.CreateElement("marc.hi.ehrs.svc.messaging.multi")) as XmlElement;
                // Handler node
                XmlElement handlerNode = mmhNode.SelectSingleNode("./*[local-name() = 'handlers']") as XmlElement;
                if (handlerNode == null)
                    handlerNode = mmhNode.AppendChild(configurationDom.CreateElement("handlers")) as XmlElement;
                // Add node?
                if (handlerNode.SelectSingleNode(String.Format("./*[local-name() = 'add'][@type = '{0}']", typeof(HL7MessageHandler).AssemblyQualifiedName)) == null)
                {
                    var addNode = handlerNode.AppendChild(configurationDom.CreateElement("add"));
                    addNode.Attributes.Append(configurationDom.CreateAttribute("type")).Value = typeof(HL7MessageHandler).AssemblyQualifiedName;
                }
            }

            // Loop through enabled revisions and see if we need to configure them
            servicesNode.RemoveAll();
            foreach (var serviceDefn in this.m_configuration.Services)
            {
                XmlElement serviceNode = configurationDom.CreateElement("service");
                serviceNode.Attributes.Append(configurationDom.CreateAttribute("name"));
                serviceNode.Attributes["name"].Value = serviceDefn.Name;
                serviceNode.Attributes.Append(configurationDom.CreateAttribute("timeout"));
                serviceNode.Attributes["timeout"].Value = serviceDefn.ReceiveTimeout.ToString();
                serviceNode.Attributes.Append(configurationDom.CreateAttribute("address"));
                serviceNode.Attributes["address"].Value = serviceDefn.Address.ToString();

                // Attributes
                foreach (var attr in serviceDefn.Attributes)
                {
                    XmlElement attrNode = configurationDom.CreateElement("attribute");
                    attrNode.Attributes.Append(configurationDom.CreateAttribute("name"));
                    attrNode.Attributes["name"].Value = attr.Key;
                    attrNode.Attributes.Append(configurationDom.CreateAttribute("value"));
                    attrNode.Attributes["value"].Value = attr.Value;
                    serviceNode.AppendChild(attrNode);
                }

                // Handlers
                foreach (var handlr in serviceDefn.Handlers)
                {
                    XmlSerializer xsz = new XmlSerializer(typeof(HandlerDefinition));
                    MemoryStream ms = new MemoryStream();
                    xsz.Serialize(ms, handlr);
                    ms.Seek(0, SeekOrigin.Begin);
                    XmlDocument loadDoc = new XmlDocument();
                    loadDoc.Load(ms);
                    serviceNode.AppendChild(configurationDom.ImportNode(loadDoc.DocumentElement, true));
                }

                servicesNode.AppendChild(serviceNode);
            }

            this.m_needsSync = true;
        }

        /// <summary>
        /// True if the configuration is enabled
        /// </summary>
        public bool EnableConfiguration
        {
            get;
            set;
        }

        /// <summary>
        /// True if the option is configured
        /// </summary>
        public bool IsConfigured(System.Xml.XmlDocument configurationDom)
        {
            // Determine if this is configured from the dom
            XmlNode configSection = configurationDom.SelectSingleNode("//*[local-name() = 'section'][@name = 'marc.hi.ehrs.cr.messaging.hl7']"),
               messagingSection = configurationDom.SelectSingleNode("//*[local-name() = 'marc.hi.ehrs.cr.messaging.hl7']"),
                multiNode = configurationDom.SelectSingleNode(String.Format("//*[local-name() = 'marc.hi.ehrs.svc.messaging.multi']//*[local-name() = 'add'][@type = '{0}']", typeof(HL7MessageHandler).AssemblyQualifiedName)) as XmlElement;

            bool isConfigured = configSection != null && messagingSection != null && multiNode != null;
            
            // Get connection string
            if (!this.m_needsSync)
                return isConfigured;
            this.m_needsSync = false;
            this.EnableConfiguration = isConfigured;
            // Load
            if (messagingSection != null)
            {
                ConfigurationSectionHandler handler = new ConfigurationSectionHandler();
                this.m_configuration = handler.Create(null, null, messagingSection) as HL7ConfigurationSection;
            }
            this.m_panel.Configuration = this.m_configuration;
            this.m_panel.Handlers = this.m_templates;
            // Enable configuration
            return isConfigured;
        }

        /// <summary>
        /// Ggets the name of the configuration
        /// </summary>
        public string Name
        {
            get { return "Messaging/nHAPI"; }
        }

        /// <summary>
        /// Gets the control panel
        /// </summary>
        public System.Windows.Forms.Control Panel
        {
            get { return this.m_panel; }
        }

        /// <summary>
        /// Uninstall the option
        /// </summary>
        public void UnConfigure(System.Xml.XmlDocument configurationDom)
        {
            // This is a complex configuration so here we go.
            XmlElement configSectionNode = configurationDom.SelectSingleNode("//*[local-name() = 'configSections']/*[local-name() = 'section'][@name = 'marc.hi.ehrs.cr.messaging.hl7']") as XmlElement,
                configRoot = configurationDom.SelectSingleNode("//*[local-name() = 'marc.hi.ehrs.cr.messaging.hl7']") as XmlElement,
                multiNode = configurationDom.SelectSingleNode(String.Format("//*[local-name() = 'marc.hi.ehrs.svc.messaging.multi']//*[local-name() = 'add'][@type = '{0}']", typeof(HL7MessageHandler).AssemblyQualifiedName)) as XmlElement;


            // Remove the sections
            if (configSectionNode != null)
                configSectionNode.ParentNode.RemoveChild(configSectionNode);
            if (configRoot != null)
                configRoot.ParentNode.RemoveChild(configRoot);
            if (multiNode != null)
                multiNode.ParentNode.RemoveChild(multiNode);

            this.m_needsSync = true;
        }

        /// <summary>
        /// Validate the configuration options
        /// </summary>
        public bool Validate(System.Xml.XmlDocument configurationDom)
        {
            bool isValid = true;
            foreach (var defn in this.m_configuration.Services)
            {
                isValid &= defn.Address != null && !String.IsNullOrEmpty(defn.Name);
            }
            return isValid;
        }

        #endregion

        /// <summary>
        /// String Representation for UI
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return "HAPI Message Processor";
        }
    }
}
