using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MARC.HI.EHRS.SVC.Core.ComponentModel;

namespace MARC.HI.EHRS.CR.Core.ComponentModel
{
    /// <summary>
    /// Query parameters 
    /// </summary>
    public class QueryParameters : HealthServiceRecordComponent
    {

        /// <summary>
        /// Constructs a new query parameter object
        /// </summary>
        public QueryParameters()
        {
            this.MatchingAlgorithm = MatchAlgorithm.Default;
            this.MatchStrength = MatchStrength.Exact;
        }

        /// <summary>
        /// Gets or sets the desired matching algorithm
        /// </summary>
        public MatchAlgorithm MatchingAlgorithm { get; set; }

        /// <summary>
        /// Desired match strength
        /// </summary>
        public MatchStrength MatchStrength { get; set; }
    }
}
