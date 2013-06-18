using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MARC.HI.EHRS.CR.Messaging.FHIR.DataTypes;
using System.Xml.Serialization;

namespace MARC.HI.EHRS.CR.Messaging.FHIR.Resources
{
    /// <summary>
    /// Represents a language of communication
    /// </summary>
    [XmlType("Language", Namespace = "http://hl7.org/fhir")]
    public class Language : Shareable
    {
        /// <summary>
        /// Gets or sets the language code
        /// </summary>
        [XmlElement("language")]
        public CodeableConcept Value { get; set; }
        /// <summary>
        /// Gets or sets the mode of communication
        /// </summary>
        [XmlElement("mode")]
        public CodeableConcept Mode { get; set; }
        /// <summary>
        /// Gets or sets the proficiency level
        /// </summary>
        [XmlElement("proficiencyLevel")]
        public CodeableConcept ProficiencyLevel { get; set; }
        /// <summary>
        /// Gets or sets the preference indicator
        /// </summary>
        [XmlElement("preference")]
        public FhirBoolean Preference { get; set; }
    }
}
