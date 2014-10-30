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
 * Date: 7-8-2012
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MARC.HI.EHRS.SVC.Core.Services;
using MARC.HI.EHRS.SVC.Core.DataTypes;
using System.Data;
using System.ComponentModel;
using MARC.HI.EHRS.CR.Core.ComponentModel;
using MARC.HI.EHRS.SVC.Core;
using MARC.HI.EHRS.SVC.Core.Exceptions;
using MARC.HI.EHRS.SVC.Core.ComponentModel;
using MARC.HI.EHRS.SVC.Core.ComponentModel.Components;
using MARC.HI.EHRS.CR.Core.Services;
using System.Diagnostics;

namespace MARC.HI.EHRS.CR.Persistence.Data.ComponentPersister
{
    /// <summary>
    /// Persister for personal relationships
    /// </summary>
    public class PersonalRelationshipPersister : IComponentPersister, IQueryComponentPersister
    {
        #region IComponentPersister Members


        /// <summary>
        /// Gets the type of component that this persister handles
        /// </summary>
        public Type HandlesComponent
        {
            get { return typeof(PersonalRelationship); }
        }

        /// <summary>
        /// Persist a component to the database
        /// </summary>
        public MARC.HI.EHRS.SVC.Core.DataTypes.VersionedDomainIdentifier Persist(System.Data.IDbConnection conn, System.Data.IDbTransaction tx, System.ComponentModel.IComponent data, bool isUpdate)
        {
            // Get config service
            ISystemConfigurationService configService = ApplicationContext.ConfigurationService; //ApplicationContext.Current.GetService(typeof(ISystemConfigurationService)) as ISystemConfigurationService;

            // Create the personal relationship in a strongly typed fashion
            PersonalRelationship pr = data as PersonalRelationship;
            Person clientContainer = pr.Site.Container as Person,
                relationshipPerson = null;
                
            // Get the person persister
            var persister = new PersonPersister();

            // First, let's see if we can fetch the client
            if (pr.Id != default(decimal))
                relationshipPerson = persister.GetPerson(conn, tx, new DomainIdentifier()
                {
                    Domain = configService.OidRegistrar.GetOid(ClientRegistryOids.CLIENT_CRID).Oid,
                    Identifier = pr.Id.ToString()
                }, true);
            else if (pr.AlternateIdentifiers != null)
            {
                int i = 0;
                while (relationshipPerson == null && i < pr.AlternateIdentifiers.Count)
                    relationshipPerson = persister.GetPerson(conn, tx, pr.AlternateIdentifiers[i++], true);
            }

            // Did we get one?
            // If not, then we need to register a patient in the database 
            if (relationshipPerson == null)
            {
                if (pr.LegalName == null)
                    throw new DataException(ApplicationContext.LocaleService.GetString("DBCF00B"));
                relationshipPerson = new Person()
                {
                    AlternateIdentifiers = pr.AlternateIdentifiers,
                    Names = new List<NameSet>() { pr.LegalName },
                    Addresses = new List<AddressSet>() { pr.PerminantAddress },
                    GenderCode = pr.GenderCode,
                    BirthTime = pr.BirthTime,
                    TelecomAddresses = pr.TelecomAddresses,
                    Status = StatusType.Active,
                    RoleCode = PersonRole.PRS
                };

                var registrationEvent = DbUtil.GetRegistrationEvent(pr).Clone() as RegistrationEvent;
                registrationEvent.Id = default(decimal);
                registrationEvent.EventClassifier = RegistrationEventType.ComponentEvent;
                registrationEvent.RemoveAllFromRole(HealthServiceRecordSiteRoleType.SubjectOf);
                registrationEvent.Add(relationshipPerson, "SUBJ", HealthServiceRecordSiteRoleType.SubjectOf, null);


                // Persist or merge?
                new RegistrationEventPersister().Persist(conn, tx, registrationEvent, isUpdate);
                //var clientIdentifier = persister.Persist(conn, tx, relationshipPerson, isUpdate); // Should persist
            }

            // Validate
            if (pr.LegalName == null)
                Trace.TraceWarning("Linking patients solely on identifier: This can be dangerous");
            else if (!relationshipPerson.Names.Exists(o => QueryUtil.MatchName(pr.LegalName, o) >= DatabasePersistenceService.ValidationSettings.PersonNameMatch))
                throw new DataException(ApplicationContext.LocaleService.GetString("DBCF00A"));
            // If the container for this personal relationship is a client, then we'll need to link that
            // personal relationship with the client to whom they have a relation with.
            if (clientContainer != null) // We need to do some linking
                LinkClients(conn, tx, relationshipPerson.Id, clientContainer.Id, pr.RelationshipKind, pr.Status.ToString());
            else if (clientContainer == null) // We need to do some digging to find out "who" this person is related to (the record target)
                throw new ConstraintException(ApplicationContext.LocaleService.GetString("DBCF003"));
                            
            // todo: Container is a HSR

            return new VersionedDomainIdentifier() 
                {
                    Domain = configService.OidRegistrar.GetOid(ClientRegistryOids.CLIENT_CRID).Oid,
                    Identifier = relationshipPerson.Id.ToString()
                };
        }

        /// <summary>
        /// Link two clients together
        /// </summary>
        private void LinkClients(System.Data.IDbConnection conn, System.Data.IDbTransaction tx, decimal source, decimal target, string kind, string status)
        {
            IDbCommand cmd = DbUtil.CreateCommandStoredProc(conn, tx);
            try
            {
                cmd.CommandText = "crt_psn_rltnshp";
                cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "src_psn_id_in", DbType.Decimal, source));
                cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "trg_psn_id_in", DbType.Decimal, target));
                cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "status_cs_in", DbType.StringFixedLength, status));
                cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "kind_in", DbType.StringFixedLength, kind));

                // Insert
                cmd.ExecuteNonQuery();
            }
            catch { }
            finally
            {
                cmd.Dispose();
            }
        }

        /// <summary>
        /// De-persist a record from the database
        /// </summary>
        public System.ComponentModel.IComponent DePersist(System.Data.IDbConnection conn, decimal identifier, IContainer container, HealthServiceRecordSiteRoleType? roleType, bool loadFast)
        {
            // Return value
            PersonalRelationship retVal = new PersonalRelationship();

            // De-persist the observation record
            ISystemConfigurationService sysConfig = ApplicationContext.ConfigurationService;

            // First, de-persist the client portions
            var clientDataRetVal = new PersonPersister().GetPerson(conn, null, new DomainIdentifier()
            {
                Domain = sysConfig.OidRegistrar.GetOid(ClientRegistryOids.CLIENT_CRID).Oid,
                Identifier = identifier.ToString()
            }, true);
            
            // Add the client components
            retVal.AlternateIdentifiers.AddRange(clientDataRetVal.AlternateIdentifiers);


            if (clientDataRetVal.Names != null)
                retVal.LegalName = clientDataRetVal.Names.Find(o => o.Use == NameSet.NameSetUse.Legal) ?? clientDataRetVal.Names[0];
            retVal.Add(clientDataRetVal, "SUBJ", HealthServiceRecordSiteRoleType.SubjectOf, null);
            //if (!loadFast)
            //{
            //    retVal.BirthTime = clientDataRetVal.BirthTime;
            //    foreach (IComponent cmp in clientDataRetVal.Components)
            //        retVal.Add(cmp, cmp.Site.Name, (cmp.Site as HealthServiceRecordSite).SiteRoleType, (cmp.Site as HealthServiceRecordSite).OriginalIdentifier);
            //    retVal.GenderCode = clientDataRetVal.GenderCode;
            //    retVal.Id = clientDataRetVal.Id;
            //    retVal.IsMasked = clientDataRetVal.IsMasked;
            //    if (clientDataRetVal.Addresses != null && clientDataRetVal.Addresses.Count > 0)
            //        retVal.PerminantAddress = clientDataRetVal.Addresses.Find(o => o.Use == AddressSet.AddressSetUse.HomeAddress) ?? clientDataRetVal.Addresses[0];
            //    retVal.TelecomAddresses.AddRange(clientDataRetVal.TelecomAddresses);
            //    retVal.Timestamp = clientDataRetVal.Timestamp;
            //}
            // Load the personal relationship
            using (IDbCommand cmd = DbUtil.CreateCommandStoredProc(conn, null))
            {
                cmd.CommandText = "get_psn_rltnshp";
                cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "src_psn_id_in", DbType.Decimal, clientDataRetVal.Id));
                cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "src_psn_vrsn_id_in", DbType.Decimal, (container as Person).VersionId));
                cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "trg_psn_id_in", DbType.Decimal, (container as Person).Id));

                decimal rltdId = 0;

                // Execute the reader
                using (IDataReader rdr = cmd.ExecuteReader())
                {
                    if (rdr.Read())
                    {
                        retVal.Id = identifier;
                        retVal.RelationshipKind = Convert.ToString(rdr["kind_cs"]);
                        rltdId = Convert.ToDecimal(rdr["trg_psn_id"]);


                        // Add prs
                        retVal.AlternateIdentifiers.Add(new DomainIdentifier()
                        {
                            Domain = sysConfig.OidRegistrar.GetOid(ClientRegistryOids.RELATIONSHIP_OID).Oid,
                            Identifier = rdr["rltnshp_id"].ToString()
                        });
                        retVal.Id = Convert.ToDecimal(rdr["rltnshp_id"]);
                    }
                }
                
                // Append to the container
                if (container is Person)
                    (container as Person).Add(retVal, Guid.NewGuid().ToString(), MARC.HI.EHRS.SVC.Core.ComponentModel.HealthServiceRecordSiteRoleType.RepresentitiveOf, null);

            }

            // Load the sub-components
            //DbUtil.DePersistComponents(conn, retVal, this, loadFast);

            return retVal;

        }

        #endregion

        /// <summary>
        /// Build the filter
        /// </summary>
        public string BuildFilter(IComponent data, bool forceExact)
        {
            PersonalRelationship prs = data as PersonalRelationship;

            // Subject relative
            Person subjectRelative = new Person()
            {
                RoleCode = PersonRole.PRS | PersonRole.PAT,
                Names = new List<NameSet>() { prs.LegalName }
            };

            PersonPersister prsp = new PersonPersister();
            var filterString = prsp.BuildFilter(subjectRelative, forceExact);

            var registrationEvent = DbUtil.GetRegistrationEvent(data);

            StringBuilder sb = new StringBuilder();
            if (registrationEvent != null) // We don't discriminate on queries for related persons
            {
                if (registrationEvent.EventClassifier == RegistrationEventType.Register || registrationEvent.EventClassifier == RegistrationEventType.Replace)
                    return ""; // We don't discriminate against registration events based on who they're related to
                else
                    sb.Append("SELECT DISTINCT HSR_ID FROM HSR_VRSN_TBL INNER JOIN PSN_VRSN_TBL ON (PSN_VRSN_TBL.REG_VRSN_ID = HSR_VRSN_TBL.HSR_VRSN_ID) INNER JOIN PSN_RLTNSHP_TBL ON (PSN_VRSN_TBL.PSN_ID = PSN_RLTNSHP_TBL.TRG_PSN_ID) WHERE PSN_VRSN_TBL.OBSLT_UTC IS NULL AND PSN_VRSN_TBL.PSN_VRSN_ID BETWEEN PSN_RLTNSHP_TBL.EFFT_VRSN_ID AND COALESCE(PSN_RLTNSHP_TBL.OBSLT_VRSN_ID, PSN_VRSN_TBL.PSN_VRSN_ID) ");
            }
            else
                sb.Append("SELECT DISTINCT TRG_PSN_ID AS PSN_ID FROM PSN_RLTNSHP_TBL WHERE OBSLT_UTC IS NULL ");

            sb.AppendFormat(" AND KIND_CS = '{0}' AND SRC_PSN_ID IN (SELECT PSN_TBL.PSN_ID FROM ({1}) AS PSN_TBL) ", prs.RelationshipKind.Replace("'", "''"), filterString);

            return sb.ToString();
            //return String.Empty;
        }

        /// <summary>
        /// The OID
        /// </summary>
        public string ComponentTypeOid
        {
            get { return ApplicationContext.ConfigurationService.OidRegistrar.GetOid(ClientRegistryOids.RELATIONSHIP_OID).Oid; }
        }

        /// <summary>
        /// Build control cclass
        /// </summary>
        public string BuildControlClauses(IComponent queryComponent)
        {
            return string.Empty;
        }
    }
}
