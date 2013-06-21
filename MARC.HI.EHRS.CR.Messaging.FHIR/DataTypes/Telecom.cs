using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace MARC.HI.EHRS.CR.Messaging.FHIR.DataTypes
{
    /// <summary>
    /// Represents a telecommunications address
    /// </summary>
    [XmlType("Telecom", Namespace="http://hl7.org/fhir")]
    public class Telecom : Shareable
    {
        /// <summary>
        /// Gets or sets the type of contact
        /// </summary>
        [XmlElement("system")]
        public PrimitiveCode<String> System { get; set; }
        /// <summary>
        /// Gets or sets the value of the standard
        /// </summary>
        [XmlElement("value")]
        public FhirString Value { get; set; }
        /// <summary>
        /// Gets or sets the use of the standard
        /// </summary>
        [XmlElement("use")]
        public PrimitiveCode<String> Use { get; set; }
        /// <summary>
        /// Gets or sets the period the telecom is valid
        /// </summary>
        [XmlElement("period")]
        public Period Period { get; set; }

        /// <summary>
        /// Write text
        /// </summary>
        internal override void WriteText(System.Xml.XmlWriter w)
        {
            w.WriteStartElement("a", NS_XHTML);
            w.WriteAttributeString("href", this.Value);
            w.WriteString(this.Value.ToString());
            w.WriteEndElement(); // a
            w.WriteString(String.Format("({0})", this.Use));
        }
    }
}
