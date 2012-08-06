using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using MARC.HI.EHRS.SVC.Core.ComponentModel.Components;
using MARC.HI.EHRS.SVC.Core.DataTypes;

namespace MARC.HI.EHRS.CR.Core.ComponentModel
{
    /// <summary>
    /// Employment
    /// </summary>
    [XmlType("Person", Namespace = "urn:marc-hi:ca/cr")]
    [Serializable]
    public class Employment
    {

        /// <summary>
        /// Gets ors ets the id
        /// </summary>
        [XmlAttribute("id")]
        public decimal Id { get; set; }

        /// <summary>
        /// Gets or sets the update mode
        /// </summary>
        [XmlAttribute("updateMode")]
        public UpdateModeType UpdateMode { get; set; }

        /// <summary>
        /// Gets or sets the effective time
        /// </summary>
        [XmlElement("efft")]
        public TimestampSet EffectiveTime { get; set; }

        /// <summary>
        /// Gets or sets the occupation 
        /// </summary>
        [XmlElement("occupation")]
        public CodeValue Occupation { get; set; }

        /// <summary>
        /// Gets or sets the status
        /// </summary>
        [XmlAttribute("status")]
        public StatusType Status { get; set; }

    }
}
