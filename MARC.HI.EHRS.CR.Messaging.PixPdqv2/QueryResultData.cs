/**
 * Copyright 2012-2012 Mohawk College of Applied Arts and Technology
 * 
 * Licensed under the Apache License, Version 2.0 (the "License"); you 
 * may not use this file except in compliance with the License. You may 
 * obtain a copy of the License at 
 * 
 * http://www.apache.org/licenses/LICENSE-2.0 
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS, WITHOUT
 * WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the 
 * License for the specific language governing permissions and limitations under 
 * the License.
 * 
 * User: fyfej
 * Date: 16-8-2012
 */
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
