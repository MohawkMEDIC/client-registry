using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace MARC.HI.EHRS.CR.Messaging.FHIR.DataTypes
{
    /// <summary>
    /// Schedule repeat
    /// </summary>
    [XmlType("ScheduleRepeat")]
    public class ScheduleRepeat
    {
        /// <summary>
        /// The frequency of per duration
        /// </summary>
        [XmlElement("frequency")]
        public FhirInt Frequency { get; set; }
        /// <summary>
        /// The event occurance duration 
        /// </summary>
        [XmlElement("when")]
        public PrimitiveCode<String> When { get; set; }
        /// <summary>
        /// Repeating or event-related duration
        /// </summary>
        [XmlElement("duration")]
        public Primitive<Decimal> Duration { get; set; }
        /// <summary>
        /// The units of time for the duration
        /// </summary>
        [XmlElement("units")]
        public FhirString Units { get; set; }
        /// <summary>
        /// The number of times to repeat
        /// </summary>
        [XmlElement("count")]
        public FhirInt Count { get; set; }
        /// <summary>
        /// The stop date
        /// </summary>
        [XmlElement("stop")]
        public Date Stop { get; set; }
    }
}
