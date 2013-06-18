using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using MARC.HI.EHRS.CR.Messaging.FHIR.DataTypes;

namespace MARC.HI.EHRS.CR.Messaging.FHIR.Resources
{
    /// <summary>
    /// The patient resource
    /// </summary>
    [XmlType("Patient", Namespace = "http://hl7.org/fhir")]
    [XmlRoot("Patient", Namespace = "http://hl7.org/fhir")] 
    public class Patient : ResourceBase
    {

        /// <summary>
        /// Patient constructor
        /// </summary>
        public Patient()
        {
            this.Link = new List<Resource<Patient>>();
            this.Identifier = new List<Identifier>();
            this.Active = new FhirBoolean(true);
        }

        /// <summary>
        /// Link between this patient and others
        /// </summary>
        [XmlElement("link")]
        public List<Resource<Patient>> Link { get; set; }

        /// <summary>
        /// True when the patient is active
        /// </summary>
        [XmlElement("active")]
        public FhirBoolean Active { get; set; }

        /// <summary>
        /// Gets or sets a list of identifiers
        /// </summary>
        [XmlElement("identifier")]
        public List<Identifier> Identifier { get; set; }

        /// <summary>
        /// Patient demographics 
        /// </summary>
        [XmlElement("details")]
        public Demographics Details { get; set; }

        /// <summary>
        /// Contact details
        /// </summary>
        [XmlElement("contact")]
        public List<Contact> Contact { get; set; }

        /// <summary>
        /// Animal reference
        /// </summary>
        [XmlElement("animal")]
        public Animal Animal { get; set; }

        /// <summary>
        /// Provider of the patient resource
        /// </summary>
        [XmlElement("provider")]
        public Resource<Organization> Provider { get; set; }

        /// <summary>
        /// The multiple birth indicator
        /// </summary>
        [XmlElement("multipleBirthInteger", typeof(FhirInt))]
        [XmlElement("multipleBirthBoolean", typeof(FhirBoolean))]
        public Shareable MultipleBirth { get; set; }

        /// <summary>
        /// The deceased date of the resource
        /// </summary>
        [XmlElement("deceasedDate")]
        public Primitive<DateTime> DeceasedDate { get; set; }

    }
}
