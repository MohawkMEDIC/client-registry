using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MARC.HI.EHRS.CR.Core.ComponentModel;

namespace MARC.HI.EHRS.CR.Messaging.PixPdqv2
{
    /// <summary>
    /// Query result data
    /// </summary>
    public struct QueryResultData
    {

        /// <summary>
        /// Identifies the first record number that is to be returned in the set
        /// </summary>
        public int StartRecordNumber { get; set; }

        /// <summary>
        /// Continuation pointer
        /// </summary>
        public string ContinuationPtr { get; set; }
        /// <summary>
        /// Gets or sets the identifier of the query the result set is for
        /// </summary>
        public string QueryTag { get; set; }
        /// <summary>
        /// Gets or sets the results for the query
        /// </summary>
        public RegistrationEvent[] Results { get; set; }
        /// <summary>
        /// Gets or sets the total results for the query
        /// </summary>
        public int TotalResults { get; set; }
        /// <summary>
        /// Empty result
        /// </summary>
        public static QueryResultData Empty = new QueryResultData()
        {
            Results = new RegistrationEvent[] { }
        };
    }
}
