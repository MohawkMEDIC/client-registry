﻿/**
 * Copyright 2012-2015 Mohawk College of Applied Arts and Technology
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
 * User: Justin
 * Date: 12-7-2015
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;
using System.Xml;
using System.Security.Cryptography.X509Certificates;

namespace MARC.HI.EHRS.CR.Notification.PixPdq.Configuration
{
    /// <summary>
    /// Configuration section handler for the PIXPDQv3 notification 
    /// </summary>
    /// <example>
    /// <targets>
    ///<add connectionString="servicename=xds" name="MARC-HI XDS Registry">
    ///<notify domain="1.3.6.1.4.1.33349.3.1.2.1.0">
    ///    <action type="Any" asRevise="false"/>
    ///</notify>
    ///<notify domain="1.2.840.114350.1.13.99998.8734">
    ///    <action type="Any" asRevise="false"/>
    ///</notify>
    ///</add>
    ///</targets>
    /// </example>
    public class ConfigurationSectionHandler : IConfigurationSectionHandler
    {
        #region IConfigurationSectionHandler Members

        /// <summary>
        /// Choose a certificate
        /// </summary>
        public static X509Certificate2 ChooseCertificate(StoreName storeName, StoreLocation storeLocation, bool server)
        {
            X509Store store = new X509Store(storeName, storeLocation);
            try
            {
                store.Open(OpenFlags.ReadOnly);
                var certs = store.Certificates.Find(X509FindType.FindByApplicationPolicy, server ? "1.3.6.1.5.5.7.3.1" : "1.3.6.1.5.5.7.3.2", true);
                var selected = X509Certificate2UI.SelectFromCollection(certs, "Select Certificate", server ? "Select a trusted issuer or server certificate. Server certificate chains must have this certificate present in the chain to be considered valid" : "Select a client certificate for this endpoint", X509SelectionFlag.SingleSelection);

                if (selected.Count > 0)
                    return selected[0];
                return null;
            }
            finally
            {
                store.Close();
            }
        }

        /// <summary>
        /// Find a certificate
        /// </summary>
        public static X509Certificate2 FindCertificate(StoreName storeName, StoreLocation storeLocation, X509FindType findType, string findValue)
        {
            X509Store store = new X509Store(storeName, storeLocation);
            try
            {
                store.Open(OpenFlags.ReadOnly);
                var certs = store.Certificates.Find(findType, findValue, false);
                if (certs.Count > 0)
                    return certs[0];
                else
                    throw new InvalidOperationException("Cannot locate certificate");

            }
            finally
            {
                store.Close();
            }

        }
        /// <summary>
        /// Create the configuration section
        /// </summary>
        public object Create(object parent, object configContext, System.Xml.XmlNode section)
        {
            // throw new NotImplementedException();
            var targetsElement = section.SelectNodes("./*[local-name() = 'targets']/*[local-name() = 'add']");
            var retVal = new NotificationConfiguration(Environment.ProcessorCount);

            // Targets element not found
            if (targetsElement == null || targetsElement.Count == 0)
                return retVal;

            if (section.Attributes["concurrencyLevel"] != null)
                retVal = new NotificationConfiguration(Int32.Parse(section.Attributes["concurrencyLevel"].Value));


            // Iterate through the <targets><add> elements and add them to the configuration
            foreach (XmlElement targ in targetsElement)
            {
                // VAlidate that the target has a domain and if not raise an error
                string connectionString = String.Empty;
                if(targ.Attributes["connectionString"] == null)
                    throw new ConfigurationErrorsException("Target must have a connectionString");
                else
                    connectionString = targ.Attributes["connectionString"].Value;

                // Default of target name is the domain, unless a new name is specified
                string targetName = connectionString;
                if (targ.Attributes["name"] != null)
                    targetName = targ.Attributes["name"].Value;

                string actorType = "PAT_IDENTITY_X_REF_MGR";
                if (targ.Attributes["myActor"] != null)
                    actorType = targ.Attributes["myActor"].Value;

                string deviceId = String.Empty;
                if (targ.Attributes["deviceId"] != null)
                    deviceId = targ.Attributes["deviceId"].Value;

                // Now create the target
                TargetConfiguration targetConfig = new TargetConfiguration(targetName, connectionString, actorType, deviceId);

                // Parse certificate data
                var certificateNode = targ.SelectSingleNode("./*[local-name() = 'trustedIssuerCertificate']");
                if (certificateNode != null)
                {
                    XmlAttribute storeLocationAtt = certificateNode.Attributes["storeLocation"],
                                storeNameAtt = certificateNode.Attributes["storeName"],
                                findTypeAtt = certificateNode.Attributes["x509FindType"],
                                findValueAtt = certificateNode.Attributes["findValue"];

                    if (findTypeAtt == null || findValueAtt == null) throw new ConfigurationErrorsException("Must supply x509FindType and findValue"); // can't find if nothing to find...
                    
                    targetConfig.TrustedIssuerCertLocation = (StoreLocation)Enum.Parse(typeof(StoreLocation), storeLocationAtt == null ? "LocalMachine" : storeLocationAtt.Value);
                    targetConfig.TrustedIssuerCertStore = (StoreName)Enum.Parse(typeof(StoreName), storeNameAtt == null ? "My" : storeNameAtt.Value);
                    targetConfig.TrustedIssuerCertificate = ConfigurationSectionHandler.FindCertificate(targetConfig.TrustedIssuerCertStore, targetConfig.TrustedIssuerCertLocation, (X509FindType)Enum.Parse(typeof(X509FindType), findTypeAtt.Value), findValueAtt.Value);
                }
                certificateNode = targ.SelectSingleNode("./*[local-name() = 'clientLLPCertificate']");
                if (certificateNode != null)
                {
                    XmlAttribute storeLocationAtt = certificateNode.Attributes["storeLocation"],
                                storeNameAtt = certificateNode.Attributes["storeName"],
                                findTypeAtt = certificateNode.Attributes["x509FindType"],
                                findValueAtt = certificateNode.Attributes["findValue"];

                    if (findTypeAtt == null || findValueAtt == null) throw new ConfigurationErrorsException("Must supply x509FindType and findValue"); // can't find if nothing to find...

                    targetConfig.LlpClientCertLocation = (StoreLocation)Enum.Parse(typeof(StoreLocation), storeLocationAtt == null ? "LocalMachine" : storeLocationAtt.Value);
                    targetConfig.LlpClientCertStore = (StoreName)Enum.Parse(typeof(StoreName), storeNameAtt == null ? "My" : storeNameAtt.Value);
                    targetConfig.LlpClientCertificate = ConfigurationSectionHandler.FindCertificate(targetConfig.LlpClientCertStore, targetConfig.LlpClientCertLocation, (X509FindType)Enum.Parse(typeof(X509FindType), findTypeAtt.Value), findValueAtt.Value);
                }

                // Get the notification domains and add them to the configuration
                var notificationElements = targ.SelectNodes("./*[local-name() = 'notify']");
                foreach (XmlElement ne in notificationElements)
                {
                    // Attempt to parse the notification element configuration
                    string notificationDomain = string.Empty;
                    if (ne.Attributes["domain"] == null)
                        throw new ConfigurationErrorsException("Notification element must have a domain");
                    else
                        notificationDomain = ne.Attributes["domain"].Value;

                    NotificationDomainConfiguration notificationConfig = new NotificationDomainConfiguration(notificationDomain);
                    
                    // Parse the actions
                    var actionsElements = ne.SelectNodes("./*[local-name() = 'action']");
                    foreach (XmlElement ae in actionsElements)
                    {

                        // Action types
                        ActionType value = ActionType.Create;
                        if(ae.Attributes["type"] == null)
                            throw new ConfigurationErrorsException("Action element must have a type");
                        else if (!Enum.TryParse(ae.Attributes["type"].Value, out value))
                            throw new ConfigurationErrorsException(String.Format("Invalid action type '{0}'", ae.Attributes["type"].Value));

                        notificationConfig.Actions.Add(new ActionConfiguration(value));
                    }

                    targetConfig.NotificationDomain.Add(notificationConfig);
                }

                retVal.Targets.Add(targetConfig);
            }


            return retVal;
        }

        #endregion
    }
}
