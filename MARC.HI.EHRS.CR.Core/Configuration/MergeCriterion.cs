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
