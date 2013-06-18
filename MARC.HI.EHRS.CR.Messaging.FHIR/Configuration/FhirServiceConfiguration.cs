using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MARC.HI.EHRS.CR.Messaging.FHIR.Configuration
{
    /// <summary>
    /// FHIR service configuration
    /// </summary>
    public class FhirServiceConfiguration
    {

        /// <summary>
        /// Creates a new instance of the WcfEndpoint
        /// </summary>
        public FhirServiceConfiguration(string wcfEndpoint)
        {
            this.WcfEndpoint = wcfEndpoint;
        }

        /// <summary>
        /// Gets the WCF endpoint name that the FHIR service listens on
        /// </summary>
        public string WcfEndpoint { get; private set; }

    }
}
