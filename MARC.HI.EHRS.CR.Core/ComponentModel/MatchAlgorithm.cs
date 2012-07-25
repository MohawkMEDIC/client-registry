using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MARC.HI.EHRS.CR.Core.ComponentModel
{
    /// <summary>
    /// Confidence type
    /// </summary>
    public enum MatchAlgorithm
    {
        /// <summary>
        /// Not specified
        /// </summary>
        Unspecified = 0,
        /// <summary>
        /// Names must exactly match one another
        /// </summary>
        Exact = 0x1,
        /// <summary>
        /// Match based on "Sounds Like"
        /// </summary>
        Soundex = 0x2,
        /// <summary>
        /// Match on variants
        /// </summary>
        Variant = 0x4,
        /// <summary>
        /// Default match
        /// </summary>
        Default = Exact | Soundex
    }
}
