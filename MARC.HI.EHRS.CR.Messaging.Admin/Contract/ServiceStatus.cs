using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace MARC.HI.EHRS.CR.Messaging.Admin.Contract
{
    /// <summary>
    /// Service statuses
    /// </summary>
    [XmlRoot("ServiceStatus")]
    [XmlType("ServiceStatus")]
    public class ServiceStatus
    {

        /// <summary>
        /// Service Class
        /// </summary>
        [XmlAttribute("contract")]
        public String Contract { get; set; }

        /// <summary>
        /// Service name
        /// </summary>
        [XmlAttribute("class")]
        public String Class { get; set; }

        /// <summary>
        /// Version
        /// </summary>
        [XmlAttribute("version")]
        public String Version { get; set; }

    }
}
