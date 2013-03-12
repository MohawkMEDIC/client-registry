using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MARC.HI.EHRS.CR.Messaging.Rest.Configuration
{
    /// <summary>
    /// Client registry interface configuration
    /// </summary>
    public class ClientRegistryInterfaceConfiguration
    {

        /// <summary>
        /// Creates a new instance of the client registry interface configuration
        /// </summary>
        public ClientRegistryInterfaceConfiguration(Uri address)
        {
            this.Address = address;
        }

        /// <summary>
        /// Gets the address of the client registry REST interface
        /// </summary>
        public Uri Address { get; private set; }
    }
}
