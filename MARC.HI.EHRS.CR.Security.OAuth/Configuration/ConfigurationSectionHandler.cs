using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace MARC.HI.EHRS.CR.Security.OAuth.Configuration
{
    /// <summary>
    /// The configuration section handler
    /// </summary>
    /// <example>
    /// <![CDATA[
    ///     <marc.hi.ehrs.cr.security.oauth>
    ///         <endpoint idp="" realm="" />
    ///         <applicationCredential client_id="" client_secret=""/>
    ///         <deviceCredential device_id="" device_secret=""/> // If Client Certificate is not set
    ///     </marc.hi.ehrs.cr.security.oauth>
    /// ]]>
    /// </example>
    public class ConfigurationSectionHandler : IConfigurationSectionHandler
    {
        /// <summary>
        /// Create the configuration
        /// </summary>
        public object Create(object parent, object configContext, XmlNode section)
        {
            XmlElement endpointNode = section.SelectSingleNode("./*[local-name() = 'endpoint']") as XmlElement,
                appNode = section.SelectSingleNode("./*[local-name() = 'applicationCredential']") as XmlElement,
                deviceNode = section.SelectSingleNode("./*[local-name() = 'deviceCredential']") as XmlElement;

            String idp = endpointNode?.Attributes["idp"]?.Value,
                realm = endpointNode?.Attributes["realm"]?.Value,
                clientId = appNode?.Attributes["client_id"]?.Value,
                clientSecret = appNode?.Attributes["client_secret"]?.Value,
                jwtSecret = endpointNode?.Attributes["hmacSecret"]?.Value,
                issuer = endpointNode?.Attributes["jwtIssuer"]?.Value;

            if (String.IsNullOrEmpty(idp))
                throw new ConfigurationErrorsException("IdP Endpoint must be specified", endpointNode ?? section);
            if(String.IsNullOrEmpty(clientId))
                throw new ConfigurationErrorsException("client_id must be specified", appNode ?? section);
            if (String.IsNullOrEmpty(clientSecret))
                throw new ConfigurationErrorsException("client_secret must be specified", appNode ?? section);


            return new OAuthSecurityConfigurationSection(idp, clientId, clientSecret, realm, jwtSecret, issuer);
        }
    }
}
