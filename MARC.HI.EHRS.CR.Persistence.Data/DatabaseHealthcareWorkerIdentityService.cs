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
 * Date: 19-7-2012
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MARC.HI.EHRS.SVC.HealthWorkerIdentity;
using MARC.HI.EHRS.CR.Persistence.Data.ComponentPersister;
using System.Data;

namespace MARC.HI.EHRS.CR.Persistence.Data
{
    
    /// <summary>
    /// Just a quick implementation of a healthcare worker identity service that reads from the SHR database.
    /// </summary>
    public class DatabaseHealthcareWorkerIdentityService : IHealthcareWorkerIdentityService
    {
        #region IHealthcareWorkerIdentityService Members

        /// <summary>
        /// Find a participant based on an external identifier
        /// </summary>
        /// <param name="identifier"></param>
        /// <returns></returns>
        public SVC.Core.ComponentModel.Components.HealthcareParticipant FindParticipant(SVC.Core.DataTypes.DomainIdentifier identifier)
        {
            HealthcareParticipantPersister persister = new HealthcareParticipantPersister();
            // HACK: I norder to work around Client Registry Hack
            ApplicationContext.CurrentContext = Context;
            // First we want to find the appropriate helper
            IDbConnection conn = DatabasePersistenceService.ReadOnlyConnectionManager.GetConnection();
            try
            {
                return persister.GetProvider(conn, null, identifier);
            }
            finally
            {
                DatabasePersistenceService.ConnectionManager.ReleaseConnection(conn);
            }
        }

        #endregion

        #region IUsesHostContext Members

        /// <summary>
        /// Gets or sets the context within which this service operates
        /// </summary>
        public SVC.Core.HostContext Context { get; set;  }
        #endregion
    }
}
