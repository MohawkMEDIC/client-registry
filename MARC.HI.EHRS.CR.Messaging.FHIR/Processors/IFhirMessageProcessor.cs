using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Collections.Specialized;
using MARC.HI.EHRS.CR.Messaging.FHIR.Resources;
using MARC.HI.EHRS.CR.Messaging.FHIR.Util;
using MARC.Everest.Connectors;

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
        /// Get the resource type
        /// </summary>
        Type ResourceType { get; }

        /// <summary>
        /// Get the component type
        /// </summary>
        Type ComponentType { get; }

        /// <summary>
        /// Parse a query
        /// </summary>
        DataUtil.FhirQuery ParseQuery(NameValueCollection parameters, List<IResultDetail> dtls);

        /// <summary>
        /// Process a resource
        /// </summary>
        IComponent ProcessResource(ResourceBase resource, List<IResultDetail> dtls);

        /// <summary>
        /// Creates a resource from a component
        /// </summary>
        ResourceBase ProcessComponent(IComponent component, List<IResultDetail> dtls);
    }
}
