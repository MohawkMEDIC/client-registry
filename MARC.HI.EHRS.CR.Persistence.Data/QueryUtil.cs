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
using MARC.HI.EHRS.SVC.Core.DataTypes;

namespace MARC.HI.EHRS.CR.Persistence.Data
{
    /// <summary>
    /// Query utility class
    /// </summary>
    internal static class QueryUtil
    {

        /// <summary>
        /// Matches the two names and gives a percentage probability that name
        /// A matches name B
        /// </summary>
        internal static float MatchName(NameSet a, NameSet b)
        {
            return a.SimilarityTo(b);
        }

        /// <summary>
        /// Match address
        /// </summary>
        internal static float MatchAddress(AddressSet a, AddressSet b)
        {
            if (a == null || b == null && a != b)
                return 0.0f;

            int nMatched = 0;
            foreach (var part in a.Parts)
                nMatched += b.Parts.Count(o => o.PartType == part.PartType && o.AddressValue == part.AddressValue);
            return (float)nMatched / a.Parts.Count;
        }
    }
}
