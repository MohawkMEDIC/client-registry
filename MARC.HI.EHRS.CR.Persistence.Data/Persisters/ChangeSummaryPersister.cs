using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MARC.HI.EHRS.SVC.Core.ComponentModel.Components;
using MARC.HI.EHRS.CR.Core.ComponentModel;
using MARC.HI.EHRS.SVC.Core.ComponentModel;
using MARC.HI.EHRS.SVC.Core.Services;
using System.Data;
using MARC.HI.EHRS.SVC.Core.DataTypes;

namespace MARC.HI.EHRS.CR.Persistence.Data.ComponentPersister
{
    /// <summary>
    /// Represents a persister that can persist and create change summaries
    /// </summary>
    public class ChangeSummaryPersister : IComponentPersister
    {
        #region IComponentPersister Members

        /// <summary>
        /// Gets the type that this component handles
        /// </summary>
        public Type HandlesComponent
        {
            get { return typeof(ChangeSummary); }
        }

        /// <summary>
        /// Persists the change summary (which is really just a health service event in the sHR)
        /// </summary>
        public SVC.Core.DataTypes.VersionedDomainIdentifier Persist(System.Data.IDbConnection conn, System.Data.IDbTransaction tx, System.ComponentModel.IComponent data, bool isUpdate)
        {

            // Change summary cast
            ChangeSummary cs = data as ChangeSummary;
            
            //if(isUpdate)
            //    return cs.AlternateIdentifier; // can't update a change summary

            // copy fields to hsr
            ChangeSummary hsr = new ChangeSummary()
            {
                ChangeType = cs.ChangeType,
                EffectiveTime = cs.EffectiveTime,
                AlternateIdentifier = cs.AlternateIdentifier,
                Context = cs.Context,
                Id = cs.Id,
                IsMasked = cs.IsMasked,
                Status = cs.Status,
                Timestamp = cs.Timestamp,
                LanguageCode = cs.LanguageCode
            };

            // persist HSR
            hsr.AlternateIdentifier = CreateHSRRecord(conn, tx, hsr);
            hsr.Id = Convert.ToDecimal(hsr.AlternateIdentifier.Identifier);
            hsr.VersionIdentifier = Convert.ToDecimal(hsr.AlternateIdentifier.Version);

            // Is there any sort of linkage we need to create
            if (hsr.Site != null && hsr.Site.Container != null &&
                !(hsr.Site as HealthServiceRecordSite).IsSymbolic)
                new RegistrationEventPersister().LinkHSRRecord(conn, tx, hsr);

            // Persist components
            cs.AlternateIdentifier = hsr.AlternateIdentifier;
            cs.Id = hsr.Id;
            cs.VersionIdentifier = hsr.VersionIdentifier;
            DbUtil.PersistComponents(conn, tx, isUpdate, this, cs);

            return hsr.AlternateIdentifier;
        }

        /// <summary>
        /// Create an HSR record
        /// </summary>
        private VersionedDomainIdentifier CreateHSRRecord(IDbConnection conn, IDbTransaction tx, ChangeSummary hsr)
        {
            IDbCommand cmd = DbUtil.CreateCommandStoredProc(conn, tx);
            cmd.CommandText = "crt_hsr";

            // Get the terminology service
            ISystemConfigurationService iscs = ApplicationContext.ConfigurationService; //ApplicationContext.Current.GetService(typeof(ISystemConfigurationService)) as ISystemConfigurationService;

            // Parameters
            // classifier = 0x400 = Change Summary
            cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "hsr_cls_in", DbType.Decimal, (int)0x400));
            // event type code
            cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "evt_typ_cd_id_in", DbType.Decimal, DbUtil.CreateCodedValue(conn, tx, hsr.ChangeType)));
            // refuted indicator
            cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "refuted_ind_in", DbType.Boolean, false));

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
                id.Domain = iscs.OidRegistrar.GetOid(ClientRegistryOids.EVENT_OID).Oid;

                return id;
            }
            finally
            {
                resultRdr.Close();
            }
        }

        /// <summary>
        /// De-persist the specified change summary
        /// </summary>
        public System.ComponentModel.IComponent DePersist(System.Data.IDbConnection conn, decimal identifier, System.ComponentModel.IContainer container, SVC.Core.ComponentModel.HealthServiceRecordSiteRoleType? role, bool loadFast)
        {
            // TODO: Ensure that when a parent with context conduction exists, to grab contextual data (authors, etc...) from the parent
            ChangeSummary retVal = new ChangeSummary();

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
                        Domain = configService.OidRegistrar.GetOid(ClientRegistryOids.EVENT_OID).Oid,
                        Identifier = retVal.Id.ToString(),
                        Version = retVal.VersionIdentifier.ToString()
                    };
                    retVal.LanguageCode = reader["lang_cs"].ToString();
                    retVal.Timestamp = DateTime.Parse(Convert.ToString(reader["aut_utc"]));
                    retVal.Status = (StatusType)Enum.Parse(typeof(StatusType), Convert.ToString(reader["status_cs"]));
                    tsId = reader["efft_ts_set_id"] == DBNull.Value ? default(decimal) : Convert.ToDecimal(reader["efft_ts_set_id"]);
                    cdId =  Convert.ToDecimal(reader["evt_typ_cd_id"]);
                    rplcVersionId = reader["rplc_vrsn_id"] == DBNull.Value ? default(decimal) : Convert.ToDecimal(reader["rplc_vrsn_id"]);
                }
                finally
                {
                    reader.Close();
                }

                // Read codes and times
                retVal.ChangeType = DbUtil.GetCodedValue(conn, null, cdId);
                if(tsId != default(decimal))
                    retVal.EffectiveTime = DbUtil.GetEffectiveTimestampSet(conn, null, tsId);

                if (container != null)
                    container.Add(retVal);

                if (role.HasValue && (role.Value & HealthServiceRecordSiteRoleType.ReplacementOf) == HealthServiceRecordSiteRoleType.ReplacementOf)
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
    }
}
