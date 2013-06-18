using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace MARC.HI.EHRS.CR.Messaging.FHIR.DataTypes
{
    /// <summary>
    /// Represents a range of values
    /// </summary>
    [XmlType("Range", Namespace = "http://hl7.org/fhir")]
    public class Range : Shareable
    {

        /// <summary>
        /// Lower bound of the range
        /// </summary>
        [XmlElement("low")]
        public Quantity Low { get; set; }

        /// <summary>
        /// Upper bound of the range
        /// </summary>
        [XmlElement("high")]
        public Quantity High { get; set; }

    }
}
