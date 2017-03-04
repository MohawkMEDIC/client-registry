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
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace MEDIC.Empi.Client
{
    /// <summary>
    /// Patient
    /// </summary>
    [Guid("0671117F-3922-4E83-A573-4810C7CA989A")]
    [ComVisible(true)]
    [ClassInterface(ClassInterfaceType.None)]
    public class Patient : IPatient
    {

        /// <summary>
        /// Patient
        /// </summary>
        public Patient()
        {
            this.MotherIdentifiers = new IdentifierTypeCollection();
            this.Identifiers = new IdentifierTypeCollection();
            this.Names = new PatientNameCollection();
            this.MotherName = new PatientName();
            this.Addresses = new PatientAddressCollection();
        }

        /// <summary>
        /// Surnames
        /// </summary>
        public PatientNameCollection Names {
            get;
            set;
        }

        /// <summary>
        /// Gender code
        /// </summary>
        public GenderType Gender { get; set; }

        /// <summary>
        /// Date of birth
        /// </summary>
        public DateTime DateOfBirth { get; set; }

        /// <summary>
        /// Mother's given name
        /// </summary>
        public PatientName MotherName { get; set; }

        /// <summary>
        /// Identifiers
        /// </summary>
        public IdentifierTypeCollection Identifiers { get; set; }

        /// <summary>
        /// Gets or sets the mothers identifiers
        /// </summary>
        public IdentifierTypeCollection MotherIdentifiers { get; set; }

        /// <summary>
        /// Gets or sets the telephone number
        /// </summary>
        public String Telephone { get; set; }

        /// <summary>
        /// Gets or sets the addresses
        /// </summary>
        public PatientAddressCollection Addresses { get; set; }

        /// <summary>
        /// Deceased date
        /// </summary>
        public DateTime DeceasedDate { get; set; }

        /// <summary>
        /// Multiple birth indicator
        /// </summary>
        public bool MultipleBirthIndicator { get; set; }
    }
}
