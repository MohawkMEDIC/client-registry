using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MARC.HI.EHRS.SVC.Core;
using System.Xml;
using System.IO;
using System.Windows.Forms;
using MARC.HI.EHRS.SVC.Messaging.Everest.Configuration;
using System.Xml.XPath;

namespace MARC.HI.EHRS.SVC.Config.Messaging
{
    class ConfigHelper
    {
        internal static HostConfigurationSectionHandler m_environmentConfig = null;

        /// <summary>
        /// Save the configuration
        /// </summary>
        internal static void SaveConfig(EverestConfigurationSectionHandler sectionConfig)
        {
            string configFile = Path.Combine(Path.GetDirectoryName(typeof(ConfigHelper).Assembly.Location), "SharedHealthRecord.exe.config");

            // Open Config
            try
            {
                XmlDocument configDocument = new XmlDocument();
                configDocument.Load(configFile);
                
                // Get the config section
                XmlNode configSection = configDocument.SelectSingleNode("//*[local-name() = 'marc.hi.ehrs.svc.messaging.everest']");

                // clear all child nodes
                configSection.RemoveAll();

                // Loop through and re-create each revision
                foreach (var rev in sectionConfig.Revisions)
                {
                    XmlElement revConfigSection = configDocument.CreateElement("revision");
                    XmlAttribute validateAtt = configDocument.CreateAttribute("validate"),
                        nameAtt = configDocument.CreateAttribute("name"),
                        formatterAtt = configDocument.CreateAttribute("formatter"),
                        aideAtt = configDocument.CreateAttribute("aide"),
                        assemblyAtt = configDocument.CreateAttribute("assembly");
                    validateAtt.Value = rev.ValidateInstances.ToString().ToLower();
                    nameAtt.Value = rev.Name;
                    formatterAtt.Value = rev.Formatter.AssemblyQualifiedName;
                    aideAtt.Value = rev.GraphAide.AssemblyQualifiedName;
                    assemblyAtt.Value = rev.Assembly.FullName;
                    revConfigSection.Attributes.Append(validateAtt);
                    revConfigSection.Attributes.Append(nameAtt);
                    revConfigSection.Attributes.Append(formatterAtt);
                    revConfigSection.Attributes.Append(aideAtt);
                    revConfigSection.Attributes.Append(assemblyAtt);

                    // Create listeners
                    foreach (var lsnr in rev.Listeners)
                    {
                        XmlElement lsnrConfig = configDocument.CreateElement("listen");
                        XmlAttribute connectionStringAtt = configDocument.CreateAttribute("connectionString"),
                            typeAtt = configDocument.CreateAttribute("type"),
                            modeAtt = configDocument.CreateAttribute("mode");
                        connectionStringAtt.Value = lsnr.ConnectionString;
                        typeAtt.Value = lsnr.ConnectorType.AssemblyQualifiedName;
                        modeAtt.Value = lsnr.Mode.ToString();
                        lsnrConfig.Attributes.Append(connectionStringAtt);
                        lsnrConfig.Attributes.Append(typeAtt);
                        lsnrConfig.Attributes.Append(modeAtt);
                        revConfigSection.AppendChild(lsnrConfig);
                    }

                    // Create the type cache
                    var cacheTypeNs = from t in rev.CacheTypes
                                      group t by t.Namespace into g
                                      select new { Namespace = g.Key, Types = g };
                    foreach (var nsName in cacheTypeNs)
                    {
                        XmlElement nsCacheElement = configDocument.CreateElement("cacheTypes");
                        XmlAttribute nsAtt = configDocument.CreateAttribute("namespace");
                        nsAtt.Value = nsName.Namespace;
                        nsCacheElement.Attributes.Append(nsAtt);
                        foreach (var type in nsName.Types)
                        {
                            XmlElement addElement = configDocument.CreateElement("add");
                            XmlAttribute typeNameAtt = configDocument.CreateAttribute("name");
                            typeNameAtt.Value = type.Name;
                            addElement.Attributes.Append(typeNameAtt);
                            nsCacheElement.AppendChild(addElement);
                        }
                        revConfigSection.AppendChild(nsCacheElement);
                    }
                    
                    // Handlers
                    foreach (var hdlr in rev.MessageHandlers)
                    {
                        XmlElement handlerElement = configDocument.CreateElement("handler");
                        XmlAttribute typeAtt = configDocument.CreateAttribute("type");
                        typeAtt.Value = hdlr.Handler.GetType().AssemblyQualifiedName;
                        handlerElement.Attributes.Append(typeAtt);
                        // Interactions
                        foreach (var inter in hdlr.Interactions)
                        {
                            XmlElement interactionElement = configDocument.CreateElement("interactionId");
                            XmlAttribute intNameAtt = configDocument.CreateAttribute("name");
                            intNameAtt.Value = inter.Id;
                            interactionElement.Attributes.Append(intNameAtt);
                            handlerElement.AppendChild(interactionElement);
                        }
                        revConfigSection.AppendChild(handlerElement);
                    }

                    configSection.AppendChild(revConfigSection);
                }


                configDocument.Save(configFile);
            }
            catch (Exception e)
            {
                MessageBox.Show(String.Format("Could not load file '{0}', exception:\r\n{1}", configFile, e));
                return;
            }
        }

        /// <summary>
        /// Get raw configuration data
        /// </summary>
        internal static List<String> GetRawConfigurationData(string[] XPath)
        {
            string configFile = Path.Combine(Path.GetDirectoryName(typeof(ConfigHelper).Assembly.Location), "SharedHealthRecord.exe.config");
            
            // Open Config
            try
            {
                XPathDocument configDocument = new XPathDocument(configFile);
                XPathNavigator nav = configDocument.CreateNavigator();
                List<String> retVal = new List<string>();
                foreach (string s in XPath)
                {
                    var v = nav.SelectSingleNode(s);
                    retVal.Add(v == null ? "" : v.Value);
                }
                return retVal;
            }
            catch (Exception e)
            {
                MessageBox.Show(String.Format("Could not load file '{0}', exception:\r\n{1}", configFile, e));
                return new List<string>();
            }
        }

        /// <summary>
        /// Update or create a WCF Service
        /// </summary>
        /// <remarks>This method is horribly ugly, unfortunately there is no easy way 
        /// to </remarks>
        internal static void UpdateWcfService(
            string serviceName,
            string bindingName,
            string uri,
            bool wsrm,
            string wssec,
            bool stackTrace,
            bool metaData, 
            bool enableHelp)
        {
            var m_sectionConfig = new EverestConfigurationSectionHandler();
            string configFile = Path.Combine(Path.GetDirectoryName(typeof(ConfigHelper).Assembly.Location), "SharedHealthRecord.exe.config");

            // Open Config
            try
            {
                XmlDocument configDocument = new XmlDocument();
                configDocument.Load(configFile);

                // Service node
                XmlNode serviceModelNode = configDocument.SelectSingleNode("//system.serviceModel"),
                    servicesNode = configDocument.SelectSingleNode("//system.serviceModel/services"),
                    behaviorsNode = configDocument.SelectSingleNode("//system.serviceModel/behaviors"),
                    serviceBehaviorsNode = configDocument.SelectSingleNode("//system.serviceModel/behaviors/serviceBehaviors"),
                    bindingsNode = configDocument.SelectSingleNode("//system.serviceModel/bindings"),
                    bindingNode = configDocument.SelectSingleNode(String.Format("//system.serviceModel/bindings/{0}", bindingName));

                // Sanity check (make sure that we have all our nodes in the config file)
                #region
                if(serviceModelNode == null)
                {
                    serviceModelNode = configDocument.CreateElement("system.serviceModel");
                    configDocument.DocumentElement.AppendChild(serviceModelNode);
                }
                if(servicesNode == null)
                {
                    servicesNode = configDocument.CreateElement("services");
                    serviceModelNode.AppendChild(servicesNode);
                }
                if(behaviorsNode == null)
                {
                    behaviorsNode = configDocument.CreateElement("behaviors");
                    serviceModelNode.AppendChild(behaviorsNode);
                }
                if(serviceBehaviorsNode == null)
                {
                    serviceBehaviorsNode = configDocument.CreateElement("serviceBehaviors");
                    behaviorsNode.AppendChild(serviceBehaviorsNode);
                }
                if(bindingsNode == null)
                {
                    bindingsNode = configDocument.CreateElement("bindings");
                    serviceModelNode.AppendChild(bindingsNode);
                }
                if(bindingNode == null)
                {
                    bindingNode = configDocument.CreateElement(bindingName);
                    bindingsNode.AppendChild(bindingNode);
                }
                #endregion

                // Do we have a service that matches our service name?
                #region 
                XmlNode serviceNode = servicesNode.SelectSingleNode(String.Format("./service[@name='{0}']", serviceName));
                if(serviceNode == null) // create a service node
                {
                    serviceNode = configDocument.CreateElement("service");
                    XmlAttribute nameAtt = configDocument.CreateAttribute("name");
                    nameAtt.Value = serviceName;
                    serviceNode.Attributes.Append(nameAtt);
                    servicesNode.AppendChild(serviceNode);
                }
                if(serviceNode.Attributes["behaviorConfiguration"] == null)
                {
                    XmlAttribute behaviorAtt = configDocument.CreateAttribute("behaviorConfiguration");
                    behaviorAtt.Value = String.Format("{0}_Behavior", serviceName);
                    serviceNode.Attributes.Append(behaviorAtt);
                }

                XmlNode endpointNode = serviceNode.SelectSingleNode("./endpoint");
                if(endpointNode == null)
                {
                    endpointNode = configDocument.CreateElement("endpoint");
                    XmlAttribute addressAtt = configDocument.CreateAttribute("address"),
                        contractAtt = configDocument.CreateAttribute("contract"),
                        bindingAtt = configDocument.CreateAttribute("binding"),
                        bindingNsAtt = configDocument.CreateAttribute("bindingNamespace");
                    
                    contractAtt.Value = "MARC.Everest.Connectors.WCF.Core.IConnectorContract";
                    bindingNsAtt.Value = "http://tempuri.org/";
                    endpointNode.Attributes.Append(addressAtt);
                    endpointNode.Attributes.Append(contractAtt);
                    endpointNode.Attributes.Append(bindingAtt);
                    endpointNode.Attributes.Append(bindingNsAtt);
                    serviceNode.AppendChild(endpointNode);
                }
                if(endpointNode.Attributes["bindingConfiguration"] == null)
                {
                    XmlAttribute bindingConfigAtt = configDocument.CreateAttribute("bindingConfiguration");
                    bindingConfigAtt.Value = String.Format("{0}_Binding", serviceName);
                    endpointNode.Attributes.Append(bindingConfigAtt);
                }
                

                // Get the address and binding
                endpointNode.Attributes["address"].Value = uri;
                endpointNode.Attributes["binding"].Value = bindingName;
                #endregion

                // Behavior configuration
                #region 
                XmlNode behaviorConfigNode = serviceBehaviorsNode.SelectSingleNode(String.Format("./behavior[@name='{0}']", serviceNode.Attributes["behaviorConfiguration"].Value));
                if (behaviorConfigNode == null)
                {
                    behaviorConfigNode = configDocument.CreateElement("behavior");
                    XmlAttribute nameAtt = configDocument.CreateAttribute("name");
                    nameAtt.Value = serviceNode.Attributes["behaviorConfiguration"].Value;
                    behaviorConfigNode.Attributes.Append(nameAtt);
                    serviceBehaviorsNode.AppendChild(behaviorConfigNode);
                }
                XmlNode serviceDebugElement = behaviorConfigNode.SelectSingleNode("./serviceDebug"),
                    metaDataElement = behaviorConfigNode.SelectSingleNode("./serviceMetadata");
                if (serviceDebugElement == null)
                {
                    serviceDebugElement = configDocument.CreateElement("serviceDebug");
                    behaviorConfigNode.AppendChild(serviceDebugElement);
                }
                if (serviceDebugElement.Attributes["includeExceptionDetailInFaults"] == null)
                    serviceDebugElement.Attributes.Append(configDocument.CreateAttribute("includeExceptionDetailInFaults"));
                if(serviceDebugElement.Attributes["httpHelpPageEnabled"] == null)
                    serviceDebugElement.Attributes.Append(configDocument.CreateAttribute("httpHelpPageEnabled"));
                if (serviceDebugElement.Attributes["httpHelpPageUrl"] == null)
                    serviceDebugElement.Attributes.Append(configDocument.CreateAttribute("httpHelpPageUrl"));
                serviceDebugElement.Attributes["includeExceptionDetailInFaults"].Value = stackTrace.ToString();
                serviceDebugElement.Attributes["httpHelpPageEnabled"].Value = enableHelp.ToString();
                serviceDebugElement.Attributes["httpHelpPageUrl"].Value = uri;

                if (metaDataElement == null)
                {
                    metaDataElement = configDocument.CreateElement("serviceMetadata");
                    behaviorConfigNode.AppendChild(metaDataElement);
                }
                if(metaDataElement.Attributes["httpGetEnabled"] == null)
                    metaDataElement.Attributes.Append(configDocument.CreateAttribute("httpGetEnabled"));
                if(metaDataElement.Attributes["httpGetUrl"] == null)
                    metaDataElement.Attributes.Append(configDocument.CreateAttribute("httpGetUrl"));
                metaDataElement.Attributes["httpGetEnabled"].Value = metaData.ToString();
                metaDataElement.Attributes["httpGetUrl"].Value = String.Format("{0}", uri);
                #endregion

                // Store binding configuration
                #region 
                XmlNode bindingConfigNode = bindingNode.SelectSingleNode(String.Format("./binding[@name='{0}']", endpointNode.Attributes["bindingConfiguration"].Value));
                if(bindingConfigNode == null)
                {
                    bindingConfigNode = configDocument.CreateElement("binding");
                    XmlAttribute nameAtt = configDocument.CreateAttribute("name");
                    nameAtt.Value = endpointNode.Attributes["bindingConfiguration"].Value;
                    bindingConfigNode.Attributes.Append(nameAtt);
                    bindingNode.AppendChild(bindingConfigNode);
                }

                // TODO: Persist the rest of WCF configuration
                XmlNode reliableSessionNode = bindingConfigNode.SelectSingleNode("./reliableSession"),
                    securityNode = bindingConfigNode.SelectSingleNode("./security");
                if(reliableSessionNode == null && !bindingName.Equals("basicHttpBinding"))
                {
                    reliableSessionNode = configDocument.CreateElement("reliableSession");
                    reliableSessionNode.Attributes.Append(configDocument.CreateAttribute("enabled"));
                    bindingConfigNode.AppendChild(reliableSessionNode);
                    
                }
                if(!bindingName.Equals("basicHttpBinding"))
                    reliableSessionNode.Attributes["enabled"].Value = wsrm.ToString();
                // Security mode
                if (securityNode == null)
                {
                    securityNode = configDocument.CreateElement("security");
                    securityNode.Attributes.Append(configDocument.CreateAttribute("mode"));
                    bindingConfigNode.AppendChild(securityNode);
                }
                else if (securityNode.Attributes["mode"] == null)
                    securityNode.Attributes.Append(configDocument.CreateAttribute("mode"));
                securityNode.Attributes["mode"].Value = wssec;

                #endregion

                configDocument.Save(configFile);

            }
            catch (Exception e)
            {
                MessageBox.Show(String.Format("Could not load file '{0}', exception:\r\n{1}", configFile, e));
            }
        }

        /// <summary>
        /// Load the configuraion
        /// </summary>
        internal static EverestConfigurationSectionHandler LoadConfig()
        {
            var m_sectionConfig = new EverestConfigurationSectionHandler();
            string configFile = Path.Combine(Path.GetDirectoryName(typeof(ConfigHelper).Assembly.Location), "SharedHealthRecord.exe.config");


            // Open Config
            try
            {
                m_sectionConfig.ConfigMode = true;

                XmlDocument configDocument = new XmlDocument();
                configDocument.Load(configFile);

                if (m_environmentConfig == null)
                {
                    m_environmentConfig = new HostConfigurationSectionHandler();
                    m_environmentConfig.Create(null, null, configDocument.SelectSingleNode("//*[local-name() = 'marc.hi.ehrs.svc.core']"));
                }
                m_sectionConfig.Create(null, null, configDocument.SelectSingleNode("//*[local-name() = 'marc.hi.ehrs.svc.messaging.everest']"));
                return m_sectionConfig;
            }
            catch (Exception e)
            {
                MessageBox.Show(String.Format("Could not load file '{0}', exception:\r\n{1}", configFile, e));
                return m_sectionConfig;
            }
        }

    }
}
