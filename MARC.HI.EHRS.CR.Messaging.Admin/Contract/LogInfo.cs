using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace MARC.HI.EHRS.CR.Messaging.Admin.Contract
{
    /// <summary>
    /// Log information
    /// </summary>
    [XmlRoot("LogInfo")]
    [XmlType("LogInfo")]
    public class LogInfo
    {

        /// <summary>
        /// Log ID
        /// </summary>
        [XmlAttribute("id")]
        public String Id { get; set; }

        /// <summary>
        /// Size of the file
        /// </summary>
        [XmlElement("size")]
        public long Size { get; set; }

        /// <summary>
        /// Last modified
        /// </summary>
        [XmlElement("lastModified")]
        public DateTime LastModified { get; set; }
    }
}
