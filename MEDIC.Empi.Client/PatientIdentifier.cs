/**
 * Copyright 2012-2017 Mohawk College of Applied Arts and Technology
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
 * User: justi
 * Date: 3-3-2017
 */
using MEDIC.Empi.Client.Interop;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace MEDIC.Empi.Client
{
    /// <summary>
    /// Identifier type
    /// </summary>
    [Guid("78BCE323-3849-2039-A573-4810C7CA989A")]
    [ComVisible(true)]
    [ClassInterface(ClassInterfaceType.None)]
    public class PatientIdentifier : IPatientIdentifier
    {

        /// <summary>
        /// New identifier type
        /// </summary>
        public PatientIdentifier()
        {
            
        }

        /// <summary>
        /// Create new identifier type
        /// </summary>
        public PatientIdentifier(string domain, string value)
        {
            this.Domain = domain;
            this.Value = value;
        }

        /// <summary>
        /// Domain name
        /// </summary>
        public string Domain { get; set; }

        /// <summary>
        /// Value name
        /// </summary>
        public string Value { get; set; }

    }

    /// <summary>
    /// Identifier type collection
    /// </summary>
    [Guid("78BCE323-3922-4E83-A573-4810C7CA989A")]
    [ComVisible(true)]
    [ClassInterface(ClassInterfaceType.None)]
    public class IdentifierTypeCollection : List<PatientIdentifier>, IIdentifierCollection
    {

        /// <summary>
        /// Add new identifier type
        /// </summary>
        public void Add (string domain, string value)
        {
            this.Add(new PatientIdentifier(domain, value));
        }

        /// <summary>
        /// Find the specified value
        /// </summary>
        public PatientIdentifier Find(string domainName)
        {
            return this.Find(o => o.Domain == domainName);
        }
    }
}