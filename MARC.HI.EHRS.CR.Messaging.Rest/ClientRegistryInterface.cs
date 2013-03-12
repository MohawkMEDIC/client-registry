/**
 * Copyright 2013-2013 Mohawk College of Applied Arts and Technology
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
 * Date: 25-2-2013
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MARC.HI.EHRS.CR.Messaging.Rest.Configuration;
using System.Configuration;

namespace MARC.HI.EHRS.CR.Messaging.Rest
{
    /// <summary>
    /// Client registry interface
    /// </summary>
    public class ClientRegistryInterface : IClientRegistryInterface
    {

        // Configuration
        private ClientRegistryInterfaceConfiguration m_configuration;

        /// <summary>
        /// Creates a new instance of the client registry interface
        /// </summary>
        public ClientRegistryInterface()
        {
            this.m_configuration = ConfigurationManager.GetSection("marc.hi.ehrs.cr.messaging.rest") as ClientRegistryInterfaceConfiguration;
        }

        #region IClientRegistryInterface Members

        /// <summary>
        /// Get all clients matching the query 
        /// </summary>
        public System.ServiceModel.Syndication.Atom10FeedFormatter GetClients()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Get one client from the client registry
        /// </summary>
        public Core.ComponentModel.Person GetClient(string id)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
