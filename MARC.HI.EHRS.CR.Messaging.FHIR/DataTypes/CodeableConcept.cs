using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace MARC.HI.EHRS.CR.Messaging.FHIR.DataTypes
{
    /// <summary>
    /// Represents a codeable concept
    /// </summary>
    [XmlType("CodeableConcept", Namespace = "http://hl7.org/fhir")]
    public class CodeableConcept : Shareable
    {
        /// <summary>
        /// Coding
        /// </summary>
        [XmlElement("coding")]
        public List<Coding> Coding { get; set; }

        /// <summary>
        /// Gets or sets the text
        /// </summary>
        [XmlElement("text")]
        public FhirString Text { get; set; }

        /// <summary>
        /// Gets or sets the primary
        /// </summary>
        [XmlElement("primary")]
        public IdRef Primary { get; set; }

    }
}
