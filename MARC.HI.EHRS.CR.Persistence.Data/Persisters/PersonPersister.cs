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
 * Date: 17-9-2012
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MARC.HI.EHRS.CR.Persistence.Data.ComponentPersister;
using MARC.HI.EHRS.CR.Core.ComponentModel;
using System.Data;
using MARC.HI.EHRS.SVC.Core.DataTypes;
using System.Diagnostics;
using MARC.HI.EHRS.SVC.Core.ComponentModel.Components;
using MARC.HI.EHRS.SVC.Core.ComponentModel;
using MARC.HI.EHRS.CR.Core.Services;
using MARC.HI.EHRS.CR.Core;
using System.ComponentModel;

namespace MARC.HI.EHRS.CR.Persistence.Data.ComponentPersister
{
    /// <summary>
    /// Persister that is responsible for the persisting of a person
    /// </summary>
    public class PersonPersister : IComponentPersister, IQueryComponentPersister, IVersionComponentPersister
    {
        #region IComponentPersister Members

        /// <summary>
        /// Gets the type that this persister can persist
        /// </summary>
        public Type HandlesComponent
        {
            get { return typeof(Person); }
        }

        /// <summary>
        /// Persist <paramref name="data"/> to the <paramref name="conn"/> on <paramref name="tx"/>
        /// updating if <paramref name="isUpdate"/> is true
        /// </summary>
        public SVC.Core.DataTypes.VersionedDomainIdentifier Persist(System.Data.IDbConnection conn, System.Data.IDbTransaction tx, System.ComponentModel.IComponent data, bool isUpdate)
        {
            Person psn = data as Person;

            try
            {

                // Call persistence service
                IDataTriggerService<Person> triggerService = null;

                try
                {
                    triggerService = ApplicationContext.CurrentContext.GetService(typeof(IDataTriggerService<Person>)) as IDataTriggerService<Person>;
                    if (triggerService != null)
                    {
                        Trace.TraceInformation($"Will fire trigger {triggerService.GetType()}");
                        triggerService.Context = ApplicationContext.CurrentContext;
                    }
                    // Components
                    psn = triggerService?.Persisting(psn) ?? psn;
                }
                catch (Exception e)
                {
                    Trace.TraceInformation("Skipping pre-persist trigger : {0}", e);

                }

                // Older version of, don't persist just return a record
                if (psn.Site != null && (psn.Site as HealthServiceRecordSite).SiteRoleType == HealthServiceRecordSiteRoleType.OlderVersionOf)
                {
                    return new VersionedDomainIdentifier()
                    {
                        Identifier = psn.Id.ToString(),
                        Version = psn.VersionId.ToString(),
                        Domain = ClientRegistryOids.CLIENT_CRID
                    };
                }
                else
                {
                    // Start by persisting the person component
                    if (isUpdate || psn.Id != default(decimal))
                    {
                        // validate
                        if (psn.Id == default(decimal))
                            throw new DataException(ApplicationContext.LocaleService.GetString("DTPE002"));

                        // TODO: Diff the person here
                        // Create a person version
                        this.CreatePersonVersion(conn, tx, psn);
                    }
                    else
                        this.CreatePerson(conn, tx, psn);
                }

                

                DbUtil.PersistComponents(conn, tx, false, this, psn);
                psn = triggerService?.Persisted(psn) ?? psn;

                // Next we'll load the alternate identifiers from the database into this client person
                // this is done because higher level functions may need access to the complete list of 
                // identifiers for things like notifications, etc.
                if (psn.AlternateIdentifiers == null)
                    psn.AlternateIdentifiers = new List<DomainIdentifier>();
                psn.AlternateIdentifiers.Clear();
                GetPersonAlternateIdentifiers(conn, tx, psn, false);
                
                // If there is a person ref in this component then we may have to update our version identifier

                var retVal = new VersionedDomainIdentifier()
                {
                    Identifier = psn.Id.ToString(),
                    Version = psn.VersionId.ToString(),
                    Domain = ApplicationContext.ConfigurationService.OidRegistrar.GetOid(ClientRegistryOids.CLIENT_CRID).Oid
                };
                // Also add the local CRID to the list
                psn.AlternateIdentifiers.Add(retVal);
                return retVal;
            }
            catch (Exception e)
            {
                Trace.TraceError(e.ToString());
                throw;
            }
        }

        /// <summary>
        /// Create a person object
        /// </summary>
        private void CreatePerson(IDbConnection conn, IDbTransaction tx, Person psn)
        {
            // Create the person
            var regEvt = DbUtil.GetRegistrationEvent(psn);

            using (IDbCommand cmd = DbUtil.CreateCommandStoredProc(conn, tx))
            {

                // Setup the database command
                cmd.CommandText = "crt_psn";

                // Set parameters
                cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "reg_vrsn_id_in", DbType.Decimal, regEvt.VersionIdentifier));
                cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "status_in", DbType.Decimal, (int)psn.Status));
                cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "gndr_in", DbType.String, psn.GenderCode));
                cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "brth_ts_in", DbType.Decimal, psn.BirthTime == null || psn.BirthTime.UpdateMode == UpdateModeType.Ignore ? DBNull.Value : (object)DbUtil.CreateTimestamp(conn, tx, psn.BirthTime, null)));
                cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "mb_ord_in", DbType.Decimal, psn.BirthOrder.HasValue ? (object)psn.BirthOrder.Value : DBNull.Value));
                decimal? religionCode = null,
                    vipCode = null,
                    maritalStatusCode = null,
                    birthLocation = null;
                if (psn.ReligionCode != null && psn.ReligionCode.UpdateMode != UpdateModeType.Ignore)
                    religionCode = DbUtil.CreateCodedValue(conn, tx, psn.ReligionCode);
                if (psn.VipCode != null && psn.VipCode.UpdateMode != UpdateModeType.Ignore)
                    vipCode = DbUtil.CreateCodedValue(conn, tx, psn.VipCode);
                if (psn.MaritalStatus != null && psn.MaritalStatus.UpdateMode != UpdateModeType.Ignore)
                    maritalStatusCode = DbUtil.CreateCodedValue(conn, tx, psn.MaritalStatus);
                if (psn.BirthPlace != null)
                {
                    // More tricky, but here is how it works
                    // 1. Get the persister for the SDL
                    var sdlPersister = new PlacePersister();
                    var sdlId = sdlPersister.Persist(conn, tx, psn.BirthPlace, false);
                    // Delete the sdl from the container (so it doesn't get persisted)
                    psn.Remove(psn.BirthPlace);
                    birthLocation = Decimal.Parse(sdlId.Identifier);
                }
                cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "rlgn_cd_id_in", DbType.Decimal, religionCode.HasValue ? (object)religionCode.Value : DBNull.Value));
                cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "vip_cd_id_in", DbType.Decimal, vipCode.HasValue ? (object)vipCode.Value : DBNull.Value));
                cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "mrtl_sts_cd_id_in", DbType.Decimal, maritalStatusCode.HasValue ? (object)maritalStatusCode.Value : DBNull.Value));
                cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "brth_plc_id_in", DbType.Decimal, birthLocation.HasValue ? (object)birthLocation.Value : DBNull.Value));

                if (psn.RoleCode == (PersonRole.PAT | PersonRole.PRS))
                    psn.RoleCode = PersonRole.PAT;
                cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "rol_cs_in", DbType.String, psn.RoleCode.ToString()));


                // Execute
                using (IDataReader rdr = cmd.ExecuteReader())
                {
                    if (rdr.Read())
                    {
                        // Setup the person identifier
                        psn.Id = Convert.ToDecimal(rdr["id"]);
                        psn.VersionId = Convert.ToDecimal(rdr["vrsn_id"]);

                    }
                    else
                        throw new DataException(ApplicationContext.LocaleService.GetString("DTPE003"));
                }

                // Persist the components of the 
                this.PersistPersonComponents(conn, tx, psn, UpdateModeType.AddOrUpdate);



            }
        }

        /// <summary>
        /// Persist person components
        /// </summary>
        private void PersistPersonComponents(IDbConnection conn, IDbTransaction tx, Person psn, UpdateModeType defaultUpdateMode)
        {
            // Addresses
            if(psn.Addresses != null)
                foreach (var addr in psn.Addresses)
                    this.PersistPersonAddress(conn, tx, psn, addr);

            // Names
            if(psn.Names != null)
                foreach (var name in psn.Names)
                    this.PersistPersonNames(conn, tx, psn, name);

            // Telecommunications addresses
            if(psn.TelecomAddresses != null)
                foreach (var tel in psn.TelecomAddresses)
                    this.PersistPersonTelecom(conn, tx, psn, tel);

            // Alternate identifiers
            if (psn.AlternateIdentifiers != null)
                foreach (var altId in psn.AlternateIdentifiers)
                    this.PersistPersonAlternateIdentifier(conn, tx, psn, altId);

            // Other Identifiers
            if (psn.OtherIdentifiers != null)
                foreach (var othId in psn.OtherIdentifiers)
                    this.PersistPersonOtherIdentifiers(conn, tx, psn, othId);

            // Persist person race
            if (psn.Race != null)
                foreach (var race in psn.Race)
                    this.PersistPersonRace(conn, tx, psn, race);

            // Persist person ethnic group codes
            if (psn.EthnicGroup != null)
                foreach (var eth in psn.EthnicGroup)
                    this.PersistPersonEthnicGroup(conn, tx, psn, eth);

            // Persist person language
            if (psn.Language != null)
                foreach (var lang in psn.Language)
                    this.PersistPersonLanguage(conn, tx, psn, lang);

            // Persist person citizenships
            if (psn.Citizenship != null)
                foreach (var cit in psn.Citizenship)
                    this.PersistPersonCitizenship(conn, tx, psn, cit);

            // Persist person employments
            if (psn.Employment != null)
                foreach (var emp in psn.Employment)
                    this.PersistPersonEmployment(conn, tx, psn, emp);
        }
        
        /// <summary>
        /// Persist person ethnic group
        /// </summary>
        private void PersistPersonEthnicGroup(IDbConnection conn, IDbTransaction tx, Person psn, CodeValue eth)
        { 
            if (eth == null || eth.UpdateMode == UpdateModeType.Ignore) return; // skip

            // Update or add or update? we first have to obsolete the existing
            if (eth.UpdateMode == UpdateModeType.Remove || eth.UpdateMode == UpdateModeType.Update || eth.UpdateMode == UpdateModeType.AddOrUpdate)
                using (IDbCommand cmd = DbUtil.CreateCommandStoredProc(conn, tx))
                {
                    cmd.CommandText = "obslt_psn_eth_grp";
                    cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "psn_id_in", DbType.Decimal, psn.Id));
                    cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "psn_vrsn_id_in", DbType.Decimal, psn.VersionId));
                    cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "eth_grp_cd_id_in", DbType.Decimal, eth.Key));
                    cmd.ExecuteNonQuery(); // obsolete
                }

            // Add telecom
            if (eth.UpdateMode != UpdateModeType.Remove)
                using (IDbCommand cmd = DbUtil.CreateCommandStoredProc(conn, tx))
                {

                    decimal codeId = DbUtil.CreateCodedValue(conn, tx, eth);

                    cmd.CommandText = "crt_psn_eth_grp";

                    cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "psn_id_in", DbType.Decimal, psn.Id));
                    cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "psn_vrsn_id_in", DbType.Decimal, psn.VersionId));
                    cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "eth_grp_cd_id_in", DbType.Decimal, codeId));

                    // Execute
                    cmd.ExecuteNonQuery();
                    eth.Key = codeId;

                }
        }

        /// <summary>
        /// Persists a person's citizenship(s)
        /// </summary>
        private void PersistPersonCitizenship(IDbConnection conn, IDbTransaction tx, Person psn, Citizenship cit)
        {
            if (cit == null || cit.UpdateMode == UpdateModeType.Ignore) return; // skip

            // Update or add or update? we first have to obsolete the existing
            if (cit.UpdateMode == UpdateModeType.Remove || cit.UpdateMode == UpdateModeType.Update || cit.UpdateMode == UpdateModeType.AddOrUpdate)
                using (IDbCommand cmd = DbUtil.CreateCommandStoredProc(conn, tx))
                {
                    cmd.CommandText = "obslt_psn_ctznshp";
                    cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "psn_id_in", DbType.Decimal, psn.Id));
                    cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "psn_vrsn_id_in", DbType.Decimal, psn.VersionId));
                    cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "ntn_cs_in", DbType.String, cit.CountryCode));
                    cmd.ExecuteNonQuery(); // obsolete
                }

            // Add citizenship
            if (cit.UpdateMode != UpdateModeType.Remove)
                using (IDbCommand cmd = DbUtil.CreateCommandStoredProc(conn, tx))
                {

                    cmd.CommandText = "crt_psn_ctznshp";

                    // Create the timestamp first
                    decimal? efftTsId = null;
                    if (cit.EffectiveTime != null)
                        efftTsId = DbUtil.CreateTimeset(conn, tx, cit.EffectiveTime);

                    // Add parameters
                    cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "psn_id_in", DbType.Decimal, psn.Id));
                    cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "psn_vrsn_id_in", DbType.Decimal, psn.VersionId));
                    cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "ntn_cs_in", DbType.String, cit.CountryCode));
                    cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "ntn_name_in", DbType.String, cit.CountryName));
                    cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "efft_ts_set_id_in", DbType.Decimal, efftTsId.HasValue ? (object)efftTsId.Value : DBNull.Value));
                    cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "status_cs_in", DbType.Decimal, (int)cit.Status));

                    // Execute
                    cmd.ExecuteNonQuery();

                }
        }

        /// <summary>
        /// Persists a person's citizenship(s)
        /// </summary>
        private void PersistPersonEmployment(IDbConnection conn, IDbTransaction tx, Person psn, Employment emp)
        {
            if (emp == null || emp.UpdateMode == UpdateModeType.Ignore) return; // skip

            // Update or add or update? we first have to obsolete the existing
            if (emp.UpdateMode == UpdateModeType.Remove || emp.UpdateMode == UpdateModeType.Update || emp.UpdateMode == UpdateModeType.AddOrUpdate)
                using (IDbCommand cmd = DbUtil.CreateCommandStoredProc(conn, tx))
                {
                    cmd.CommandText = "obslt_psn_empl";
                    cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "psn_id_in", DbType.Decimal, psn.Id));
                    cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "psn_vrsn_id_in", DbType.Decimal, psn.VersionId));
                    cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "emp_id_in", DbType.String, emp.Id));
                    cmd.ExecuteNonQuery(); // obsolete
                }

            // Add citizenship
            if (emp.UpdateMode != UpdateModeType.Remove)
                using (IDbCommand cmd = DbUtil.CreateCommandStoredProc(conn, tx))
                {

                    cmd.CommandText = "crt_psn_empl";

                    // Create the timestamp first
                    decimal? efftTsId = null, occupationId = null;
                    if (emp.EffectiveTime != null)
                        efftTsId = DbUtil.CreateTimeset(conn, tx, emp.EffectiveTime);
                    if (emp.Occupation != null)
                        occupationId = DbUtil.CreateCodedValue(conn, tx, emp.Occupation);

                    // Add parameters
                    cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "psn_id_in", DbType.Decimal, psn.Id));
                    cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "psn_vrsn_id_in", DbType.Decimal, psn.VersionId));
                    cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "emp_cd_id", DbType.Decimal, occupationId.HasValue ? (object)occupationId.Value : DBNull.Value));
                    cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "efft_ts_set_id_in", DbType.Decimal, efftTsId.HasValue ? (object)efftTsId.Value : DBNull.Value));
                    cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "status_cs_in", DbType.Decimal, (int)emp.Status));

                    // Execute
                    cmd.ExecuteNonQuery();

                }
        }

        /// <summary>
        /// Persist a person's language
        /// </summary>
        private void PersistPersonLanguage(IDbConnection conn, IDbTransaction tx, Person psn, PersonLanguage lang)
        {
            if (lang == null || lang.UpdateMode == UpdateModeType.Ignore) return; // skip

            // Update or add or update? we first have to obsolete the existing
            if (lang.UpdateMode == UpdateModeType.Remove || lang.UpdateMode == UpdateModeType.Update || lang.UpdateMode == UpdateModeType.AddOrUpdate)
                using (IDbCommand cmd = DbUtil.CreateCommandStoredProc(conn, tx))
                {
                    cmd.CommandText = "obslt_psn_lang";
                    cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "psn_id_in", DbType.Decimal, psn.Id));
                    cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "psn_vrsn_id_in", DbType.Decimal, psn.VersionId));
                    cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "lang_cs_in", DbType.String, lang.Language));
                    cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "mode_cs_in", DbType.Decimal, (decimal)lang.Type));
                    cmd.ExecuteNonQuery(); // obsolete
                }

            // Add telecom
            if (lang.UpdateMode != UpdateModeType.Remove)
                using (IDbCommand cmd = DbUtil.CreateCommandStoredProc(conn, tx))
                {

                    cmd.CommandText = "crt_psn_lang";

                    cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "psn_id_in", DbType.Decimal, psn.Id));
                    cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "psn_vrsn_id_in", DbType.Decimal, psn.VersionId));
                    cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "lang_cs_in", DbType.String, lang.Language));
                    cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "mode_cs_in", DbType.Decimal, (decimal)lang.Type));
                    
                    // Execute
                    cmd.ExecuteNonQuery();

                }
        }

        /// <summary>
        /// Persist a person's race
        /// </summary>
        private void PersistPersonRace(IDbConnection conn, IDbTransaction tx, Person psn, CodeValue race)
        {
            if (race == null || race.UpdateMode == UpdateModeType.Ignore) return; // skip

            // Update or add or update? we first have to obsolete the existing
            if (race.UpdateMode == UpdateModeType.Remove || race.UpdateMode == UpdateModeType.Update || race.UpdateMode == UpdateModeType.AddOrUpdate)
                using (IDbCommand cmd = DbUtil.CreateCommandStoredProc(conn, tx))
                {
                    cmd.CommandText = "obslt_psn_race";
                    cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "psn_id_in", DbType.Decimal, psn.Id));
                    cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "psn_vrsn_id_in", DbType.Decimal, psn.VersionId));
                    cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "race_cd_id_in", DbType.Decimal, race.Key));
                    cmd.ExecuteNonQuery(); // obsolete
                }

            // Add telecom
            if (race.UpdateMode != UpdateModeType.Remove)
                using (IDbCommand cmd = DbUtil.CreateCommandStoredProc(conn, tx))
                {

                    decimal codeId = DbUtil.CreateCodedValue(conn, tx, race);

                    cmd.CommandText = "crt_psn_race";

                    cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "psn_id_in", DbType.Decimal, psn.Id));
                    cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "psn_vrsn_id_in", DbType.Decimal, psn.VersionId));
                    cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "race_cd_id_in", DbType.Decimal, codeId));

                    // Execute
                    cmd.ExecuteNonQuery();
                    race.Key = codeId;

                }
        }

        /// <summary>
        /// Persist person's other (non-hcn) identifiers
        /// </summary>
        private void PersistPersonOtherIdentifiers(IDbConnection conn, IDbTransaction tx, Person psn, KeyValuePair<CodeValue, DomainIdentifier> othId)
        {
            if (othId.Equals(default(KeyValuePair<CodeValue, DomainIdentifier>)) || othId.Value.UpdateMode == UpdateModeType.Ignore) return; // skip

            // Update or add or update? we first have to obsolete the existing
            if (othId.Value.UpdateMode == UpdateModeType.Remove || othId.Value.UpdateMode == UpdateModeType.Update || othId.Value.UpdateMode == UpdateModeType.AddOrUpdate)
                using (IDbCommand cmd = DbUtil.CreateCommandStoredProc(conn, tx))
                {
                    cmd.CommandText = "obslt_psn_alt_id";
                    cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "psn_id_in", DbType.Decimal, psn.Id));
                    cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "psn_vrsn_id_in", DbType.Decimal, psn.VersionId));
                    cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "id_domain_in", DbType.String, othId.Value.Domain));
                    cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "id_value_in", DbType.String, othId.Value.Identifier));
                    cmd.ExecuteNonQuery(); // obsolete
                }

            // Add telecom
            if (othId.Value.UpdateMode != UpdateModeType.Remove)
                try
                {
                    using (IDbCommand cmd = DbUtil.CreateCommandStoredProc(conn, tx))
                    {

                        decimal? codeId = null;
                        if(othId.Key != null)
                            codeId = DbUtil.CreateCodedValue(conn, tx, othId.Key);

                        cmd.CommandText = "crt_psn_alt_id";

                        cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "psn_id_in", DbType.Decimal, psn.Id));
                        cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "psn_vrsn_id_in", DbType.Decimal, psn.VersionId));
                        cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "is_hcn_in", DbType.Boolean, false));
                        cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "is_prvt_in", DbType.Boolean, othId.Value.IsPrivate));
                        cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "id_purp_in", DbType.Decimal, codeId.HasValue ? (object)codeId.Value : DBNull.Value));
                        cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "id_domain_in", DbType.String, othId.Value.Domain));
                        cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "id_value_in", DbType.String, othId.Value.Identifier));
                        cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "id_auth_in", DbType.String, (object)othId.Value.AssigningAuthority ?? DBNull.Value));
                        // Execute
                        cmd.ExecuteNonQuery();

                    }
                }
                catch (Exception e)
                {
                    Trace.TraceError(e.ToString());
                    throw new DuplicateNameException(ApplicationContext.LocaleService.GetString("DBCF008"));
                }
        }

        /// <summary>
        /// Persist an alternate identifier
        /// </summary>
        private void PersistPersonAlternateIdentifier(IDbConnection conn, IDbTransaction tx, Person psn, DomainIdentifier altId)
        {
            if (altId == null || altId.UpdateMode == UpdateModeType.Ignore) return; // skip

            // Update or add or update? we first have to obsolete the existing
            if (altId.UpdateMode == UpdateModeType.Remove || altId.UpdateMode == UpdateModeType.Update || altId.UpdateMode == UpdateModeType.AddOrUpdate)
                using (IDbCommand cmd = DbUtil.CreateCommandStoredProc(conn, tx))
                {
                    cmd.CommandText = "obslt_psn_alt_id";
                    cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "psn_id_in", DbType.Decimal, psn.Id));
                    cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "psn_vrsn_id_in", DbType.Decimal, psn.VersionId));
                    cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "id_domain_in", DbType.String, altId.Domain));
                    cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "id_value_in", DbType.String, altId.Identifier));
                    cmd.ExecuteNonQuery(); // obsolete
                }

            // Adding only permitted for authorized ids
            var oidData = ApplicationContext.ConfigurationService.OidRegistrar.GetOid(altId.Domain);
#if DEBUG
            if (oidData == null || !oidData.Attributes.Exists(a => a.Key == "GloballyAssignable" && Boolean.Parse(a.Value)))
                Trace.TraceInformation("Registering new globally assignable identifier from domain {0}", altId.Domain);
            else if (altId.UpdateMode == UpdateModeType.Add && !(altId is AuthorityAssignedDomainIdentifier))
                throw new ConstraintException("Cannot register an ID without appropriate assigning authority!");
#endif

            // Add id
            if (altId.UpdateMode != UpdateModeType.Remove)
                try
                {
                    using (IDbCommand cmd = DbUtil.CreateCommandStoredProc(conn, tx))
                    {
                        cmd.CommandText = "crt_psn_alt_id";
                        cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "psn_id_in", DbType.Decimal, psn.Id));
                        cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "psn_vrsn_id_in", DbType.Decimal, psn.VersionId));
                        cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "is_hcn_in", DbType.Boolean, true));
                        cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "is_prvt_in", DbType.Boolean, altId.IsPrivate));
                        cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "id_purp_in", DbType.Decimal, DBNull.Value));
                        cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "id_domain_in", DbType.String, altId.Domain));
                        cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "id_value_in", DbType.String, altId.Identifier));
                        cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "id_auth_in", DbType.String, (object)altId.AssigningAuthority ?? DBNull.Value));

                        // Execute
                        cmd.ExecuteNonQuery();

                    }
                }
                catch(Exception e)
                {
                    throw new DuplicateNameException(String.Format(ApplicationContext.LocaleService.GetString("DBCF008"), e.Message));
                }
        }

        /// <summary>
        /// Persist a person's telecommunications address
        /// </summary>
        private void PersistPersonTelecom(IDbConnection conn, IDbTransaction tx, Person psn, TelecommunicationsAddress tel)
        {
            if (tel == null || tel.UpdateMode == UpdateModeType.Ignore) return; // skip

            // Update or add or update? we first have to obsolete the existing
            if (tel.UpdateMode == UpdateModeType.Remove || tel.UpdateMode == UpdateModeType.Update || tel.UpdateMode == UpdateModeType.AddOrUpdate)
                using (IDbCommand cmd = DbUtil.CreateCommandStoredProc(conn, tx))
                {
                    cmd.CommandText = "obslt_psn_tel";
                    cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "psn_id_in", DbType.Decimal, psn.Id));
                    cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "tel_value_in", DbType.String, tel.Value));
                    cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "psn_vrsn_id_in", DbType.Decimal, psn.VersionId));
                    cmd.ExecuteNonQuery(); // obsolete
                }

            // Add
            if (tel.UpdateMode != UpdateModeType.Remove)
                using (IDbCommand cmd = DbUtil.CreateCommandStoredProc(conn, tx))
                {
                    cmd.CommandText = "crt_psn_tel";
                    cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "psn_id_in", DbType.Decimal, psn.Id));
                    cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "psn_vrsn_id_in", DbType.Decimal, psn.VersionId));
                    cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "telecom_in", DbType.String, tel.Value));
                    cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "telecom_use_in", DbType.String, tel.Use));
                    cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "telecom_cap_in", DbType.String, (Object)tel.Capability ?? DBNull.Value));
                    
                    // Execute
                    tel.Key = Convert.ToDecimal(cmd.ExecuteScalar());

                }
        }

        /// <summary>
        /// Persist person names
        /// </summary>
        private void PersistPersonNames(IDbConnection conn, IDbTransaction tx, Person psn, NameSet name)
        {
            if (name == null || name.UpdateMode == UpdateModeType.Ignore) return; // skip

            // Update or add or update? we first have to obsolete the existing
            if (name.UpdateMode == UpdateModeType.Remove || name.UpdateMode == UpdateModeType.Update || name.UpdateMode == UpdateModeType.AddOrUpdate && name.Key != default(decimal))
                using (IDbCommand cmd = DbUtil.CreateCommandStoredProc(conn, tx))
                {
                    cmd.CommandText = "obslt_psn_name_set";
                    cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "psn_vrsn_id_in", DbType.Decimal, psn.VersionId));
                    cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "name_set_id_in", DbType.Decimal, name.Key));
                    cmd.ExecuteNonQuery(); // obsolete
                }

            // Add
            if (name.UpdateMode != UpdateModeType.Remove)
                using (IDbCommand cmd = DbUtil.CreateCommandStoredProc(conn, tx))
                {
                    cmd.CommandText = "crt_psn_name_set";
                    cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "psn_id_in", DbType.Decimal, psn.Id));
                    cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "psn_vrsn_id_in", DbType.Decimal, psn.VersionId));
                    cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "name_set_use_in", DbType.Decimal, name.Use));

                    // Execute
                    name.Key = Convert.ToDecimal(cmd.ExecuteScalar());

                    // Next we'll clean and persist the components
                    DbUtil.CreateNameSet(conn, tx, name);
                }
        }

        /// <summary>
        /// Persist a person's address
        /// </summary>
        private void PersistPersonAddress(IDbConnection conn, IDbTransaction tx, Person psn, AddressSet addr)
        {
            if (addr == null || addr.UpdateMode == UpdateModeType.Ignore) return; // skip

            // Update or add or update? we first have to obsolete the existing
            if (addr.UpdateMode == UpdateModeType.Remove || addr.UpdateMode == UpdateModeType.Update || addr.UpdateMode == UpdateModeType.AddOrUpdate && addr.Key != default(decimal))
                using (IDbCommand cmd = DbUtil.CreateCommandStoredProc(conn, tx))
                {
                    cmd.CommandText = "obslt_psn_addr_set";
                    cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "psn_vrsn_id_in", DbType.Decimal, psn.VersionId));
                    cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "addr_set_id_in", DbType.Decimal, addr.Key));
                    cmd.ExecuteNonQuery(); // obsolete
                }

            // Add
            if (addr.UpdateMode != UpdateModeType.Remove)
                using (IDbCommand cmd = DbUtil.CreateCommandStoredProc(conn, tx))
                {
                    cmd.CommandText = "crt_psn_addr_set";
                    cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "psn_id_in", DbType.Decimal, psn.Id));
                    cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "psn_vrsn_id_in", DbType.Decimal, psn.VersionId));
                    cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "addr_set_use_in", DbType.Decimal, (int)addr.Use));

                    // Execute
                    addr.Key = Convert.ToDecimal(cmd.ExecuteScalar());

                    // Next we'll clean and persist the components
                    DbUtil.CreateAddressSet(conn, tx, addr);
                }
        }

        /// <summary>
        /// Create a new version of a patient record
        /// </summary>
        internal void CreatePersonVersion(IDbConnection conn, IDbTransaction tx, Person psn)
        {
            
            // Create the person
            var regEvt = DbUtil.GetRegistrationEvent(psn);

            using (IDbCommand cmd = DbUtil.CreateCommandStoredProc(conn, tx))
            {

                // Setup the database command
                cmd.CommandText = "crt_psn_vrsn";

                // Set parameters
                cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "psn_id_in", DbType.Decimal, psn.Id));
                cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "reg_vrsn_id_in", DbType.Decimal, regEvt.VersionIdentifier));
                cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "status_in", DbType.Decimal, (int)psn.Status));
                cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "gndr_in", DbType.String, psn.GenderCode));
                cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "brth_ts_in", DbType.Decimal, psn.BirthTime != null && psn.BirthTime.UpdateMode != UpdateModeType.Ignore ? (object)DbUtil.CreateTimestamp(conn, tx, psn.BirthTime, null) : DBNull.Value));
                cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "dcsd_ts_in", DbType.Decimal, psn.DeceasedTime != null && psn.DeceasedTime.UpdateMode != UpdateModeType.Ignore ? (object)DbUtil.CreateTimestamp(conn, tx, psn.DeceasedTime, null) : DBNull.Value));
                cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "mb_ord_in", DbType.Decimal, psn.BirthOrder.HasValue ? (object)psn.BirthOrder.Value : DBNull.Value));

                decimal? religionCode = null,
                    vipCode = null,
                    maritalStatusCode = null,
                    birthLocation = null;
                if (psn.ReligionCode != null && psn.ReligionCode.UpdateMode != UpdateModeType.Ignore)
                    religionCode = DbUtil.CreateCodedValue(conn, tx, psn.ReligionCode);
                if (psn.VipCode != null && psn.VipCode.UpdateMode != UpdateModeType.Ignore)
                    vipCode = DbUtil.CreateCodedValue(conn, tx, psn.VipCode);
                if (psn.MaritalStatus != null && psn.MaritalStatus.UpdateMode != UpdateModeType.Ignore)
                    maritalStatusCode = DbUtil.CreateCodedValue(conn, tx, psn.MaritalStatus);
                if (psn.BirthPlace != null)
                {
                    // More tricky, but here is how it works
                    // 1. Get the persister for the SDL
                    var sdlPersister = new PlacePersister();
                    var sdlId = sdlPersister.Persist(conn, tx, psn.BirthPlace, false);
                    // Delete the sdl from the container (so it doesn't get persisted)
                    psn.Remove(psn.BirthPlace);
                    birthLocation = Decimal.Parse(sdlId.Identifier);
                }
                cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "rlgn_cd_id_in", DbType.Decimal, religionCode.HasValue ? (object)religionCode.Value : DBNull.Value));
                cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "vip_cd_id_in", DbType.Decimal, vipCode.HasValue ? (object)vipCode.Value : DBNull.Value));
                cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "mrtl_sts_cd_id_in", DbType.Decimal, maritalStatusCode.HasValue ? (object)maritalStatusCode.Value : DBNull.Value));
                cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "brth_plc_id_in", DbType.Decimal, birthLocation.HasValue ? (object)birthLocation.Value : DBNull.Value));

                if (psn.RoleCode == (PersonRole.PAT | PersonRole.PRS))
                    psn.RoleCode = PersonRole.PAT;

                cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "rol_cs_in", DbType.String, psn.RoleCode.ToString()));


                // Execute
                
                psn.VersionId = Convert.ToDecimal(cmd.ExecuteScalar());

                // Persist the components of the 
                this.PersistPersonComponents(conn, tx, psn, UpdateModeType.AddOrUpdate);

 
            }
        }

        /// <summary>
        /// Get a person's most recent version
        /// </summary>
        internal Person GetPerson(IDbConnection conn, IDbTransaction tx, DomainIdentifier domainIdentifier, bool loadFast)
        {
#if DEBUG
            Trace.TraceInformation("Get person {0}@{1}", domainIdentifier.Identifier, domainIdentifier.Domain);
#endif
            return GetPerson(conn, tx, new VersionedDomainIdentifier()
            {
                Domain = domainIdentifier.Domain,
                Identifier = domainIdentifier.Identifier
            }, loadFast);
        }

        /// <summary>
        /// Get a person from the database
        /// </summary>
        internal Person GetPerson(IDbConnection conn, IDbTransaction tx, VersionedDomainIdentifier domainIdentifier, bool loadFast)
        {


            // Create the command
            using (IDbCommand cmd = DbUtil.CreateCommandStoredProc(conn, tx))
            {
                if (domainIdentifier.Domain != ApplicationContext.ConfigurationService.OidRegistrar.GetOid(ClientRegistryOids.CLIENT_CRID).Oid)
                {
                    cmd.CommandText = "get_psn_extern";
                    // Create parameters
                    cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "id_domain_in", DbType.String, domainIdentifier.Domain));
                    cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "id_value_in", DbType.String, domainIdentifier.Identifier));
                }
                else if (String.IsNullOrEmpty(domainIdentifier.Version))
                {
                    cmd.CommandText = "get_psn_crnt_vrsn";
                    // Create parameters
                    cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "psn_id_in", DbType.Decimal, Decimal.Parse(domainIdentifier.Identifier)));
                }
                else
                {
                    cmd.CommandText = "get_psn_vrsn";
                    // Create parameters
                    cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "psn_id_in", DbType.Decimal, Decimal.Parse(domainIdentifier.Identifier)));
                    cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "psn_vrsn_id_in", DbType.Decimal, Decimal.Parse(domainIdentifier.Version)));
                }
    
#if PERFMON
                Trace.TraceInformation("{0} : {1}", DateTime.Now.ToString("HH:mm:ss.ffff"), cmd.CommandText);
#endif
                // Execute the command
                using (IDataReader rdr = cmd.ExecuteReader())
                {
                    if (rdr.Read())
                    {
                        Person retVal = new Person();
                        retVal.Id = Convert.ToDecimal(rdr["psn_id"]);
                        retVal.VersionId = Convert.ToDecimal(rdr["psn_vrsn_id"]);
                        retVal.Status = (StatusType)Convert.ToInt32(rdr["status_cs_id"]);
                        retVal.GenderCode = rdr["gndr_cs"] == DBNull.Value ? null : Convert.ToString(rdr["gndr_cs"]);
                        retVal.BirthOrder = (int?)(rdr["mb_ord"] == DBNull.Value ? (object)null : Convert.ToInt32(rdr["mb_ord"]));
                        retVal.Timestamp = Convert.ToDateTime(rdr["crt_utc"]);
                        retVal.RoleCode = (PersonRole)Enum.Parse(typeof(PersonRole), Convert.ToString(rdr["rol_cs"]));
                        // Other fetched data
                        decimal? birthTs = null,
                            deceasedTs = null,
                            religionCode = null,
                            replacesVersion = null,
                            brthPlc = null,
                            mrtlStatus = null;

                        if (rdr["mrtl_sts_cd_id"] != DBNull.Value)
                            mrtlStatus = Convert.ToDecimal(rdr["mrtl_sts_cd_id"]);
                        if (rdr["brth_ts"] != DBNull.Value)
                            birthTs = Convert.ToDecimal(rdr["brth_ts"]);
                        if (rdr["dcsd_ts"] != DBNull.Value)
                            deceasedTs = Convert.ToDecimal(rdr["dcsd_ts"]);
                        if (rdr["rlgn_cd_id"] != DBNull.Value)
                            religionCode = Convert.ToDecimal(rdr["rlgn_cd_id"]);
                        if (rdr["rplc_vrsn_id"] != DBNull.Value)
                            replacesVersion = Convert.ToDecimal(rdr["rplc_vrsn_id"]);
                        if (rdr["brth_plc_id"] != DBNull.Value)
                            brthPlc = Convert.ToDecimal(rdr["brth_plc_id"]);
                        // Close the reader and read dependent values
                        rdr.Close();

#if PERFMON
                        Trace.TraceInformation("EO {0} : {1}", DateTime.Now.ToString("HH:mm:ss.ffff"), cmd.CommandText);
#endif

                        // Load immediate values
                        if (mrtlStatus.HasValue)
                            retVal.MaritalStatus = DbUtil.GetCodedValue(conn, tx, mrtlStatus);

                        if (birthTs.HasValue)
                            retVal.BirthTime = DbUtil.GetEffectiveTimestampSet(conn, tx, birthTs.Value).Parts[0];
                        if (deceasedTs.HasValue)
                            retVal.DeceasedTime = DbUtil.GetEffectiveTimestampSet(conn, tx, deceasedTs.Value).Parts[0];
                        if (religionCode.HasValue)
                            retVal.ReligionCode = DbUtil.GetCodedValue(conn, tx, religionCode);
                        if(brthPlc.HasValue)
                        {
                            var place = new PlacePersister().DePersist(conn, brthPlc.Value, retVal, null, false) as Place;
                            place.Site.Name = "BRTH";
                        }
                        // Load other properties
                        GetPersonNames(conn, tx, retVal, loadFast);
                        GetPersonAlternateIdentifiers(conn, tx, retVal, loadFast);
                        GetPersonAddresses(conn, tx, retVal, loadFast);
                        GetPersonLanguages(conn, tx, retVal);
                        GetPersonRaces(conn, tx, retVal);
                        GetPersonTelecomAddresses(conn, tx, retVal);
                        GetPersonEthnicGroups(conn, tx, retVal);

                        if(!retVal.AlternateIdentifiers.Exists(o=>o.Domain == domainIdentifier.Domain && o.Identifier == domainIdentifier.Identifier))
                            retVal.AlternateIdentifiers.Add(domainIdentifier);

                        // Person is replaced?
                        if (replacesVersion.HasValue && !loadFast)
                        {
                            // Older version of the person
                            var olderVersionPerson = this.GetPerson(conn, tx, new VersionedDomainIdentifier()
                            {
                                Domain = domainIdentifier.Domain,
                                Identifier = domainIdentifier.Identifier,
                                Version = replacesVersion.ToString()
                            }, loadFast);
                            if (olderVersionPerson != null)
                            {
                                retVal.Add(olderVersionPerson, Guid.NewGuid().ToString(), HealthServiceRecordSiteRoleType.OlderVersionOf, null);
                                DbUtil.DePersistComponents(conn, olderVersionPerson, this, loadFast);
                            }
                        }

                        
                        return retVal;
                    }
                    else
                        return null;
                }

            }

        }

        /// <summary>
        /// Get person's ethnic groups 
        /// </summary>
        private void GetPersonEthnicGroups(IDbConnection conn, IDbTransaction tx, Person person)
        {
#if PERFMON
            Trace.TraceInformation("{0} : {1}", DateTime.Now.ToString("HH:mm:ss.ffff"), "get_psn_eth_grp");
#endif
            using (IDbCommand cmd = DbUtil.CreateCommandStoredProc(conn, tx))
            {
                cmd.CommandText = "get_psn_eth_grp";
                cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "psn_id_in", DbType.Decimal, person.Id));
                cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "psn_vrsn_id_in", DbType.Decimal, person.VersionId));

                // Ethnic Group
                person.EthnicGroup = new List<CodeValue>();
                using (IDataReader rdr = cmd.ExecuteReader())
                    while (rdr.Read())
                        person.EthnicGroup.Add(new CodeValue() { Key = Convert.ToDecimal(rdr["eth_grp_cd_id"]) });

                // Fill out races
                for (int i = 0; i < person.EthnicGroup.Count; i++)
                    person.EthnicGroup[i] = DbUtil.GetCodedValue(conn, tx, person.EthnicGroup[i].Key);
            }
#if PERFMON
            Trace.TraceInformation("EO {0} : {1}", DateTime.Now.ToString("HH:mm:ss.ffff"), "get_psn_eth_grp");
#endif
        }

        /// <summary>
        /// Get person's telecom addresses
        /// </summary>
        private void GetPersonTelecomAddresses(IDbConnection conn, IDbTransaction tx, Person person)
        {

#if PERFMON
            Trace.TraceInformation("{0} : {1}", DateTime.Now.ToString("HH:mm:ss.ffff"), "get_psn_tels");
#endif
            using (IDbCommand cmd = DbUtil.CreateCommandStoredProc(conn, tx))
            {
                cmd.CommandText = "get_psn_tels";
                cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "psn_id_in", DbType.Decimal, person.Id));
                cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "psn_vrsn_id_in", DbType.Decimal, person.VersionId));

                // Telecoms
                person.TelecomAddresses = new List<TelecommunicationsAddress>();
                using (IDataReader rdr = cmd.ExecuteReader())
                    while (rdr.Read())
                        person.TelecomAddresses.Add(new TelecommunicationsAddress()
                        {
                            Use = Convert.ToString(rdr["tel_use"]),
                            Value = Convert.ToString(rdr["tel_value"]),
                            Capability = Convert.ToString(rdr["tel_cap"])
                        });

            }

#if PERFMON
            Trace.TraceInformation("EO {0} : {1}", DateTime.Now.ToString("HH:mm:ss.ffff"), "get_psn_tels");
#endif
        }

        /// <summary>
        /// Get person's alternate identifier
        /// </summary>
        private void GetPersonAlternateIdentifiers(IDbConnection conn, IDbTransaction tx, Person person, bool loadFast)
        {

#if PERFMON
            Trace.TraceInformation("{0} : {1}", DateTime.Now.ToString("HH:mm:ss.ffff"), "get_psn_alt_id");
#endif
            using (IDbCommand cmd = DbUtil.CreateCommandStoredProc(conn, tx))
            {
                cmd.CommandText = "get_psn_alt_id";
                if (!loadFast)
                    cmd.CommandText += "_efft";

                cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "psn_id_in", DbType.Decimal, person.Id));
                cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "psn_vrsn_id_in", DbType.Decimal, person.VersionId));

                person.AlternateIdentifiers = new List<DomainIdentifier>();
                person.OtherIdentifiers = new List<KeyValuePair<CodeValue,DomainIdentifier>>();

                // Read
                using (IDataReader rdr = cmd.ExecuteReader())
                {
                    while (rdr.Read())
                    {
                        if (Convert.ToBoolean(rdr["is_hcn"]))
                            person.AlternateIdentifiers.Add(new DomainIdentifier()
                            {
                                Key = Convert.ToDecimal(rdr["efft_vrsn_id"]),
                                Domain = Convert.ToString(rdr["id_domain"]),
                                Identifier = rdr["id_value"] == DBNull.Value ? null : Convert.ToString(rdr["id_value"]),
                                AssigningAuthority = rdr["id_auth"] == DBNull.Value ? null : Convert.ToString(rdr["id_auth"]),
                                IsPrivate = Convert.ToBoolean(rdr["is_prvt"]),
                                UpdateMode = UpdateModeType.Ignore,
                                EffectiveTime = !loadFast ? Convert.ToDateTime(rdr["efft_utc"]) : person.Timestamp,
                                ObsoleteTime = !loadFast && rdr["obslt_utc"] != DBNull.Value ? (DateTime?)Convert.ToDateTime(rdr["obslt_utc"]) : null
                            });
                        else
                            person.OtherIdentifiers.Add(new KeyValuePair<CodeValue, DomainIdentifier>(
                                rdr["id_purp_cd_id"] == DBNull.Value ? null : new CodeValue() { Key = Convert.ToDecimal(rdr["id_purp_cd_id"]) },
                                new DomainIdentifier()
                                {
                                    Key = Convert.ToDecimal(rdr["efft_vrsn_id"]),
                                    Domain = Convert.ToString(rdr["id_domain"]),
                                    Identifier = rdr["id_value"] == DBNull.Value ? null : Convert.ToString(rdr["id_value"]),
                                    AssigningAuthority = rdr["id_auth"] == DBNull.Value ? null : Convert.ToString(rdr["id_auth"]),
                                    IsPrivate = Convert.ToBoolean(rdr["is_prvt"]),
                                    UpdateMode = UpdateModeType.Ignore,
                                    EffectiveTime = !loadFast ? Convert.ToDateTime(rdr["efft_utc"]) : person.Timestamp,
                                    ObsoleteTime = !loadFast && rdr["obslt_utc"] != DBNull.Value ? (DateTime?)Convert.ToDateTime(rdr["obslt_utc"]) : null
                                })
                            );
                    }

                    person.AlternateIdentifiers.RemoveAll(o => o.IsPrivate);
                    person.OtherIdentifiers.RemoveAll(o => o.Value.IsPrivate);
                    // Close the reader
                    rdr.Close();

                    // Fill in other identifiers
                    foreach (var kv in person.OtherIdentifiers)
                    {
                        if (kv.Key == null)
                            continue;

                        var cd = DbUtil.GetCodedValue(conn, tx, kv.Key.Key);
                        kv.Key.Code = cd.Code;
                        kv.Key.CodeSystem = cd.CodeSystem;
                        kv.Key.CodeSystemName = cd.CodeSystemName;
                        kv.Key.CodeSystemVersion = cd.CodeSystemVersion;
                        kv.Key.OriginalText = cd.OriginalText;
                        kv.Key.Qualifies = cd.Qualifies;
                    }

                }
            }

#if PERFMON
            Trace.TraceInformation("EO {0} : {1}", DateTime.Now.ToString("HH:mm:ss.ffff"), "get_psn_alt_id");
#endif
        }

        /// <summary>
        /// Get a person's race codes
        /// </summary>
        private void GetPersonRaces(IDbConnection conn, IDbTransaction tx, Person person)
        {
#if PERFMON
            Trace.TraceInformation("{0} : {1}", DateTime.Now.ToString("HH:mm:ss.ffff"), "get_psn_races");
#endif
            using (IDbCommand cmd = DbUtil.CreateCommandStoredProc(conn, tx))
            {
                cmd.CommandText = "get_psn_races";
                cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "psn_id_in", DbType.Decimal, person.Id));
                cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "psn_vrsn_id_in", DbType.Decimal, person.VersionId));

                // Races
                person.Race = new List<CodeValue>();
                using (IDataReader rdr = cmd.ExecuteReader())
                    while (rdr.Read())
                        person.Race.Add(new CodeValue() { Key = Convert.ToDecimal(rdr["race_cd_id"]) });

                // Fill out races
                for (int i = 0; i < person.Race.Count; i++)
                    person.Race[i] = DbUtil.GetCodedValue(conn, tx, person.Race[i].Key);
            }
#if PERFMON
            Trace.TraceInformation("EO {0} : {1}", DateTime.Now.ToString("HH:mm:ss.ffff"), "get_psn_races");
#endif
        }

        /// <summary>
        /// Get person's languages
        /// </summary>
        private void GetPersonLanguages(IDbConnection conn, IDbTransaction tx, Person person)
        {
#if PERFMON
            Trace.TraceInformation("{0} : {1}", DateTime.Now.ToString("HH:mm:ss.ffff"), "get_psn_langs");
#endif
            using (IDbCommand cmd = DbUtil.CreateCommandStoredProc(conn, tx))
            {
                cmd.CommandText = "get_psn_langs";
                cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "psn_id_in", DbType.Decimal, person.Id));
                cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "psn_vrsn_id_in", DbType.Decimal, person.VersionId));

                // Languages
                person.Language = new List<PersonLanguage>();
                using (IDataReader rdr = cmd.ExecuteReader())
                    while (rdr.Read())
                        person.Language.Add(new PersonLanguage()
                        {
                            Language = Convert.ToString(rdr["lang_cs"]),
                            Type = (LanguageType)Convert.ToInt32(rdr["mode_cs"]),
                            UpdateMode = UpdateModeType.Ignore
                        });
            }
#if PERFMON
            Trace.TraceInformation("EO {0} : {1}", DateTime.Now.ToString("HH:mm:ss.ffff"), "get_psn_langs");
#endif
        }

        /// <summary>
        /// Get a person's addresses
        /// </summary>
        private void GetPersonAddresses(IDbConnection conn, IDbTransaction tx, Person person, bool loadFast)
        {
#if PERFMON
            Trace.TraceInformation("{0} : {1}", DateTime.Now.ToString("HH:mm:ss.ffff"), "get_psn_addr_sets");
#endif
            using (IDbCommand cmd = DbUtil.CreateCommandStoredProc(conn, tx))
            {
                cmd.CommandText = "get_psn_addr_sets";
                cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "psn_id_in", DbType.Decimal, person.Id));
                cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "psn_vrsn_id_in", DbType.Decimal, person.VersionId));

                // Addresses
                person.Addresses = new List<AddressSet>();
                using (IDataReader rdr = cmd.ExecuteReader())
                    while (rdr.Read())
                    {
                        person.Addresses.Add(new AddressSet()
                        {
                            UpdateMode = UpdateModeType.Ignore,
                            Key = Convert.ToDecimal(rdr["addr_set_id"]),
                            
                            Use = (AddressSet.AddressSetUse)Convert.ToInt32(rdr["addr_set_use"])
                        });
                    }

                // Detail load each address
                foreach (var addr in person.Addresses)
                {
                    var dtl = DbUtil.GetAddress(conn, tx, addr.Key, loadFast);
                    addr.Parts = dtl.Parts;
                    addr.EffectiveTime = dtl.EffectiveTime;
                    addr.ObsoleteTime = dtl.ObsoleteTime;
                }
            }
#if PERFMON
            Trace.TraceInformation("EO {0} : {1}", DateTime.Now.ToString("HH:mm:ss.ffff"), "get_psn_addr_sets");
#endif
        }

        /// <summary>
        /// Get person names
        /// </summary>
        private void GetPersonNames(IDbConnection conn, IDbTransaction tx, Person person, bool loadFast)
        {
#if PERFMON
            Trace.TraceInformation("{0} : {1}", DateTime.Now.ToString("HH:mm:ss.ffff"), "get_psn_name_sets");
#endif
            using (IDbCommand cmd = DbUtil.CreateCommandStoredProc(conn, tx))
            {
                cmd.CommandText = "get_psn_name_sets";
                cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "psn_id_in", DbType.Decimal, person.Id));
                cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "psn_vrsn_id_in", DbType.Decimal, person.VersionId));

                // Names
                person.Names = new List<NameSet>();
                using (IDataReader rdr = cmd.ExecuteReader())
                    while (rdr.Read())
                    {
                        person.Names.Add(new NameSet()
                        {
                            Key = Convert.ToDecimal(rdr["name_set_id"]),
                            UpdateMode = UpdateModeType.Ignore,
                            Use = (NameSet.NameSetUse)Convert.ToInt32(rdr["name_set_use"])
                        });
                    }

                // Detail load each address
                foreach (var name in person.Names)
                {
                    var dtl = DbUtil.GetName(conn, tx, name.Key, loadFast);
                    name.Parts = dtl.Parts;
                    name.EffectiveTime = dtl.EffectiveTime;
                    name.ObsoleteTime = dtl.ObsoleteTime;
                }
            }
#if PERFMON
            Trace.TraceInformation("EO {0} : {1}", DateTime.Now.ToString("HH:mm:ss.ffff"), "get_psn_name_sets");
#endif
        }

        /// <summary>
        /// De-persist an object with <paramref name="identifier"/> from the specified <paramref name="conn"/>
        /// placing it within the specified <paramref name="container"/> in the specified <paramref name="role"/>.
        /// When <paramref name="loadFast"/> is true, forgo advanced de-persisting such as prior versions, etc...
        /// </summary>
        public System.ComponentModel.IComponent DePersist(System.Data.IDbConnection conn, decimal identifier, System.ComponentModel.IContainer container, SVC.Core.ComponentModel.HealthServiceRecordSiteRoleType? role, bool loadFast)
        {

            // Prior versions need not apply!
            if (role.HasValue && role.Value == HealthServiceRecordSiteRoleType.OlderVersionOf && loadFast)
                return null;

            // De-persist a person
            var person = GetPerson(conn, null, new VersionedDomainIdentifier()
            {
                Domain = ApplicationContext.ConfigurationService.OidRegistrar.GetOid(ClientRegistryOids.CLIENT_CRID).Oid,
                Identifier = identifier.ToString()
            }, false);

            // De-persist components
            DbUtil.DePersistComponents(conn, person, this, loadFast);

            return person;
        }

        #endregion

        /// <summary>
        /// Merge two persons
        /// </summary>
        internal void MergePersons(Person newPerson, Person oldPerson)
        {
            MergePersons(newPerson, oldPerson, false);
        }

        /// <summary>
        /// Merge person records together ensuring that appropriate update modes are set. This 
        /// will clean newPerson to only include data which is changing. Data which remains the same
        /// will be removed from newPerson
        /// </summary>
        /// <param name="newerOnly">When true, do the merge based on newer keys rather than newPerson always overrides oldPerson</param>
        internal void MergePersons(Person newPerson, Person oldPerson, bool newerOnly)
        {
            Trace.TraceInformation("Copy person {0}v{1} into {2}", oldPerson.Id, oldPerson.VersionId, newPerson.Id);
            // Start the merging process for addresses
            // For each of the addresses in the new person record, determine if
            // they are additions (new addresses), modifications (old addresses 
            // with the same use) or removals (not in the new but in old)
            
            if (newPerson.Addresses != null && oldPerson.Addresses != null) 
            {
                foreach (var addr in newPerson.Addresses)
                {
                    if (addr.UpdateMode == UpdateModeType.Remove) continue;

                        UpdateModeType desiredUpdateMode = UpdateModeType.AddOrUpdate;
                        var candidateOtherAddress = oldPerson.Addresses.FindAll(o => o.Use == addr.Use);
                        if (candidateOtherAddress.Count == 1)
                        {
                            if (QueryUtil.MatchAddress(addr, candidateOtherAddress[0]) == 1) // Remove .. no change
                            {
                                //candidateOtherAddress[0].Key = -1;
                                addr.UpdateMode = UpdateModeType.Ignore;
                            }
                            else if (!newerOnly || candidateOtherAddress[0].Key < addr.Key)
                            {
                                addr.UpdateMode = UpdateModeType.Update;
                                addr.Key = candidateOtherAddress[0].Key;
                                //candidateOtherAddress[0].Key = -1;
                            }
                        }
                        else if (candidateOtherAddress.Count != 0)
                        {
                            // Find this address in a collection of same use addresses
                            var secondLevelFoundAddress = candidateOtherAddress.Find(o => QueryUtil.MatchAddress(o, addr) > 0.9);
                            if (secondLevelFoundAddress != null)
                            {
                                if (QueryUtil.MatchAddress(secondLevelFoundAddress, addr) == 1) // Exact match address, no change
                                    addr.UpdateMode = UpdateModeType.Ignore;
                                else if (!newerOnly || secondLevelFoundAddress.Key < addr.Key)
                                {
                                    addr.UpdateMode = UpdateModeType.Update;
                                    addr.Key = secondLevelFoundAddress.Key;
                                }
                            }
                            else
                                addr.UpdateMode = UpdateModeType.Add;
                            //secondLevelFoundAddress.Key = -1;
                        }
                        else // Couldn't find an address in the old in the new so it is an add
                        {
                            // Are we just changing the use?
                            var secondLevelFoundAddress = oldPerson.Addresses.Find(o => QueryUtil.MatchAddress(addr, o) == 1);
                            if (secondLevelFoundAddress == null)
                                addr.UpdateMode = UpdateModeType.Add;
                            else if (!newerOnly || secondLevelFoundAddress.Key < addr.Key)
                            {
                                // maybe an update (of the use) or a remove (if marked bad)

                                addr.Key = secondLevelFoundAddress.Key;
                                //secondLevelFoundAddress.Key = -1;
                                addr.UpdateMode = (addr.Use & AddressSet.AddressSetUse.BadAddress) == AddressSet.AddressSetUse.BadAddress ? UpdateModeType.Remove : UpdateModeType.Update;
                            }
                    }
                }

                //// Add all addresses in the old person that cannot be found in the new person to the list
                //// of addresses to remove
                //foreach (var addr in oldPerson.Addresses)
                //    if (addr.Key > 0)
                //        addr.UpdateMode = UpdateModeType.Remove;
                newPerson.Addresses.AddRange(oldPerson.Addresses.FindAll(o => o.UpdateMode == UpdateModeType.Remove));
                //newPerson.Addresses.RemoveAll(o => o.Key < 0);
            }

            // Next merge the citizenship information
            if (newPerson.Citizenship != null && oldPerson.Citizenship != null)
            {
                foreach (var cit in newPerson.Citizenship)
                {
                    if (cit.UpdateMode == UpdateModeType.Remove) continue;

                    var candidateOtherCit = oldPerson.Citizenship.Find(o => o.CountryCode == cit.CountryCode);
                    if (candidateOtherCit != null) // Matched on the name of the country, therefore it is an update
                    {
                        if(candidateOtherCit.Status == cit.Status) // no change
                            cit.Id = -2;
                        else if(!newerOnly || candidateOtherCit.Id < cit.Id)
                        {
                            
                            cit.UpdateMode = UpdateModeType.Update;
                            cit.Id = candidateOtherCit.Id;

                        }
                    }
                }

                //newPerson.Citizenship.RemoveAll(o => o.Id < 0);
            }

            // Next merge the employment information
            if (newPerson.Employment != null && oldPerson.Employment != null)
            {
                foreach (var emp in newPerson.Employment)
                {
                    if (emp.UpdateMode == UpdateModeType.Remove) continue;
                    var candidateOtherEmp = oldPerson.Employment.Find(o => o.Occupation == null && emp.Occupation == null || o.Occupation != null && emp.Occupation != null && o.Occupation.Code == emp.Occupation.Code && o.Occupation.CodeSystem == emp.Occupation.CodeSystem);
                    if (candidateOtherEmp != null) // Matched on the name of the country, therefore it is an update
                    {
                        if (candidateOtherEmp.Status == emp.Status) // no change
                            emp.Id = -2;
                        else if(!newerOnly || candidateOtherEmp.Id < emp.Id)
                        {
                            emp.UpdateMode = UpdateModeType.Update;
                            emp.Id = candidateOtherEmp.Id;
                        }
                    }
                }

                //newPerson.Employment.RemoveAll(o => o.Id < 0);
            }

            // Next we want to do the same for names
            if (newPerson.Names != null && oldPerson.Names != null)
            {
                foreach (var name in newPerson.Names)
                {
                    if (name == null || name.UpdateMode == UpdateModeType.Remove) continue;
                    var candidateOtherName = oldPerson.Names.FindAll(o => o.Use == name.Use);
                    if (candidateOtherName.Count == 1)
                    {
                        if (QueryUtil.MatchName(candidateOtherName[0], name) == 1) // No difference so no db operation
                        {
                            //candidateOtherName[0].Key = -1;
                            name.UpdateMode = UpdateModeType.Ignore;
                        }
                        else if(!newerOnly ||  candidateOtherName[0].Key < name.Key) // Need to update the contents of the name 
                        {
                            name.UpdateMode = UpdateModeType.Update;
                            name.Key = candidateOtherName[0].Key;
                            //candidateOtherName[0].Key = -1;
                        }
                    }
                    else if (candidateOtherName.Count != 0) // There are more than one name(s) which have the use, try to find a name that matches in content
                    {
                        // Find this name in a collection of same use names
                        var secondLevelFoundName = candidateOtherName.Find(o => QueryUtil.MatchName(o, name) >= DatabasePersistenceService.ValidationSettings.PersonNameMatch);

                        if (secondLevelFoundName != null)
                        {
                            if (QueryUtil.MatchName(secondLevelFoundName, name) == 1) // No change
                                name.UpdateMode = UpdateModeType.Ignore;
                            else if(!newerOnly || secondLevelFoundName.Key < name.Key)
                            {
                                name.UpdateMode = UpdateModeType.Update;
                                name.Key = secondLevelFoundName.Key;
                            }
                        }
                        else // Could not find a name that sufficiently matched
                            name.UpdateMode = UpdateModeType.Add;
                        
                        //secondLevelFoundName.Key = -1;
                    }
                    else // Couldn't find an name in the old in the new so it is an add or maybe remove?
                    {
                        // Are we just changing the use?
                        var secondLevelFoundName = oldPerson.Names.Find(o => QueryUtil.MatchName(name, o) == 1);
                        if (secondLevelFoundName == null) // Couldn't find a name that exactly matches (with different use) so it is an add
                            name.UpdateMode = UpdateModeType.Add;
                        else if(!newerOnly || secondLevelFoundName.Key < name.Key)
                        { // Found another name that exactly matches, this means it is an update or remove
                            name.Key = secondLevelFoundName.Key;
                            //secondLevelFoundName.Key = -1;
                            name.UpdateMode = UpdateModeType.Update;
                        }
                    }
                }

                // Add all name in the old person that cannot be found in the new person to the list
                // of name to remove
                //foreach (var name in oldPerson.Names)
                //    if (name.Key > 0)
                //        name.UpdateMode = UpdateModeType.Remove;
                //newPerson.Names.AddRange(oldPerson.Names.FindAll(o => o.UpdateMode == UpdateModeType.Remove));
                //newPerson.Names.RemoveAll(o => o.Key < 0);
                newPerson.Names.RemoveAll(o => o == null);
            }

            // Birth time
            if (newPerson.BirthTime != null && oldPerson.BirthTime != null &&
                newPerson.BirthTime.Value == oldPerson.BirthTime.Value)
                newPerson.BirthTime.UpdateMode = UpdateModeType.Ignore;

            // Religion code
            if (newPerson.ReligionCode != null && oldPerson.ReligionCode != null &&
                newPerson.ReligionCode.Code == oldPerson.ReligionCode.Code &&
                newPerson.ReligionCode.CodeSystem == oldPerson.ReligionCode.CodeSystem)
                newPerson.ReligionCode.UpdateMode = UpdateModeType.Ignore;

            // Deceased
            if (newPerson.DeceasedTime != null && oldPerson.DeceasedTime != null &&
                newPerson.DeceasedTime.Value == oldPerson.DeceasedTime.Value)
                newPerson.DeceasedTime.UpdateMode = UpdateModeType.Ignore;

            // Marital Status
            if (newPerson.MaritalStatus != null && oldPerson.MaritalStatus != null &&
                newPerson.MaritalStatus.Code == oldPerson.MaritalStatus.Code)
                newPerson.MaritalStatus.UpdateMode = UpdateModeType.Ignore;

            // Ethnicity Codes
            if (newPerson.EthnicGroup != null && oldPerson.EthnicGroup != null)
            {
                foreach (var rce in newPerson.EthnicGroup)
                {
                    if (rce.UpdateMode == UpdateModeType.Remove) continue;

                    var candidateRace = oldPerson.EthnicGroup.Find(o => o.CodeSystem == (rce.CodeSystem ?? "") && o.Code == rce.Code);
                    if (candidateRace != null) // New exists in the old
                    {
                        rce.UpdateMode = UpdateModeType.Ignore;
                        //candidateRace.Key = -1;
                    }
                    else
                        rce.UpdateMode = UpdateModeType.Add;
                }
                //newPerson.Race.RemoveAll(o => o.Key < 0);
            }

            // Race codes 
            if (newPerson.Race != null && oldPerson.Race != null)
            {
                foreach (var rce in newPerson.Race)
                {
                    if (rce.UpdateMode == UpdateModeType.Remove) continue;
                    var candidateRace = oldPerson.Race.Find(o => o.CodeSystem == (rce.CodeSystem ?? "") && o.Code == rce.Code);
                    if (candidateRace != null) // New exists in the old
                    {
                        rce.UpdateMode = UpdateModeType.Ignore;
                        //candidateRace.Key = -1;
                    }
                    else
                        rce.UpdateMode = UpdateModeType.Add;
                }
                //newPerson.Race.RemoveAll(o => o.Key < 0);
            }

            // Language codes
            if (newPerson.Language != null && oldPerson.Language != null)
            {
                List<PersonLanguage> garbagePail = new List<PersonLanguage>();
                foreach (var lang in newPerson.Language)
                {
                    if (lang.UpdateMode == UpdateModeType.Remove) continue;
                    var candidateLanguage = oldPerson.Language.Find(o => o.Language == lang.Language);
                    if (candidateLanguage != null) // New exists in the old
                    {
                        if (candidateLanguage.Type != lang.Type)
                            lang.UpdateMode = UpdateModeType.Update;
                        else
                            garbagePail.Add(lang); // Remove
                    }
                    else
                        lang.UpdateMode = UpdateModeType.Add;
                }
                foreach (var itm in garbagePail)
                    newPerson.Language.Remove(itm);

                // Find all race codes in the old that aren't in the new (remove)
                //foreach (var lang in oldPerson.Language)
                //    if (!newPerson.Language.Exists(o => o.Language == lang.Language))
                //        lang.UpdateMode = UpdateModeType.Remove;
                //newPerson.Language.AddRange(oldPerson.Language.FindAll(o => o.UpdateMode == UpdateModeType.Remove));
                //newPerson.Language.RemoveAll(o => garbagePail.Contains(o));
            }

            if (newPerson.TelecomAddresses != null && oldPerson.TelecomAddresses != null)
            {
                // Telecom addresses
                foreach (var tel in newPerson.TelecomAddresses)
                {
                    if (tel.UpdateMode == UpdateModeType.Remove) continue;
                    var candidateTel = oldPerson.TelecomAddresses.Find(o => o.Use == tel.Use && tel.Value == o.Value);
                    if (candidateTel != null) // New exists in the old
                    {
                        tel.UpdateMode = UpdateModeType.Ignore;
                        //candidateTel.Key = -1;
                    }
                    else
                    {
                        candidateTel = oldPerson.TelecomAddresses.Find(o => o.Value == tel.Value);
                        if (candidateTel == null)
                        {
                            // Same scheme and use? Then it is an update
                            candidateTel = oldPerson.TelecomAddresses.Find(o => o.Use == tel.Use && tel.Value.Contains(":") && o.Value.Contains(":") &&
                                tel.Value.Substring(0, tel.Value.IndexOf(":")) == o.Value.Substring(0, o.Value.IndexOf(":")));
                            if (candidateTel == null)
                                tel.UpdateMode = UpdateModeType.Add; // add
                            else
                            {
                                tel.UpdateMode = UpdateModeType.Update;
                                tel.Key = candidateTel.Key;
                            }
                        }
                        else if (!newerOnly || candidateTel.Key < tel.Key)
                        {
                            tel.UpdateMode = UpdateModeType.Update;
                            tel.Key = candidateTel.Key;
                        }
                    }
                }

                // Find all race codes in the old that aren't in the new (remove)
                //foreach (var alt in oldPerson.TelecomAddresses)
                //    if (alt.Key > 0)
                //        alt.UpdateMode = UpdateModeType.Remove;
                //newPerson.TelecomAddresses.AddRange(oldPerson.TelecomAddresses.FindAll(o => o.UpdateMode == UpdateModeType.Remove));
                //newPerson.TelecomAddresses.RemoveAll(o => o.Key < 0);
            }

            if (newPerson.AlternateIdentifiers != null && oldPerson.AlternateIdentifiers != null)
            {
                // Alternate identifiers
                foreach (var alt in newPerson.AlternateIdentifiers)
                {
                    if (alt.UpdateMode == UpdateModeType.Remove) continue;
                    var candidateAlt = oldPerson.AlternateIdentifiers.Find(o => o.Domain == alt.Domain && o.Identifier == alt.Identifier);
                    if (candidateAlt != null) // New exists in the old
                    {
                        alt.UpdateMode = UpdateModeType.Ignore;
                        //candidateAlt.Key = -1;
                    }
                    else
                    {
                        // Subsumption?
                        //candidateAlt = oldPerson.AlternateIdentifiers.Find(o => o.Domain == alt.Domain);
                        //if (candidateAlt != null && (!newerOnly || candidateAlt.Key < alt.Key))
                        //{
                        ////    // Remove the old alt id
                        //      candidateAlt.UpdateMode = UpdateModeType.Remove;
                        ////    // Add the new
                        ////    // Send an duplicates resolved message
                        //      //IClientNotificationService notificationService = ApplicationContext.CurrentContext.GetService(typeof(IClientNotificationService)) as IClientNotificationService;
                        //      //if (notificationService != null)
                        //      //  notificationService.NotifyDuplicatesResolved(newPerson, candidateAlt);
                        //}
                        //else
                            alt.UpdateMode = UpdateModeType.Add;
                    }
                }

                // Find all race codes in the old that aren't in the new (remove)
                //foreach (var alt in oldPerson.AlternateIdentifiers)
                //    if (alt.Key > 0)
                //        alt.UpdateMode = UpdateModeType.Remove;
                newPerson.AlternateIdentifiers.AddRange(oldPerson.AlternateIdentifiers.FindAll(o => o.UpdateMode == UpdateModeType.Remove));
                //newPerson.AlternateIdentifiers.RemoveAll(o => o.Key < 0);
            }

            if (newPerson.OtherIdentifiers != null && oldPerson.OtherIdentifiers != null)
            {
                // Other identifiers
                foreach (var alt in newPerson.OtherIdentifiers)
                {
                    if (alt.Value.UpdateMode == UpdateModeType.Remove) continue;
                    var candidateAlt = oldPerson.OtherIdentifiers.Find(o => o.Key != null && alt.Key != null && o.Key.Code == alt.Key.Code && o.Key.CodeSystem == alt.Key.CodeSystem);
                    if (!candidateAlt.Equals(default(KeyValuePair<CodeValue, DomainIdentifier>))) // New exists in the old
                    {
                        // Found based on the code, so this is an update
                        if (alt.Value.Identifier == candidateAlt.Value.Identifier &&
                            alt.Value.Domain == candidateAlt.Value.Identifier)
                            alt.Value.UpdateMode = UpdateModeType.Ignore;
                        else
                            alt.Value.UpdateMode = UpdateModeType.Update;
                        //candidateAlt.Value.Key = -1;
                    }
                    else
                    {
                        candidateAlt = oldPerson.OtherIdentifiers.Find(o => o.Value.Identifier == alt.Value.Identifier && o.Value.Domain == alt.Value.Domain);
                        if (!candidateAlt.Equals(default(KeyValuePair<CodeValue, DomainIdentifier>))) // New exists in the old
                        {
                            // Found other based on identifier so may be an update
                            if (alt.Key != candidateAlt.Key)
                                alt.Value.UpdateMode = UpdateModeType.Update;
                            else
                                alt.Value.UpdateMode = UpdateModeType.Ignore;
                            //candidateAlt.Value.Key = -1;
                        }
                        else
                            alt.Value.UpdateMode = UpdateModeType.Add;
                    }
                }
                // Find all race codes in the old that aren't in the new (remove)
                //foreach (var alt in oldPerson.OtherIdentifiers)
                //    if (alt.Value.Key > 0)
                //        alt.Value.UpdateMode = UpdateModeType.Remove;
                //newPerson.OtherIdentifiers.AddRange(oldPerson.OtherIdentifiers.FindAll(o => o.Value.UpdateMode == UpdateModeType.Remove));
                //newPerson.OtherIdentifiers.RemoveAll(o => o.Value.Key < 0);
            }

            // Copy over extended attributes not mentioned in the new person
            var newPsnRltnshps = newPerson.FindAllComponents(HealthServiceRecordSiteRoleType.RepresentitiveOf).FindAll(o=>o is PersonalRelationship);
            
            foreach (HealthServiceRecordComponent cmp in oldPerson.Components)
                if (cmp is ExtendedAttribute && newPerson.FindExtension(o => o.PropertyPath == (cmp as ExtendedAttribute).PropertyPath && o.Name == (cmp as ExtendedAttribute).Name) == null)
                {
                    newPerson.Add(cmp, cmp.Site.Name, (cmp.Site as HealthServiceRecordSite).SiteRoleType, null);
                }
                else if (cmp is PersonalRelationship)
                {
                    var oldPsnRltnshp = cmp as PersonalRelationship;
                    
                    var newPsnRltnshpMatch = newPsnRltnshps.Find(o =>
                        (o as PersonalRelationship).RelationshipKind == oldPsnRltnshp.RelationshipKind && PersonalRelationshipPersister.NON_DUPLICATE_REL.Contains(oldPsnRltnshp.RelationshipKind) && (o as PersonalRelationship).LegalName?.Parts.FirstOrDefault().Value != oldPsnRltnshp.LegalName?.Parts.FirstOrDefault().Value

                    );
                    
                    if (newPsnRltnshpMatch == null) // Need to copy?
                        newPerson.Add(cmp, cmp.Site.Name ?? Guid.NewGuid().ToString(), HealthServiceRecordSiteRoleType.RepresentitiveOf, null);
                    else if ((newPsnRltnshpMatch as PersonalRelationship).Id == default(Decimal))// HACK: Massage the IDs
                    {
                        (newPsnRltnshpMatch as PersonalRelationship).Id = oldPsnRltnshp.Id;
                    }
                }
            // Add a relationship of person reference
            //newPerson.Add(oldPerson, "OBSLT", HealthServiceRecordSiteRoleType.OlderVersionOf, null);
        }

        #region IVersionComponentPersister Members

        /// <summary>
        /// Get a specific version of the patient
        /// </summary>
        public System.ComponentModel.IComponent DePersist(IDbConnection conn, decimal identifier, decimal versionId, System.ComponentModel.IContainer container, HealthServiceRecordSiteRoleType? role, bool loadFast)
        {
            var person = this.GetPerson(conn, null, new VersionedDomainIdentifier()
            {
                Domain = ApplicationContext.ConfigurationService.OidRegistrar.GetOid(ClientRegistryOids.CLIENT_CRID).Oid,
                Identifier = identifier.ToString(),
                Version = versionId.ToString()
            }, loadFast);
            DbUtil.DePersistComponents(conn, person, this, true);
            return person;
        }

        #endregion

        #region IQueryComponentPersister Members

        /// <summary>
        /// Build a filter
        /// </summary>
        public string BuildFilter(System.ComponentModel.IComponent data, bool forceExact)
        {
            
            // Person filter
            var personFilter = data as Person;

            if (personFilter.Addresses == null &&
                (personFilter.AlternateIdentifiers == null  || personFilter.AlternateIdentifiers.Count == 0) &&
                !personFilter.BirthOrder.HasValue &&
                personFilter.BirthPlace == null &&
                personFilter.BirthTime == null &&
                personFilter.Citizenship == null &&
                personFilter.DeceasedTime == null &&
                personFilter.Employment == null &&
                (personFilter.EthnicGroup == null || personFilter.EthnicGroup.Count == 0) &&
                personFilter.GenderCode == null &&
                personFilter.Id == default(decimal) &&
                personFilter.Language == null &&
                personFilter.MaritalStatus == null &&
                personFilter.MothersName == null &&
                personFilter.Names == null &&
                personFilter.OtherIdentifiers == null &&
                personFilter.Race == null &&
                personFilter.ReligionCode == null &&
                personFilter.TelecomAddresses == null &&
                personFilter.VipCode == null)
                throw new ConstraintException(ApplicationContext.LocaleService.GetString("MSGE077"));
            var registrationEvent = DbUtil.GetRegistrationEvent(data);
            var queryEvent = personFilter.Site.Container as HealthServiceRecordContainer;
            while (!(queryEvent is QueryEvent) && queryEvent != null && queryEvent.Site != null)
                queryEvent = queryEvent.Site.Container as HealthServiceRecordContainer;

            QueryParameters queryFilter = queryEvent.FindComponent(HealthServiceRecordSiteRoleType.FilterOf) as QueryParameters;

            // Get the registration event's filter parameters (master filter specificity)
            if(registrationEvent != null && queryFilter == null)
                queryFilter = (registrationEvent as HealthServiceRecordContainer).FindComponent(HealthServiceRecordSiteRoleType.FilterOf) as QueryParameters;                
            
            // Query Filter
            if (queryFilter == null || queryFilter.MatchingAlgorithm == MatchAlgorithm.Unspecified)
                queryFilter = new QueryParameters()
                {
                    MatchStrength = DatabasePersistenceService.ValidationSettings.DefaultMatchStrength,
                    MatchingAlgorithm = DatabasePersistenceService.ValidationSettings.DefaultMatchAlgorithms
                };


            
            // Matching?
            StringBuilder sb = new StringBuilder();
            if(registrationEvent != null) // There should be no query parameters added
                sb.Append("SELECT DISTINCT HSR_ID, HSR_VRSN_ID FROM PSN_VRSN_TBL INNER JOIN HSR_VRSN_TBL ON (HSR_VRSN_TBL.HSR_VRSN_ID = PSN_VRSN_TBL.REG_VRSN_ID) ");
            else
                sb.Append("SELECT DISTINCT PSN_VRSN_TBL.PSN_ID, PSN_VRSN_TBL.PSN_VRSN_ID FROM PSN_VRSN_TBL ");

            Stack<String> subqueryParms = new Stack<string>();

            // Identifiers
            if (personFilter.Id != default(decimal))
            {
                sb.AppendFormat(" WHERE PSN_VRSN_TBL.PSN_ID = {0} ", personFilter.Id);
                return sb.ToString();
            }
            else if (personFilter.AlternateIdentifiers != null && personFilter.AlternateIdentifiers.Count > 0)
                subqueryParms.Push(BuildFilterIdentifiers(personFilter.AlternateIdentifiers, registrationEvent == null || registrationEvent.EventClassifier == RegistrationEventType.Query ? "UNION" : "INTERSECT"));

            #region Join Parameters Conditions

            // Match names
            if (personFilter.Names != null && personFilter.Names.Count > 0)
                subqueryParms.Push(BuildFilterNames(personFilter.Names, !forceExact ? queryFilter : new QueryParameters() { MatchingAlgorithm = MatchAlgorithm.Exact }));

            // Match birth time
            if (personFilter.BirthTime != null)
                subqueryParms.Push(String.Format("SELECT PSN_ID FROM FIND_PSN_BY_BRTH_TS('{0:yyyy-MM-dd HH:mm:sszz}','{1}')", personFilter.BirthTime.Value, personFilter.BirthTime.Precision));

            // Telecom Addresses
            if (personFilter.TelecomAddresses != null && personFilter.TelecomAddresses.Count > 0)
                subqueryParms.Push(BuildFilterTelecom(personFilter.TelecomAddresses));


            // is this a simple query?
            if (subqueryParms.Count(o => o.Contains(" UNION ") || o.Contains(" INTERSECT ")) > 0) // Complex
            {
                Trace.TraceWarning("PERFORMANCE: QUERY CONTAINS UNION/INTERSECT (OR/AND SEMANTICS) WHICH FORCES THE QUERY ANALYZER TO USE A SLOWER QUERY MECHANISM");
                // Fallback to old ways
                sb.Append(" WHERE PSN_VRSN_TBL.OBSLT_UTC IS NULL ");
                while (subqueryParms.Count > 0)
                    sb.AppendFormat(" AND PSN_VRSN_TBL.PSN_ID IN ({0}) ", subqueryParms.Pop());
            }
            else
            {
                Trace.TraceInformation("PERFORMANCE: USING FAST QUERY SEMANTICS");
                bool hasSubquery = subqueryParms.Count > 0;
                string closeBrace = "";
                if (hasSubquery)
                    sb.Append("INNER JOIN (");

                // build the join condition
                while (subqueryParms.Count > 0)
                {
                    string subQueryCondition = subqueryParms.Pop();

                    if (subqueryParms.Count > 0) //more so we want to join internal?
                    {
                        sb.AppendFormat("{0}, ARRAY(", subQueryCondition.Substring(0, subQueryCondition.Length - 1));
                        closeBrace += "))";
                    }
                    else
                        sb.AppendFormat("{0}{1}", subQueryCondition, closeBrace);
                }

                // finish join
                if (hasSubquery)
                    sb.Append(") AS SUBQ ON (SUBQ.PSN_ID = PSN_VRSN_TBL.PSN_ID) WHERE PSN_VRSN_TBL.OBSLT_UTC IS NULL ");
                else
                    sb.Append(" WHERE PSN_VRSN_TBL.OBSLT_UTC IS NULL ");
            }

            #endregion

            #region Filter Parameters

            if (personFilter.Status == StatusType.Unknown)
                sb.Append("AND PSN_VRSN_TBL.STATUS_CS_ID NOT IN (16,64) ");
            else
            {
                sb.Append("AND PSN_VRSN_TBL.STATUS_CS_ID IN (");
                foreach (var fi in Enum.GetValues(typeof(StatusType)))
                    if ((personFilter.Status & (StatusType)fi) != 0)
                        sb.AppendFormat("{0},", (int)fi);
                sb.Remove(sb.Length - 1, 1);
                sb.Append(") ");
            }


            // Addresses
            if (personFilter.Addresses != null && personFilter.Addresses.Count > 0)
                sb.AppendFormat("AND PSN_VRSN_TBL.PSN_ID IN ({0}) ", BuildFilterAddress(personFilter.Addresses));

            // Other Identifiers
            if (personFilter.OtherIdentifiers != null && personFilter.OtherIdentifiers.Count > 0)
                sb.AppendFormat("AND PSN_VRSN_TBL.PSN_ID IN ({0})", BuildFilterIdentifiers(personFilter.OtherIdentifiers));


            if (personFilter.RoleCode != (PersonRole.PAT | PersonRole.PRS))
                sb.AppendFormat("AND ROL_CS = '{0}' ", personFilter.RoleCode.ToString());

            // Mutliple
            if (personFilter.BirthOrder.HasValue)
                sb.AppendFormat("AND MB_ORD = {0} ", personFilter.BirthOrder);

            // Gender
            if (!String.IsNullOrEmpty(personFilter.GenderCode))
                sb.AppendFormat("AND GNDR_CS = '{0}' ", personFilter.GenderCode.Replace("'","''"));

            #endregion 

            if(data.Site == null)
                sb.Append(" ORDER BY PSN_VRSN_ID DESC");


            // Now output the query
            return sb.ToString();
        }

        /// <summary>
        /// Get the component type Oid
        /// </summary>
        public string ComponentTypeOid
        {
            get { return ApplicationContext.ConfigurationService.OidRegistrar.GetOid(ClientRegistryOids.CLIENT_CRID).Oid; }
        }

        #endregion



        /// <summary>
        /// Filter telecommunications address
        /// </summary>
        private string BuildFilterTelecom(List<TelecommunicationsAddress> telecoms)
        {
            StringBuilder retVal = new StringBuilder();
            foreach (var tel in telecoms)
            {
                if (tel.Value.Contains('*'))
                    retVal.AppendFormat("SELECT PSN_ID FROM FIND_PSN_BY_TEL_LIKE('{0}',", tel.Value.Replace("*", "%").Replace("'", "''"));
                else
                    retVal.AppendFormat("SELECT PSN_ID FROM FIND_PSN_BY_TEL('{0}',", tel.Value.Replace("'","''"));
                if (tel.Use != null)
                    retVal.AppendFormat("'{0}')", tel.Use);
                else
                    retVal.AppendFormat("NULL)");
                if (!tel.Equals(telecoms.Last()))
                    retVal.AppendFormat(" UNION ");
            }
            return retVal.ToString();
        }

        /// <summary>
        /// Build a filter for addresses
        /// </summary>
        private string BuildFilterAddress(List<AddressSet> addresses)
        {
            StringBuilder retVal = new StringBuilder();
            foreach (var addr in addresses)
            {
                if (addr == null)
                    continue;
                // Build the filter
                StringBuilder filterString = new StringBuilder(),
                    cmpTypeString = new StringBuilder(),
                    addrStateCondition = new StringBuilder();


                int ncf = 0;
                foreach (var cmp in addr.Parts)
                {
                    if(cmp.PartType == AddressPart.AddressPartType.State || cmp.PartType == AddressPart.AddressPartType.Country)
                    {
                        addrStateCondition.AppendFormat(" AND EXISTS(SELECT ADDR_SET_ID FROM ADDR_CMP_TBL AS B INNER JOIN ADDR_CDTBL C ON (C.ADDR_ID = B.ADDR_CMP_VALUE) WHERE B.ADDR_SET_ID = ADDR_SET_ID AND ADDR_VALUE ILIKE '{0}' AND ADDR_CMP_CLS = {1})", cmp.AddressValue.Replace("'","''"), (int)cmp.PartType);
                        continue;
                    }
                    ncf++;
                    filterString.AppendFormat("(ADDR_VALUE ILIKE '{0}') {1} ", cmp.AddressValue.Replace("'", "''").Replace("*", "%"), cmp == addr.Parts.Last() ? "" : "OR");
                }
                if (filterString.ToString().EndsWith("OR "))
                    filterString.Remove(filterString.Length - 3, 3);

                // Match strength & algorithms
                retVal.AppendFormat("( SELECT PSN_ID FROM PSN_ADDR_SET_TBL WHERE PSN_ADDR_SET_TBL.PSN_ID = PSN_VRSN_TBL.PSN_ID AND ADDR_SET_ID IN (SELECT ADDR_SET_ID FROM ADDR_CMP_TBL INNER JOIN ADDR_CDTBL ON (ADDR_ID = ADDR_CMP_VALUE) WHERE PSN_ADDR_SET_TBL.ADDR_SET_ID = ADDR_CMP_TBL.ADDR_SET_ID AND ({0}) AND OBSLT_VRSN_ID IS NULL {1} GROUP BY ADDR_CMP_TBL.ADDR_SET_ID HAVING COUNT(ADDR_CMP_ID) = {2} {3}))",
                    filterString, addr.Use == AddressSet.AddressSetUse.Search ? null : String.Format("AND ADDR_SET_USE = {0}", (int)addr.Use), 
                    ncf,
                    addrStateCondition);

                if (addr != addresses.Last())
                    retVal.AppendFormat(" UNION ");
            }
            return retVal.ToString();
        }

        /// <summary>
        /// Build filter identifiers
        /// </summary>
        private string BuildFilterIdentifiers(List<KeyValuePair<CodeValue, DomainIdentifier>> identifiers)
        {
            StringBuilder retVal = new StringBuilder();
            foreach (var id in identifiers)
            {
                if (!String.IsNullOrEmpty(id.Value.Identifier))
                    retVal.AppendFormat("SELECT PSN_ID FROM GET_PSN_EXTERN('{0}','{1}')", id.Value.Domain.Replace("'", "''"), id.Value.Identifier.Replace("'", "''"), identifiers.Count * 4);
                else
                    retVal.AppendFormat("SELECT PSN_ID FROM FIND_PSN_EXTERN('{0}')", id.Value.Domain.Replace("'", "''"), identifiers.Count * 4);
                if (!id.Equals(identifiers.Last()))
                    retVal.AppendFormat(" UNION ");
            }
            return retVal.ToString();
        }

        /// <summary>
        /// Build filter for names
        /// </summary>
        private string BuildFilterNames(List<NameSet> names, QueryParameters parameters)
        {
            StringBuilder retVal = new StringBuilder();
            foreach (var nm in names)
            {
                if (nm == null)
                    continue;
                // Build the filter
                StringBuilder filterString = new StringBuilder(),
                    cmpTypeString = new StringBuilder();
                foreach (var cmp in nm.Parts)
                {

                    filterString.AppendFormat("{0}{1}", cmp.Value.Replace("%", "").Replace("*", "%"), cmp == nm.Parts.Last() ? "" : ",");
                    cmpTypeString.AppendFormat("{0}{1}", (decimal)cmp.Type, cmp == nm.Parts.Last() ? "" : ",");
                }

                // Match strength & algorithms
                int desiredMatchLevel = 6;
                bool useVariant = false;
                if (nm.Use == NameSet.NameSetUse.Search)
                {
                    useVariant = (parameters.MatchingAlgorithm & MatchAlgorithm.Variant) != 0;
                    if ((parameters.MatchingAlgorithm & MatchAlgorithm.Soundex) != 0) // no soundex is allowed so exact only
                        desiredMatchLevel = 4;
                    else
                        desiredMatchLevel = 5;
                }

                retVal.AppendFormat("SELECT PSN_ID FROM FIND_PSN_BY_NAME_SET('{{{0}}}','{{{1}}}', {3}, {4}, {2})",
                    filterString.Replace("'", "''"), cmpTypeString, nm.Use == NameSet.NameSetUse.Search ? (object)"NULL" : (decimal)nm.Use, desiredMatchLevel, useVariant);

                if (nm != names.Last())
                    retVal.AppendFormat(" UNION ");
            }
            return retVal.ToString();
        }



        /// <summary>
        /// Build filter on identifiers
        /// </summary>
        private string BuildFilterIdentifiers(List<DomainIdentifier> identifiers, String mode)
        {
            StringBuilder retVal = new StringBuilder();
            foreach (var id in identifiers)
            {
                if (id.Domain != ApplicationContext.ConfigurationService.OidRegistrar.GetOid(ClientRegistryOids.CLIENT_CRID).Oid)
                {
                    if (!String.IsNullOrEmpty(id.Identifier))
                    {
                        if(id.Domain == null)
                            retVal.AppendFormat("SELECT PSN_ID FROM GET_PSN_EXTERN(NULL,'{0}')", id.Identifier.Replace("'", "''"), identifiers.Count * 4);
                        else
                            retVal.AppendFormat("SELECT PSN_ID FROM GET_PSN_EXTERN('{0}','{1}')", id.Domain.Replace("'", "''"), id.Identifier.Replace("'", "''"), identifiers.Count * 4);
                    }
                    else
                        retVal.AppendFormat("SELECT PSN_ID FROM FIND_PSN_EXTERN('{0}')", id.Domain.Replace("'", "''"));

                }
                else
                {
                    decimal localId = 0;
                    if (String.IsNullOrEmpty(id.Identifier))
                        retVal.AppendFormat("SELECT PSN_ID FROM PSN_TBL");
                    else if (Decimal.TryParse(id.Identifier, out localId))
                        retVal.AppendFormat("SELECT {0} AS PSN_ID", localId); // look for one id
                    else
                        throw new InvalidOperationException("Invalid CR_CID domain identifier");
                }
                if (id != identifiers.Last())
                {
                    retVal.AppendFormat(" {0} ", mode);
                }
            }
            return retVal.ToString();
        }


        /// <summary>
        /// Build control clause
        /// </summary>
        public string BuildControlClauses(System.ComponentModel.IComponent queryComponent)
        {
            return "";
        }
    }
}
