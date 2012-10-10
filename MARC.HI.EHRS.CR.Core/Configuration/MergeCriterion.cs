using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MARC.HI.EHRS.CR.Core.Configuration
{
    /// <summary>
    /// Represents merge criteria
    /// </summary>
    public class MergeCriterion
    {

        /// <summary>
        /// Creates a new instance of the merge criteria
        /// </summary>
        public MergeCriterion(string fieldName)
        {
            this.FieldName = fieldName;
            this.MergeCriteria = new List<MergeCriterion>();
        }

        /// <summary>
        /// The field that should match
        /// </summary>
        public string FieldName { get; private set; }

        /// <summary>
        /// Represents merge criteria
        /// </summary>
        public List<MergeCriterion> MergeCriteria { get; private set; }
    }
}
