using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;
using System.Xml;

namespace MARC.HI.EHRS.CR.Messaging.FHIR.Configuration
{
    /// <summary>
    /// Configuration section handler for FHIR
    /// </summary>
    public class ConfigurationSectionHandler : IConfigurationSectionHandler
    {
        #region IConfigurationSectionHandler Members

        /// <summary>
        /// Create the configuration object
        /// </summary>
        public object Create(object parent, object configContext, System.Xml.XmlNode section)
        {
            
            // Section
            XmlElement serviceElement = section.SelectSingleNode("./*[local-name() = 'service']") as XmlElement;
            string wcfServiceName = String.Empty;

            if (serviceElement != null)
            {
                XmlAttribute serviceName = serviceElement.Attributes["wcfServiceName"];
                if (serviceName != null)
                    wcfServiceName = serviceName.Value;
                else
                    throw new ConfigurationErrorsException("Missing wcfServiceName attribute", serviceElement);
            }
            else
                throw new ConfigurationErrorsException("Missing serviceElement", section);

            return new FhirServiceConfiguration(wcfServiceName);
        }

        #endregion
    }
}
