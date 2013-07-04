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
 * Date: 19-7-2012
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using MARC.HI.EHRS.SVC.Core.DataTypes;

namespace MARC.HI.EHRS.CR.Core.ComponentModel
{
    /// <summary>
    /// Language type
    /// </summary>
    public enum LanguageType
    {
        Written = 1,
        Spoken = 2,
        Preferred = 4,
        Fluency = 8,
        WrittenAndSpoken = Written | Spoken
    }

    /// <summary>
    /// Identifies a language component
    /// </summary>
    [Serializable]
    [XmlType("PersonLanguage")]
    public class PersonLanguage
    {

        /// <summary>
        /// Gets or sets the update mode type
        /// </summary>
        [XmlAttribute("updateMode")]
        public UpdateModeType UpdateMode { get; set; }

        /// <summary>
        /// The type of language
        /// </summary>
        [XmlAttribute("type")]
        public LanguageType Type { get; set; }

        /// <summary>
        /// Identifies ISO639-1 code 
        /// </summary>
        public String Language { get; set; }
    }
}
