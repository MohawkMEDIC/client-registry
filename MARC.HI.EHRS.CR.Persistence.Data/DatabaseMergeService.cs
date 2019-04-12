using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MARC.HI.EHRS.CR.Core.Services;
using MARC.HI.EHRS.SVC.Core.DataTypes;
using MARC.HI.EHRS.SVC.Core.Services;
using MARC.HI.EHRS.CR.Core.ComponentModel;
using MARC.HI.EHRS.SVC.Core.ComponentModel;
using System.Data;
using MARC.HI.EHRS.SVC.Core.ComponentModel.Components;
using System.Diagnostics;

namespace MARC.HI.EHRS.CR.Persistence.Data
{
    /// <summary>
    /// Databased merge provider
    /// </summary>
    public class DatabaseMergeService : IClientRegistryMergeService
    {

        // Context
        private IServiceProvider m_context;

        #region IClientRegistryMergeService Members

        /// <summary>
        /// Mark merge conflicts in the database
        /// </summary>
        public virtual void MarkConflicts(SVC.Core.DataTypes.VersionedDomainIdentifier recordId, IEnumerable<SVC.Core.DataTypes.VersionedDomainIdentifier> matches)
        {
            if (recordId.Domain != ApplicationContext.ConfigurationService.OidRegistrar.GetOid(ClientRegistryOids.REGISTRATION_EVENT).Oid)
                throw new ArgumentException(String.Format("Must be drawn from the '{0}' domain", ApplicationContext.ConfigurationService.OidRegistrar.GetOid(ClientRegistryOids.REGISTRATION_EVENT).Oid), "recordId");


            // Construct connection
            IDbConnection conn = DatabasePersistenceService.ConnectionManager.GetConnection();
            IDbTransaction tx = null;
            try
            {
                tx = conn.BeginTransaction();

                // Save matches
                foreach (var id in matches)
                    using (IDbCommand cmd = DbUtil.CreateCommandStoredProc(conn, tx))
                    {
                        cmd.CommandText = "crt_mrg_cand";
                        cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "hsr_id_in", DbType.Decimal, Decimal.Parse(recordId.Identifier)));
                        cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "cand_hsr_id_in", DbType.Decimal, Decimal.Parse(id.Identifier)));
                        cmd.ExecuteNonQuery();
                    }
                tx.Commit();
            }
            catch (Exception e)
            {
                if (tx != null)
                    tx.Rollback();
            }
            finally
            {
                DatabasePersistenceService.ConnectionManager.ReleaseConnection(conn);
            }
        }

        /// <summary>
        /// Merge two items together
        /// </summary>
        public virtual void Resolve(IEnumerable<SVC.Core.DataTypes.VersionedDomainIdentifier> victimIds, SVC.Core.DataTypes.VersionedDomainIdentifier survivorId, SVC.Core.Services.DataPersistenceMode mode)
        {
            // First, we load the survivor
            if (survivorId.Domain != ApplicationContext.ConfigurationService.OidRegistrar.GetOid(ClientRegistryOids.REGISTRATION_EVENT).Oid)
                throw new ArgumentException(String.Format("Must be drawn from the '{0}' domain", ApplicationContext.ConfigurationService.OidRegistrar.GetOid(ClientRegistryOids.REGISTRATION_EVENT).Oid), "survivorId");

            // Load the survivor
            var persistenceService = this.Context.GetService(typeof(IDataPersistenceService)) as IDataPersistenceService;
            var survivorRegistrationEvent = persistenceService.GetContainer(survivorId, true) as RegistrationEvent;
            if(survivorRegistrationEvent == null)
                throw new InvalidOperationException("Could not load target registration event");
            var survivorPerson = survivorRegistrationEvent.FindComponent(HealthServiceRecordSiteRoleType.SubjectOf) as Person;
            if (survivorPerson == null)
                throw new InvalidOperationException("Target registration event has no SubjectOf relationship of type Person");

            // Create the merge registration event
            RegistrationEvent mergeRegistrationEvent = new RegistrationEvent()
            {
                Mode = RegistrationEventType.Replace,
                EventClassifier = RegistrationEventType.Register,
                LanguageCode = survivorRegistrationEvent.LanguageCode
            };
            mergeRegistrationEvent.EventType = new CodeValue("ADMIN_MRG");
            mergeRegistrationEvent.Add(new ChangeSummary()
            {
                ChangeType = new CodeValue("ADMIN_MRG"),
                EffectiveTime = new TimestampSet() { Parts = new List<TimestampPart>() { new TimestampPart(TimestampPart.TimestampPartType.Standlone, DateTime.Now, "F") } },
                Timestamp = DateTime.Now,
                LanguageCode = survivorRegistrationEvent.LanguageCode
            }, "CHG", HealthServiceRecordSiteRoleType.ReasonFor | HealthServiceRecordSiteRoleType.OlderVersionOf, null);
            survivorPerson.Site = null;
            
            mergeRegistrationEvent.Add(survivorPerson, "SUBJ", HealthServiceRecordSiteRoleType.SubjectOf, null);

            // Next, we do a replaces relationship for each of the victims (loading them as well since the ID is of the patient not reg event)
            foreach (var id in victimIds)
            {
                var replacementReg = persistenceService.GetContainer(id, true) as RegistrationEvent;
                if (replacementReg == null)
                    throw new InvalidOperationException("Could not load victim registration event");
                var replacementPerson = replacementReg.FindComponent(HealthServiceRecordSiteRoleType.SubjectOf) as Person;
                if (replacementPerson == null)
                    throw new InvalidOperationException("Victim registration event has no SubjectOf relationship of type Person");

                // Now, create replaces
                survivorPerson.Add(new PersonRegistrationRef()
                {
                    AlternateIdentifiers = new List<DomainIdentifier>() {
                        new DomainIdentifier() {
                            Domain = ApplicationContext.ConfigurationService.OidRegistrar.GetOid(ClientRegistryOids.CLIENT_CRID).Oid,
                            Identifier = replacementPerson.Id.ToString()
                        }
                    }
                }, Guid.NewGuid().ToString(), HealthServiceRecordSiteRoleType.ReplacementOf, null);
                
            }

            // Now persist the replacement
            IDbConnection conn = DatabasePersistenceService.ConnectionManager.GetConnection();
            IDbTransaction tx = null;
            try
            {
                                  
                // Update container
                var vid = persistenceService.UpdateContainer(mergeRegistrationEvent, DataPersistenceMode.Production);

                tx = conn.BeginTransaction();

                foreach (var id in victimIds)
                {
                    using (IDbCommand cmd = DbUtil.CreateCommandStoredProc(conn, tx))
                    {
                        cmd.CommandText = "mrg_cand";
                        cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "from_id_in", DbType.Decimal, Decimal.Parse(id.Identifier)));
                        cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "to_id_in", DbType.Decimal, Decimal.Parse(survivorId.Identifier)));
                        cmd.ExecuteNonQuery();
                    }
                    // Obsolete the victim identifier merges
                    using (IDbCommand cmd = DbUtil.CreateCommandStoredProc(conn, tx))
                    {
                        cmd.CommandText = "obslt_mrg";
                        cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "from_id_in", DbType.Decimal, Decimal.Parse(id.Identifier)));
                        cmd.ExecuteNonQuery();
                    }

                }
                tx.Commit();
            }
            catch (Exception e)
            {
                if (tx != null)
                    tx.Rollback();
                throw;
            }
            finally
            {
                DatabasePersistenceService.ConnectionManager.ReleaseConnection(conn);
            }
            
        }

        /// <summary>
        /// Mark resolved
        /// </summary>
        public virtual void MarkResolved(VersionedDomainIdentifier recordId)
        {
            // First, we load the survivor
            if (recordId.Domain != ApplicationContext.ConfigurationService.OidRegistrar.GetOid(ClientRegistryOids.REGISTRATION_EVENT).Oid)
                throw new ArgumentException(String.Format("Must be drawn from the '{0}' domain", ApplicationContext.ConfigurationService.OidRegistrar.GetOid(ClientRegistryOids.REGISTRATION_EVENT).Oid), "recordId");

            // Now persist the replacement
            IDbConnection conn = DatabasePersistenceService.ConnectionManager.GetConnection();
            try
            {

                using (IDbCommand cmd = DbUtil.CreateCommandStoredProc(conn, null))
                {
                    cmd.CommandText = "mrg_ignr";
                    cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "hsr_id_in", DbType.Decimal, Decimal.Parse(recordId.Identifier)));
                    cmd.ExecuteNonQuery();
                }

            }
            finally
            {
                DatabasePersistenceService.ConnectionManager.ReleaseConnection(conn);
            }
        }

        /// <summary>
        /// Find conflicts using identifiers
        /// </summary>
        public virtual IEnumerable<SVC.Core.DataTypes.VersionedDomainIdentifier> FindIdConflicts(RegistrationEvent registration)
        {
            
            var registrationService = this.Context.GetService(typeof(IDataRegistrationService)) as IDataRegistrationService;
            VersionedDomainIdentifier[] pid = null;

            // Check if the person exists just via the identifier?
            // This is important because it is not necessarily a merge but an "update if exists"
            var subject = registration.FindComponent(SVC.Core.ComponentModel.HealthServiceRecordSiteRoleType.SubjectOf) as Person;
            QueryParameters qp = new QueryParameters()
            {
                Confidence = 1.0f,
                MatchingAlgorithm = MatchAlgorithm.Exact,
                MatchStrength = MatchStrength.Exact
            };

            var config = this.Context.GetService(typeof(ISystemConfigurationService)) as ISystemConfigurationService;

            List<VersionedDomainIdentifier> conflictIds = new List<VersionedDomainIdentifier>();
            foreach (var id in subject.AlternateIdentifiers)
            {
                var data = config.OidRegistrar.FindData(id.Domain);
                if (data == null || !data.Attributes.Exists(a => a.Key == "IsUniqueIdentifier" && Convert.ToBoolean(a.Value)))
                    continue;
                var patientQuery = new QueryEvent();
                var registrationEvent = new RegistrationEvent();
                patientQuery.Add(registrationEvent, "SUBJ", HealthServiceRecordSiteRoleType.SubjectOf, null);
                patientQuery.Add(qp, "FLT", SVC.Core.ComponentModel.HealthServiceRecordSiteRoleType.FilterOf, null);
                registrationEvent.Add(new Person() { AlternateIdentifiers = new List<DomainIdentifier>() { id }}, "SUBJ", SVC.Core.ComponentModel.HealthServiceRecordSiteRoleType.SubjectOf, null);
                // Perform the query
                pid = registrationService.QueryRecord(patientQuery);
                foreach (var result in pid)
                {
                    if (!conflictIds.Exists(o => o.Identifier.ToString() == result.Identifier))
                        conflictIds.Add(result);
                }
            }
            return conflictIds.ToArray();

        }

        /// <summary>
        /// Find conflicts using fuzzy match
        /// </summary>
        public virtual IEnumerable<VersionedDomainIdentifier> FindFuzzyConflicts(RegistrationEvent registration)
        {
            var registrationService = this.Context.GetService(typeof(IDataRegistrationService)) as IDataRegistrationService;
            var persistenceService = this.Context.GetService(typeof(IDataPersistenceService)) as IDataPersistenceService;

            var clientRegistryConfigService = this.Context.GetService(typeof(IClientRegistryConfigurationService)) as IClientRegistryConfigurationService;

            VersionedDomainIdentifier[] pid = null;

            var subject = registration.FindComponent(SVC.Core.ComponentModel.HealthServiceRecordSiteRoleType.SubjectOf) as Person;
            QueryParameters qp = new QueryParameters()
            {
                Confidence = 1.0f,
                MatchingAlgorithm = MatchAlgorithm.Exact,
                MatchStrength = MatchStrength.Exact
            };
            if (subject.Status != StatusType.Active)
                return pid;

            var patientQuery = new QueryEvent();
            var registrationEvent = new RegistrationEvent();
            patientQuery.Add(registrationEvent, "SUBJ", HealthServiceRecordSiteRoleType.SubjectOf, null);
            patientQuery.Add(qp, "FLT", SVC.Core.ComponentModel.HealthServiceRecordSiteRoleType.FilterOf, null);
            // Create merge filter for fuzzy match
            var ssubject = clientRegistryConfigService.CreateMergeFilter(subject);

            if (ssubject != null) // Minimum criteria was met
            {
                registrationEvent.Add(ssubject, "SUBJ", HealthServiceRecordSiteRoleType.SubjectOf, null);
                pid = registrationService.QueryRecord(patientQuery);
            }
            else
                pid = new VersionedDomainIdentifier[0];


            // Now, look for all with an assigning authority
            if (pid.Length > 0)
            {
                pid = pid.Where(o => !(o.Identifier == registration.Id.ToString() && o.Domain == ApplicationContext.ConfigurationService.OidRegistrar.GetOid(ClientRegistryOids.REGISTRATION_EVENT).Oid)).ToArray();
                // Load each match quickly and ensure that they don't already have a 
                // different identifier from an assigning authority (not CR_CID) provided 
                // in the registration method. For example, if John Smith, Male, 1984-05-22, 123 Main Street West is
                // registered from system X with ID 102 , and a subsequent registration message for John Smith, Male, 1984-05-22, 123 Main Street West
                // is received from system X with ID 104, it is pretty much assured they aren't the same person. If however the
                // latter message came from system Y with ID 104, then the two should be considered a match.
                ssubject.AlternateIdentifiers = new List<DomainIdentifier>();
                List<VersionedDomainIdentifier> exclude = new List<VersionedDomainIdentifier>();
                foreach (var altId in subject.AlternateIdentifiers)
                    if (altId.Domain != ApplicationContext.ConfigurationService.OidRegistrar.GetOid(ClientRegistryOids.CLIENT_CRID).Oid)
                    {
                        var oidData = ApplicationContext.ConfigurationService.OidRegistrar.FindData(altId.Domain);
                        if (oidData == null ||
                            oidData.Attributes.Find(o => o.Key == "IsUniqueIdentifier").Value == null ||
                            Boolean.Parse(oidData.Attributes.Find(o => o.Key == "IsUniqueIdentifier").Value))
                        {
                            ssubject.AlternateIdentifiers.Add(new DomainIdentifier() { Domain = altId.Domain });
                        }
                    }
                

                foreach(var p in pid)
                {
                    var re = persistenceService.GetContainer(p, true) as RegistrationEvent;
                    var pat = re.FindComponent(HealthServiceRecordSiteRoleType.SubjectOf) as Person;
                    if (pat.Id == subject.Id || pat.Status != StatusType.Active ) // same person
                        exclude.Add(re.AlternateIdentifier);
                    else if (pat.AlternateIdentifiers.Exists(o => ssubject.AlternateIdentifiers.Exists(r => r.Domain == o.Domain)))
                        exclude.Add(re.AlternateIdentifier);
                }

                pid = pid.Where(o => !exclude.Exists(i => i.Domain == o.Domain && i.Identifier == o.Identifier)).ToArray();
            }
            return pid;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="recordId"></param>
        /// <returns></returns>
        public IEnumerable<SVC.Core.DataTypes.VersionedDomainIdentifier> GetConflicts(SVC.Core.DataTypes.VersionedDomainIdentifier recordId)
        {
            if(recordId.Domain != ApplicationContext.ConfigurationService.OidRegistrar.GetOid(ClientRegistryOids.REGISTRATION_EVENT).Oid)
                throw new ArgumentException(String.Format("Must be drawn from the '{0}' domain", ApplicationContext.ConfigurationService.OidRegistrar.GetOid(ClientRegistryOids.REGISTRATION_EVENT).Oid), "recordId");

            // Construct connection
            IDbConnection conn = DatabasePersistenceService.ConnectionManager.GetConnection();
            try
            {
                List<VersionedDomainIdentifier> retVal = new List<VersionedDomainIdentifier>();
                using (IDbCommand cmd = DbUtil.CreateCommandStoredProc(conn, null))
                {
                    cmd.CommandText = "get_mrg_cand";
                    cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "hsr_id_in", DbType.Decimal, recordId.Identifier));

                    using(IDataReader rdr = cmd.ExecuteReader())
                        while (rdr.Read())
                        {
                            retVal.Add(new VersionedDomainIdentifier()
                            {
                                Domain= ApplicationContext.ConfigurationService.OidRegistrar.GetOid(ClientRegistryOids.REGISTRATION_EVENT).Oid,
                                Identifier = Convert.ToString(rdr["cand_hsr_id"]),
                                Version = Convert.ToString(rdr["cand_vrsn_id"])
                            });
                        }
                }
                return retVal;
            }
            finally
            {
                DatabasePersistenceService.ConnectionManager.ReleaseConnection(conn);
            }
        }

        /// <summary>
        /// Gets all HSR identifiers where a merge is possible
        /// </summary>
        public virtual IEnumerable<VersionedDomainIdentifier> GetOutstandingConflicts()
        {
            // Construct connection
            IDbConnection conn = DatabasePersistenceService.ConnectionManager.GetConnection();
            try
            {
                List<VersionedDomainIdentifier> retVal = new List<VersionedDomainIdentifier>();
                using (IDbCommand cmd = DbUtil.CreateCommandStoredProc(conn, null))
                {
                    cmd.CommandText = "get_outsd_mrg";
                    using(IDataReader rdr = cmd.ExecuteReader())
                        while (rdr.Read())
                        {
                            retVal.Add(new VersionedDomainIdentifier()
                            {
                                Domain= ApplicationContext.ConfigurationService.OidRegistrar.GetOid(ClientRegistryOids.REGISTRATION_EVENT).Oid,
                                Identifier = Convert.ToString(rdr["hsr_id"]),
                                Version = Convert.ToString(rdr["efft_vrsn_id"])
                            });
                        }
                }
                return retVal;
            }
            finally
            {
                DatabasePersistenceService.ConnectionManager.ReleaseConnection(conn);
            }
        }

        #endregion

        #region IUsesHostContext Members

        /// <summary>
        /// Gets or sets the context for the merge service
        /// </summary>
        public IServiceProvider Context
        {
            get
            {
                return this.m_context;
            }
            set
            {
                this.m_context = value;
            }
        }

        #endregion


        /// <summary>
        /// Update conflict records
        /// </summary>
        /// <param name="oldIdentifier">The old HSR event id</param>
        /// <param name="recordId">The new HSR event id</param>
        public virtual void ObsoleteConflicts(VersionedDomainIdentifier recordId)
        {
            // First, we load the survivor
            if (recordId.Domain != ApplicationContext.ConfigurationService.OidRegistrar.GetOid(ClientRegistryOids.REGISTRATION_EVENT).Oid)
                throw new ArgumentException(String.Format("Must be drawn from the '{0}' domain", ApplicationContext.ConfigurationService.OidRegistrar.GetOid(ClientRegistryOids.REGISTRATION_EVENT).Oid), "recordId");

            // Now persist the replacement
            IDbConnection conn = DatabasePersistenceService.ConnectionManager.GetConnection();
            try
            {
                using (IDbCommand cmd = DbUtil.CreateCommandStoredProc(conn, null))
                {
                    cmd.CommandText = "obslt_mrg";
                    cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "old_hsr_id_in", DbType.Decimal, Decimal.Parse(recordId.Identifier)));
                    cmd.ExecuteNonQuery();
                }
            }
            finally
            {
                DatabasePersistenceService.ConnectionManager.ReleaseConnection(conn);
            }
            
        }
    }
}
