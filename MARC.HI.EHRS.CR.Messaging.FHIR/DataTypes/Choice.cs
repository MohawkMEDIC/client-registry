using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace MARC.HI.EHRS.CR.Messaging.FHIR.DataTypes
{

    /// <summary>
    /// Option
    /// </summary>
    [XmlType("Option", Namespace = "http://hl7.org/fhir")]
    public class Option
    {
        /// <summary>
        /// Gets or sets the code
        /// </summary>
        [XmlElement("code")]
        public PrimitiveCode<String> Code { get; set; }

        /// <summary>
        /// Gets or sets the display name
        /// </summary>
        [XmlElement("display")]
        public PrimitiveCode<String> Display { get; set; }
    }

    /// <summary>
    /// Choice element
    /// </summary>
    [XmlType("Choice", Namespace = "http://hl7.org/fhir")]
    public class Choice : Shareable
    {

        /// <summary>
        /// Gets or sets the primary selected code
        /// </summary>
        [XmlElement("code")]
        public PrimitiveCode<String> Code { get; set; }

        /// <summary>
        /// Gets or sets the alternate options 
        /// </summary>
        [XmlElement("option")]
        public List<Option> Option { get; set; }

        /// <summary>
        /// Gets or sets whether the options are ordered
        /// </summary>
        [XmlElement("isOrdered")]
        public FhirBoolean IsOrdered { get; set; }
    }
}
