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
using System.ComponentModel;
using System.Data;
using MARC.HI.EHRS.CR.Persistence.Data.ComponentPersister;
using MARC.HI.EHRS.SVC.ClientIdentity;
using MARC.HI.EHRS.SVC.Core.ComponentModel.Components;
using MARC.HI.EHRS.SVC.Core.DataTypes;

namespace MARC.HI.EHRS.CR.Persistence.Data
{
    /// <summary>
    /// Implements an IClientLookupService that uses the shared health 
    /// record client information as a source for its information. This should
    /// only be used in scenarios whereby the shared health record is configured 
    /// to receive patient add/update notifications from the client registry.
    /// </summary>
    [Description("ADO.NET Client Lookup Service")] 
    public class DatabaseClientLookupService : IClientIdentityService
    {
        #region IClientLookupService Members

        /// <summary>
        /// Find a client
        /// </summary>
        public Client FindClient(DomainIdentifier identifier)
        {
            PersonPersister persister = new PersonPersister();
            // HACK: I norder to work around Client Registry Hack
            ApplicationContext.CurrentContext = Context;
            // First we want to find the appropriate helper
            IDbConnection conn = DatabasePersistenceService.ReadOnlyConnectionManager.GetConnection();
            try
            {
                var tPerson = persister.GetPerson(conn, null, identifier, true);
                Client retVal = new Client()
                {
                    AlternateIdentifiers = tPerson.AlternateIdentifiers,
                    BirthTime = tPerson.BirthTime,
                    GenderCode = tPerson.GenderCode,
                    Id = tPerson.Id,
                    IsMasked = tPerson.IsMasked,
                    LegalName = tPerson.Names == null || tPerson.Names.Count == 0 ? null : tPerson.Names.Find(n => n.Use == NameSet.NameSetUse.Legal) ?? tPerson.Names[0],
                    PerminantAddress = tPerson.Addresses == null || tPerson.Addresses.Count == 0 ? null : tPerson.Addresses.Find(n => n.Use == AddressSet.AddressSetUse.HomeAddress) ?? tPerson.Addresses[0],
                    TelecomAddresses = tPerson.TelecomAddresses
                };
                retVal.Add(tPerson, "PSN", SVC.Core.ComponentModel.HealthServiceRecordSiteRoleType.TargetOf, null);
                return retVal;
            }
            finally
            {
                DatabasePersistenceService.ConnectionManager.ReleaseConnection(conn);
            }
        }


        /// <summary>
        /// Find client by name
        /// </summary>
        /// TODO: Make this more friendly for other DBMS other than PGSQL
        public Client[] FindClient(NameSet name, string genderCode, TimestampPart birthTime)
        {
            throw new NotImplementedException("Find not supported");
        }

        #endregion

        #region IUsesHostContext Members

        /// <summary>
        /// Gets or sets the host context
        /// </summary>
        public MARC.HI.EHRS.SVC.Core.HostContext Context
        {
            get;
            set;
        }

        #endregion
    }
}
