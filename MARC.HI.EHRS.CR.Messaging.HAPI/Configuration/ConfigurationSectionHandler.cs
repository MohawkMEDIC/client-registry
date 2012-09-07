using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;
using System.Xml;
using System.Reflection;

namespace MARC.HI.EHRS.CR.Messaging.HL7.Configuration
{
    /// <summary>
    /// Configuration section handler
    /// </summary>
    public class ConfigurationSectionHandler : IConfigurationSectionHandler
    {

        #region IConfigurationSectionHandler Members

        /// <summary>
        /// Create the configuration section
        /// </summary>
        public object Create(object parent, object configContext, System.Xml.XmlNode section)
        {
            HL7ConfigurationSection config = new HL7ConfigurationSection();

            // Read the configuration
            var xConnectionDatas = section.SelectNodes("./*[local-name() = 'services']/*[local-name() = 'service']");
            foreach (XmlElement xConnection in xConnectionDatas)
            {
                if (xConnection.Attributes["address"] != null)
                {
                    ServiceDefinition sd = new ServiceDefinition();
                    if (xConnection.Attributes["name"] != null)
                        sd.Name = xConnection.Attributes["name"].Value;
                    sd.Address = new Uri(xConnection.Attributes["address"].Value);
                    
                    // Timeout
                    TimeSpan timeout = new TimeSpan(0,0,0,0,500);
                    if (xConnection.Attributes["timeout"] != null && !TimeSpan.TryParse(xConnection.Attributes["timeout"].Value, out timeout))
                        throw new ConfigurationErrorsException("Cannot parse the 'timeout' attribute");
                    sd.ReceiveTimeout = timeout;

                    // Message handlers
                    var xHandlers = xConnection.SelectNodes("./*[local-name() = 'handler']");
                    foreach (XmlElement xHandler in xHandlers)
                    {
                        if (xHandler.Attributes["type"] == null)
                            throw new ConfigurationErrorsException("handler element must have a type attribute");
                        Type hType = Type.GetType(xHandler.Attributes["type"].Value);
                        if (hType == null)
                            throw new ConfigurationErrorsException(String.Format("Cannot find type described by '{0}'", xHandler.Attributes["type"].Value));
                        ConstructorInfo ci = hType.GetConstructor(Type.EmptyTypes);
                        if (ci == null)
                            throw new ConfigurationErrorsException(String.Format("Type '{0}' does not have a parameterless constructor", xHandler.Attributes["type"].Value));
                        IHL7MessageHandler msh = ci.Invoke(null) as IHL7MessageHandler;
                        if (msh == null)
                            throw new ConfigurationErrorsException(String.Format("Type '{0}' must implement '{1}'", hType.FullName, typeof(IHL7MessageHandler).FullName));

                        HandlerDefinition hd = new HandlerDefinition();
                        hd.Handler = msh;
                        // Message types
                        foreach(XmlElement typ in xHandler.ChildNodes)
                            if(typ.Name == "message" && typ.Attributes["name"] != null)
                                hd.Types.Add(typ.Attributes["name"].Value);
                        sd.Handlers.Add(hd);
                    }

                    config.Services.Add(sd);
                }
                else
                    throw new ConfigurationErrorsException("PIX configuration listen element must have attribute 'url'");
            }

            return config;
        }

        #endregion
    }
}
