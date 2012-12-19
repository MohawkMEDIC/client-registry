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
 * Date: 17-10-2012
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;
using System.Xml;

namespace MARC.HI.EHRS.CR.Core.Configuration
{
    /// <summary>
    /// Configuration section handler
    /// </summary>
    public class ClientRegistryConfigurationSectionHandler : IConfigurationSectionHandler
    {
        #region IConfigurationSectionHandler Members

        /// <summary>
        /// Create the configuration object
        /// </summary>
        public object Create(object parent, object configContext, System.Xml.XmlNode section)
        {

            ClientRegistryConfiguration retVal = new ClientRegistryConfiguration();

            // Registration section
            XmlElement registrationSection = section.SelectSingleNode("./*[local-name() = 'registration']") as XmlElement;
            if (registrationSection != null)
            {
                // core attributes
                retVal.Registration = new RegistrationConfiguration();
                if (registrationSection.Attributes["autoMerge"] != null)
                    retVal.Registration.AutoMerge = Convert.ToBoolean(registrationSection.Attributes["autoMerge"].Value);
                if (registrationSection.Attributes["updateIfExists"] != null)
                    retVal.Registration.UpdateIfExists = Convert.ToBoolean(registrationSection.Attributes["updateIfExists"].Value);
                if(registrationSection.Attributes["minimumAutoMergeMatchCriteria"] != null)
                    retVal.Registration.MinimumMergeMatchCriteria = Convert.ToInt32(registrationSection.Attributes["minimumAutoMergeMatchCriteria"].Value);
                else if(retVal.Registration.AutoMerge)
                    throw new ConfigurationErrorsException("'minimumAutoMergeMatchCriteria' must be specified when autoMerge is enabled");

                // Process match criteria
                this.ProcessMatchCriteriaElements(registrationSection.SelectNodes("./*[local-name() = 'mergeCriterion']"), retVal.Registration.MergeCriteria);

            }

            return retVal;
        }

        /// <summary>
        /// Process match criteria elements
        /// </summary>
        private void ProcessMatchCriteriaElements(XmlNodeList xmlNodeList, List<MergeCriterion> criteriaList)
        {
            foreach (XmlElement ele in xmlNodeList)
            {
                String fieldName = null;
                if (ele.Attributes["field"] != null)
                    fieldName = ele.Attributes["field"].Value;
                MergeCriterion criterion = new MergeCriterion(fieldName);

                // Recurse?
                this.ProcessMatchCriteriaElements(ele.SelectNodes("./*[local-name() = 'mergeCriterion']"), criterion.MergeCriteria);
                criteriaList.Add(criterion);
            }
        }

        #endregion
    }
}
