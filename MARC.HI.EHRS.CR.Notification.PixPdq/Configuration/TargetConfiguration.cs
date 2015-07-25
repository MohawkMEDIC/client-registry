/**
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
using System.Security.Cryptography.X509Certificates;
using System.ComponentModel;
using System.Configuration;
using System.Xml.Serialization;

namespace MARC.HI.EHRS.CR.Notification.PixPdq.Configuration
{

   
    /// <summary>
    /// Target node configuration
    /// </summary>
    public class TargetConfiguration
    {
        // The notifier
        private INotifier m_notifier;

        /// <summary>
        /// Creates a new target configuration
        /// </summary>
        public TargetConfiguration(string name, string connectionString, String actAs, string deviceId)
        {
            this.Name = name;
            this.ConnectionString = connectionString;
            this.DeviceIdentifier = deviceId;
            this.NotificationDomain = new List<NotificationDomainConfiguration>();

            var notifierType = Array.Find(typeof(TargetConfiguration).Assembly.GetTypes(), t => t.Name == actAs);
            if (notifierType == null)
                throw new ConfigurationErrorsException(String.Format("Could not find the specified actor implementation {0}", actAs));
            var ci = notifierType.GetConstructor(Type.EmptyTypes);
            if(ci == null)
                throw new ConfigurationErrorsException(String.Format("Could not find the specified actor implementation {0}", actAs));
            this.m_notifier = ci.Invoke(null) as INotifier;
            this.m_notifier.Target = this;
            
        }


        /// <summary>
        /// Gets or sets the name of the notification configuration
        /// </summary>
        [XmlAttribute("name")]
        public string Name { get; private set; }

        /// <summary>
        /// Gets or sets the WcfServiceConnector connection string
        /// </summary>
        [XmlIgnore]
        public string ConnectionString { get; internal set; }

        /// <summary>
        /// Gets or sets the notification domains to be sent when an action is 
        /// created
        /// </summary>
        [XmlIgnore]
        public List<NotificationDomainConfiguration> NotificationDomain { get; set; }

        /// <summary>
        /// Configuration device identifier
        /// </summary>
        [XmlIgnore]
        public string DeviceIdentifier { get; private set; }

        /// <summary>
        /// Gets the notifier technology implementation
        /// </summary>
        [XmlIgnore]
        public INotifier Notifier
        {
            get
            {
                return this.m_notifier;
            }
        }

        /// <summary>
        /// Identifies the location to scan fo the server certificate
        /// </summary>
        [XmlIgnore]
        public StoreLocation LlpClientCertLocation { get; internal set; }

        /// <summary>
        /// Identifies the name of the server certificate store
        /// </summary>
        [XmlIgnore]
        public StoreName LlpClientCertStore { get; internal set; }

        /// <summary>
        /// Llp client certificate
        /// </summary>
        [XmlIgnore]
        public X509Certificate2 LlpClientCertificate { get; internal set; }
        
        /// <summary>
        /// Identifies the location to scan fo the server certificate
        /// </summary>
        [XmlIgnore]
        public StoreLocation TrustedIssuerCertLocation { get; internal set; }

        /// <summary>
        /// Identifies the name of the server certificate store
        /// </summary>
        [XmlIgnore]
        public StoreName TrustedIssuerCertStore { get; internal set; }

        /// <summary>
        /// Gets the server certificate
        /// </summary>
        [XmlIgnore]
        public X509Certificate2 TrustedIssuerCertificate { get; internal set; }
    }
}
