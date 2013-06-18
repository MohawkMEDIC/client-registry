using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using MARC.HI.EHRS.CR.Messaging.FHIR.DataTypes;

namespace MARC.HI.EHRS.CR.Messaging.FHIR.Resources
{
    /// <summary>
    /// Represents data related to animal patients
    /// </summary>
    [XmlType("Animal", Namespace = "http://hl7.org/fhir")]
    public class Animal
    {
        /// <summary>
        /// Gets or sets the species code
        /// </summary>
        [XmlElement("species")]
        public CodeableConcept Species { get; set; }

        /// <summary>
        /// Gets or sets the breed code
        /// </summary>
        [XmlElement("breed")]
        public CodeableConcept Breed { get; set; }

        /// <summary>
        /// Gets or sets the status of the gender
        /// </summary>
        [XmlElement("genderStatus")]
        public CodeableConcept GenderStatus { get; set; }

    }
}
