/**
 * Copyright 2012-2012 Mohawk College of Applied Arts and Technology
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
 * Date: 5-12-2012
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MARC.HI.EHRS.SVC.Core.Configuration;
using Npgsql;
using System.Xml;
using System.IO;

namespace MARC.HI.EHRS.CR.Configurator.Database.Postgresql9
{
    /// <summary>
    /// Database configurator for PostgreSQL 9.0
    /// </summary>
    public class DatabaseConfigurator : IDatabaseConfigurator
    {
        #region IDatabaseConfigurator Members

        /// <summary>
        /// Get the name of the configurator
        /// </summary>
        public string Name
        {
            get { return "PostgreSQL 9.0 via Npgsql"; }
        }

        /// <summary>
        /// Gets the invariant name of the configurator
        /// </summary>
        public string InvariantName
        {
            get { return "Npgsql"; }
        }

        public void DeployFeature(string featureName, string connectionStringName, System.Xml.XmlDocument configurationDom)
        {
            // Get the embedded resource
            TextReader tr = null;
            try
            {
                tr = new StreamReader(this.GetType().Assembly.GetManifestResourceStream(String.Format("{0}.{1}.SQL", this.GetType().Namespace, featureName)));
                // Deploy the feature
                string connectionString = configurationDom.SelectSingleNode(String.Format("//connectionStrings/add[@name='{0}']/@connectionString", connectionStringName)).Value;
                NpgsqlConnection conn = new NpgsqlConnection(connectionString);
                try
                {
                    conn.Open();
                    using(NpgsqlCommand cmd = new NpgsqlCommand(tr.ReadToEnd(), conn))
                        cmd.ExecuteNonQuery();
                }
                finally
                {
                    conn.Close();
                }
            }
            finally
            {
                if (tr != null)
                    tr.Close();
            }
        }

        public void UnDeployFeature(string featureName, string connectionStringName, System.Xml.XmlDocument configurationDom)
        {
            // Get the embedded resource
            TextReader tr = null;
            try
            {
                tr = new StreamReader(this.GetType().Assembly.GetManifestResourceStream(String.Format("{0}.{1}_CLEAN.SQL", this.GetType().Namespace, featureName)));
                // Deploy the feature
                string connectionString = configurationDom.SelectSingleNode(String.Format("//connectionStrings/add[@name='{0}']/@connectionString", connectionStringName)).Value;
                NpgsqlConnection conn = new NpgsqlConnection(connectionString);
                try
                {
                    conn.Open();
                    using (NpgsqlCommand cmd = new NpgsqlCommand(tr.ReadToEnd(), conn))
                        cmd.ExecuteNonQuery();
                }
                finally
                {
                    conn.Close();
                }
            }
            finally
            {
                if (tr != null)
                    tr.Close();
            }
        }

        /// <summary>
        /// Create connection string element
        /// </summary>
        public string CreateConnectionStringElement(System.Xml.XmlDocument configurationDom, string serverName, string userName, string password, string databaseName)
        {
            // Two elements we need
            XmlElement connectionStringElement = configurationDom.SelectSingleNode("//*[local-name() = 'connectionStrings']") as XmlElement,
                dataProviderElement = configurationDom.SelectSingleNode("//*[local-name() = 'system.data']") as XmlElement;
            string connectionString = CreateConnectionString(serverName, userName, password, databaseName);

            // Register data provider
            if (dataProviderElement == null)
            {
                dataProviderElement = configurationDom.CreateElement("system.data");
                configurationDom.DocumentElement.AppendChild(dataProviderElement);
            }

            // Provider factory
            XmlElement dbProviderFactoryElement = dataProviderElement.SelectSingleNode("./*[local-name() = 'DbProviderFactories']") as XmlElement;
            if (dbProviderFactoryElement == null)
            {
                dbProviderFactoryElement = configurationDom.CreateElement("DbProviderFactories");
                dataProviderElement.AppendChild(dbProviderFactoryElement);
            }

            // Register element
            XmlElement pgsqlProviderFactoryElement = dbProviderFactoryElement.SelectSingleNode("./*[local-name() = 'add'][@invariant = 'Npgsql']") as XmlElement;
            if (pgsqlProviderFactoryElement == null)
            {
                pgsqlProviderFactoryElement = configurationDom.CreateElement("add");
                pgsqlProviderFactoryElement.Attributes.Append(configurationDom.CreateAttribute("name"));
                pgsqlProviderFactoryElement.Attributes.Append(configurationDom.CreateAttribute("invariant"));
                pgsqlProviderFactoryElement.Attributes.Append(configurationDom.CreateAttribute("description"));
                pgsqlProviderFactoryElement.Attributes.Append(configurationDom.CreateAttribute("type"));
                pgsqlProviderFactoryElement.Attributes["name"].Value = "PostgreSQL Data Provider";
                pgsqlProviderFactoryElement.Attributes["invariant"].Value = this.InvariantName;
                pgsqlProviderFactoryElement.Attributes["description"].Value = "PostgreSQL .NET Framework Data Provider";
                pgsqlProviderFactoryElement.Attributes["type"].Value = "Npgsql.NpgsqlFactory, Npgsql, Version=2.0.1.0, Culture=neutral, PublicKeyToken=5d8b90d52f46fda7";
                dbProviderFactoryElement.AppendChild(pgsqlProviderFactoryElement);
            }

            // Add connection string
            Guid connStrId = Guid.NewGuid();
            string connStrName = String.Format("conn{0}", connStrId.ToString().Substring(1, connStrId.ToString().IndexOf("-") - 1));
            if (connectionStringElement == null)
            {
                connectionStringElement = configurationDom.CreateElement("connectionStrings");
                configurationDom.DocumentElement.AppendChild(connectionStringElement);
            }

            XmlElement addConnectionElement = connectionStringElement.SelectSingleNode(String.Format("./*[local-name() = 'add'][@connectionString = '{0}']", connectionString)) as XmlElement;
            if (addConnectionElement == null)
            {
                addConnectionElement = configurationDom.CreateElement("add");
                addConnectionElement.Attributes.Append(configurationDom.CreateAttribute("name"));
                addConnectionElement.Attributes.Append(configurationDom.CreateAttribute("connectionString"));
                addConnectionElement.Attributes.Append(configurationDom.CreateAttribute("providerName"));
                addConnectionElement.Attributes["name"].Value = connStrName;
                addConnectionElement.Attributes["connectionString"].Value = connectionString;
                addConnectionElement.Attributes["providerName"].Value = this.InvariantName;
                connectionStringElement.AppendChild(addConnectionElement);
            }
            else
                connStrName = addConnectionElement.Attributes["name"].Value;

            return connStrName;

        }


        /// <summary>
        /// Tostring method for GUI
        /// </summary>
        public override string ToString()
        {
            return this.Name;
        }
        #endregion

        #region IDatabaseConfigurator Members

        /// <summary>
        /// Get all databases
        /// </summary>
        public string[] GetDatabases(string serverName, string userName, string password)
        {
            NpgsqlConnection conn = new NpgsqlConnection(CreateConnectionString(serverName, userName, password, null));
            try
            {
                conn.Open();
                NpgsqlCommand cmd = new NpgsqlCommand("SELECT datname FROM pg_database;", conn);
                List<String> retVal = new List<string>(10);
                using (var reader = cmd.ExecuteReader())
                    while (reader.Read())
                        retVal.Add(Convert.ToString(reader[0]));
                return retVal.ToArray();
            }
            catch
            {
                throw;
            }
            finally
            {
                conn.Close();
            }
        }

        #endregion

        #region Utility Functions
        /// <summary>
        /// Create connection string
        /// </summary>
        private string CreateConnectionString(string serverName, string userName, string password, string databaseName)
        {
            NpgsqlConnectionStringBuilder builder = new NpgsqlConnectionStringBuilder();
            builder.UserName = userName;
            builder.Database = databaseName; 
            builder.Host = serverName;
            builder.Pooling = true;
            builder.MinPoolSize = 10;
            builder.MaxPoolSize = 20;
            builder.CommandTimeout = 240;
            builder.Add("password", password);
            return builder.ConnectionString;
        }

        #region IDatabaseConfigurator Members

        /// <summary>
        /// Get connection string element
        /// </summary>
        public string GetConnectionStringElement(XmlDocument configurationDom, ConnectionStringPartType partType, string connectionString)
        {
            string connectionData = configurationDom.SelectSingleNode(String.Format("//*[local-name() = 'connectionStrings']/*[local-name() = 'add'][@name = '{0}']/@connectionString", connectionString)).Value;
            if (connectionData == null)
                return String.Empty;
            NpgsqlConnectionStringBuilder bldr = new NpgsqlConnectionStringBuilder(connectionData);
            switch (partType)
            {
                case ConnectionStringPartType.Database:
                    return bldr.Database;
                case ConnectionStringPartType.Host:
                    return bldr.Host;
                case ConnectionStringPartType.Password:
                    return bldr[Keywords.Password].ToString();
                case ConnectionStringPartType.UserName:
                    return bldr.UserName;
                default:
                    return String.Empty;
            }
        }

        #endregion
        #endregion
    }
}
