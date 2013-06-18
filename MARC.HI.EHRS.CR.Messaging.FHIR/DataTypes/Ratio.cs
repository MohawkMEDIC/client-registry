using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace MARC.HI.EHRS.CR.Messaging.FHIR.DataTypes
{
    /// <summary>
    /// Represents a ratio of two quantities
    /// </summary>
    [XmlType("Ratio", Namespace = "http://hl7.org/fhir")]
    public class Ratio : Shareable
    {

        /// <summary>
        /// Numerator
        /// </summary>
        [XmlElement("numerator")]
        public Quantity Numerator { get; set; }

        /// <summary>
        /// Denominator
        /// </summary>
        [XmlElement("denominator")]
        public Quantity Denominator { get; set; }

    }
}
