/* 
 * Copyright 2008-2011 Mohawk College of Applied Arts and Technology
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
 * User: Justin Fyfe
 * Date: 08-24-2011
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MARC.HI.EHRS.CR.Persistence.Data.Configuration
{
    /// <summary>
    /// Validation section for configuration
    /// </summary>
    public class ValidationSection
    {

        /// <summary>
        /// Gets the percent match that a client's name must be in order to insert the record
        /// </summary>
        public float PersonNameMatch { get; internal set; }

        /// <summary>
        /// If true, allow duplicate records
        /// </summary>
        public bool AllowDuplicateRecords { get; set; }

        /// <summary>
        /// Gets or sets whether the client MUST exist to continue the persistence of a record
        /// </summary>
        public bool PersonsMustExist { get; set; }

        /// <summary>
        /// Gets or sets whether healthcare participants must be validated
        /// with the provider registry prior to having a new canonical SHR 
        /// record persisted.
        /// </summary>
        public bool ValidateHealthcareParticipants { get; set; }

        /// <summary>
        /// Gets or sets whether clients must be validated with the client 
        /// registry prior to having a new canonical SHR record persisted
        /// </summary>
        public bool ValidateClients { get; set; }

    }
}
