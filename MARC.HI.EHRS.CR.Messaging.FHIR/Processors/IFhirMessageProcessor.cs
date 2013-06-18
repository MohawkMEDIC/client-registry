using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Collections.Specialized;
using MARC.HI.EHRS.CR.Messaging.FHIR.Resources;

namespace MARC.HI.EHRS.CR.Messaging.FHIR.Processors
{
    /// <summary>
    /// FHIR message processor
    /// </summary>
    public interface IFhirMessageProcessor
    {

        /// <summary>
        /// Gets the resource that this message processor handles
        /// </summary>
        string ResourceName { get; }

        /// <summary>
        /// Parse a query
        /// </summary>
        IComponent ParseQuery(NameValueCollection parameters);

        /// <summary>
        /// Process a resource
        /// </summary>
        IComponent ProcessResource(ResourceBase resource);

        /// <summary>
        /// Creates a resource from a component
        /// </summary>
        ResourceBase ProcessComponent(IComponent component);
    }
}
