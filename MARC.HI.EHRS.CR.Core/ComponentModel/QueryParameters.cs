/**
 * Copyright 2012-2013 Mohawk College of Applied Arts and Technology
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
 * Date: 26-7-2012
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MARC.HI.EHRS.SVC.Core.ComponentModel;
using System.Xml.Serialization;

namespace MARC.HI.EHRS.CR.Core.ComponentModel
{
    /// <summary>
    /// Query parameters 
    /// </summary>
    [Serializable]
    [XmlType("QueryParameters")]
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

        /// <summary>
        /// When used in a return, indicates the confidence of the match
        /// </summary>
        public float Confidence { get; set; }
    }
}
