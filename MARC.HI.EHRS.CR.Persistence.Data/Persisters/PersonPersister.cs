using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MARC.HI.EHRS.CR.Persistence.Data.ComponentPersister;
using MARC.HI.EHRS.CR.Core.ComponentModel;
using System.Data;
using MARC.HI.EHRS.SVC.Core.DataTypes;
using System.Diagnostics;

namespace MARC.HI.EHRS.CR.Persistence.Data.ComponentPersister
{
    /// <summary>
    /// Persister that is responsible for the persisting of a person
    /// </summary>
    public class PersonPersister : IComponentPersister
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
                // Start by persisting the person component
                if (isUpdate)
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

                return new VersionedDomainIdentifier()
                {
                    Identifier = psn.Id.ToString(),
                    Version = psn.VersionId.ToString(),
                    Domain = ClientRegistryOids.CLIENT_CRID
                };
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
                cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "status_in", DbType.String, psn.Status.ToString()));
                cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "gndr_in", DbType.String, psn.GenderCode));
                cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "brth_ts_in", DbType.Decimal, DbUtil.CreateTimestamp(conn, tx, psn.BirthTime, null)));
                cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "mb_ord_in", DbType.Decimal, psn.BirthOrder.HasValue ? (object)psn.BirthOrder.Value : DBNull.Value));

                decimal? religionCode = null;
                if (psn.ReligionCode != null)
                    religionCode = DbUtil.CreateCodedValue(conn, tx, psn.ReligionCode);

                cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "rlgn_cd_id_in", DbType.Decimal, religionCode.HasValue ? (object)religionCode.Value : DBNull.Value));

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

            // Persist person language
            if (psn.Language != null)
                foreach (var lang in psn.Language)
                    this.PersistPersonLanguage(conn, tx, psn, lang);

            // TODO: Components
            DbUtil.PersistComponents(conn, tx, false, this, psn);
        }

        /// <summary>
        /// Persist a person's language
        /// </summary>
        private void PersistPersonLanguage(IDbConnection conn, IDbTransaction tx, Person psn, PersonLanguage lang)
        {
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
                using (IDbCommand cmd = DbUtil.CreateCommandStoredProc(conn, tx))
                {

                    decimal codeId = DbUtil.CreateCodedValue(conn, tx, othId.Key);

                    cmd.CommandText = "crt_psn_alt_id";

                    cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "psn_id_in", DbType.Decimal, psn.Id));
                    cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "psn_vrsn_id_in", DbType.Decimal, psn.VersionId));
                    cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "is_hcn_in", DbType.Boolean, false));
                    cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "id_purp_in", DbType.String, codeId));
                    cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "id_domain_in", DbType.String, othId.Value.Domain));
                    cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "id_value_in", DbType.String, othId.Value.Identifier));

                    // Execute
                    cmd.ExecuteNonQuery();

                }
        }

        /// <summary>
        /// Persist an alternate identifier
        /// </summary>
        private void PersistPersonAlternateIdentifier(IDbConnection conn, IDbTransaction tx, Person psn, DomainIdentifier altId)
        {
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

            // Add telecom
            if (altId.UpdateMode != UpdateModeType.Remove)
                using (IDbCommand cmd = DbUtil.CreateCommandStoredProc(conn, tx))
                {
                    cmd.CommandText = "crt_psn_alt_id";
                    cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "psn_id_in", DbType.Decimal, psn.Id));
                    cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "psn_vrsn_id_in", DbType.Decimal, psn.VersionId));
                    cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "is_hcn_in", DbType.Boolean, true));
                    cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "id_purp_in", DbType.Decimal, DBNull.Value));
                    cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "id_domain_in", DbType.String, altId.Domain));
                    cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "id_value_in", DbType.String, altId.Identifier));

                    // Execute
                    cmd.ExecuteNonQuery();

                }
        }

        /// <summary>
        /// Persist a person's telecommunications address
        /// </summary>
        private void PersistPersonTelecom(IDbConnection conn, IDbTransaction tx, Person psn, TelecommunicationsAddress tel)
        {
            // Update or add or update? we first have to obsolete the existing
            if (tel.UpdateMode == UpdateModeType.Remove || tel.UpdateMode == UpdateModeType.Update || tel.UpdateMode == UpdateModeType.AddOrUpdate)
                using (IDbCommand cmd = DbUtil.CreateCommandStoredProc(conn, tx))
                {
                    cmd.CommandText = "obslt_psn_tel";
                    cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "psn_id_in", DbType.Decimal, psn.Id));
                    cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "tel_value_in", DbType.String, tel.Value));
                    cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "tel_use_in", DbType.String, tel.Use));
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
                    cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "telecom_cap_in", DbType.String, DBNull.Value));
                    
                    // Execute
                    tel.Key = Convert.ToDecimal(cmd.ExecuteScalar());

                }
        }

        /// <summary>
        /// Persist person names
        /// </summary>
        private void PersistPersonNames(IDbConnection conn, IDbTransaction tx, Person psn, NameSet name)
        {
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
                    cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "name_set_use_in", DbType.String, name.Use));

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
                    cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "addr_set_use_in", DbType.String, addr.Use));

                    // Execute
                    addr.Key = Convert.ToDecimal(cmd.ExecuteScalar());

                    // Next we'll clean and persist the components
                    DbUtil.CreateAddressSet(conn, tx, addr);
                }
        }

        /// <summary>
        /// Create a new version of a patient record
        /// </summary>
        private void CreatePersonVersion(IDbConnection conn, IDbTransaction tx, Person psn)
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
                cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "status_in", DbType.String, psn.Status.ToString()));
                cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "gndr_in", DbType.String, psn.GenderCode));
                cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "brth_ts_in", DbType.Decimal, psn.BirthTime != null ? (object)DbUtil.CreateTimestamp(conn, tx, psn.BirthTime, null) : DBNull.Value));
                cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "dcsd_ts_in", DbType.Decimal, psn.DeceasedTime != null ? (object)DbUtil.CreateTimestamp(conn, tx, psn.DeceasedTime, null) : DBNull.Value));
                cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "mb_ord_in", DbType.Decimal, psn.BirthOrder.HasValue ? (object)psn.BirthOrder.Value : DBNull.Value));
                
                decimal? religionCode = null;
                if (psn.ReligionCode != null)
                    religionCode = DbUtil.CreateCodedValue(conn, tx, psn.ReligionCode);

                cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "rlgn_cd_id_in", DbType.Decimal, religionCode.HasValue ? (object)religionCode.Value : DBNull.Value));

                // Execute
                
                psn.VersionId = Convert.ToDecimal(cmd.ExecuteScalar());

                // Persist the components of the 
                this.PersistPersonComponents(conn, tx, psn, UpdateModeType.AddOrUpdate);

 
            }
        }

        /// <summary>
        /// Get a person from the database
        /// </summary>
        internal Person GetPerson(IDbConnection conn, IDbTransaction tx, DomainIdentifier domainIdentifier)
        {
            return null;
        }

        /// <summary>
        /// De-persist an object with <paramref name="identifier"/> from the specified <paramref name="conn"/>
        /// placing it within the specified <paramref name="container"/> in the specified <paramref name="role"/>.
        /// When <paramref name="loadFast"/> is true, forgo advanced de-persisting such as prior versions, etc...
        /// </summary>
        public System.ComponentModel.IComponent DePersist(System.Data.IDbConnection conn, decimal identifier, System.ComponentModel.IContainer container, SVC.Core.ComponentModel.HealthServiceRecordSiteRoleType? role, bool loadFast)
        {
            throw new NotImplementedException();
        }

        #endregion

        /// <summary>
        /// Merge person records together ensuring that appropriate update modes are set. This 
        /// will clean newPerson to only include data which is changing. Data which remains the same
        /// will be removed from newPerson
        /// </summary>
        internal void MergePersons(Person newPerson, Person oldPerson)
        {

        }
    }
}
