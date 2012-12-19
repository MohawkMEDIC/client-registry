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
using MARC.HI.EHRS.SVC.Core.DataTypes;
using System.Xml.Serialization;

namespace MARC.HI.EHRS.CR.Core.ComponentModel
{
    /// <summary>
    /// Repository device
    /// </summary>
    [Serializable][XmlType("RepositoryDevice", Namespace = "urn:marc-hi:ca/cr")]
    public class RepositoryDevice : HealthServiceRecordComponent
    {

        /// <summary>
        /// Gets or sets the domain identifier for the device
        /// </summary>
        [XmlElement("altId")]
        public DomainIdentifier AlternateIdentifier { get; set; }

        /// <summary>
        /// Gets or sets the name of the repository device
        /// </summary>
        [XmlAttribute("name")]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the jurisdiction name
        /// </summary>
        [XmlAttribute("jurisdiction")]
        public string Jurisdiction { get; set; }
    }
}
