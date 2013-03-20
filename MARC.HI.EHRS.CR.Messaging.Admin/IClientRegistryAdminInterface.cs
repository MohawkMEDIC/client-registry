/**
 * Copyright 2013-2013 Mohawk College of Applied Arts and Technology
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
 * Date: 25-2-2013
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel;
using System.ServiceModel.Syndication;
using System.ServiceModel.Web;
using MARC.HI.EHRS.CR.Core.ComponentModel;

namespace MARC.HI.EHRS.CR.Messaging.Admin
{
    /// <summary>
    /// Client registry interface contract
    /// </summary>
    [ServiceContract(ConfigurationName = "CrAdmin")]
    [XmlSerializerFormat]
    public interface IClientRegistryAdminInterface
    {

        /// <summary>
        /// Get all registrations in the system matching a key
        /// </summary>
        [OperationContract(Action = "GetRegistrations")]
        RegistrationEventCollection GetRegistrations(Person queryPrototype);

        /// <summary>
        /// Get registration event
        /// </summary>
        [OperationContract(Action = "GetRegistration")]
        RegistrationEvent GetRegistrationEvent(decimal id);

        /// <summary>
        /// Get conflicts 
        /// </summary>
        [OperationContract(Action = "GetConflicts")]
        ConflictCollection GetConflicts();

        /// <summary>
        /// Get conflicts 
        /// </summary>
        [OperationContract(Action = "GetConflict")]
        ConflictCollection GetConflict(decimal id);

        /// <summary>
        /// Merge registration events
        /// </summary>
        [OperationContract(Action = "Merge")]
        RegistrationEvent Merge(decimal[] sourceIds, decimal targetId);

        /// <summary>
        /// Source id
        /// </summary>
        [OperationContract(Action = "Resolve")]
        void Resolve(decimal sourceId);

    }
}
