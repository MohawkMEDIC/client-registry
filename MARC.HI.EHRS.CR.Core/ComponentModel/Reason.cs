using System.Xml.Serialization;
using MARC.HI.EHRS.SVC.Core.ComponentModel;
using MARC.HI.EHRS.SVC.Core.ComponentModel.Components;
using MARC.HI.EHRS.SVC.Core.DataTypes;
using System;

namespace MARC.HI.EHRS.CR.Core.ComponentModel
{
    /// <summary>
    /// Indicates a reason for performing a task
    /// </summary>
    [Serializable][XmlType("Reason")]
    public class Reason : HealthServiceRecordComponent
    {
        /// <summary>
        /// Identifies the type of reason
        /// </summary>
        [XmlElement("reason")]
        public CodeValue ReasonType { get; set; }
        /// <summary>
        /// Identifies the status of the reason
        /// </summary>
        [XmlElement("status")]
        public StatusType Status { get; set; }
        /// <summary>
        /// Identifies the textual description of the reason
        /// </summary>
        [XmlElement("text")]
        public string Text { get; set; }
        /// <summary>
        /// Identifies the value of the reason
        /// </summary>
        [XmlElement("value")]
        public CodeValue Value { get; set; }
    }
}
