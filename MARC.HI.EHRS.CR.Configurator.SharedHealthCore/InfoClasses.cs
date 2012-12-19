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
 * Date: 5-12-2012
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.ComponentModel;

namespace MARC.HI.EHRS.SVC.Config.Messaging
{
    /// <summary>
    /// Wrapper for combo-box items for typeos
    /// </summary>
    public class TypeInfo
    {
        private string m_name = String.Empty;
        /// <summary>
        /// Gets or sets the type
        /// </summary>
        public Type Type { get; set; }
        /// <summary>
        /// Gets the name of the asm
        /// </summary>
        public string Name
        {
            get
            {
                if (Type != null && String.IsNullOrEmpty(m_name))
                {
                    object[] obj = Type.GetCustomAttributes(typeof(DescriptionAttribute), true);
                    if (obj.Length > 0)
                        m_name = (obj[0] as DescriptionAttribute).Description;
                }
                return m_name;
            }
        }
        /// <summary>
        /// Represent as a string
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return Name;
        }
    }


    /// <summary>
    /// Wrapper for combo-box items for assemblies
    /// </summary>
    public class AssemblyInfo
    {
        private string m_name = String.Empty;
        /// <summary>
        /// Gets or sets the asm
        /// </summary>
        public Assembly Assembly { get; set; }
        /// <summary>
        /// Gets the name of the asm
        /// </summary>
        public string Name
        {
            get
            {
                if (Assembly != null && String.IsNullOrEmpty(m_name))
                {
                    object[] obj = Assembly.GetCustomAttributes(typeof(AssemblyTitleAttribute), true);
                    m_name = (obj[0] as AssemblyTitleAttribute).Title;
                }
                return m_name;
            }
        }
        /// <summary>
        /// Represent as a string
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return Name;
        }
    }
}
