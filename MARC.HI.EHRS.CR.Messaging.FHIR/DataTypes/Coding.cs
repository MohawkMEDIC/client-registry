using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace MARC.HI.EHRS.CR.Messaging.FHIR.DataTypes
{
    /// <summary>
    /// Codified concept
    /// </summary>
    [XmlType("Coding", Namespace = "http://hl7.org/fhir")]
    public class Coding : Shareable
    {

        /// <summary>
        /// The codification system
        /// </summary>
        [XmlElement("system")]
        public FhirUri System { get; set; }

        /// <summary>
        /// The code 
        /// </summary>
        [XmlElement("code")]
        public PrimitiveCode<String> Code { get; set; }

        /// <summary>
        /// Gets or sets the display
        /// </summary>
        [XmlElement("display")]
        public FhirString Display { get; set; }

    }
}
