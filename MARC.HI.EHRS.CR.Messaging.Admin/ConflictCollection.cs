using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace MARC.HI.EHRS.CR.Messaging.Admin
{
    /// <summary>
    /// Conflict collection
    /// </summary>
    [XmlType("ConflictCollection", Namespace = "urn:marc-hi:svc:componentModel")]
    [XmlRoot("conflictCollection", Namespace = "urn:marc-hi:svc:componentModel")]
    public class ConflictCollection
    {

        /// <summary>
        /// Creates a new instance of the conflict collection
        /// </summary>
        public ConflictCollection()
        {
            this.Conflict = new List<Conflict>();
        }

        /// <summary>
        /// Count of results
        /// </summary>
        [XmlAttribute("count")]
        public int Count { get; set; }
        /// <summary>
        /// Conflict items
        /// </summary>
        [XmlElement("conflict")]
        public List<Conflict> Conflict { get; set; }

    }
}
