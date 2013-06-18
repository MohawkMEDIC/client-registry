using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MARC.HI.EHRS.CR.Messaging.FHIR.DataTypes;
using System.Xml.Serialization;

namespace MARC.HI.EHRS.CR.Messaging.FHIR.Resources
{
    /// <summary>
    /// Base for all resources
    /// </summary>
    [XmlType("ResourceBase", Namespace = "http://hl7.org/fhir")]
    public abstract class ResourceBase : Shareable
    {
        /// <summary>
        /// Gets or sets the internal identifier for the resource
        /// </summary>
        [XmlIgnore()]
        public decimal Id { get; set; }

    }
}
