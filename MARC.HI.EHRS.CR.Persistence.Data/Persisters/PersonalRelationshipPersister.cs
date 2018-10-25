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

        public static readonly List<String> NON_DUPLICATE_REL = new List<string>() {
            "MTH", "FTH" 
        };

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
            {
                var personalRelationship = this.DePersist(conn, pr.Id, pr.Site.Container, (pr.Site as HealthServiceRecordSite).SiteRoleType, true) as PersonalRelationship;
                relationshipPerson = personalRelationship.FindComponent(HealthServiceRecordSiteRoleType.SubjectOf) as Person;
            }
            else if (pr.AlternateIdentifiers != null)
            {
                int i = 0;
                while (relationshipPerson == null && i < pr.AlternateIdentifiers.Count)
                    relationshipPerson = persister.GetPerson(conn, tx, pr.AlternateIdentifiers[i++], true);
            }
            
            if(relationshipPerson == null)
            {
                List<DomainIdentifier> candidateId = new List<DomainIdentifier>();

                // Is this an existing person (same name and relation)
                using (IDbCommand cmd = DbUtil.CreateCommandStoredProc(conn, tx))
                {
                    cmd.CommandText = "get_psn_rltnshps";
                    cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "src_psn_id_in", DbType.Decimal, clientContainer.Id));
                    cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "src_psn_vrsn_id_in", DbType.Decimal, clientContainer.VersionId));
                    using (IDataReader rdr = cmd.ExecuteReader())
                    {
                        while (rdr.Read())
                        {
                            if (rdr["kind_cs"].ToString() == pr.RelationshipKind)
                            {
                                candidateId.Add(new DomainIdentifier()
                                {
                                    Domain = configService.OidRegistrar.GetOid("CR_CID").Oid,
                                    Identifier = rdr["src_psn_id"].ToString()
                                });
                            }
                        }
                    }
                }

                // Now load candidates and check
                foreach (var id in candidateId)
                {
                    var candidate = persister.GetPerson(conn, tx, id, true);
                    if (NON_DUPLICATE_REL.Contains(pr.RelationshipKind))
                    {
                        relationshipPerson = candidate;
                        break;
                    }
                    else if (candidate.Names.Exists(n => n.SimilarityTo(pr.LegalName) >= DatabasePersistenceService.ValidationSettings.PersonNameMatch))
                    {
                        relationshipPerson = candidate;
                        break;
                    }
                }

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
                registrationEvent.Status = StatusType.Completed;
                
                // Persist or merge?
                new RegistrationEventPersister().Persist(conn, tx, registrationEvent, isUpdate);
                //var clientIdentifier = persister.Persist(conn, tx, relationshipPerson, isUpdate); // Should persist
            }
            else if(relationshipPerson.RoleCode != PersonRole.PAT)
            {
                var updatedPerson = new Person()
                {
                    Id = relationshipPerson.Id,
                    AlternateIdentifiers = pr.AlternateIdentifiers,
                    Names = new List<NameSet>() { pr.LegalName },
                    Addresses = new List<AddressSet>() {  },
                    GenderCode = pr.GenderCode,
                    BirthTime = pr.BirthTime,
                    TelecomAddresses = pr.TelecomAddresses,
                    Status = StatusType.Active,
                    RoleCode = relationshipPerson.RoleCode
                };

                if (pr.PerminantAddress != null)
                    updatedPerson.Addresses.Add(pr.PerminantAddress);

                persister.MergePersons(updatedPerson, relationshipPerson, true);
                relationshipPerson = updatedPerson;

                var registrationEvent = DbUtil.GetRegistrationEvent(pr).Clone() as RegistrationEvent;
                registrationEvent.Id = default(decimal);
                registrationEvent.EventClassifier = RegistrationEventType.ComponentEvent;
                registrationEvent.RemoveAllFromRole(HealthServiceRecordSiteRoleType.SubjectOf);
                registrationEvent.Add(relationshipPerson, "SUBJ", HealthServiceRecordSiteRoleType.SubjectOf, null);
                registrationEvent.Status = StatusType.Completed;
                new RegistrationEventPersister().Persist(conn, tx, registrationEvent, isUpdate);

            }

            // Validate
            if (!NON_DUPLICATE_REL.Contains(pr.RelationshipKind) && pr.AlternateIdentifiers.Count == 0 && !relationshipPerson.Names.Exists(o => QueryUtil.MatchName(pr.LegalName, o) >= DatabasePersistenceService.ValidationSettings.PersonNameMatch))
                throw new DataException(ApplicationContext.LocaleService.GetString("DBCF00A"));
            // If the container for this personal relationship is a client, then we'll need to link that
            // personal relationship with the client to whom they have a relation with.
            if (clientContainer != null) // We need to do some linking
                pr.Id = LinkClients(conn, tx, relationshipPerson.Id, clientContainer.Id, pr.RelationshipKind, pr.Status);
            else if (clientContainer == null) // We need to do some digging to find out "who" this person is related to (the record target)
                throw new ConstraintException(ApplicationContext.LocaleService.GetString("DBCF003"));
                            
            // todo: Container is a HSR

            return new VersionedDomainIdentifier() 
                {
                    Domain = configService.OidRegistrar.GetOid(ClientRegistryOids.RELATIONSHIP_OID).Oid,
                    Identifier = pr.Id.ToString()
                };
        }

        /// <summary>
        /// Create a registration event version linking to the person reflecting a change
        /// </summary>
        private RegistrationEvent GetRegistrationEvent(IDbConnection conn, IDbTransaction tx, Person psn)
        {
            // First, get the registration version for the person id
            decimal regEvtVrsn = 0,
                regEvtId = 0;
            using (IDbCommand cmd = DbUtil.CreateCommandStoredProc(conn, tx))
            {
                cmd.CommandText = "get_psn_hsr_evt";
                cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "psn_id", DbType.Decimal, psn.Id));
                cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "psn_vrsn_id", DbType.Decimal, psn.VersionId));

                // Get the registration version
                using (IDataReader rdr = cmd.ExecuteReader())
                {
                    if (!rdr.Read())
                        throw new ConstraintException("Cannot determine the registration event");
                    regEvtVrsn = Convert.ToDecimal(rdr["hsr_vrsn_id"]);
                    regEvtId = Convert.ToDecimal(rdr["hsr_id"]);
                }
            }

            // Load and return
            return new RegistrationEventPersister().DePersist(conn, regEvtId, regEvtVrsn, null, null, true) as RegistrationEvent;
        }

        /// <summary>
        /// Link two clients together
        /// </summary>
        private decimal LinkClients(System.Data.IDbConnection conn, System.Data.IDbTransaction tx, decimal source, decimal target, string kind, StatusType status)
        {
            IDbCommand cmd = DbUtil.CreateCommandStoredProc(conn, tx);
            try
            {
                cmd.CommandText = "crt_psn_rltnshp";
                cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "src_psn_id_in", DbType.Decimal, source));
                cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "trg_psn_id_in", DbType.Decimal, target));
                cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "status_cs_in", DbType.Decimal, (int)status));
                cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "kind_in", DbType.StringFixedLength, kind));

                // Insert
                return (decimal)cmd.ExecuteScalar();
            }
            catch { throw; }
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

            // De-persist 
            using (IDbCommand cmd = DbUtil.CreateCommandStoredProc(conn, null))
            {
                cmd.CommandText = "get_psn_rltnshp";
                cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "psn_rltnshp_id_in", DbType.Decimal, identifier));
                String clientId = String.Empty;
                // Execute the reader
                using (IDataReader rdr = cmd.ExecuteReader())
                {
                    if (rdr.Read())
                    {
                        retVal.Id = identifier;
                        retVal.RelationshipKind = Convert.ToString(rdr["kind_cs"]);

                        // Add prs
                        retVal.AlternateIdentifiers.Add(new DomainIdentifier()
                        {
                            Domain = sysConfig.OidRegistrar.GetOid(ClientRegistryOids.RELATIONSHIP_OID).Oid,
                            Identifier = rdr["rltnshp_id"].ToString()
                        });
                        retVal.Id = Convert.ToDecimal(rdr["rltnshp_id"]);
                        clientId = rdr["src_psn_id"].ToString();
                    }
                    else
                        return null;
                }
                // First, de-persist the client portions
                var clientDataRetVal = new PersonPersister().GetPerson(conn, null, new DomainIdentifier()
                {
                    Domain = sysConfig.OidRegistrar.GetOid(ClientRegistryOids.CLIENT_CRID).Oid,
                    Identifier = clientId.ToString()
                }, true);

                if (clientDataRetVal.Names != null)
                    retVal.LegalName = clientDataRetVal.Names.FindAll(o => o.Use == NameSet.NameSetUse.Legal).LastOrDefault() ?? clientDataRetVal.Names.LastOrDefault();

                if(clientDataRetVal.RoleCode == PersonRole.PAT)
                    retVal.AlternateIdentifiers.AddRange(clientDataRetVal.AlternateIdentifiers.Where(o => o.Domain != sysConfig.OidRegistrar.GetOid(ClientRegistryOids.RELATIONSHIP_OID).Oid));
                else
                    retVal.AlternateIdentifiers.AddRange(clientDataRetVal.AlternateIdentifiers.Where(o=>o.Domain != sysConfig.OidRegistrar.GetOid(ClientRegistryOids.CLIENT_CRID).Oid));

                retVal.Add(clientDataRetVal, "SUBJ", HealthServiceRecordSiteRoleType.SubjectOf, null);

            }

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
                Site = data.Site
            };

            if (prs.LegalName != null)
                subjectRelative.Names = new List<NameSet>() { prs.LegalName };
            if (prs.AlternateIdentifiers != null && prs.AlternateIdentifiers.Count > 0)
                subjectRelative.AlternateIdentifiers = new List<DomainIdentifier>(prs.AlternateIdentifiers);

            PersonPersister prsp = new PersonPersister();
            var filterString = prsp.BuildFilter(subjectRelative, forceExact);

            var registrationEvent = DbUtil.GetRegistrationEvent(data);

            StringBuilder sb = new StringBuilder();
            if (registrationEvent != null) // We don't discriminate on queries for related persons
            {
                if (registrationEvent.EventClassifier == RegistrationEventType.Register || registrationEvent.EventClassifier == RegistrationEventType.Replace)
                    return ""; // We don't discriminate against registration events based on who they're related to
                else
                    sb.Append("SELECT DISTINCT REG_VRSN_ID FROM PSN_VRSN_TBL INNER JOIN PSN_RLTNSHP_TBL ON (PSN_VRSN_TBL.PSN_ID = PSN_RLTNSHP_TBL.TRG_PSN_ID) WHERE PSN_VRSN_TBL.OBSLT_UTC IS NULL AND PSN_VRSN_TBL.PSN_VRSN_ID BETWEEN PSN_RLTNSHP_TBL.EFFT_VRSN_ID AND COALESCE(PSN_RLTNSHP_TBL.OBSLT_VRSN_ID, PSN_VRSN_TBL.PSN_VRSN_ID) ");
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
