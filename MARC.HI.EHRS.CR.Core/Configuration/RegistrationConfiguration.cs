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
 * Date: 17-10-2012
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MARC.HI.EHRS.SVC.Core.DataTypes;

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
            OidRegistrar.ExtendedAttributes.Add("IsUniqueIdentifier", typeof(Boolean));
            OidRegistrar.ExtendedAttributes.Add("GloballyAssignable", typeof(bool));
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

        /// <summary>
        /// True if the patients must a registered identiifer
        /// </summary>
        public bool MustHaveRegisteredIdentifier { get; internal set; }
    }
}
