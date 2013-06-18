using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace MARC.HI.EHRS.CR.Messaging.FHIR.DataTypes
{
    /// <summary>
    /// Represents a human name
    /// </summary>
    [XmlType("HumanName", Namespace = "http://hl7.org/fhir")]
    public class HumanName : Shareable
    {

        /// <summary>
        /// Get or sets the use
        /// </summary>
        [XmlElement("use")]
        public PrimitiveCode<String> Use { get; set; }

        /// <summary>
        /// Gets or sets the textual representation of the full name
        /// </summary>
        [XmlElement("text")]
        public FhirString Text { get; set; }

        /// <summary>
        /// Gets or sets the family name
        /// </summary>
        [XmlElement("family")]
        public List<FhirString> Family { get; set; }

        /// <summary>
        /// Gets or sets the given name
        /// </summary>
        [XmlElement("given")]
        public List<FhirString> Given { get; set; }

        /// <summary>
        /// Gets or sets the prefix
        /// </summary>
        [XmlElement("prefix")]
        public List<FhirString> Prefix { get; set; }

        /// <summary>
        /// Gets or sets the suffix name
        /// </summary>
        [XmlElement("suffix")]
        public List<FhirString> Suffix { get; set; }

        /// <summary>
        /// Gets or sets the period
        /// </summary>
        [XmlElement("period")]
        public Period Period { get; set; }
    }
}
