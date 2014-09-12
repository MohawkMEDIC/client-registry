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
 * Date: 1-8-2012
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MARC.HI.EHRS.CR.Persistence.Data
{
    /// <summary>
    /// A class that represents a collection of OID names
    /// </summary>
    internal static class ClientRegistryOids
    {
        /// <summary>
        /// Device OID
        /// </summary>
        internal const string DEVICE_CRID = "DEV_CRID";
        /// <summary>
        /// OID for a registration event
        /// </summary>
        internal const string REGISTRATION_EVENT = "REG_EVT";
        /// <summary>
        /// OID for a client CRID
        /// </summary>
        internal const string CLIENT_CRID = "CR_CID";
        /// <summary>
        /// OID for client version identifier
        /// </summary>
        internal const string CLIENT_VERSION_CRID = "CR_CID_VRSN";
        /// <summary>
        /// OID for a provider CRID
        /// </summary>
        internal const string PROVIDER_CRID = "CR_PID";
        /// <summary>
        /// OID for a location CRID
        /// </summary>
        internal const string LOCATION_CRID = "CR_LID";
        /// <summary>
        /// OID for a health service event version
        /// </summary>
        internal const string REGISTRATION_EVENT_VERSION = "CR_REG_VRSN_ID";
        /// <summary>
        /// OID for events and change summaries
        /// </summary>
        internal const string EVENT_OID = "EVT_ID";
        /// <summary>
        /// Relationship ID
        /// </summary>
        internal const string RELATIONSHIP_OID = "CR_PRID";
    }
}
