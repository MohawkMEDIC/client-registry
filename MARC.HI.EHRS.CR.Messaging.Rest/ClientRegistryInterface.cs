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
