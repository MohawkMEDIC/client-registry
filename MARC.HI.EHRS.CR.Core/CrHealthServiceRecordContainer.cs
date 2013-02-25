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
 * Date: 16-7-2012
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MARC.HI.EHRS.SVC.Core.ComponentModel;
using System.Xml.Serialization;
using System.ComponentModel;
using MARC.HI.EHRS.CR.Core.ComponentModel;
using MARC.HI.EHRS.SVC.Core.ComponentModel.Components;

namespace MARC.HI.EHRS.CR.Core
{
    /// <summary>
    /// Represents a registration event
    /// </summary>
    [Serializable]
    [XmlType("CrHealthServiceRecordContainer")]
    public class CrHealthServiceRecordContainer : HealthServiceRecordContainer
    {

        // Supported components
        private readonly Type[] m_supportedTypes = new Type[] {
            typeof(Person),
            typeof(ExtendedAttribute),
            typeof(PersonRegistrationRef),
            typeof(QueryParameters)
        };

        /// <summary>
        /// Component entries for the Xml Serializer
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Browsable(false)]
        [XmlElement("extension", typeof(ExtendedAttribute))]
        [XmlElement("person", typeof(Person))]
        [XmlElement("personRegRef", typeof(PersonRegistrationRef))]
        [XmlElement("crQueryParameters", typeof(QueryParameters))]
        public List<HealthServiceRecordComponent> CrSpecificComponents
        {
            get
            {
                var retVal = new List<HealthServiceRecordComponent>(this.Components.Count);
                foreach (var mc in this.Components)
                    if (mc is HealthServiceRecordComponent &&
                        Array.Exists(m_supportedTypes, o => o.Equals(mc.GetType())))
                        retVal.Add(mc as HealthServiceRecordComponent);
                return retVal;
            }
            set
            {
                if (this.m_components == null)
                    this.m_components = new List<IComponent>(value.Count);
                foreach (var mv in value)
                {
                    (mv.Site as HealthServiceRecordSite).Container = this;
                    (mv.Site as HealthServiceRecordSite).Component = mv;
                    (mv.Site as HealthServiceRecordSite).Context = this.Context;
                    this.m_components.Add(mv);
                }
            }
        }

    }
}
