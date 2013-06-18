using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using MARC.HI.EHRS.CR.Messaging.FHIR.DataTypes;

namespace MARC.HI.EHRS.CR.Messaging.FHIR.Resources
{
    /// <summary>
    /// Identifies a contact entity
    /// </summary>
    [XmlType("ContactEntity", Namespace = "http://hl7.org/fhir")]
    public class ContactEntity : Shareable
    {
        /// <summary>
        /// Gets or sets the type of contact entity
        /// </summary>
        [XmlElement("type")]
        public CodeableConcept Type { get; set; }
        /// <summary>
        /// Gets or sets the name of the contact entity
        /// </summary>
        [XmlElement("name")]
        public HumanName Name { get; set; }
        /// <summary>
        /// Gets or sets the telecommunications address of the entity
        /// </summary>
        [XmlElement("telecom")]
        public List<Telecom> Telecom { get; set; }
        /// <summary>
        /// Gets or sets the address of the entity
        /// </summary>
        [XmlElement("address")]
        public Address Address { get; set; }
    }
}
