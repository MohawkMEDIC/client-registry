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
using MARC.HI.EHRS.SVC.Core.Services;
using System.ComponentModel;
using MARC.HI.EHRS.SVC.Core.DataTypes;
using System.Data;
using MARC.HI.EHRS.SVC.Core;
using MARC.HI.EHRS.SVC.Core.Exceptions;
using MARC.HI.EHRS.SVC.Core.ComponentModel;
using MARC.HI.EHRS.SVC.Core.ComponentModel.Components;

namespace MARC.HI.EHRS.CR.Persistence.Data.ComponentPersister
{
    /// <summary>
    /// Identifies a class that persists components
    /// </summary>
    public interface IComponentPersister
    {

        /// <summary>
        /// Gets the component that this persister handles
        /// </summary>
        Type HandlesComponent { get; }

        /// <summary>
        /// Persist a component to the database
        /// </summary>
        VersionedDomainIdentifier Persist(IDbConnection conn, IDbTransaction tx, IComponent data, bool isUpdate);

        /// <summary>
        /// De-persist a component from the database
        /// </summary>
        /// <param name="conn">The connection to use for loading data</param>
        /// <param name="container">The container that this component will become a member of</param>
        /// <param name="identifier">The identifier of the component to de-persist</param>
        /// <param name="role">The role that the component is expected to take within the containerff</param>
        IComponent DePersist(IDbConnection conn, decimal identifier, IContainer container, HealthServiceRecordSiteRoleType? role, bool loadFast);

    }
}
