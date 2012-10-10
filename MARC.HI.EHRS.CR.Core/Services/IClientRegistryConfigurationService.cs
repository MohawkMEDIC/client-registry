using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MARC.HI.EHRS.CR.Core.Configuration;
using MARC.HI.EHRS.CR.Core.ComponentModel;

namespace MARC.HI.EHRS.CR.Core.Services
{
    public interface IClientRegistryConfigurationService
    {

        /// <summary>
        /// Gets the client registry configuration
        /// </summary>
        ClientRegistryConfiguration Configuration { get; }

        /// <summary>
        /// Create a merge filter
        /// </summary>
        Person CreateMergeFilter(Person p);
    }
}
