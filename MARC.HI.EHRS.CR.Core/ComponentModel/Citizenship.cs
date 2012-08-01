using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using MARC.HI.EHRS.SVC.Core.DataTypes;
using MARC.HI.EHRS.SVC.Core.ComponentModel.Components;

namespace MARC.HI.EHRS.CR.Core.ComponentModel
{
    /// <summary>
    /// Citizenship
    /// </summary>
    [Serializable]
    [XmlType("Citizenship", Namespace = "urn:marc-hi:ca/cr")]
    public class Citizenship
    {

        /// <summary>
        /// Gets the unique identifier for the citizenship
        /// </summary>
        [XmlAttribute("id")]
        public decimal Id { get; set; }

        /// <summary>
        /// Gets or sets the status of the citizenship
        /// </summary>
        [XmlAttribute("status")]
        public StatusType Status { get; set; }

        /// <summary>
        /// Gets or sets the effective time of the citizenship
        /// </summary>
        [XmlElement("efft")]
        public TimestampSet EffectiveTime { get; set; }

        /// <summary>
        /// Identifies the country of citizenship
        /// </summary>
        [XmlAttribute("code")]
        public String CountryCode { get; set; }

        /// <summary>
        /// Gets or sets the country name
        /// </summary>
        [XmlAttribute("name")]
        public String CountryName { get; set; }

    }
}
