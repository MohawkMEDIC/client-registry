using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MARC.HI.EHRS.CR.Messaging.FHIR.DataTypes;
using System.Xml.Serialization;

namespace MARC.HI.EHRS.CR.Messaging.FHIR.Resources
{

    /// <summary>
    /// Identifies an organization
    /// </summary>
    [XmlRoot("Organization",Namespace = "http://hl7.org/fhir")] 
    [XmlType("Organization", Namespace = "http://hl7.org/fhir")]
    public class Organization : ResourceBase
    {

        /// <summary>
        /// Gets or sets the unique identifiers for the organization
        /// </summary>
        [XmlElement("identifier")]
        public List<Identifier> Identifier { get; set; }

        /// <summary>
        /// Gets or sets the name of the organization
        /// </summary>
        [XmlElement("name")]
        public FhirString Name { get; set; }

        /// <summary>
        /// Gets or sets the type of organization
        /// </summary>
        [XmlElement("type")]
        public CodeableConcept Type { get; set; }

        /// <summary>
        /// Gets or sets the addresses of the 
        /// </summary>
        [XmlElement("address")]
        public List<Address> Address { get; set; }

        /// <summary>
        /// Gets or sets the telecommunications addresses
        /// </summary>
        [XmlElement("telecom")]
        public List<Telecom> Telecom { get; set; }
        
        /// <summary>
        /// Gets or sets the active flag for the item
        /// </summary>
        [XmlElement("active")]
        public FhirBoolean Active { get; set; }

        /// <summary>
        /// Gets or sets the accrediations for the organization
        /// </summary>
        [XmlElement("accreditation")]
        public List<Accreditation> Accreditation { get; set; }

        /// <summary>
        /// Gets or sets the contact entities
        /// </summary>
        [XmlElement("contactEntity")]
        public List<ContactEntity> ContactEntity { get; set; }
    }
}
