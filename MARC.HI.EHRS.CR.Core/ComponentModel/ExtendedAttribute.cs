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
 * Date: 21-8-2012
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MARC.HI.EHRS.SVC.Core.ComponentModel;
using System.Xml.Serialization;
using System.ComponentModel;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

namespace MARC.HI.EHRS.CR.Core.ComponentModel
{

    /// <summary>
    /// Utility for searching
    /// </summary>
    public static class ExtendedAttributeExtensions
    {

        /// <summary>
        /// Find an extension
        /// </summary>
        public static ExtendedAttribute FindExtension(this HealthServiceRecordContainer me, Predicate<ExtendedAttribute> match)
        {
            foreach (var cmp in me.Components)
                if (cmp is ExtendedAttribute && match.Invoke(cmp as ExtendedAttribute))
                    return cmp as ExtendedAttribute;
            return null;
        }

        /// <summary>
        /// Find an extension
        /// </summary>
        public static IEnumerable<ExtendedAttribute> FindAllExtensions(this HealthServiceRecordContainer me, Predicate<ExtendedAttribute> match)
        {
            List<ExtendedAttribute> retr = new List<ExtendedAttribute>();
            foreach (var cmp in me.Components)
                if (cmp is ExtendedAttribute && match.Invoke(cmp as ExtendedAttribute))
                    retr.Add(cmp as ExtendedAttribute);
            return retr;
        }
    }

    /// <summary>
    /// Represents a component which is an extension to the existing data model
    /// </summary>
    [Serializable]
    [XmlType("Extension")]
    public class ExtendedAttribute : CrHealthServiceRecordContainer
    {

        /// <summary>
        /// The path within the parent container to which this extension applies
        /// </summary>
        [XmlAttribute("property")]
        public string PropertyPath { get; set; }

        /// <summary>
        /// The name of the extensin
        /// </summary>
        [XmlAttribute("key")]
        public string Name { get; set; }

        /// <summary>
        /// The value of the extension
        /// </summary>
        [XmlIgnore]
        public object Value { get; set; }

        /// <summary>
        /// Data of the value
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Browsable(false)]
        [XmlElement("data")]
        public byte[] ValueData
        {
            get
            {
                if (this.Value == null) return null;

                // Serialize
                BinaryFormatter bf = new BinaryFormatter();
                MemoryStream ms = new MemoryStream();
                try
                {
                    bf.Serialize(ms, this.Value);
                    byte[] retVal = new byte[ms.Length];
                    ms.Seek(0, SeekOrigin.Begin);
                    ms.Read(retVal, 0, (int)ms.Length);
                    return retVal;
                }
                finally
                {
                    ms.Dispose();
                }
            }
            set
            {
                if (value == null) return;

                // Deserialize
                BinaryFormatter bf = new BinaryFormatter();
                MemoryStream ms = new MemoryStream(value);
                try
                {
                    this.Value = bf.Deserialize(ms);
                }
                finally
                {
                    ms.Dispose();
                }
            }
        }
    }
}
