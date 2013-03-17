using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MARC.HI.EHRS.SVC.Core.DataTypes;
using System.Xml.Serialization;
using MARC.HI.EHRS.CR.Core.ComponentModel;

namespace MARC.HI.EHRS.CR.Messaging.Admin
{
    /// <summary>
    /// Conflicted version identifier
    /// </summary>
    [XmlType("ConflictedVersionIdentifier")]
    public class Conflict
    {

        /// <summary>
        /// Creates a new instance of the conflicted version identifier
        /// </summary>
        public Conflict()
        {
            this.Match = new List<RegistrationEvent>();
        }

        /// <summary>
        /// Identifier of the record in conflict
        /// </summary>
        [XmlElement("source")]
        public RegistrationEvent Source { get; set; }

        /// <summary>
        /// Gets the list of matches
        /// </summary>
        [XmlElement("matches")]
        public List<RegistrationEvent> Match { get; set; }
    }
}
