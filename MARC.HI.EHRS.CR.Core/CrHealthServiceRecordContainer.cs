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
        /// <summary>
        /// Component entries for the Xml Serializer
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Browsable(false)]
        [XmlElement("client", typeof(Person))]
        [XmlElement("healthcareParticipant", typeof(HealthcareParticipant))]
        [XmlElement("changeSummary", typeof(ChangeSummary))]
        [XmlElement("healthServiceRecordComponentRef", typeof(HealthServiceRecordComponentRef))]
        [XmlElement("mask", typeof(MaskingIndicator))]
        [XmlElement("personalRelationship", typeof(PersonalRelationship))]
        [XmlElement("queryRestriction", typeof(QueryRestriction))]
        [XmlElement("serviceDeliveryLocation", typeof(ServiceDeliveryLocation))]
        [XmlElement("extension", typeof(ExtendedAttribute))]
        public override List<HealthServiceRecordComponent> XmlComponents
        {
            get
            {
                return base.XmlComponents;
            }
            set
            {
                base.XmlComponents = value;
            }
        }

    }
}
