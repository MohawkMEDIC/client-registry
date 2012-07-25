/* 
 * Copyright 2008-2011 Mohawk College of Applied Arts and Technology
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
 * User: Justin Fyfe
 * Date: 08-24-2011
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MARC.HI.EHRS.CR.Core.ComponentModel;

namespace MARC.HI.EHRS.CR.Persistence.Data.Configuration
{
    /// <summary>
    /// Validation section for configuration
    /// </summary>
    public class ValidationSection
    {

        /// <summary>
        /// If true, allow duplicate records
        /// </summary>
        public bool AllowDuplicateRecords { get; set; }

        /// <summary>
        /// Default matching strength
        /// </summary>
        public MatchStrength DefaultMatchStrength { get; set; }

        /// <summary>
        /// Default matching algorithm. Query parmaeters override this
        /// </summary>
        public MatchAlgorithm DefaultMatchAlgorithms { get; set; }

        /// <summary>
        /// When true, instructs the CR to first try to make an exact match prior to 
        /// making other matches. As always, query parameters from messages always 
        /// override this
        /// </summary>
        public bool ExactMatchFirst { get; set; }

        /// <summary>
        /// When true, persons (such a providers) must exist before being recorded
        /// </summary>
        public bool PersonsMustExist { get; set; }

        /// <summary>
        /// When true, healthcare participants must be validated
        /// </summary>
        public bool ValidateHealthcareParticipants { get; set; }

        /// <summary>
        /// Minimum degree of name matches when VALIDATING persons
        /// </summary>
        public float PersonNameMatch { get; set; }
    }
}
