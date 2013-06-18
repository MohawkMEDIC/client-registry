using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using MARC.HI.EHRS.CR.Messaging.FHIR.DataTypes;

namespace MARC.HI.EHRS.CR.Messaging.FHIR.Resources
{
    /// <summary>
    /// Identifies a practitioner
    /// </summary>
    [XmlType("Practitioner", Namespace = "http://hl7.org/fhir")]
    [XmlRoot("Practitioner", Namespace = "http://hl7.org/fhir")]
    public class Practictioner : ResourceBase
    {
        /// <summary>
        /// Gets or sets identifier
        /// </summary>
        [XmlElement("identifier")]
        public List<Identifier> Identifier { get; set; }
        /// <summary>
        /// Gets or set the details
        /// </summary>
        [XmlElement("details")]
        public Demographics Details { get; set; }
        /// <summary>
        /// Gets or sets the organization
        /// </summary>
        [XmlElement("organization")]
        public Resource<Organization> Organization { get; set; }
        /// <summary>
        /// Gets or sets the role
        /// </summary>
        [XmlElement("role")]
        public CodeableConcept Role { get; set; }
        /// <summary>
        /// Gets or sets the specialty
        /// </summary>
        [XmlElement("specialty")]
        public List<CodeableConcept> Specialty { get; set; }
        /// <summary>
        /// Gets or sets the period
        /// </summary>
        [XmlElement("period")]
        public Period Period { get; set; }
        /// <summary>
        /// Gets or sets the qualifications of the practicioner
        /// </summary>
        [XmlElement("qualification")]
        public List<Qualification> Qualification { get; set; }

    }
}
