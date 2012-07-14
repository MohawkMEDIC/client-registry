/* 
 * Copyright 2008-2011 Mohawk College of Applied Arts and Technology
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
 * User: Justin Fyfe
 * Date: 08-24-2011
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;
using MARC.HI.EHRS.CR.Persistence.Data.Connection;
using System.Xml;
using System.Data.Common;
using System.Diagnostics;
using System.Reflection;

namespace MARC.HI.EHRS.CR.Persistence.Data.Configuration
{
    /// <summary>
    /// PostgreSQL Configuration Handler
    /// </summary>
    public class ConfigurationSectionHandler : IConfigurationSectionHandler
    {
        #region IConfigurationSectionHandler Members

        /// <summary>
        /// The connection manager is responsible for handling connections to the database
        /// </summary>
        public ConnectionManager ConnectionManager { get; private set; }

        /// <summary>
        /// Connection manager that is to be used for readonly connections
        /// </summary>
        public ConnectionManager ReadonlyConnectionManager { get; private set; }

        /// <summary>
        /// Gets the validation configuration section
        /// </summary>
        public ValidationSection Validation { get; private set; }

        /// <summary>
        /// Create the configuration section
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="configContext"></param>
        /// <param name="section"></param>
        /// <returns></returns>
        public object Create(object parent, object configContext, System.Xml.XmlNode section)
        {

            XmlNode connectionManagerConfig = section.SelectSingleNode("./*[local-name() = 'connectionManager']"), 
                validationConfig = section.SelectSingleNode("./*[local-name() = 'validation']");
            if (connectionManagerConfig == null)
                throw new ConfigurationErrorsException("connectionManager must be specified", section);

            // Setup default validation section
            this.Validation = new ValidationSection() {
                PersonNameMatch = 1.0f
            };

            // Connection manager settings
            string connectionString = String.Empty, roConnectionString = connectionString;
            DbProviderFactory provider = null;

            // Validation config
            if (validationConfig != null)
            {
                // Validation Configuration
                if (validationConfig.Attributes["minPersonNameMatch"] != null)
                    this.Validation.PersonNameMatch = (float)Double.Parse(validationConfig.Attributes["minPersonNameMatch"].Value);
                if (validationConfig.Attributes["personMustExist"] != null)
                    this.Validation.PersonsMustExist = Boolean.Parse(validationConfig.Attributes["personMustExist"].Value);
                if (validationConfig.Attributes["allowDuplicates"] != null)
                    this.Validation.AllowDuplicateRecords = Boolean.Parse(validationConfig.Attributes["allowDuplicates"].Value);
                if (validationConfig.Attributes["validateClientsAgainstCR"] != null)
                    this.Validation.ValidateClients = Boolean.Parse(validationConfig.Attributes["validateClientsAgainstCR"].Value);
                if (validationConfig.Attributes["validateProvidersAgainstPR"] != null)
                    this.Validation.ValidateHealthcareParticipants = Boolean.Parse(validationConfig.Attributes["validateProvidersAgainstPR"].Value);
            }

            // Connection manager configuration
            if (connectionManagerConfig.Attributes["connection"] != null)
            {
                ConnectionStringSettings settings = ConfigurationManager.ConnectionStrings[connectionManagerConfig.Attributes["connection"].Value];
                if (settings == null)
                    throw new ConfigurationErrorsException(String.Format("Cannot find the connection string '{0}'", connectionManagerConfig.Attributes["connection"].Value), connectionManagerConfig);

                // Create the dbProvider and cstring
                connectionString = settings.ConnectionString;

                // get the type
                provider = DbProviderFactories.GetFactory(settings.ProviderName);
                if(provider == null)
                    throw new ConfigurationErrorsException(String.Format("Can't find provider type '{0}'", settings.ProviderName), connectionManagerConfig);
            }
            else
            {
                Trace.TraceError("Cannot determine the connection string settings");
                throw new ConfigurationErrorsException("Cannot determine the connection string to use", connectionManagerConfig);
            }

            // Connection manager configuration
            if (connectionManagerConfig.Attributes["readOnlyConnection"] != null)
            {
                ConnectionStringSettings settings = ConfigurationManager.ConnectionStrings[connectionManagerConfig.Attributes["readOnlyConnection"].Value];
                if (settings == null)
                    throw new ConfigurationErrorsException(String.Format("Cannot find the connection string '{0}'", connectionManagerConfig.Attributes["readOnlyConnection"].Value), connectionManagerConfig);

                // Create the dbProvider and cstring
                roConnectionString = settings.ConnectionString;

            }
            

            // Create the manager
            this.ConnectionManager = new ConnectionManager(
                connectionString, provider
                );

            if (!String.IsNullOrEmpty(roConnectionString))
                this.ReadonlyConnectionManager = new ConnectionManager(
                    roConnectionString, provider);
            else
                this.ReadonlyConnectionManager = this.ConnectionManager;
            return this;
        }

        #endregion
    }
}
