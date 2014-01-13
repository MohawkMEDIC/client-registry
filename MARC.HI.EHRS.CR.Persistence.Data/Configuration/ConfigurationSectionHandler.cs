/**
 * Copyright 2012-2013 Mohawk College of Applied Arts and Technology
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
 * User: fyfej
 * Date: 26-7-2012
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
using MARC.HI.EHRS.CR.Core.ComponentModel;
using MARC.HI.EHRS.SVC.Core.Services;

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
        /// Override the persistence mode
        /// </summary>
        public DataPersistenceMode? OverridePersistenceMode { get; private set; }

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
                validationConfig = section.SelectSingleNode("./*[local-name() = 'validation']"),
                matchConfig = section.SelectSingleNode("./*[local-name() = 'nameMatching']");

            if (connectionManagerConfig == null)
                throw new ConfigurationErrorsException("connectionManager must be specified", section);

            // Setup default validation section
            this.Validation = new ValidationSection() {
                AllowDuplicateRecords = false,
                DefaultMatchAlgorithms = 0,
                DefaultMatchStrength = Core.ComponentModel.MatchStrength.Exact
            };

            // Connection manager settings
            string connectionString = String.Empty, roConnectionString = connectionString;
            DbProviderFactory provider = null;

            // Validation config
            if (validationConfig != null)
            {
                // Validation Configuration
                if (validationConfig.Attributes["allowDuplicates"] != null)
                    this.Validation.AllowDuplicateRecords = Boolean.Parse(validationConfig.Attributes["allowDuplicates"].Value);
                if (validationConfig.Attributes["personMustExist"] != null)
                    this.Validation.PersonsMustExist = Boolean.Parse(validationConfig.Attributes["personMustExist"].Value);
                if (validationConfig.Attributes["validateProvidersAgainstPR"] != null)
                    this.Validation.ValidateHealthcareParticipants = Boolean.Parse(validationConfig.Attributes["validateProvidersAgainstPR"].Value);
                if (validationConfig.Attributes["minPersonNameMatch"] != null)
                    this.Validation.PersonNameMatch = (float)Double.Parse(validationConfig.Attributes["minPersonNameMatch"].Value);

            }

            // Match config
            if (matchConfig != null)
            {
                if (matchConfig.Attributes["defaultMatchStr"] != null)
                    this.Validation.DefaultMatchStrength = (MatchStrength)Enum.Parse(typeof(MatchStrength), matchConfig.Attributes["defaultMatchStr"].Value);
                if(matchConfig.Attributes["seekExactMatchFirst"] != null)
                    this.Validation.ExactMatchFirst = bool.Parse(matchConfig.Attributes["seekExactMatchFirst"].Value);

                foreach (var nd in matchConfig.ChildNodes)
                    if (nd is XmlElement && (nd as XmlElement).Name == "algorithm" && (nd as XmlElement).Attributes["name"] != null)
                        Validation.DefaultMatchAlgorithms |= (MatchAlgorithm)Enum.Parse(typeof(MatchAlgorithm), (nd as XmlElement).Attributes["name"].Value);

                if (Validation.DefaultMatchAlgorithms == 0)
                    Validation.DefaultMatchAlgorithms = MatchAlgorithm.Default;
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

            if (connectionManagerConfig.Attributes["overrideProcessingID"] != null)
                this.OverridePersistenceMode = connectionManagerConfig.Attributes["overrideProcessingID"].Value == "P" ? DataPersistenceMode.Production : DataPersistenceMode.Debugging;

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
