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
    [XmlType("ConflictCollection")]
    [XmlRoot("conflictCollection")]
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
        /// Conflict items
        /// </summary>
        [XmlElement("conflict")]
        public List<Conflict> Conflict { get; set; }

    }
}
