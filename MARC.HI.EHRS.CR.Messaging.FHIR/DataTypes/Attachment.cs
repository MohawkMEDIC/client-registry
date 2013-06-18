using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace MARC.HI.EHRS.CR.Messaging.FHIR.DataTypes
{
    /// <summary>
    /// Identifies an attachment
    /// </summary>
    [XmlType("Attachment", Namespace = "http://hl7.org/fhir")]
    public class Attachment : Shareable
    {

        /// <summary>
        /// Gets or sets the content-type
        /// </summary>
        [XmlElement("contentType")]
        public PrimitiveCode<String> ContentType { get; set; }

        /// <summary>
        /// Gets or sets the language
        /// </summary>
        [XmlElement("language")]
        public PrimitiveCode<String> Language { get; set; }

        /// <summary>
        /// Gets or sets the data for the attachment
        /// </summary>
        [XmlElement("data")]
        public Primitive<byte[]> Data { get; set; }

        /// <summary>
        /// Gets or sets a url reference
        /// </summary>
        [XmlElement("url")]
        public FhirUri Url { get; set; }

        /// <summary>
        /// Gets or sets the size
        /// </summary>
        [XmlElement("size")]
        public FhirInt Size { get; set; }

        /// <summary>
        /// Gets or sets the hash code
        /// </summary>
        [XmlElement("hash")]
        public Primitive<byte[]> Hash { get; set; }

        /// <summary>
        /// Gets or sets the title
        /// </summary>
        [XmlElement("title")]
        public FhirString Title { get; set; }

    }
}
