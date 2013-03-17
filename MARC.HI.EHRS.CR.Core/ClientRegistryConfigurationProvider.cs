/**
 * Copyright 2013-2013 Mohawk College of Applied Arts and Technology
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
 * Date: 6-2-2013
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MARC.HI.EHRS.CR.Core.Services;
using MARC.HI.EHRS.CR.Core.Configuration;
using System.Configuration;
using MARC.HI.EHRS.CR.Core.ComponentModel;
using System.Collections;

namespace MARC.HI.EHRS.CR.Core
{
    /// <summary>
    /// Represents a client registry configuration provider
    /// </summary>
    public class ClientRegistryConfigurationProvider : IClientRegistryConfigurationService
    {

        // Sync object
        private static Object s_lockObject = new object();

        // Configuration
        private static ClientRegistryConfiguration s_configuration = null;
 
        #region IClientRegistryConfigurationService Members

        /// <summary>
        /// Client registry configuration
        /// </summary>
        public Configuration.ClientRegistryConfiguration Configuration
        {
            get 
            {  
                if(s_configuration == null)
                    lock (s_lockObject)
                    {
                        if (s_configuration == null)
                            s_configuration = ConfigurationManager.GetSection("marc.hi.ehrs.cr") as ClientRegistryConfiguration;
                    }
                return s_configuration;
            }
        }

        /// <summary>
        /// Create a merge filter
        /// </summary>
        public ComponentModel.Person CreateMergeFilter(ComponentModel.Person p)
        {
           
            Person retVal = new Person();
            
            // Sanity
            //if (!this.Configuration.Registration.AutoMerge)
            //    return null;

            int nCriteria = this.CopyCriterionPropertyValues(this.Configuration.Registration.MergeCriteria, retVal, p, false);

            if(nCriteria < this.Configuration.Registration.MinimumMergeMatchCriteria)
                return null;
            return retVal;           
        }

        /// <summary>
        /// Copy the criterion values
        /// </summary>
        private int CopyCriterionPropertyValues(List<MergeCriterion> criteria, Person filter, Person original, bool firstOnly)
        {
            // Merge copy
            int nCriteria = 0;
            foreach (var crit in criteria)
            {

                if (!String.IsNullOrEmpty(crit.FieldName))
                {
                    var propertyInfo = typeof(Person).GetProperty(crit.FieldName);
                    if (propertyInfo == null)
                        throw new InvalidOperationException(String.Format("Cannot get property '{0}'", crit.FieldName));

                    // Copy properties
                    var otherInstance = propertyInfo.GetValue(original, null);
                    propertyInfo.SetValue(filter, otherInstance, null);

                    if ((otherInstance != null) ^ (otherInstance is ICollection && (otherInstance as ICollection).Count == 0))
                        nCriteria++;
                    if(firstOnly && ((otherInstance != null) ^ (otherInstance is ICollection && (otherInstance as ICollection).Count == 0)))
                        return nCriteria;
                }
                else
                {
                    nCriteria += this.CopyCriterionPropertyValues(crit.MergeCriteria, filter, original, true);
                    if (firstOnly && nCriteria > 0)
                        return nCriteria;
                }

            }

            return nCriteria;
        }

        #endregion
    }
}
