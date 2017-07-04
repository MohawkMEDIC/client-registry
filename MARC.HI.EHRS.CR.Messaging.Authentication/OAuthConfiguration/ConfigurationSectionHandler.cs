using MARC.HI.EHRS.CR.Messaging.Authentication.Security;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IdentityModel.Tokens;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace MARC.HI.EHRS.CR.Messaging.Authentication.OAuthConfiguration
{
    /// <summary>
    /// Configuration section handler
    /// </summary>
    /// <remarks>
    /// <![CDATA[
    /// <marc.hi.ehrs.cr.messaging.authentication>
    ///     <security>
    ///         <basic requireClientAuth="true" realm="">
    ///             <!-- Claims allowed to be made by clients on basic auth -->
    ///             <allowedClaims>
    ///                 <add claimType=""/>
    ///             </allowedClaims>
    ///         </basic>
    ///         <token realm="">
    ///             <audience>
    ///                 <add name=""/>
    ///             </audience>
    ///             <issuers customCertificateValidator="">
    ///                 <add name="issuerName" findValue="" storeLocation="" storeName="" x509FindType=""/>
    ///                 <add name="issuerName" symmetricKey=""/>
    ///             </issuers>
    ///         </token>
    ///     </security>
    /// </marc.hi.ehrs.cr.messaging.authentication>
    /// ]]>
    /// </remarks>
    public class ConfigurationSectionHandler : IConfigurationSectionHandler
    {
        /// <summary>
        /// Create the configuration
        /// </summary>
        public object Create(object parent, object configContext, XmlNode section)
        {
            OAuthConfiguration retVal = new OAuthConfiguration();

            // Nodes
            XmlElement securityNode = section.SelectSingleNode("./security") as XmlElement,
                threadingNode = section.SelectSingleNode("./threading") as XmlElement;

            retVal.ThreadPoolSize = Int32.Parse(threadingNode?.Attributes["poolSize"].Value ?? Environment.ProcessorCount.ToString());
            // Security?
            if (securityNode != null)
            {
                retVal.Security = new OAuthSecurityConfiguration();

                XmlElement basicSecurityNode = securityNode.SelectSingleNode("./basic") as XmlElement,
                    tokenSecurityNode = securityNode.SelectSingleNode("./token") as XmlElement;

                if (tokenSecurityNode != null)
                {
                    retVal.Security.ClaimsAuth = new OAuthClaimsAuthorization();
                    retVal.Security.ClaimsAuth.Realm = tokenSecurityNode.Attributes["realm"]?.Value;

                    foreach (XmlNode aud in tokenSecurityNode.SelectNodes("./audience/add/@name"))
                        retVal.Security.ClaimsAuth.Audiences.Add(aud.Value);
                    foreach (XmlNode iss in tokenSecurityNode.SelectNodes("./issuer/add"))
                    {
                        String name = iss.Attributes["name"]?.Value,
                            thumbprint = iss.Attributes["findValue"]?.Value,
                            storeLocation = iss.Attributes["storeLocation"]?.Value,
                            storeName = iss.Attributes["storeName"]?.Value,
                            findType = iss.Attributes["x509FindType"]?.Value,
                            symmetricKey = iss.Attributes["symmetricKey"]?.Value;

                        if (String.IsNullOrEmpty(name))
                            throw new ConfigurationException("Issuer must have name");

                        if (!String.IsNullOrEmpty(symmetricKey))
                            retVal.Security.ClaimsAuth.IssuerKeys.Add(name, new InMemorySymmetricSecurityKey(Convert.FromBase64String(symmetricKey)));
                        else
                            retVal.Security.ClaimsAuth.IssuerKeys.Add(name, new X509SecurityKey(
                                SecurityUtils.FindCertificate(
                                    storeLocation,
                                    storeName,
                                    findType,
                                    thumbprint
                                    ))
                            );

                    }
                }
                else if (basicSecurityNode != null)
                {
                    retVal.Security.BasicAuth = new OAuthBasicAuthorization();
                    retVal.Security.BasicAuth.RequireClientAuth = basicSecurityNode.Attributes["requireClientAuth"]?.Value == "true";
                    retVal.Security.BasicAuth.Realm = basicSecurityNode.Attributes["realm"]?.Value;
                    // Allowed claims
                    XmlNodeList allowedClaims = basicSecurityNode.SelectNodes("./allowedClaims/add/@claimType");
                    retVal.Security.BasicAuth.AllowedClientClaims = new List<string>();
                    foreach (XmlNode all in allowedClaims)
                        retVal.Security.BasicAuth.AllowedClientClaims.Add(all.Value);
                }

                var certThumbprintSection = securityNode.SelectSingleNode("./thumbprint");

                retVal.CertificateThumbprint = certThumbprintSection.Attributes["value"].Value;

                var authenticationEndpoint = securityNode.SelectSingleNode("./authenticationEndpoint");
                
                retVal.URLEndpoint = authenticationEndpoint.Attributes["value"].Value;
            }
            
            return retVal;
        }
    }
}
