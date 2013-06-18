using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MARC.HI.EHRS.CR.Messaging.FHIR.DataTypes;
using System.Xml.Serialization;

namespace MARC.HI.EHRS.CR.Messaging.FHIR.Resources
{
    /// <summary>
    /// Contact information
    /// </summary>
    [XmlType("Contact", Namespace = "http://hl7.org/fhir")]
    public class Contact : Shareable
    {

        /// <summary>
        /// Gets or sets the relationships between the container
        /// </summary>
        [XmlElement("relationship")]
        public List<CodeableConcept> Relationship { get; set; }

        /// <summary>
        /// Gets or sets the patient details
        /// </summary>
        [XmlElement("details")]
        public Demographics Details { get; set; }

        /// <summary>
        /// Gets or sets the organization
        /// </summary>
        [XmlElement("organization")]
        public Resource<Organization> Organization { get; set; }

    }
}
