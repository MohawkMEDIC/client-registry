using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MARC.HI.EHRS.SVC.Core.ComponentModel;
using MARC.HI.EHRS.SVC.Core.DataTypes;
using System.Xml.Serialization;

namespace MARC.HI.EHRS.CR.Core.ComponentModel
{
    /// <summary>
    /// Repository device
    /// </summary>
    [Serializable][XmlType("RepositoryDevice", Namespace = "urn:marc-hi:ca/cr")]
    public class RepositoryDevice : HealthServiceRecordComponent
    {

        /// <summary>
        /// Gets or sets the domain identifier for the device
        /// </summary>
        [XmlElement("altId")]
        public DomainIdentifier AlternateIdentifier { get; set; }

        /// <summary>
        /// Gets or sets the name of the repository device
        /// </summary>
        [XmlAttribute("name")]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the jurisdiction name
        /// </summary>
        [XmlAttribute("jurisdiction")]
        public string Jurisdiction { get; set; }
    }
}
