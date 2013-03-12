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
 * Date: 16-7-2012
 */

using System.Xml.Serialization;
using MARC.HI.EHRS.SVC.Core.ComponentModel;
using MARC.HI.EHRS.SVC.Core.ComponentModel.Components;
using MARC.HI.EHRS.SVC.Core.DataTypes;
using System;

namespace MARC.HI.EHRS.CR.Core.ComponentModel
{
    /// <summary>
    /// Indicates a reason for performing a task
    /// </summary>
    [Serializable][XmlType("Reason")]
    public class Reason : HealthServiceRecordComponent
    {
        /// <summary>
        /// Identifies the type of reason
        /// </summary>
        [XmlElement("reason")]
        public CodeValue ReasonType { get; set; }
        /// <summary>
        /// Identifies the status of the reason
        /// </summary>
        [XmlElement("status")]
        public StatusType Status { get; set; }
        /// <summary>
        /// Identifies the textual description of the reason
        /// </summary>
        [XmlElement("text")]
        public string Text { get; set; }
        /// <summary>
        /// Identifies the value of the reason
        /// </summary>
        [XmlElement("value")]
        public CodeValue Value { get; set; }
    }
}
