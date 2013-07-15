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
 * Date: 4-9-2012
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MARC.HI.EHRS.CR.Core.ComponentModel;
using MARC.HI.EHRS.SVC.Core.ComponentModel;
using System.Data;
using System.Reflection;
using MARC.HI.EHRS.CR.Core.Services;
using System.ComponentModel;
using MARC.HI.EHRS.SVC.Core.ComponentModel.Components;

namespace MARC.HI.EHRS.CR.Persistence.Data.ComponentPersister
{
    /// <summary>
    /// Person registration reference persister
    /// </summary>
    /// <remarks>
    /// This is a special persister in that it updates other records
    /// </remarks>
    public class PersonRegistrationRefPersister : IComponentPersister
    {
        #region IComponentPersister Members

        /// <summary>
        /// A person registration reference persister
        /// </summary>
        public Type HandlesComponent
        {
            get { return typeof(PersonRegistrationRef); }
        }

        /// <summary>
        /// Persist the person relationship
        /// </summary>
        public SVC.Core.DataTypes.VersionedDomainIdentifier Persist(System.Data.IDbConnection conn, System.Data.IDbTransaction tx, System.ComponentModel.IComponent data, bool isUpdate)
        {
            // Is this a replacement
            var pp = new PersonPersister();
            PersonRegistrationRef refr = data as PersonRegistrationRef;
            Person psn = pp.GetPerson(conn, tx, refr.AlternateIdentifiers[0], false);
            Person cntrPsn = data.Site.Container as Person;

            if (psn == null || cntrPsn == null)
                throw new ConstraintException(ApplicationContext.LocaleService.GetString("DBCF00B"));
            else if (psn.Id == cntrPsn.Id)
                throw new ConstraintException(ApplicationContext.LocaleService.GetString("DBCF00D"));

            // Load the container person from DB so we get all data
            Person dbCntrPsn = pp.GetPerson(conn, tx, new SVC.Core.DataTypes.DomainIdentifier()
            {
                Domain = ApplicationContext.ConfigurationService.OidRegistrar.GetOid(ClientRegistryOids.CLIENT_CRID).Oid,
                Identifier = cntrPsn.Id.ToString()
            }, true);
            pp.MergePersons(dbCntrPsn, cntrPsn);

            // Load the components for the person
            DbUtil.DePersistComponents(conn, psn, this, true);

            if (psn == null || dbCntrPsn == null)
                throw new ConstraintException(ApplicationContext.LocaleService.GetString("DBCF00B"));
            dbCntrPsn.Site = (data.Site.Container as IComponent).Site;

            var role = (refr.Site as HealthServiceRecordSite).SiteRoleType;
            var symbolic = (refr.Site as HealthServiceRecordSite).IsSymbolic; // If true, the replacement does not cascade and is a symbolic replacement of only the identifiers listed

            // Replacement?
            if (role == HealthServiceRecordSiteRoleType.ReplacementOf)
            {

                // First, we obsolete all records with the existing person
                foreach (var id in psn.AlternateIdentifiers.FindAll(o => refr.AlternateIdentifiers.Exists(a => a.Domain == o.Domain)))
                    id.UpdateMode = SVC.Core.DataTypes.UpdateModeType.Remove;
                //psn.AlternateIdentifiers.RemoveAll(o => o.UpdateMode != SVC.Core.DataTypes.UpdateModeType.Remove);

                // Not symbolic, means that we do a hard replace
                // Symbolic replace = Just replace the reference to that identifier
                // Hard replace = Merge the new and old record and then replace them
                if(!symbolic)
                {

                    // Now to copy the components of the current version down
                    //foreach (IComponent cmp in refr.Site.Container.Components)
                    //    if (cmp != refr)
                    //        dbCntrPsn.Add((cmp as HealthServiceRecordComponent).Clone() as IComponent);

                    // Merge the two records in memory taking the newer data
                    // This is a merge from old to new in order to capture any data elements 
                    // that have been updated in the old that might be newer (or more accurate) than the 
                    // the new
                    if(psn.AlternateIdentifiers == null)
                        dbCntrPsn.AlternateIdentifiers = new List<SVC.Core.DataTypes.DomainIdentifier>();
                    else if(psn.OtherIdentifiers == null)
                        dbCntrPsn.OtherIdentifiers = new List<KeyValuePair<SVC.Core.DataTypes.CodeValue, SVC.Core.DataTypes.DomainIdentifier>>();
                    foreach (var id in psn.AlternateIdentifiers)
                    {
                        // Remove the identifier from the original
                        id.UpdateMode = SVC.Core.DataTypes.UpdateModeType.Remove;

                        // If this is a duplicate id then don't add
                        if(dbCntrPsn.AlternateIdentifiers.Exists(i => i.Domain == id.Domain && i.Identifier == id.Identifier))
                            continue;

                        bool isPrivate = false;
                        var oidData = ApplicationContext.ConfigurationService.OidRegistrar.FindData(id.Domain);
                        if (oidData != null)
                            isPrivate = oidData.Attributes.Exists(o => o.Key == "IsUniqueIdentifier") && Convert.ToBoolean(oidData.Attributes.Find(o => o.Key == "IsUniqueIdentifier").Value);

                        // Add to alternate identifiers
                        dbCntrPsn.AlternateIdentifiers.Add(new SVC.Core.DataTypes.DomainIdentifier()
                        {
                            AssigningAuthority = id.AssigningAuthority,
                            UpdateMode = SVC.Core.DataTypes.UpdateModeType.AddOrUpdate,
                            IsLicenseAuthority = false,
                            IsPrivate = isPrivate, // TODO: Make this a configuration flag (cntrPsn.AlternateIdentifiers.Exists(i=>i.Domain == id.Domain)),
                            Identifier = id.Identifier,
                            Domain = id.Domain
                        });

                    }
                    foreach (var id in psn.OtherIdentifiers)
                    {
                        // Remove the identifier from the original
                        id.Value.UpdateMode = SVC.Core.DataTypes.UpdateModeType.Remove;
                        
                        // If this is a duplicate id then don't add
                        if(dbCntrPsn.OtherIdentifiers.Exists(i => i.Value.Domain == id.Value.Domain && i.Value.Identifier == id.Value.Identifier))
                            continue;

                        // Add to other identifiers
                        var oth = new KeyValuePair<SVC.Core.DataTypes.CodeValue,SVC.Core.DataTypes.DomainIdentifier>(
                            id.Key,
                            new SVC.Core.DataTypes.DomainIdentifier()
                            {
                                AssigningAuthority = id.Value.AssigningAuthority,
                                UpdateMode = SVC.Core.DataTypes.UpdateModeType.Add,
                                IsLicenseAuthority = false,
                                IsPrivate = (dbCntrPsn.OtherIdentifiers.Exists(i => i.Value.Domain == id.Value.Domain)),
                                Identifier = id.Value.Identifier,
                                Domain = id.Value.Domain
                            });

                        // Copy extensions
                        var extns = psn.FindAllExtensions(o => o.PropertyPath == String.Format("OtherIdentifiers[{0}{1}]", oth.Value.Domain, oth.Value.Identifier));
                        if(extns != null)
                            foreach(var ex in extns)
                                if(dbCntrPsn.FindExtension(o => o.PropertyPath == ex.PropertyPath && o.Name == ex.Name) == null)
                                    dbCntrPsn.Add(ex);
                        dbCntrPsn.OtherIdentifiers.Add(oth);
                    }

                    // Make sure we don't update what we don't need to 
                    dbCntrPsn.Addresses = psn.Addresses = null;
                    dbCntrPsn.Citizenship = psn.Citizenship = null;
                    dbCntrPsn.Employment = psn.Employment = null;
                    dbCntrPsn.Language = psn.Language = null;
                    dbCntrPsn.Names = psn.Names = null;
                    dbCntrPsn.Race = psn.Race = null;
                    dbCntrPsn.TelecomAddresses = psn.TelecomAddresses = null;
                    dbCntrPsn.BirthTime = psn.BirthTime = null;
                    dbCntrPsn.DeceasedTime = psn.DeceasedTime = null;

                    // Remove the old person from the db
                    psn.Status = SVC.Core.ComponentModel.Components.StatusType.Obsolete; // obsolete the old person
                }
                else // migrate identifiers
                    foreach (var id in refr.AlternateIdentifiers)
                    {
                        bool isPrivate = false;
                        var oidData = ApplicationContext.ConfigurationService.OidRegistrar.FindData(id.Domain);
                        if (oidData != null)
                            isPrivate = oidData.Attributes.Exists(o => o.Key == "IsUniqueIdentifier") && Convert.ToBoolean(oidData.Attributes.Find(o => o.Key == "IsUniqueIdentifier").Value);

                        dbCntrPsn.AlternateIdentifiers.Add(new SVC.Core.DataTypes.DomainIdentifier()
                        {
                            AssigningAuthority = id.AssigningAuthority,
                            UpdateMode = SVC.Core.DataTypes.UpdateModeType.AddOrUpdate,
                            IsLicenseAuthority = false,
                            IsPrivate = isPrivate, // TODO: Make this a configuration flag (cntrPsn.AlternateIdentifiers.Exists(i=>i.Domain == id.Domain)),
                            Identifier = id.Identifier,
                            Domain = id.Domain
                        });
                    }

                // Now update the person
                //psn.Site = refr.Site;
                //pp.Persist(conn, tx, psn, true); // update the person record
                var regEvent = this.GetRegistrationEvent(conn, tx, psn); // get the registration event
                var changeSummary = DbUtil.GetRegistrationEvent(refr).FindComponent(HealthServiceRecordSiteRoleType.ReasonFor | HealthServiceRecordSiteRoleType.OlderVersionOf) as ChangeSummary;
                regEvent.RemoveAllFromRole(HealthServiceRecordSiteRoleType.ReasonFor | HealthServiceRecordSiteRoleType.OlderVersionOf);
                regEvent.RemoveAllFromRole(HealthServiceRecordSiteRoleType.SubjectOf);
                regEvent.Add(new ChangeSummary() {
                    ChangeType = changeSummary.ChangeType,
                    EffectiveTime = changeSummary.EffectiveTime,
                    LanguageCode = changeSummary.LanguageCode,
                    Status = changeSummary.Status,
                    Timestamp = changeSummary.Timestamp
                }, "CHG", HealthServiceRecordSiteRoleType.ReasonFor | HealthServiceRecordSiteRoleType.OlderVersionOf, null);
                regEvent.Add(psn, "SUBJ", HealthServiceRecordSiteRoleType.SubjectOf, null);
                if (!symbolic)
                    regEvent.Status = StatusType.Obsolete; // obsolete

                new RegistrationEventPersister().Persist(conn, tx, regEvent, true);

                //pp.CreatePersonVersion(conn, tx, psn);
                //DbUtil.PersistComponents(conn, tx, false, this, psn);
                
                // Now, we have to prepare an event so that this all makes sense
                // if we de-persist the most recent version (to reflect changes made)
                // Store the merged new record
                pp.CreatePersonVersion(conn, tx, dbCntrPsn);

                // Components
                DbUtil.PersistComponents(conn, tx, false, this, dbCntrPsn);
                // Now update the backreference to up the chain it gets updated
                cntrPsn.VersionId = dbCntrPsn.VersionId;
            }

            // Create the link
            using (var cmd = DbUtil.CreateCommandStoredProc(conn, tx))
            {
                cmd.CommandText = "crt_psn_lnk";
                cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "psn_id_in", DbType.Decimal, dbCntrPsn.Id));
                cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "psn_vrsn_id_in", DbType.Decimal, dbCntrPsn.VersionId));
                cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "lnk_psn_id_in", DbType.Decimal, psn.Id));
                cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "lnk_cls_in", DbType.Decimal, (decimal)role));
                cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "symbolic_in", DbType.Boolean, symbolic));
                cmd.ExecuteNonQuery();
            }
            
            // Send notification that duplicates were resolved
            //if (symbolic)
            //{
            //    // Send an duplicates resolved message
            //    IClientNotificationService notificationService = ApplicationContext.CurrentContext.GetService(typeof(IClientNotificationService)) as IClientNotificationService;
            //    if (notificationService != null)
            //        notificationService.NotifyDuplicatesResolved(cntrPsn, refr.AlternateIdentifiers[0]);
            //}

            // Person identifier
            return new SVC.Core.DataTypes.VersionedDomainIdentifier()
            {
                Identifier = psn.Id.ToString(),
                Version = psn.VersionId.ToString()
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
        /// De-persist the component
        /// </summary>
        public System.ComponentModel.IComponent DePersist(System.Data.IDbConnection conn, decimal identifier, System.ComponentModel.IContainer container, SVC.Core.ComponentModel.HealthServiceRecordSiteRoleType? role, bool loadFast)
        {
            return new PersonPersister().DePersist(conn, identifier, container, role, loadFast); // Simply return the de-persisted person
        }

        #endregion
    }
}
