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
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Diagnostics;

namespace MARC.HI.EHRS.CR.Persistence.Data.Connection
{
    /// <summary>
    /// Connection pool manager
    /// </summary>
    public class ConnectionManager 
    {

        /// <summary>
        /// Sync-lock
        /// </summary>
        private object m_syncLock = new object();

        /// <summary>
        /// Data provider factory
        /// </summary>
        private DbProviderFactory m_dataProviderFactory;

        /// <summary>
        /// Gets the connection string
        /// </summary>
        public string ConnectionString { get; private set; }

        /// <summary>
        /// Creates a new instance of the connection manager class
        /// </summary>
        public ConnectionManager(string connectionString, DbProviderFactory provider)
        {
            this.ConnectionString = connectionString;
            this.m_dataProviderFactory = provider;
        }


        /// <summary>
        /// Get a connection from the pool
        /// </summary>
        public DbConnection GetConnection()
        {
            #if DEBUG
            Trace.TraceInformation("Creating a connection {0}", ConnectionString);
            #endif
                var rv = m_dataProviderFactory.CreateConnection();
                rv.ConnectionString = ConnectionString;
                rv.Open();
                return rv;
        
        }


        /// <summary>
        /// Release a connection
        /// </summary>
        internal void ReleaseConnection(IDbConnection conn)
        {
            conn.Close();
            conn.Dispose();
        }
    }
}
