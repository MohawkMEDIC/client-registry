using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace MARC.HI.EHRS.CR.Messaging.FHIR.DataTypes
{
    /// <summary>
    /// Identifies a postal address
    /// </summary>
    [XmlType("Address", Namespace = "http://hl7.org/fhir")]
    public class Address : Shareable
    {

        /// <summary>
        /// The use of the value
        /// </summary>
        [XmlElement("use")]
        public PrimitiveCode<String> Use { get; set; }

        /// <summary>
        /// Gets or sets the text
        /// </summary>
        [XmlElement("text")]
        public FhirString Text { get; set; }

        /// <summary>
        /// Gets or sets the line items of the address
        /// </summary>
        [XmlElement("line")]
        public List<FhirString> Line { get; set; }

        /// <summary>
        /// Gets or sets the city 
        /// </summary>
        [XmlElement("city")]
        public FhirString City { get; set; }

        /// <summary>
        /// Gets or sets the state
        /// </summary>
        [XmlElement("state")]
        public FhirString State { get; set; }

        /// <summary>
        /// Gets or sets the zip
        /// </summary>
        [XmlElement("zip")]
        public FhirString Zip { get; set; }

        /// <summary>
        /// Gets or sets the country
        /// </summary>
        [XmlElement("country")]
        public FhirString Country { get; set; }

        /// <summary>
        /// Gets or sets the period
        /// </summary>
        [XmlElement("period")]
        public Period Period { get; set; }

    }
}
