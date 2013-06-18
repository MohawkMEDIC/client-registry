using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace MARC.HI.EHRS.CR.Messaging.FHIR.DataTypes
{
    /// <summary>
    /// Represents
    /// </summary>
    [XmlType("Schedule", Namespace = "http://hl7.org/fhir")]
    public class Schedule : Shareable
    {

        /// <summary>
        /// The event that is being scheduled
        /// </summary>
        [XmlElement("event")]
        public Period Event { get; set; }

        /// <summary>
        /// Only if there is onw or none events
        /// </summary>
        [XmlElement("repeat")]
        public ScheduleRepeat Repeat { get; set; }

    }
}
