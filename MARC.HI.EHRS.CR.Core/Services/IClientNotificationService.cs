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
using MARC.HI.EHRS.SVC.Core.Services;
using MARC.HI.EHRS.CR.Core.ComponentModel;
using MARC.HI.EHRS.SVC.Core.DataTypes;

namespace MARC.HI.EHRS.CR.Core.Services
{
    /// <summary>
    /// Client notification service
    /// </summary>
    public interface IClientNotificationService : IUsesHostContext
    {

        /// <summary>
        /// Notify that an update has occurred
        /// </summary>
        /// <remarks>Contains an older version of the record that was updated</remarks>
        void NotifyUpdate(RegistrationEvent evt);

        /// <summary>
        /// Notify that a registration has occurred
        /// </summary>
        /// <remarks>Contains no older version of the registration</remarks>
        void NotifyRegister(RegistrationEvent evt);

        /// <summary>
        /// Notify that a reconcilation is required
        /// </summary>
        void NotifyReconciliationRequired(IEnumerable<VersionedDomainIdentifier> candidates);

        /// <summary>
        /// Notification that duplicates have been resolved
        /// </summary>
        void NotifyDuplicatesResolved(RegistrationEvent evt);
    }
}
