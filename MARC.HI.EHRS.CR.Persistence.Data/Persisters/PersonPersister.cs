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
                cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "brth_ts_in", DbType.Decimal, psn.BirthTime == null ? DBNull.Value : (object)DbUtil.CreateTimestamp(conn, tx, psn.BirthTime, null)));
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
            if (lang == null) return; // skip

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
            if (race == null) return; // skip

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
            if (othId.Equals(default(KeyValuePair<CodeValue, DomainIdentifier>))) return; // skip

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

                        decimal codeId = DbUtil.CreateCodedValue(conn, tx, othId.Key);

                        cmd.CommandText = "crt_psn_alt_id";

                        cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "psn_id_in", DbType.Decimal, psn.Id));
                        cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "psn_vrsn_id_in", DbType.Decimal, psn.VersionId));
                        cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "is_hcn_in", DbType.Boolean, false));
                        cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "id_purp_in", DbType.Decimal, codeId));
                        cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "id_domain_in", DbType.String, othId.Value.Domain));
                        cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "id_value_in", DbType.String, othId.Value.Identifier));

                        // Execute
                        cmd.ExecuteNonQuery();

                    }
                }
                catch 
                { 
                    throw new DuplicateNameException(ApplicationContext.LocaleService.GetString("DBCF008"));
                }
        }

        /// <summary>
        /// Persist an alternate identifier
        /// </summary>
        private void PersistPersonAlternateIdentifier(IDbConnection conn, IDbTransaction tx, Person psn, DomainIdentifier altId)
        {
            if (altId == null) return; // skip

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
                try
                {
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
                catch
                {
                    throw new DuplicateNameException(ApplicationContext.LocaleService.GetString("DBCF008"));
                }
        }

        /// <summary>
        /// Persist a person's telecommunications address
        /// </summary>
        private void PersistPersonTelecom(IDbConnection conn, IDbTransaction tx, Person psn, TelecommunicationsAddress tel)
        {
            if (tel == null) return; // skip

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
            if (name == null) return; // skip

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
            if (addr == null) return; // skip

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
        /// Get a person's most recent version
        /// </summary>
        internal Person GetPerson(IDbConnection conn, IDbTransaction tx, DomainIdentifier domainIdentifier)
        {
            return GetPerson(conn, tx, new VersionedDomainIdentifier()
            {
                Domain = domainIdentifier.Domain,
                Identifier = domainIdentifier.Identifier
            });
        }

        /// <summary>
        /// Get a person from the database
        /// </summary>
        internal Person GetPerson(IDbConnection conn, IDbTransaction tx, VersionedDomainIdentifier domainIdentifier)
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

                // Execute the command
                using (IDataReader rdr = cmd.ExecuteReader())
                {
                    if (rdr.Read())
                    {
                        Person retVal = new Person();
                        retVal.Id = Convert.ToDecimal(rdr["psn_id"]);
                        retVal.VersionId = Convert.ToDecimal(rdr["psn_vrsn_id"]);
                        retVal.Status = (StatusType)Enum.Parse(typeof(StatusType), rdr["status"].ToString());
                        retVal.GenderCode = Convert.ToString(rdr["gndr_cs"]);
                        retVal.BirthOrder = (int?)(rdr["mb_ord"] == DBNull.Value ? (object)null : Convert.ToInt32(rdr["mb_ord"]));

                        // Other fetched data
                        decimal? birthTs = null,
                            deceasedTs = null,
                            religionCode = null;
                        
                        if (rdr["brth_ts"] != DBNull.Value)
                            birthTs = Convert.ToDecimal(rdr["brth_ts"]);
                        if (rdr["dcsd_ts"] != DBNull.Value)
                            deceasedTs = Convert.ToDecimal(rdr["dcsd_ts"]);
                        if (rdr["rlgn_cd_id"] != DBNull.Value)
                            religionCode = Convert.ToDecimal(rdr["rlgn_cd_id"]);

                        // Close the reader and read dependent values
                        rdr.Close();

                        // Load immediate values
                        if (birthTs.HasValue)
                            retVal.BirthTime = DbUtil.GetEffectiveTimestampSet(conn, tx, birthTs.Value).Parts[0];
                        if (deceasedTs.HasValue)
                            retVal.DeceasedTime = DbUtil.GetEffectiveTimestampSet(conn, tx, deceasedTs.Value).Parts[0];
                        if (religionCode.HasValue)
                            retVal.ReligionCode = DbUtil.GetCodedValue(conn, tx, religionCode);

                        // Load other properties
                        GetPersonNames(conn, tx, retVal);
                        GetPersonAddresses(conn, tx, retVal);
                        GetPersonLanguages(conn, tx, retVal);
                        GetPersonRaces(conn, tx, retVal);
                        GetPersonAlternateIdentifiers(conn, tx, retVal);
                        GetPersonTelecomAddresses(conn, tx, retVal);

                        retVal.AlternateIdentifiers.Add(domainIdentifier);

                        return retVal;
                    }
                    else
                        return null;
                }

            }

        }

        /// <summary>
        /// Get person's telecom addresses
        /// </summary>
        private void GetPersonTelecomAddresses(IDbConnection conn, IDbTransaction tx, Person person)
        {
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
                            Value = Convert.ToString(rdr["tel_value"])
                        });

            }
        }

        /// <summary>
        /// Get person's alternate identifier
        /// </summary>
        private void GetPersonAlternateIdentifiers(IDbConnection conn, IDbTransaction tx, Person person)
        {

            using (IDbCommand cmd = DbUtil.CreateCommandStoredProc(conn, tx))
            {
                cmd.CommandText = "get_psn_alt_id";
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
                                Domain = Convert.ToString(rdr["id_domain"]),
                                Identifier = Convert.ToString(rdr["id_value"])
                            });
                        else
                            person.OtherIdentifiers.Add(new KeyValuePair<CodeValue,DomainIdentifier>(
                                new CodeValue() { Key = Convert.ToDecimal(rdr["id_purp_cd_id"]) },
                                new DomainIdentifier()
                                {
                                    Domain = Convert.ToString(rdr["id_domain"]),
                                    Identifier = Convert.ToString(rdr["id_value"])
                                })
                            );
                    }

                    // Close the reader
                    rdr.Close();

                    // Fill in other identifiers
                    foreach (var kv in person.OtherIdentifiers)
                    {
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

        }

        /// <summary>
        /// Get a person's race codes
        /// </summary>
        private void GetPersonRaces(IDbConnection conn, IDbTransaction tx, Person person)
        {
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
        }

        /// <summary>
        /// Get person's languages
        /// </summary>
        private void GetPersonLanguages(IDbConnection conn, IDbTransaction tx, Person person)
        {
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
                            Type = (LanguageType)Convert.ToInt32(rdr["mode_cs"])
                        });
            }
        }

        /// <summary>
        /// Get a person's addresses
        /// </summary>
        private void GetPersonAddresses(IDbConnection conn, IDbTransaction tx, Person person)
        {
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
                            Key = Convert.ToDecimal(rdr["addr_set_id"]),
                            Use = (AddressSet.AddressSetUse)Convert.ToInt32(rdr["addr_set_use"])
                        });
                    }

                // Detail load each address
                foreach (var addr in person.Addresses)
                {
                    var dtl = DbUtil.GetAddress(conn, tx, addr.Key);
                    addr.Parts = dtl.Parts;
                }
            }
        }

        /// <summary>
        /// Get person names
        /// </summary>
        private void GetPersonNames(IDbConnection conn, IDbTransaction tx, Person person)
        {
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
                            Use = (NameSet.NameSetUse)Convert.ToInt32(rdr["name_set_use"])
                        });
                    }

                // Detail load each address
                foreach (var name in person.Names)
                {
                    var dtl = DbUtil.GetName(conn, tx, name.Key);
                    name.Parts = dtl.Parts;
                }
            }
        }

        /// <summary>
        /// De-persist an object with <paramref name="identifier"/> from the specified <paramref name="conn"/>
        /// placing it within the specified <paramref name="container"/> in the specified <paramref name="role"/>.
        /// When <paramref name="loadFast"/> is true, forgo advanced de-persisting such as prior versions, etc...
        /// </summary>
        public System.ComponentModel.IComponent DePersist(System.Data.IDbConnection conn, decimal identifier, System.ComponentModel.IContainer container, SVC.Core.ComponentModel.HealthServiceRecordSiteRoleType? role, bool loadFast)
        {
            // De-persist a person
            var person = GetPerson(conn, null, new VersionedDomainIdentifier()
            {
                Domain = ApplicationContext.ConfigurationService.OidRegistrar.GetOid(ClientRegistryOids.CLIENT_CRID).Oid,
                Identifier = identifier.ToString()
            });

            // TODO: Prior versions of this person

            // De-persist components
            DbUtil.DePersistComponents(conn, person, this, loadFast);

            return person;
        }

        #endregion

        /// <summary>
        /// Merge person records together ensuring that appropriate update modes are set. This 
        /// will clean newPerson to only include data which is changing. Data which remains the same
        /// will be removed from newPerson
        /// </summary>
        internal void MergePersons(Person newPerson, Person oldPerson)
        {

            // Start the merging process for addresses
            // For each of the addresses in the new person record, determine if
            // they are additions (new addresses), modifications (old addresses 
            // with the same use) or removals (not in the new but in old)
            if (newPerson.Addresses != null)
            {
                foreach (var addr in newPerson.Addresses)
                {
                    UpdateModeType desiredUpdateMode = UpdateModeType.AddOrUpdate;
                    var candidateOtherAddress = oldPerson.Addresses.FindAll(o => o.Use == addr.Use);
                    if (candidateOtherAddress.Count == 1)
                    {
                        if (QueryUtil.MatchAddress(candidateOtherAddress[0], addr) == 1) // Remove .. no change
                        {
                            //candidateOtherAddress[0].Key = -1;
                            addr.Key = -2;
                        }
                        else
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
                                addr.Key = -2;
                            else
                                addr.UpdateMode = UpdateModeType.Update;
                        }
                        else
                            addr.UpdateMode = UpdateModeType.Add;
                        addr.Key = secondLevelFoundAddress.Key;
                        //secondLevelFoundAddress.Key = -1;
                    }
                    else // Couldn't find an address in the old in the new so it is an add
                    {
                        // Are we just changing the use?
                        var secondLevelFoundAddress = oldPerson.Addresses.Find(o => QueryUtil.MatchAddress(addr, o) == 1);
                        if (secondLevelFoundAddress == null)
                            addr.UpdateMode = UpdateModeType.Add;
                        else
                        {
                            addr.Key = secondLevelFoundAddress.Key;
                            //secondLevelFoundAddress.Key = -1;
                            addr.UpdateMode = UpdateModeType.Update;
                        }
                    }
                }

                //// Add all addresses in the old person that cannot be found in the new person to the list
                //// of addresses to remove
                //foreach (var addr in oldPerson.Addresses)
                //    if (addr.Key > 0)
                //        addr.UpdateMode = UpdateModeType.Remove;
                newPerson.Addresses.AddRange(oldPerson.Addresses.FindAll(o => o.UpdateMode == UpdateModeType.Remove));
                newPerson.Addresses.RemoveAll(o => o.Key < 0);
            }

            // Next we want to do the same for names
            if (newPerson.Names != null)
            {
                foreach (var name in newPerson.Names)
                {
                    UpdateModeType desiredUpdateMode = UpdateModeType.AddOrUpdate;
                    var candidateOtherName = oldPerson.Names.FindAll(o => o.Use == name.Use);
                    if (candidateOtherName.Count == 1)
                    {
                        if (QueryUtil.MatchName(candidateOtherName[0], name) == 1)
                        {
                            //candidateOtherName[0].Key = -1;
                            name.Key = -2;
                        }
                        else
                        {
                            name.UpdateMode = UpdateModeType.Update;
                            name.Key = candidateOtherName[0].Key;
                            //candidateOtherName[0].Key = -1;
                        }
                    }
                    else if (candidateOtherName.Count != 0)
                    {
                        // Find this name in a collection of same use names
                        var secondLevelFoundName = candidateOtherName.Find(o => QueryUtil.MatchName(o, name) > DatabasePersistenceService.ValidationSettings.PersonNameMatch);

                        if (secondLevelFoundName != null)
                        {
                            if (QueryUtil.MatchName(secondLevelFoundName, name) == 1)
                                name.Key = -2;
                            else
                                name.UpdateMode = UpdateModeType.Update;
                        }
                        else
                            name.UpdateMode = UpdateModeType.Add;
                        name.Key = secondLevelFoundName.Key;
                        //secondLevelFoundName.Key = -1;
                    }
                    else // Couldn't find an name in the old in the new so it is an add
                    {
                        // Are we just changing the use?
                        var secondLevelFoundName = oldPerson.Names.Find(o => QueryUtil.MatchName(name, o) == 1);
                        if (secondLevelFoundName == null)
                            name.UpdateMode = UpdateModeType.Add;
                        else
                        {
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
                newPerson.Names.RemoveAll(o => o.Key < 0);
            }

            // Birth time
            if (newPerson.BirthTime != null && oldPerson.BirthTime != null &&
                newPerson.BirthTime.Value == oldPerson.BirthTime.Value)
                newPerson.BirthTime = null;

            // MB order
            if (newPerson.BirthOrder == oldPerson.BirthOrder)
                newPerson.BirthOrder = null;

            // Religion code
            if (newPerson.ReligionCode != null && oldPerson.ReligionCode != null &&
                newPerson.ReligionCode.Code == oldPerson.ReligionCode.Code &&
                newPerson.ReligionCode.CodeSystem == oldPerson.ReligionCode.CodeSystem)
                newPerson.ReligionCode = null;

            // Deceased
            if (newPerson.DeceasedTime != null && oldPerson.DeceasedTime != null &&
                newPerson.DeceasedTime.Value == oldPerson.DeceasedTime.Value)
                newPerson.DeceasedTime = null;

            // Race codes 
            if (newPerson.Race != null)
            {
                foreach (var rce in newPerson.Race)
                {
                    var candidateRace = oldPerson.Race.Find(o => o.CodeSystem == rce.CodeSystem && o.Code == rce.Code);
                    if (candidateRace != null) // New exists in the old
                    {
                        rce.Key = -2;
                        //candidateRace.Key = -1;
                    }
                    else
                        rce.UpdateMode = UpdateModeType.Add;
                }
                newPerson.Race.RemoveAll(o => o.Key < 0);
            }

            // Language codes
            if (newPerson.Language != null)
            {
                List<PersonLanguage> garbagePail = new List<PersonLanguage>();
                foreach (var lang in newPerson.Language)
                {
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

                // Find all race codes in the old that aren't in the new (remove)
                //foreach (var lang in oldPerson.Language)
                //    if (!newPerson.Language.Exists(o => o.Language == lang.Language))
                //        lang.UpdateMode = UpdateModeType.Remove;
                //newPerson.Language.AddRange(oldPerson.Language.FindAll(o => o.UpdateMode == UpdateModeType.Remove));
                newPerson.Language.RemoveAll(o => garbagePail.Contains(o));
            }

            if (newPerson.TelecomAddresses != null)
            {
                // Telecom addresses
                foreach (var tel in newPerson.TelecomAddresses)
                {
                    var candidateTel = oldPerson.TelecomAddresses.Find(o => o.Use == tel.Use && tel.Value == o.Value);
                    if (candidateTel != null) // New exists in the old
                    {
                        tel.Key = -2;
                        //candidateTel.Key = -1;
                    }
                    else
                    {
                        candidateTel = oldPerson.TelecomAddresses.Find(o => o.Value == tel.Value);
                        if (candidateTel == null)
                            tel.UpdateMode = UpdateModeType.Add;
                        else
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
                newPerson.TelecomAddresses.RemoveAll(o => o.Key < 0);
            }

            if (newPerson.AlternateIdentifiers != null)
            {
                // Alternate identifiers
                foreach (var alt in newPerson.AlternateIdentifiers)
                {
                    var candidateAlt = oldPerson.AlternateIdentifiers.Find(o => o.Domain == alt.Domain && o.Identifier == alt.Identifier);
                    if (candidateAlt != null) // New exists in the old
                    {
                        alt.Key = -2;
                        //candidateAlt.Key = -1;
                    }
                    else
                        alt.UpdateMode = UpdateModeType.Add;
                }

                // Find all race codes in the old that aren't in the new (remove)
                //foreach (var alt in oldPerson.AlternateIdentifiers)
                //    if (alt.Key > 0)
                //        alt.UpdateMode = UpdateModeType.Remove;
                //newPerson.AlternateIdentifiers.AddRange(oldPerson.AlternateIdentifiers.FindAll(o => o.UpdateMode == UpdateModeType.Remove));
                newPerson.AlternateIdentifiers.RemoveAll(o => o.Key < 0);
            }

            if (newPerson.OtherIdentifiers != null)
            {
                // Other identifiers
                foreach (var alt in newPerson.OtherIdentifiers)
                {
                    var candidateAlt = oldPerson.OtherIdentifiers.Find(o => o.Key != null && alt.Key != null && o.Key.Code == alt.Key.Code && o.Key.CodeSystem == alt.Key.CodeSystem);
                    if (!candidateAlt.Equals(default(KeyValuePair<CodeValue, DomainIdentifier>))) // New exists in the old
                    {
                        // Found based on the code, so this is an update
                        if (alt.Value.Identifier == candidateAlt.Value.Identifier &&
                            alt.Value.Domain == candidateAlt.Value.Identifier)
                            alt.Value.Key = -2;
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
                                alt.Value.Key = -2;
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
                newPerson.OtherIdentifiers.RemoveAll(o => o.Value.Key < 0);
            }

            // Copy over extended attributes not mentioned in the new person
            foreach (HealthServiceRecordComponent cmp in oldPerson.Components)
                if (cmp is ExtendedAttribute && newPerson.FindExtension(o => o.PropertyPath != (cmp as ExtendedAttribute).PropertyPath && o.Name != (cmp as ExtendedAttribute).Name) == null) // copy
                    newPerson.Add(cmp);

        }
    }
}
