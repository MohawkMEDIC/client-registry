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
 * Date: 19-7-2012
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Diagnostics;
using System.Threading;
using MARC.HI.EHRS.SVC.Core.DataTypes;
using MARC.HI.EHRS.SVC.Core.Services;
using System.ComponentModel;
using MARC.HI.EHRS.CR.Core.ComponentModel;
using System.Data.Common;
using MARC.HI.EHRS.SVC.Core;
using MARC.HI.EHRS.SVC.Core.Exceptions;
using MARC.HI.EHRS.SVC.Core.ComponentModel;
using MARC.HI.EHRS.SVC.Core.ComponentModel.Components;

namespace MARC.HI.EHRS.CR.Persistence.Data.ComponentPersister
{
    /// <summary>
    /// A record persister that handles the HealthServiceRecord component
    /// </summary>
    public class RegistrationEventPersister : IComponentPersister, IQueryComponentPersister
    {
        #region IComponentPersister Members


        /// <summary>
        /// Gets the type that this persister handles
        /// </summary>
        public Type HandlesComponent
        {
            get { return typeof(RegistrationEvent); }
        }

        /// <summary>
        /// Returns true if the component has a current version
        /// </summary>
        private bool HasCurrentVersion(IDbConnection conn, IDbTransaction tx, decimal hsr_id)
        {
            // Get the current version
            IDbCommand cmd = DbUtil.CreateCommandStoredProc(conn, tx);
            cmd.CommandText = "get_hsr_crnt_vrsn";
            cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "hsr_id_in", DbType.Decimal, hsr_id));
            return cmd.ExecuteScalar() != DBNull.Value;
        }

        /// <summary>
        /// Persist the specified data
        /// </summary>
        public VersionedDomainIdentifier Persist(IDbConnection conn, IDbTransaction tx, System.ComponentModel.IComponent data, bool isUpdate)
        {
            ISystemConfigurationService configServce = ApplicationContext.ConfigurationService; //ApplicationContext.Current.GetService(typeof(ISystemConfigurationService)) as ISystemConfigurationService;
            // First, we must determine if we're going to be performing an update or a put
            RegistrationEvent hsr = data as RegistrationEvent;

            try
            {
                // Does this record have an identifier that is valid?
                if (hsr.AlternateIdentifier != null && !hsr.AlternateIdentifier.Domain.Equals(configServce.OidRegistrar.GetOid(ClientRegistryOids.REGISTRATION_EVENT).Oid))
                    throw new InvalidOperationException(String.Format("Could not find an identifier for the event that falls in the custodian domain '{0}'", configServce.OidRegistrar.GetOid(ClientRegistryOids.REGISTRATION_EVENT).Oid));
                else if (hsr.AlternateIdentifier != null)
                    hsr.Id = Decimal.Parse(hsr.AlternateIdentifier.Identifier);
                else if (isUpdate && hsr.Site == null)
                    throw new ArgumentException("Cannot update an event that is not identified");
                
                // Is there a parent that we can grab a language code from
                var parent = DbUtil.GetRegistrationEvent(data);

                // Default language code
                hsr.LanguageCode = hsr.LanguageCode ?? (parent != null ? parent.LanguageCode : configServce.JurisdictionData.DefaultLanguageCode);

                // Create an HSR event if this is not one
                if (!isUpdate || hsr.AlternateIdentifier == null)
                {
                    hsr.AlternateIdentifier = CreateHSRRecord(conn, tx, hsr);
                    hsr.Id = Convert.ToDecimal(hsr.AlternateIdentifier.Identifier);
                    hsr.VersionIdentifier = Convert.ToDecimal(hsr.AlternateIdentifier.Version);

                    // Is there any sort of linkage we need to create
                    if (hsr.Site != null && hsr.Site.Container != null &&
                        !(hsr.Site as HealthServiceRecordSite).IsSymbolic)
                        LinkHSRRecord(conn, tx, hsr);
                }
                else
                {
                    hsr.VersionIdentifier = CreateHSRVersion(conn, tx, hsr);
                    hsr.AlternateIdentifier.Version = hsr.VersionIdentifier.ToString();
                }

                DbUtil.PersistComponents(conn, tx, isUpdate, this, hsr);

            }
            catch (Exception ex)
            {
                Trace.TraceError(ex.ToString());
                throw;
            }

            return hsr.AlternateIdentifier;

        }

        /// <summary>
        /// Link this HSR record to another
        /// </summary>
        internal void LinkHSRRecord(IDbConnection conn, IDbTransaction tx, HealthServiceRecordContainer hsr)
        {

            // An HSR can only be linked to another HSR, so ... 
            // first we need to find the HSR container to link to
            IContainer hsrContainer = hsr.Site.Container;
            while (!(hsrContainer is RegistrationEvent) && hsrContainer != null)
                hsrContainer = (hsrContainer as IComponent).Site.Container;
            RegistrationEvent parentHsr = hsrContainer as RegistrationEvent;

            // Now we want to link
            if (parentHsr == null)
                throw new InvalidOperationException("Can only link an Health Service Record event to another Health Service Record Event");

            // Insert link
            IDbCommand cmd = DbUtil.CreateCommandStoredProc(conn, tx);
            try
            {
                cmd.CommandText = "add_hsr_lnk";
                cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "cmp_hsr_id_in", DbType.Decimal, hsr.Id));
                cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "cbc_hsr_id_in", DbType.Decimal, parentHsr.Id));
                cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "lnk_cls_in", DbType.Decimal, (decimal)(hsr.Site as HealthServiceRecordSite).SiteRoleType));
                cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "conduction_in", DbType.Boolean, (hsr.Site as HealthServiceRecordSite).ContextConduction));

                try
                {
                    cmd.ExecuteNonQuery();
                }
                catch (DbException e)
                {
                    throw new InvalidOperationException(string.Format("Cannot insert link between {0} and {1}. {2}",
                        hsr.Id, parentHsr.Id, e.Message));
                }
            }
            finally
            {
                cmd.Dispose();
            }

        }

        /// <summary>
        /// Create an HSR version
        /// </summary>
        private decimal CreateHSRVersion(IDbConnection conn, IDbTransaction tx, RegistrationEvent hsr)
        {
            IDbCommand cmd = DbUtil.CreateCommandStoredProc(conn, tx);
            try
            {
                cmd.CommandText = "crt_hsr_vrsn";
                cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "hsr_id_in", DbType.Decimal, hsr.Id));

                decimal? codeId = null;
                if (hsr.EventType != null)
                    codeId = DbUtil.CreateCodedValue(conn, tx, hsr.EventType);
                cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "evt_typ_cd_id_in", DbType.Decimal, codeId.HasValue ? (object)codeId.Value : DBNull.Value));
                cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "refuted_ind_in", DbType.Boolean, hsr.Refuted));

                // Effective time if needed
                decimal? hsrEfftTsId = null;
                if (hsr.EffectiveTime != null)
                    hsrEfftTsId = DbUtil.CreateTimeset(conn, tx, hsr.EffectiveTime);

                // Parameters
                cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "efft_ts_set_id_in", DbType.Decimal, (object)hsrEfftTsId ?? DBNull.Value));            
                cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "status_cs_in", DbType.StringFixedLength, hsr.Status == StatusType.Unknown ? (object)DBNull.Value : hsr.Status.ToString()));
                cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "aut_utc_in", DbType.DateTime, hsr.Timestamp == default(DateTime) ? (object)DBNull.Value : hsr.Timestamp));
                cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "lang_cs_in", DbType.StringFixedLength, (object)hsr.LanguageCode ?? DBNull.Value));

                return Convert.ToDecimal(cmd.ExecuteScalar());
            }
            finally
            {
                cmd.Dispose();
            }
        }

        /// <summary>
        /// Create the HSR record
        /// </summary>
        internal VersionedDomainIdentifier CreateHSRRecord(IDbConnection conn, IDbTransaction tx, RegistrationEvent hsr)
        {
            IDbCommand cmd = DbUtil.CreateCommandStoredProc(conn, tx);
            cmd.CommandText = "crt_hsr";

            // Get the terminology service
            //ITerminologyService its = ApplicationContext.CurrentContext.GetService(typeof(ITerminologyService)) as ITerminologyService;
            ISystemConfigurationService iscs = ApplicationContext.ConfigurationService; //ApplicationContext.Current.GetService(typeof(ISystemConfigurationService)) as ISystemConfigurationService;

            // Validate the language code
            //if (its != null)
            //{
            //    var validationError = its.Validate(hsr.LanguageCode, null, CodeSystemName.ISO639);
            //    if (validationError.Outcome != MARC.HI.EHRS.SVC.Core.Terminology.ValidationOutcome.ValidWithWarning &&
            //        validationError.Outcome != MARC.HI.EHRS.SVC.Core.Terminology.ValidationOutcome.Valid)
            //        throw new ConstraintException("Language MUST be a valid ISO639 Country code in the format XX-XX");
            //}

            // Parameters
            // classifier
            cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "hsr_cls_in", DbType.Decimal, (int)hsr.EventClassifier));
            // event type code
            cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "evt_typ_cd_id_in", DbType.Decimal, DbUtil.CreateCodedValue(conn, tx, hsr.EventType)));
            // refuted indicator
            cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "refuted_ind_in", DbType.Boolean, hsr.Refuted));

            decimal? efftTimeId = null;
            if (hsr.EffectiveTime != null)
                efftTimeId = DbUtil.CreateTimeset(conn, tx, hsr.EffectiveTime);

            cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "efft_ts_set_id_in", DbType.Decimal, efftTimeId == null ? (object)DBNull.Value : efftTimeId.Value));
            // status code
            cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "status_cs_in", DbType.String, hsr.Status == null ? (object)DBNull.Value : hsr.Status.ToString()));
            // authored time
            cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "aut_utc_in", DbType.DateTime, hsr.Timestamp == default(DateTime) ? (object)DBNull.Value : hsr.Timestamp));
            // language code
            cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "lang_cs_in", DbType.String, hsr.LanguageCode));

            // Execute the command
            IDataReader resultRdr = cmd.ExecuteReader();
            try
            {
                // Create the return value
                VersionedDomainIdentifier id = new VersionedDomainIdentifier();
                if (!resultRdr.Read())
                    return null;

                id.Version = Convert.ToString(resultRdr["VRSN_ID"]);
                id.Identifier = Convert.ToString(resultRdr["ID"]);
                id.Domain = iscs.OidRegistrar.GetOid(ClientRegistryOids.REGISTRATION_EVENT).Oid;

                return id;
            }
            finally
            {
                resultRdr.Close();
            }
        }

        
        /// <summary>
        /// De-persist the specified data
        /// </summary>
        public System.ComponentModel.IComponent DePersist(IDbConnection conn, decimal identifier, IContainer container, HealthServiceRecordSiteRoleType? roleType, bool loadFast)
        {
            // TODO: Ensure that when a parent with context conduction exists, to grab contextual data (authors, etc...) from the parent
            RegistrationEvent retVal = new RegistrationEvent();

            // Configuration service
            ISystemConfigurationService configService = ApplicationContext.ConfigurationService; //ApplicationContext.Current.GetService(typeof(ISystemConfigurationService)) as ISystemConfigurationService;

            // Get the health service event
            IDbCommand cmd = DbUtil.CreateCommandStoredProc(conn, null);
            try
            {
                cmd.CommandText = "get_hsr_crnt_vrsn";
                cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "hsr_id_in", DbType.Decimal, identifier));

                decimal tsId = default(decimal),
                    cdId = default(decimal),
                    rplcVersionId = default(decimal);

                // Read data
                IDataReader reader = cmd.ExecuteReader();
                try
                {
                    if (!reader.Read())
                        return null;

                    retVal.Id = Convert.ToDecimal(reader["hsr_id"]);
                    retVal.VersionIdentifier = Convert.ToDecimal(reader["hsr_vrsn_id"]);
                    retVal.AlternateIdentifier = new VersionedDomainIdentifier()
                    {
                        Domain = configService.OidRegistrar.GetOid(ClientRegistryOids.REGISTRATION_EVENT).Oid,
                        Identifier = retVal.Id.ToString(),
                        Version = retVal.VersionIdentifier.ToString(),
                    };
                    retVal.EventClassifier = (RegistrationEventType)Convert.ToDecimal(reader["hsr_cls"]);
                    retVal.Refuted = Convert.ToBoolean(reader["refuted_ind"]);
                    retVal.Timestamp = DateTime.Parse(Convert.ToString(reader["aut_utc"]));
                    retVal.Status = (StatusType)Enum.Parse(typeof(StatusType), Convert.ToString(reader["status_cs"]));
                    tsId = Convert.ToDecimal(reader["efft_ts_set_id"]);
                    cdId = Convert.ToDecimal(reader["evt_typ_cd_id"]);
                    rplcVersionId = reader["rplc_vrsn_id"] == DBNull.Value ? default(decimal) : Convert.ToDecimal(reader["rplc_vrsn_id"]);
                    retVal.LanguageCode = Convert.ToString(reader["lang_cs"]);
                }
                finally
                {
                    reader.Close();
                }

                // Read codes and times
                retVal.EventType = DbUtil.GetCodedValue(conn, null, cdId);
                retVal.EffectiveTime = DbUtil.GetEffectiveTimestampSet(conn, null, tsId);

                if (container != null)
                    container.Add(retVal);

                // De-persist older versions
                if (!loadFast && rplcVersionId != default(decimal))
                {
                    var oldVersion = DePersist(conn, identifier, rplcVersionId, retVal, HealthServiceRecordSiteRoleType.OlderVersionOf, false);
                    if (oldVersion != null)
                        (oldVersion.Site as HealthServiceRecordSite).SiteRoleType = HealthServiceRecordSiteRoleType.OlderVersionOf;
                }

                if (roleType.HasValue && (roleType.Value & HealthServiceRecordSiteRoleType.ReplacementOf) == HealthServiceRecordSiteRoleType.ReplacementOf)
                    ;
                else
                    DbUtil.DePersistComponents(conn, retVal, this, loadFast);
            }
            finally
            {
                cmd.Dispose();
            }

            return retVal;
        }

        ///// <summary>
        ///// De-persist old versions
        ///// </summary>
        //private IComponent DePersistOldVersion(IDbConnection conn, HealthServiceRecord hsrEvent)
        //{
        //    // Create command
        //    using (IDbCommand cmd = DbUtil.CreateCommandStoredProc(conn, null))
        //    {
        //        cmd.CommandText = "get_hsr_vrsn";

        //        // Add parameters
        //        cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "hsr_id_in", DbType.Decimal, hsrEvent.Id));
        //        cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "hsr_vrsn_id_in", DbType.Decimal, hsrEvent.VersionIdentifier));

        //        // Query version identifier
        //        decimal replacesVersionId = default(decimal);
        //        IDataReader rdr = cmd.ExecuteReader();
        //        try
        //        {
        //            if (rdr.Read() && rdr["rplc_vrsn_id"] != DBNull.Value)
        //                replacesVersionId = Convert.ToDecimal(rdr["rplc_vrsn_id"]);
        //            else
        //                return null;
        //        }
        //        finally
        //        {
        //            rdr.Close();
        //            rdr.Dispose();
        //        }

        //        // Now load the older version
        //        return DePersist(conn, hsrEvent.Id, replacesVersionId, hsrEvent, HealthServiceRecordSiteRoleType.OlderVersionOf, false);

        //    }
        //}

        /// <summary>
        /// De-persist a specific version of a HSR 
        /// </summary>
        public System.ComponentModel.IComponent DePersist(IDbConnection conn, decimal identifier, decimal versionId, IContainer container, HealthServiceRecordSiteRoleType? roleType, bool loadFast)
        {
            // TODO: Ensure that when a parent with context conduction exists, to grab contextual data (authors, etc...) from the parent
            RegistrationEvent retVal = new RegistrationEvent();

            // Configuration service
            ISystemConfigurationService configService = ApplicationContext.ConfigurationService; //ApplicationContext.Current.GetService(typeof(ISystemConfigurationService)) as ISystemConfigurationService;

            // Get the health service event
            IDbCommand cmd = DbUtil.CreateCommandStoredProc(conn, null);
            try
            {
                cmd.CommandText = "get_hsr_vrsn";
                cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "hsr_id_in", DbType.Decimal, identifier));
                cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "hsr_vrsn_id_in", DbType.Decimal, versionId));

                decimal tsId = default(decimal),
                    cdId = default(decimal),
                    rplcVersionId = default(decimal);

                // Read data
                IDataReader reader = cmd.ExecuteReader();
                try
                {
                    if (!reader.Read())
                        return null;

                    retVal.Id = Convert.ToDecimal(reader["hsr_id"]);
                    retVal.VersionIdentifier = Convert.ToDecimal(reader["hsr_vrsn_id"]);
                    retVal.AlternateIdentifier = new VersionedDomainIdentifier()
                    {
                        Domain = configService.OidRegistrar.GetOid(ClientRegistryOids.REGISTRATION_EVENT).Oid,
                        Identifier = retVal.Id.ToString(),
                        Version = retVal.VersionIdentifier.ToString(),
                    };
                    retVal.EventClassifier = (RegistrationEventType)Convert.ToDecimal(reader["hsr_cls"]);
                    retVal.Refuted = Convert.ToBoolean(reader["refuted_ind"]);
                    retVal.Timestamp = DateTime.Parse(Convert.ToString(reader["aut_utc"]));
                    retVal.Status = (StatusType)Enum.Parse(typeof(StatusType), Convert.ToString(reader["status_cs"]));
                    rplcVersionId = reader["rplc_vrsn_id"] == DBNull.Value ? default(decimal) : Convert.ToDecimal(reader["rplc_vrsn_id"]);
                    tsId = Convert.ToDecimal(reader["efft_ts_set_id"]);
                    cdId = Convert.ToDecimal(reader["evt_typ_cd_id"]);
                    retVal.LanguageCode = Convert.ToString(reader["lang_cs"]);
                }
                finally
                {
                    reader.Close();
                }

                // Read codes and times
                retVal.EventType = DbUtil.GetCodedValue(conn, null, cdId);
                retVal.EffectiveTime = DbUtil.GetEffectiveTimestampSet(conn, null, tsId);

                if (container != null)
                    container.Add(retVal);

                // De-persist older versions
                if (!loadFast && rplcVersionId != default(decimal))
                {
                    var oldVersion = DePersist(conn, identifier, rplcVersionId, retVal, HealthServiceRecordSiteRoleType.OlderVersionOf, false);
                    if (oldVersion != null)
                        (oldVersion.Site as HealthServiceRecordSite).SiteRoleType = HealthServiceRecordSiteRoleType.OlderVersionOf;
                }


                if (roleType.HasValue && (roleType.Value & HealthServiceRecordSiteRoleType.ReplacementOf) == HealthServiceRecordSiteRoleType.ReplacementOf)
                    ;
                else
                    DbUtil.DePersistComponents(conn, retVal, this, loadFast);
            }
            finally
            {
                cmd.Dispose();
            }

            return retVal;
        }


        #endregion

        #region IComponentPersister Members

        /// <summary>
        /// Gets the content-type oid
        /// </summary>
        public string ComponentTypeOid
        {
            get { return ApplicationContext.ConfigurationService.OidRegistrar.GetOid(ClientRegistryOids.REGISTRATION_EVENT).Oid; ; }
        }

        /// <summary>
        /// Build the filter
        /// </summary>
        public string BuildFilter(IComponent data, bool forceExact)
        {
            // HACK: Come back and fix this ... Should build other filter parameters such as author, etc based on the 
            // content of the registration event rather than hard-coding to the 

            // Get the subject of the query
            //var subjectOfQuery = (data as HealthServiceRecordContainer).FindComponent(HealthServiceRecordSiteRoleType.SubjectOf) as Person;
            var hsr = data as RegistrationEvent;
            // Matching?
            StringBuilder sb = new StringBuilder("SELECT DISTINCT HSR_ID, HSR_VRSN_ID FROM HSR_VRSN_TBL WHERE OBSLT_UTC IS NULL AND STATUS_CS NOT IN ('Obsolete','Nullified') ");
            sb.AppendFormat("AND HSR_ID IN (SELECT HSR_ID FROM HSR_TBL WHERE HSR_CLS IN (4, 8)) "); // only registrations
            
            return sb.ToString();

        }

        #endregion

        /// <summary>
        /// Control clauses
        /// </summary>
        public string BuildControlClauses(IComponent queryComponent)
        {
            return " ORDER BY HSR_VRSN_ID DESC";
        }
    }
}
