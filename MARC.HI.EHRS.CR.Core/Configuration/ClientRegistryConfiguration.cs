using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MARC.HI.EHRS.CR.Core.Configuration
{
    /// <summary>
    /// Represents a container that holds all client registry configuration elements
    /// </summary>
    public class ClientRegistryConfiguration
    {

        /// <summary>
        /// Gets the registration configuration
        /// </summary>
        public RegistrationConfiguration Registration { get; internal set; }

    }
       
}
