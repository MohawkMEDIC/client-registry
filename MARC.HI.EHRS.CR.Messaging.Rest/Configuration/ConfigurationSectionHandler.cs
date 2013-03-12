using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;
using System.Xml;

namespace MARC.HI.EHRS.CR.Messaging.Rest.Configuration
{
    /// <summary>
    /// Configuration section handler
    /// </summary>
    public class ConfigurationSectionHandler : IConfigurationSectionHandler
    {
        #region IConfigurationSectionHandler Members

        /// <summary>
        /// Create a configuration section
        /// </summary>
        public object Create(object parent, object configContext, System.Xml.XmlNode section)
        {
            
            XmlAttribute address = section.SelectSingleNode("./*[local-name() = 'listen']/@address") as XmlAttribute;
            if (address == null)
                throw new ConfigurationErrorsException("Missing listen element");
            else
                return new ClientRegistryInterfaceConfiguration(new Uri(address.Value));
        }

        #endregion
    }
}
