using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MARC.HI.EHRS.CR.Core.Configuration
{


    /// <summary>
    /// Represents registration configuration information
    /// </summary>
    public class RegistrationConfiguration
    {

        /// <summary>
        /// Constructs a new registration configuration
        /// </summary>
        public RegistrationConfiguration()
        {
            this.MergeCriteria = new List<MergeCriterion>();
        }

        /// <summary>
        /// Gets the behavior of registration
        /// </summary>
        public bool AutoMerge { get; internal set; }

        /// <summary>
        /// Update if the client exists
        /// </summary>
        public bool UpdateIfExists { get; internal set; }

        /// <summary>
        /// Represents minimum match criteria
        /// </summary>
        public int MinimumMergeMatchCriteria { get; internal set; }

        /// <summary>
        /// Represents match criteria for new records to be merged
        /// </summary>
        public List<MergeCriterion> MergeCriteria { get; private set; }

    }
}
