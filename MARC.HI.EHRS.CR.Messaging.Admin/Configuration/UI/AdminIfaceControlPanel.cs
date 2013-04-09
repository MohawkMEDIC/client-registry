using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MARC.HI.EHRS.SVC.Core.Configuration;
using System.Xml;
using System.Windows.Forms;
using System.Net;
using System.ComponentModel;
using System.Security.Cryptography.X509Certificates;
using System.Configuration;

namespace MARC.HI.EHRS.CR.Messaging.Admin.Configuration.UI
{
    /// <summary>
    /// Administrative interface control panel
    /// </summary>
    public class AdminIfaceControlPanel : IConfigurationPanel
    {
        #region IConfigurationPanel Members

        // Panel interface
        private pnlAdminIface m_panel = new pnlAdminIface();

        // Configuration
        private ClientRegistryInterfaceConfiguration m_configuration = new ClientRegistryInterfaceConfiguration("adminSvc");

        // True if resync is needed
        private bool m_needsSync = true;

        /// <summary>
        /// Configure the administrative interface
        /// </summary>
        /// <param name="configurationDom"></param>
        public void Configure(System.Xml.XmlDocument configurationDom)
        {

            XmlElement configSectionsNode = configurationDom.SelectSingleNode("//*[local-name() = 'configSections']") as XmlElement,
                adminNode = configurationDom.SelectSingleNode("//*[local-name() = 'marc.hi.ehrs.cr.messaging.admin']") as XmlElement,
                multiNode = configurationDom.SelectSingleNode("//*[local-name() = 'marc.hi.ehrs.svc.messaging.multi']") as XmlElement,
                coreNode = configurationDom.SelectSingleNode("//*[local-name() = 'marc.hi.ehrs.svc.core']") as XmlElement,
                wcfNode = configurationDom.SelectSingleNode("//*[local-name() = 'system.serviceModel']") as XmlElement;

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
            XmlElement configSectionNode = configSectionsNode.SelectSingleNode("./*[local-name() = 'section'][@name = 'marc.hi.ehrs.cr.messaging.admin']") as XmlElement;
            if (configSectionNode == null)
            {
                configSectionNode = configurationDom.CreateElement("section");
                configSectionNode.Attributes.Append(configurationDom.CreateAttribute("name"));
                configSectionNode.Attributes.Append(configurationDom.CreateAttribute("type"));
                configSectionNode.Attributes["name"].Value = "marc.hi.ehrs.cr.messaging.admin";
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
            if (adminNode == null)
            {
                adminNode = configurationDom.CreateElement("marc.hi.ehrs.cr.messaging.admin");
                configurationDom.DocumentElement.AppendChild(adminNode);
            }

            // Add wcf listen endpoint
            XmlElement listenNode = adminNode.SelectSingleNode("./*[local-name() = 'listen']") as XmlElement;
            if (listenNode == null)
                listenNode = adminNode.AppendChild(configurationDom.CreateElement("listen")) as XmlElement;
            if (listenNode.Attributes["wcfServiceName"] == null)
                listenNode.Attributes.Append(configurationDom.CreateAttribute("wcfServiceName"));
            listenNode.Attributes["wcfServiceName"].Value = this.m_configuration.WcfServiceName;

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
            XmlElement addServiceAsmNode = serviceAssemblyNode.SelectSingleNode("./*[local-name() = 'add'][@assembly = 'MARC.HI.EHRS.CR.Messaging.Admin.dll']") as XmlElement,
                addServiceProvNode = serviceProviderNode.SelectSingleNode(String.Format("./*[local-name() = 'add'][@type = '{0}']", (mmhType ?? typeof(ClientRegistryAdminInterface)).AssemblyQualifiedName)) as XmlElement;
            if (addServiceAsmNode == null)
            {
                addServiceAsmNode = configurationDom.CreateElement("add");
                addServiceAsmNode.Attributes.Append(configurationDom.CreateAttribute("assembly"));
                addServiceAsmNode.Attributes["assembly"].Value = "MARC.HI.EHRS.CR.Messaging.Admin.dll";
                serviceAssemblyNode.AppendChild(addServiceAsmNode);
            }
            if (addServiceProvNode == null)
            {
                addServiceProvNode = configurationDom.CreateElement("add");
                addServiceProvNode.Attributes.Append(configurationDom.CreateAttribute("type"));
                addServiceProvNode.Attributes["type"].Value = (mmhType ?? typeof(ClientRegistryAdminInterface)).AssemblyQualifiedName;
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
                if (handlerNode.SelectSingleNode(String.Format("./*[local-name() = 'add'][@type = '{0}']", typeof(ClientRegistryAdminInterface).AssemblyQualifiedName)) == null)
                {
                    var addNode = handlerNode.AppendChild(configurationDom.CreateElement("add"));
                    addNode.Attributes.Append(configurationDom.CreateAttribute("type")).Value = typeof(ClientRegistryAdminInterface).AssemblyQualifiedName;
                }
            }

            // WCF nodes
            if (wcfNode == null)
                wcfNode = configurationDom.DocumentElement.AppendChild(configurationDom.CreateElement("system.serviceModel")) as XmlElement;

            // Loop through enabled revisions and see if we need to configure them
            WcfServiceConfiguration(this.m_configuration.WcfServiceName, wcfNode);

            // Configure the URL / SSL
            try
            {
                if (this.m_panel.Address.StartsWith("https:"))
                {
                    Uri address = new Uri(this.m_panel.Address);
                    // Reserve the SSL certificate on the IP address
                    if (address.HostNameType == UriHostNameType.Dns)
                    {
                        var ipAddresses = Dns.GetHostAddresses(address.Host);
                        HttpSslTool.BindCertificate(ipAddresses[0], address.Port, this.m_panel.Certificate.GetCertHash(), this.m_panel.StoreName, this.m_panel.StoreLocation);
                    }
                    else
                        HttpSslTool.BindCertificate(IPAddress.Parse(address.Host), address.Port, this.m_panel.Certificate.GetCertHash(), this.m_panel.StoreName, this.m_panel.StoreLocation);
                }
            }
            catch (Win32Exception e)
            {
                throw new OperationCanceledException(String.Format("Error binding SSL certificate to address. Error was: {0:x} {1}", e.ErrorCode, e.Message), e);
            }


            this.m_needsSync = true;
        }

        /// <summary>
        /// Create a WCF Service configuration node
        /// </summary>
        private void WcfServiceConfiguration(string serviceName, XmlElement wcfNode)
        {

            XmlDocument configurationDom = wcfNode.OwnerDocument;
            XmlElement wcfServiceNode = wcfNode.SelectSingleNode("./*[local-name() = 'services']") as XmlElement;
            if (wcfServiceNode == null)
                wcfServiceNode = wcfNode.AppendChild(configurationDom.CreateElement("services")) as XmlElement;
            XmlElement wcfRevisionServiceNode = wcfServiceNode.SelectSingleNode(string.Format("./*[local-name() = 'service'][@name = '{0}']", serviceName[0])) as XmlElement;


            if (wcfRevisionServiceNode == null)
            {
                wcfRevisionServiceNode = wcfServiceNode.AppendChild(configurationDom.CreateElement("service")) as XmlElement;
                wcfRevisionServiceNode.Attributes.Append(configurationDom.CreateAttribute("name")).Value = serviceName;
            }

            // Behavior config?
            XmlAttribute wcfServiceBehaviorNode = wcfRevisionServiceNode.Attributes["behaviorConfiguration"];
            if (wcfServiceBehaviorNode == null)
            {
                wcfServiceBehaviorNode = wcfRevisionServiceNode.Attributes.Append(configurationDom.CreateAttribute("behaviorConfiguration")) as XmlAttribute;
                wcfServiceBehaviorNode.Value = String.Format("{0}_Behavior", serviceName);
            }

            // Create behavior?
            WcfBehaviorConfiguration(wcfServiceBehaviorNode.Value, wcfNode);

            // Host element?
            XmlElement wcfHostElement = wcfRevisionServiceNode.SelectSingleNode("./*[local-name() = 'host']") as XmlElement;
            if (wcfHostElement == null)
                wcfHostElement = wcfRevisionServiceNode.AppendChild(configurationDom.CreateElement("host")) as XmlElement;
            XmlElement wcfBaseElement = wcfHostElement.SelectSingleNode("./*[local-name() = 'baseAddresses']") as XmlElement;
            if (wcfBaseElement == null)
                wcfBaseElement = wcfHostElement.AppendChild(configurationDom.CreateElement("baseAddresses")) as XmlElement;
            XmlElement wcfAddAddress = wcfBaseElement.SelectSingleNode("./*[local-name() = 'add']") as XmlElement;
            if (wcfAddAddress == null)
                wcfAddAddress = wcfBaseElement.AppendChild(configurationDom.CreateElement("add")) as XmlElement;
            if (wcfAddAddress.Attributes["baseAddress"] == null)
                wcfAddAddress.Attributes.Append(configurationDom.CreateAttribute("baseAddress"));
            wcfAddAddress.Attributes["baseAddress"].Value = this.m_panel.Address;

            // Endpoint element
            XmlElement wcfEndpointNode = wcfRevisionServiceNode.SelectSingleNode("./*[local-name() = 'endpoint']") as XmlElement;
            if (wcfEndpointNode == null)
                wcfEndpointNode = wcfRevisionServiceNode.AppendChild(configurationDom.CreateElement("endpoint")) as XmlElement;
            if (wcfEndpointNode.Attributes["address"] == null)
                wcfEndpointNode.Attributes.Append(configurationDom.CreateAttribute("address"));
            wcfEndpointNode.Attributes["address"].Value = this.m_panel.Address;
            if (wcfEndpointNode.Attributes["contract"] == null)
                wcfEndpointNode.Attributes.Append(configurationDom.CreateAttribute("contract"));
            wcfEndpointNode.Attributes["contract"].Value = typeof(IClientRegistryAdminInterface).FullName;

            if (wcfEndpointNode.Attributes["binding"] == null)
                wcfEndpointNode.Attributes.Append(configurationDom.CreateAttribute("binding"));
            wcfEndpointNode.Attributes["binding"].Value = "wsHttpBinding";

            // Binding config?
            XmlAttribute wcfBindingConfigurationNode = wcfEndpointNode.Attributes["bindingConfiguration"];
            if (wcfBindingConfigurationNode == null)
            {
                wcfBindingConfigurationNode = wcfEndpointNode.Attributes.Append(configurationDom.CreateAttribute("bindingConfiguration")) as XmlAttribute;
                wcfBindingConfigurationNode.Value = String.Format("{0}_Binding", serviceName);
            }
            WcfBindingConfiguration(wcfBindingConfigurationNode.Value, wcfNode);


        }

        /// <summary>
        /// Create behavior
        /// </summary>
        private void WcfBehaviorConfiguration(string behaviorName, XmlElement wcfNode)
        {
            XmlDocument configurationDom = wcfNode.OwnerDocument;
            XmlElement wcfBehaviorNode = wcfNode.SelectSingleNode("./*[local-name() = 'behaviors']") as XmlElement;
            if (wcfBehaviorNode == null)
                wcfBehaviorNode = wcfNode.AppendChild(configurationDom.CreateElement("behaviors")) as XmlElement;

            XmlElement wcfServiceBehaviorNode = wcfBehaviorNode.SelectSingleNode("./*[local-name() = 'serviceBehaviors']") as XmlElement;
            if (wcfServiceBehaviorNode == null)
                wcfServiceBehaviorNode = wcfBehaviorNode.AppendChild(configurationDom.CreateElement("serviceBehaviors")) as XmlElement;

            XmlElement wcfRevisionBehaviorNode = wcfServiceBehaviorNode.SelectSingleNode(String.Format("./*[local-name() = 'behavior'][@name = '{0}']", behaviorName)) as XmlElement;
            if (wcfRevisionBehaviorNode == null)
            {
                wcfRevisionBehaviorNode = wcfServiceBehaviorNode.AppendChild(configurationDom.CreateElement("behavior")) as XmlElement;
                wcfRevisionBehaviorNode.Attributes.Append(configurationDom.CreateAttribute("name")).Value = behaviorName;
            }

            // Debug
            XmlElement wcfServiceDebugNode = wcfRevisionBehaviorNode.SelectSingleNode("./*[local-name() = 'serviceDebug']") as XmlElement;
            if (wcfServiceDebugNode == null)
                wcfServiceDebugNode = wcfRevisionBehaviorNode.AppendChild(configurationDom.CreateElement("serviceDebug")) as XmlElement;
            if (wcfServiceDebugNode.Attributes["includeExceptionDetailInFaults"] == null)
                wcfServiceDebugNode.Attributes.Append(configurationDom.CreateAttribute("includeExceptionDetailInFaults"));
            wcfServiceDebugNode.Attributes["includeExceptionDetailInFaults"].Value = this.m_panel.ServiceDebugEnabled.ToString();

            // Meta-data
            XmlElement wcfMetadataNode = wcfRevisionBehaviorNode.SelectSingleNode("./*[local-name() = 'serviceMetadata']") as XmlElement;
            if (wcfMetadataNode == null)
                wcfMetadataNode = wcfRevisionBehaviorNode.AppendChild(configurationDom.CreateElement("serviceMetadata")) as XmlElement;
            Uri urlSvc = new Uri(this.m_panel.Address);
            String attrNode = String.Format("{0}GetEnabled", urlSvc.Scheme.ToLower());
            if (wcfMetadataNode.Attributes[attrNode] == null)
                wcfMetadataNode.Attributes.Append(configurationDom.CreateAttribute(attrNode));
            wcfMetadataNode.Attributes[attrNode].Value = this.m_panel.ServiceMetaDataEnabled.ToString();
            attrNode = String.Format("{0}GetUrl", urlSvc.Scheme.ToLower());
            if (wcfMetadataNode.Attributes[attrNode] == null)
                wcfMetadataNode.Attributes.Append(configurationDom.CreateAttribute(attrNode));
            wcfMetadataNode.Attributes[attrNode].Value = this.m_panel.Address;

            // Security?
            XmlElement wcfServiceCredentialsNode = wcfRevisionBehaviorNode.SelectSingleNode("./*[local-name() = 'serviceCredentials']") as XmlElement;
            if (attrNode == "httpsGetUrl") // Security is enabled
            {
                if (wcfServiceCredentialsNode == null)
                    wcfServiceCredentialsNode = wcfRevisionBehaviorNode.AppendChild(configurationDom.CreateElement("serviceCredentials")) as XmlElement;
                wcfServiceCredentialsNode.RemoveAll();

                XmlElement wcfServiceCertificateNode = wcfServiceCredentialsNode.AppendChild(configurationDom.CreateElement("serviceCertificate")) as XmlElement;
                wcfServiceCertificateNode.Attributes.Append(configurationDom.CreateAttribute("storeLocation")).Value = this.m_panel.StoreLocation.ToString();
                wcfServiceCertificateNode.Attributes.Append(configurationDom.CreateAttribute("storeName")).Value = this.m_panel.StoreName.ToString();
                wcfServiceCertificateNode.Attributes.Append(configurationDom.CreateAttribute("x509FindType")).Value = X509FindType.FindByThumbprint.ToString();
                wcfServiceCertificateNode.Attributes.Append(configurationDom.CreateAttribute("findValue")).Value = this.m_panel.Certificate.Thumbprint;

                // Client certificates?
                if (this.m_panel.RequireClientCerts)
                {
                    XmlElement clientCertNode = wcfServiceCredentialsNode.AppendChild(configurationDom.CreateElement("clientCertificate")) as XmlElement,
                        authNode = clientCertNode.AppendChild(configurationDom.CreateElement("authentication")) as XmlElement;
                    authNode.Attributes.Append(configurationDom.CreateAttribute("certificateValidationMode"));
                    authNode.Attributes["certificateValidationMode"].Value = "ChainTrust";
                    authNode.Attributes.Append(configurationDom.CreateAttribute("trustedStoreLocation"));
                    authNode.Attributes["trustedStoreLocation"].Value = "LocalMachine";

                }

            }
            else if (wcfServiceCredentialsNode != null) // Remove the credentials node
                wcfRevisionBehaviorNode.RemoveChild(wcfServiceCredentialsNode);
        }

        /// <summary>
        /// Binding Configuration
        /// </summary>
        private void WcfBindingConfiguration(string bindingName, XmlElement wcfNode)
        {
            XmlDocument configurationDom = wcfNode.OwnerDocument;
            XmlElement wcfBindingNode = wcfNode.SelectSingleNode("./*[local-name() = 'bindings']") as XmlElement;
            if (wcfBindingNode == null)
                wcfBindingNode = wcfNode.AppendChild(configurationDom.CreateElement("bindings")) as XmlElement;

            // Get the binding name
            var bindingType = wcfNode.SelectSingleNode(string.Format(".//*[local-name() = 'service']//*[local-name() = 'endpoint'][@bindingConfiguration = '{0}']/@binding", bindingName));
            if (bindingType == null)
                throw new ConfigurationErrorsException("Cannot determine the binding for the specified configuration, does the endpoint have the binding attribute?");

            XmlElement wcfBindingTypeNode = wcfBindingNode.SelectSingleNode(String.Format("./*[local-name() = '{0}']", bindingType.Value)) as XmlElement;
            if (wcfBindingTypeNode == null)
                wcfBindingTypeNode = wcfBindingNode.AppendChild(configurationDom.CreateElement(bindingType.Value)) as XmlElement;

            // Is there a binding with our name on it?
            XmlElement wcfBindingConfigurationNode = wcfBindingTypeNode.SelectSingleNode(string.Format("./*[local-name() = 'binding'][@name = '{0}']", bindingName)) as XmlElement;
            if (wcfBindingConfigurationNode == null)
                wcfBindingConfigurationNode = wcfBindingTypeNode.AppendChild(configurationDom.CreateElement("binding")) as XmlElement;
            if (wcfBindingConfigurationNode.Attributes["name"] == null)
                wcfBindingConfigurationNode.Attributes.Append(configurationDom.CreateAttribute("name"));
            wcfBindingConfigurationNode.Attributes["name"].Value = bindingName;

            // Security?
            XmlElement wcfSecurityModeNode = wcfBindingConfigurationNode.SelectSingleNode("./*[local-name() = 'security']") as XmlElement;
            if (wcfSecurityModeNode == null)
                wcfSecurityModeNode = wcfBindingConfigurationNode.AppendChild(configurationDom.CreateElement("security")) as XmlElement;
            if (wcfSecurityModeNode.Attributes["mode"] == null)
                wcfSecurityModeNode.Attributes.Append(configurationDom.CreateAttribute("mode"));

            if (this.m_panel.Address.ToLower().StartsWith("https"))
            {
                wcfSecurityModeNode.RemoveAll();
                wcfSecurityModeNode.Attributes.Append(configurationDom.CreateAttribute("mode")).Value = "Transport";
                // Transport options
                var wcfTransportNode = wcfSecurityModeNode.AppendChild(configurationDom.CreateElement("transport")) as XmlElement;
                wcfTransportNode.Attributes.Append(configurationDom.CreateAttribute("clientCredentialType")).Value = this.m_panel.RequireClientCerts ? "Certificate" : "None";
            }
            else
                wcfSecurityModeNode.Attributes["mode"].Value = "None";

        }

        /// <summary>
        /// Gets whether the configuration is enabled or not
        /// </summary>
        public bool EnableConfiguration
        {
            get;
            set;
        }

        /// <summary>
        /// Determines if the current system is configured
        /// </summary>
        public bool IsConfigured(System.Xml.XmlDocument configurationDom)
        {
            // This is a complex configuration so here we go.
            XmlElement configSectionNode = configurationDom.SelectSingleNode("//*[local-name() = 'configSections']/*[local-name() = 'section'][@name = 'marc.hi.ehrs.cr.messaging.admin']") as XmlElement,
                configRoot = configurationDom.SelectSingleNode("//*[local-name() = 'marc.hi.ehrs.cr.messaging.admin']") as XmlElement,
                wcfRoot = configurationDom.SelectSingleNode("//*[local-name() = 'system.serviceModel']") as XmlElement,
                multiNode = configurationDom.SelectSingleNode(String.Format("//*[local-name() = 'marc.hi.ehrs.svc.messaging.multi']//*[local-name() = 'add'][@type = '{0}']", typeof(ClientRegistryAdminInterface).AssemblyQualifiedName)) as XmlElement;

            XmlNodeList revisions = configurationDom.SelectNodes("//*[local-name() = 'marc.hi.ehrs.svc.messaging.everest']/*[local-name() = 'revision']");

            // Load the current config if applicable
            if (configRoot != null)
                this.m_configuration = new ConfigurationSectionHandler().Create(null, null, configRoot) as ClientRegistryInterfaceConfiguration;
            else
                this.m_configuration = new ClientRegistryInterfaceConfiguration("adminSvc");

            bool isConfigured = configSectionNode != null && configRoot != null &&
                wcfRoot != null && this.m_configuration != null && multiNode != null;
            if (!this.m_needsSync)
                return isConfigured;
            this.EnableConfiguration = isConfigured;
            this.m_needsSync = false;

            if (configRoot == null) // makes the following logic clearer
                configRoot = configurationDom.CreateElement("marc.hi.ehrs.cr.messaging.admin");

            // Loop through the configuration templates 
            if(wcfRoot != null)
                this.m_panel.SetConfiguration(wcfRoot, this.m_configuration);


            return isConfigured;
        }

        /// <summary>
        /// Gets the name of the panel
        /// </summary>
        public string Name
        {
            get { return "Messaging/Administrative Interface"; }
        }

        /// <summary>
        /// Gets the current control panel visual
        /// </summary>
        public System.Windows.Forms.Control Panel
        {
            get { return this.m_panel; }
        }

        /// <summary>
        /// Unconfigures the feature
        /// </summary>
        /// <param name="configurationDom"></param>
        public void UnConfigure(System.Xml.XmlDocument configurationDom)
        {
            // This is a complex configuration so here we go.
            XmlElement configSectionNode = configurationDom.SelectSingleNode("//*[local-name() = 'configSections']/*[local-name() = 'section'][@name = 'marc.hi.ehrs.cr.messaging.admin']") as XmlElement,
                configRoot = configurationDom.SelectSingleNode("//*[local-name() = 'marc.hi.ehrs.cr.messaging.admin']") as XmlElement,
                wcfRoot = configurationDom.SelectSingleNode("//*[local-name() = 'system.serviceModel']") as XmlElement,
                multiNode = configurationDom.SelectSingleNode(String.Format("//*[local-name() = 'marc.hi.ehrs.svc.messaging.multi']//*[local-name() = 'add'][@type = '{0}']", typeof(ClientRegistryAdminInterface).AssemblyQualifiedName)) as XmlElement;

            // Remove the sections
            if (configSectionNode != null)
                configSectionNode.ParentNode.RemoveChild(configSectionNode);
            if (configRoot != null)
                configRoot.ParentNode.RemoveChild(configRoot);
            if (multiNode != null)
                multiNode.ParentNode.RemoveChild(multiNode);
            X509Certificate2 serviceCert = null;
            StoreLocation certificateLocation = StoreLocation.LocalMachine;
            StoreName certificateStore = StoreName.My;
            // Lookup the service information
            XmlElement serviceNode = wcfRoot.SelectSingleNode(String.Format(".//*[local-name() = 'service'][@name = '{0}']", this.m_configuration.WcfServiceName)) as XmlElement;
            if (serviceNode == null) return;
            if (serviceNode.Attributes["behaviorConfiguration"] != null)
            {
                XmlElement behavior = wcfRoot.SelectSingleNode(String.Format(".//*[local-name() = 'behavior'][@name = '{0}']", serviceNode.Attributes["behaviorConfiguration"].Value)) as XmlElement;
                if (behavior != null)
                {
                    XmlElement serviceCertificateNode = behavior.SelectSingleNode(".//*[local-name() = 'serviceCertificate']") as XmlElement;
                    if (serviceCertificateNode != null)
                    {
                        certificateStore = (StoreName)Enum.Parse(typeof(StoreName), serviceCertificateNode.Attributes["storeName"].Value);
                        certificateLocation = (StoreLocation)Enum.Parse(typeof(StoreLocation), serviceCertificateNode.Attributes["storeLocation"].Value);
                        X509Store store = new X509Store(
                            certificateStore,
                            certificateLocation
                        );
                        try
                        {
                            store.Open(OpenFlags.ReadOnly);
                            var cert = store.Certificates.Find((X509FindType)Enum.Parse(typeof(X509FindType), serviceCertificateNode.Attributes["x509FindType"].Value), serviceCertificateNode.Attributes["findValue"].Value, false);
                            if (cert.Count > 0)
                                serviceCert = cert[0];
                        }
                        catch (System.Exception e)
                        {
                            MessageBox.Show("Cannot retrieve certification information");
                        }
                        finally
                        {
                            store.Close();
                        }
                    }

                    behavior.ParentNode.RemoveChild(behavior);
                }
            }

            // Remove the bindings
            XmlNodeList endpoints = serviceNode.SelectNodes(".//*[local-name() = 'endpoint']");
            foreach (XmlElement ep in endpoints)
            {
                if (ep.Attributes["bindingConfiguration"] != null)
                {
                    var binding = wcfRoot.SelectSingleNode(String.Format(".//*[local-name() = 'binding'][@name = '{0}']", ep.Attributes["bindingConfiguration"].Value)) as XmlElement;

                    if (binding != null)
                        binding.ParentNode.RemoveChild(binding);
                }

                // Un-bind the certificate
                if (serviceCert != null)
                {
                    Uri address = new Uri(ep.Attributes["address"].Value);
                    // Reserve the SSL certificate on the IP address
                    if (address.HostNameType == UriHostNameType.Dns)
                    {
                        var ipAddresses = Dns.GetHostAddresses(address.Host);
                        HttpSslTool.RemoveCertificate(ipAddresses[0], address.Port, serviceCert.GetCertHash(), certificateStore, certificateLocation);
                    }
                    else
                        HttpSslTool.RemoveCertificate(IPAddress.Parse(address.Host), address.Port, serviceCert.GetCertHash(), certificateStore, certificateLocation);
                }
            }
            serviceNode.ParentNode.RemoveChild(serviceNode);



            this.m_needsSync = true;
        }

        /// <summary>
        /// Validates the configuration
        /// </summary>
        public bool Validate(System.Xml.XmlDocument configurationDom)
        {
            // VAlidate 
            Uri r = null;
            if (String.IsNullOrEmpty(this.m_panel.Address) || !Uri.TryCreate(this.m_panel.Address, UriKind.Absolute, out r))
            {
                MessageBox.Show("Invalid address supplied for administrative interface");
                return false;
            }
            else if (r.Scheme == "https" && this.m_panel.Certificate == null)
            {
                MessageBox.Show("Secure address requires a certificate");
                return false;
            }
            return true;
        }

        /// <summary>
        /// String representation for UI
        /// </summary>
        public override string ToString()
        {
            return "Administrative Interface";
        }
        #endregion
    }
}
