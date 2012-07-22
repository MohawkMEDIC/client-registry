using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MARC.HI.EHRS.SVC.Core.DataTypes;

namespace MARC.HI.EHRS.CR.Core.ComponentModel
{
    /// <summary>
    /// Represents a versioned domain identifier that occurred as a result of a query
    /// </summary>
    public class VersionedResultIdentifier : VersionedDomainIdentifier
    {

        /// <summary>
        /// Confidence of the result matching the result
        /// </summary>
        public float Confidence { get; set; }
    }
}
