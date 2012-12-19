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
 * Date: 7-8-2012
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using MARC.HI.EHRS.SVC.Core.DataTypes;
using MARC.HI.EHRS.SVC.Core.ComponentModel.Components;

namespace MARC.HI.EHRS.CR.Core.ComponentModel
{
    /// <summary>
    /// Citizenship
    /// </summary>
    [Serializable]
    [XmlType("Citizenship", Namespace = "urn:marc-hi:ca/cr")]
    public class Citizenship
    {

        /// <summary>
        /// Gets the unique identifier for the citizenship
        /// </summary>
        [XmlAttribute("id")]
        public decimal Id { get; set; }

        /// <summary>
        /// Gets or sets the status of the citizenship
        /// </summary>
        [XmlAttribute("status")]
        public StatusType Status { get; set; }

        /// <summary>
        /// Gets or sets the effective time of the citizenship
        /// </summary>
        [XmlElement("efft")]
        public TimestampSet EffectiveTime { get; set; }

        /// <summary>
        /// Identifies the country of citizenship
        /// </summary>
        [XmlAttribute("code")]
        public String CountryCode { get; set; }

        /// <summary>
        /// Gets or sets the country name
        /// </summary>
        [XmlAttribute("name")]
        public String CountryName { get; set; }

        /// <summary>
        /// Gets or sets the update mode
        /// </summary>
        [XmlAttribute("updateMode")]
        public UpdateModeType UpdateMode { get; set; }
    }
}
