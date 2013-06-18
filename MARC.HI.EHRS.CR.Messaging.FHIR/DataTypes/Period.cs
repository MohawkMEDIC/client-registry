using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace MARC.HI.EHRS.CR.Messaging.FHIR.DataTypes
{


    /// <summary>
    /// Represents a period of time
    /// </summary>
    [XmlType("Period", Namespace = "http://hl7.org/fhir")]
    public class Period : Shareable
    {

        /// <summary>
        /// Identifies the start time of the period
        /// </summary>
        [XmlElement("start")]
        public Date Start { get; set; }

        /// <summary>
        /// Identifies the stop time of the period
        /// </summary>
        [XmlElement("stop")]
        public Date Stop { get; set; }


    }
}
