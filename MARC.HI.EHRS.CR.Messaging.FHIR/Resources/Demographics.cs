using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using MARC.HI.EHRS.CR.Messaging.FHIR.DataTypes;

namespace MARC.HI.EHRS.CR.Messaging.FHIR.Resources
{
    /// <summary>
    /// Class representing patient demographics
    /// </summary>
    [XmlType("Demographics", Namespace = "http://hl7.org/fhir")]
    public class Demographics : Shareable
    {

        /// <summary>
        /// An identifier for the individual
        /// </summary>
        [XmlElement("identifier")]
        public List<Identifier> Identifier { get; set; }
        /// <summary>
        /// The name of the individual
        /// </summary>
        [XmlElement("name")]
        public List<HumanName> Name { get; set; }
        /// <summary>
        /// The telecommunications addresses for the individual
        /// </summary>
        [XmlElement("telecom")]
        public List<Telecom> Telecom { get; set; }
        /// <summary>
        /// The gender of the individual
        /// </summary>
        [XmlElement("gender")]
        public CodeableConcept Gender { get; set; }
        /// <summary>
        /// The birth date of the individual
        /// </summary>
        [XmlElement("birthDate")]
        public Date BirthDate { get; set; }
        /// <summary>
        /// True if the individual is deceased
        /// </summary>
        [XmlElement("deceased")]
        public FhirBoolean Deceased { get; set; }
        /// <summary>
        /// Gets or sets the addresses of the user
        /// </summary>
        [XmlElement("address")]
        public List<Address> Address { get; set; }
        /// <summary>
        /// Gets or sets the photograph of the user
        /// </summary>
        [XmlElement("photo")]
        public Resource<Picture> Photo { get; set; }
        /// <summary>
        /// Gets or sets the marital status of the user
        /// </summary>
        [XmlElement("maritalStatus")]
        public CodeableConcept MaritalStatus { get; set; }
        /// <summary>
        /// Gets or sets the language of the user
        /// </summary>
        [XmlElement("language")]
        public List<Language> Language { get; set; }


    }
}
