using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MARC.HI.EHRS.CR.Core.ComponentModel
{
    /// <summary>
    /// Matching strength
    /// </summary>
    public enum MatchStrength
    {
        /// <summary>
        /// 100% String Match on name component
        /// </summary>
        Exact,
        /// <summary>
        /// 100% Sounds like match on name component (when soundex is used)
        /// </summary>
        Strong,
        /// <summary>
        /// 75% sounds like match on name component
        /// </summary>
        Moderate,
        /// <summary>
        /// 50% sounds like match on name component
        /// </summary>
        Weak
    }
}
