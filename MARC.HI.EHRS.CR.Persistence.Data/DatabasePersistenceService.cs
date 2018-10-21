/**
 * Copyright 2013-2013 Mohawk College of Applied Arts and Technology
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
 * Date: 6-2-2013
 */

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using MARC.HI.EHRS.CR.Core.ComponentModel;
using MARC.HI.EHRS.CR.Persistence.Data.ComponentPersister;
using MARC.HI.EHRS.CR.Persistence.Data.Configuration;
using MARC.HI.EHRS.CR.Persistence.Data.Connection;
using MARC.HI.EHRS.SVC.Core;
using MARC.HI.EHRS.SVC.Core.ComponentModel;
using MARC.HI.EHRS.SVC.Core.ComponentModel.Components;
using MARC.HI.EHRS.SVC.Core.DataTypes;
using MARC.HI.EHRS.SVC.Core.Exceptions;
using MARC.HI.EHRS.SVC.Core.Issues;
using MARC.HI.EHRS.SVC.Core.Services;
using System.Text;
using MARC.HI.EHRS.CR.Core.Services;
using MARC.Everest.Threading;
using MARC.HI.EHRS.CR.Core;

namespace MARC.HI.EHRS.CR.Persistence.Data
{
    /// <summary>
    /// Implements a persistence service that communicates with the database system
    /// </summary>
    /// <remarks>
    /// The document registration service code could be much much better. Currently I create a string
    /// that acts as a query filter to get the database to return only the important stuff, however 
    /// is would be much cleaner (probably faster) to actually call the db provider's SP to get the data an
    /// and build the result set in memory.
    /// </remarks>
    [Description("ADO.NET Persistence Service")]
    public class DatabasePersistenceService : IDataPersistenceService, IDataRegistrationService, IDisposable
    {
        /// <summary>
        /// Host context for instance
        /// </summary>
        private IServiceProvider m_hostContext;

        // Matcher wtp
        private WaitThreadPool m_threadPool = new WaitThreadPool(Environment.ProcessorCount);
        // Disposed?
        private bool m_disposed = false;

        private IClientRegistryMergeService m_clientRegistryMerge;

        // Notification service
        private IClientNotificationService m_notificationService;

        // Client registry configuration
        private IClientRegistryConfigurationService m_clientRegistryConfiguration;

        /// <summary>
        /// Configuration section handler
        /// </summary>
        private static ConfigurationSectionHandler m_configuration;

        /// <summary>
        /// Gets the validation settings for the context
        /// </summary>
        internal static ValidationSection ValidationSettings { get { return m_configuration.Validation; } }

        /// <summary>
        /// A list of persisters that can be used to save and read data
        /// </summary>
        private static Dictionary<Type, IComponentPersister> m_persisters;

        /// <summary>
        /// Connection manager
        /// </summary>
        public static ConnectionManager ConnectionManager { get { return m_configuration.ConnectionManager; } }

        /// <summary>
        /// Connection manager
        /// </summary>
        internal static ConnectionManager ReadOnlyConnectionManager { get { return m_configuration.ReadonlyConnectionManager; } }

        /// <summary>
        /// Get the persister for the specified type
        /// </summary>
        internal static IComponentPersister GetPersister(Type forType)
        {
            IComponentPersister pPersister = null;
            if (m_persisters.TryGetValue(forType, out pPersister))
                return pPersister;
#if DEBUG
            Trace.TraceWarning("Can't find a persister for '{0}'", forType);
#endif
            return null;
        }

        /// <summary>
        /// Static constructor for this persistence service
        /// </summary>
        static DatabasePersistenceService()
        {
            m_configuration = ConfigurationManager.GetSection("marc.hi.ehrs.cr.persistence.data") as ConfigurationSectionHandler;
            m_persisters = new Dictionary<Type, IComponentPersister>();

            // Verify that the database can be used
            foreach (var cm in new ConnectionManager[] { m_configuration.ConnectionManager, m_configuration.ReadonlyConnectionManager })
            {
                var conn = cm.GetConnection();
                try
                {
                    var dbVer = DbUtil.GetSchemaVersion(conn);
                    if (dbVer.CompareTo(typeof(DatabasePersistenceService).Assembly.GetName().Version) < 0)
                        throw new DataException(String.Format("The schema version '{0}' is less than the expected version of '{1}'",
                            dbVer, typeof(DatabasePersistenceService).Assembly.GetName().Version));
                    else
                        Trace.TraceInformation("Using Client Registry Schem Version '{0}'", dbVer);
                }
                finally
                {
                    conn.Close();
                }
            }

            // Scan this assembly for helpers
            Type[] persistenceTypes = Array.FindAll<Type>(typeof(DatabasePersistenceService).Assembly.GetTypes(), o => o.GetInterface(typeof(IComponentPersister).FullName) != null);


            // Persistence helpers
            foreach (var t in persistenceTypes)
            {

                ConstructorInfo ci = t.GetConstructor(Type.EmptyTypes);
                IComponentPersister instance = ci.Invoke(null) as IComponentPersister;
                m_persisters.Add(instance.HandlesComponent, instance);
            }

        }

        /// <summary>
        /// Aync mark conflicts code
        /// </summary>
        private void MarkConflictsAsync(object state)
        {
            Trace.TraceInformation("Performing fuzzy conflict check asynchronously");
            try
            {
                VersionedDomainIdentifier vid = state as VersionedDomainIdentifier;
                RegistrationEvent hsrEvent = this.GetContainer(vid, true) as RegistrationEvent;
                var pid = this.m_clientRegistryMerge.FindFuzzyConflicts(hsrEvent);

                Trace.TraceInformation("Post-Update Record matched with {0} records", pid.Count());
                if (pid.Count() > 0)
                    this.m_clientRegistryMerge.MarkConflicts(hsrEvent.AlternateIdentifier, pid);
            }
            catch(Exception e)
            {
                Trace.TraceError(e.ToString());
            }
        }
        #region IDataPersistenceService Members


        /// <summary>
        /// Store a container
        /// </summary>
        public VersionedDomainIdentifier StoreContainer(System.ComponentModel.IContainer storageData, DataPersistenceMode mode)
        {
            if (m_disposed) throw new ObjectDisposedException("DatabasePersistenceService");

            // Merge
            IEnumerable<VersionedDomainIdentifier> pid = null;
            var regEvent = storageData as RegistrationEvent;
            if (this.m_clientRegistryMerge != null && regEvent != null)
            {

                bool fuzzy = false;
                pid = this.m_clientRegistryMerge.FindIdConflicts(regEvent);
                
                
                // Do we have a match?
                if (pid.Count() == 0) // if we didn't find any id conflicts go to fuzzy mode
                {
                    if (this.m_clientRegistryConfiguration.Configuration.Registration.AutoMerge) // we have to do this now :(
                    {
                        fuzzy = true;
                        pid = this.m_clientRegistryMerge.FindFuzzyConflicts(regEvent);
                        if (pid.Count() == 1)
                        {
                            regEvent.AlternateIdentifier = pid.First();
                            Trace.TraceInformation("Matched with {0} records (fuzzy={1}, autoOn={2}, updateEx={3})",
                                   pid.Count(), fuzzy, this.m_clientRegistryConfiguration.Configuration.Registration.AutoMerge,
                                   this.m_clientRegistryConfiguration.Configuration.Registration.UpdateIfExists);
                            return this.UpdateContainer(regEvent, mode);
                        }
                    }
                }
                else if(this.m_clientRegistryConfiguration.Configuration.Registration.UpdateIfExists)
                {
                    // Update
                    if (pid.Count() == 1)
                    {
                        Trace.TraceInformation("Updating record {0} because it matched by identifier", regEvent.Id);
                        regEvent.AlternateIdentifier = pid.First();
                        return this.UpdateContainer(regEvent, mode);
                    }
                }
            }
            else
                pid = new List<VersionedDomainIdentifier>();

            //// do a sanity check, have we already persisted this record?
            if (!ValidationSettings.AllowDuplicateRecords)
            {
                var duplicateQuery = new QueryEvent();
                duplicateQuery.Add((storageData as ICloneable).Clone() as HealthServiceRecordContainer, "SUBJ", HealthServiceRecordSiteRoleType.SubjectOf, null);
                duplicateQuery.Add(new QueryParameters()
                {
                    MatchStrength = Core.ComponentModel.MatchStrength.Exact,
                    MatchingAlgorithm = MatchAlgorithm.Exact
                }, "FLT", HealthServiceRecordSiteRoleType.FilterOf, null);
                var storedRecords = QueryRecord(duplicateQuery as IComponent);
                if (storedRecords.Length != 0)
                    throw new DuplicateNameException(ApplicationContext.LocaleService.GetString("DTPE004"));
            }

            // Get the persister
            IComponentPersister persister = GetPersister(storageData.GetType());
            if (persister != null)
            {
                IDbTransaction tx = null;
                IDbConnection conn = null;
                try
                {
                    conn = DatabasePersistenceService.ConnectionManager.GetConnection();
                    tx = conn.BeginTransaction();

                    var retVal = persister.Persist(conn, tx, storageData as IComponent, false);

                    // Set the mode
                    if (m_configuration.OverridePersistenceMode.HasValue)
                        mode = m_configuration.OverridePersistenceMode.Value;

                    // Commit or rollback
                    if (mode == DataPersistenceMode.Production)
                    {

                        tx.Commit();
                        tx = null;

                        // Notify that reconciliation is required and mark merge candidates 
                        if (pid.Count() > 0)
                        {
                            this.m_clientRegistryMerge.MarkConflicts(retVal, pid);
                            if (this.m_notificationService != null && pid.Count() != 0)
                            {
                                var list = new List<VersionedDomainIdentifier>(pid) { retVal };
                                this.m_notificationService.NotifyReconciliationRequired(list);
                            }
                        }
                        else if (this.m_clientRegistryMerge != null)
                            this.m_threadPool.QueueUserWorkItem(this.MarkConflictsAsync, retVal);

                        // Notify register
                        if (this.m_notificationService != null && storageData is RegistrationEvent)
                            this.m_notificationService.NotifyRegister(storageData as RegistrationEvent);

                    }
                    else
                        tx.Rollback();

                    return retVal;
                }
                catch (Exception e)
                {
                    if (tx != null)
                        tx.Rollback();
                    throw;
                }
                finally
                {
                    if (tx != null)
                        tx.Dispose();
                    if (conn != null)
                    {
                        DatabasePersistenceService.ConnectionManager.ReleaseConnection(conn);
                        conn = null;
                    }
                }
            }
            else
            {
                Trace.TraceError("Can't find a persistence handler for type '{0}'", storageData.GetType().Name);
                throw new DataException(String.Format("Can't persist type '{0}'", storageData.GetType().Name));
            }
        }

        /// <summary>
        /// Update container
        /// </summary>
        public VersionedDomainIdentifier UpdateContainer(System.ComponentModel.IContainer storageData, DataPersistenceMode mode)
        {
            if (m_disposed) throw new ObjectDisposedException("DatabasePersistenceService");

            if (storageData == null)
                throw new ArgumentNullException("storageData");

            // Get the persister
            IComponentPersister persister = GetPersister(storageData.GetType());
            ISystemConfigurationService configService = ApplicationContext.ConfigurationService;

            if (persister != null)
            {
                IDbTransaction tx = null;
                IDbConnection conn = null;
                try
                {
                    conn = DatabasePersistenceService.ConnectionManager.GetConnection();

                    RegistrationEvent hsrEvent = storageData as RegistrationEvent;

                    // Is the record something that we have access to?
                    if (hsrEvent.AlternateIdentifier != null && !hsrEvent.AlternateIdentifier.Domain.Equals(configService.OidRegistrar.GetOid(ClientRegistryOids.REGISTRATION_EVENT).Oid))
                        throw new ArgumentException(String.Format("The record OID '{0}' cannot be retrieved by this repository, expecting OID '{1}'",
                            hsrEvent.AlternateIdentifier.Domain, configService.OidRegistrar.GetOid(ClientRegistryOids.REGISTRATION_EVENT).Oid));

                    decimal tryDec = default(decimal);
                    bool isDirectUpdate = false;

                    // Is there no event identifier ?
                    if (hsrEvent.AlternateIdentifier != null && !Decimal.TryParse(hsrEvent.AlternateIdentifier.Identifier, out tryDec))
                        throw new ArgumentException(String.Format("The identifier '{0}' is not a valid identifier for this repository", hsrEvent.AlternateIdentifier.Identifier));
                    else if (hsrEvent.AlternateIdentifier == null) // The alternate identifier is null ... so we need to look up the registration event to version ... interesting....
                    {
                        this.EnrichRegistrationEvent(conn, tx, hsrEvent);
                    }
                    else
                    {
                        // Get the person name
                        isDirectUpdate = true; // Explicit update
                    }

                    // Validate and duplicate the components that are to be loaded as part of the new version
                    var oldHsrEvent = GetContainer(hsrEvent.AlternateIdentifier, true) as RegistrationEvent; // Get the old container
                    if (oldHsrEvent == null)
                        throw new MissingPrimaryKeyException(String.Format("Record {1}^^^&{0}&ISO does not exist", hsrEvent.AlternateIdentifier.Domain, hsrEvent.AlternateIdentifier.Identifier));

                    PersonPersister cp = new PersonPersister();

                    // Validate the old record target
                    Person oldRecordTarget = oldHsrEvent.FindComponent(HealthServiceRecordSiteRoleType.SubjectOf) as Person,
                        newRecordTarget = hsrEvent.FindComponent(HealthServiceRecordSiteRoleType.SubjectOf) as Person;

                    Person verifyRecordTarget = null;
                    if (!isDirectUpdate)
                    {
                        Trace.TraceInformation("Not Direct update, enriching the Patient Data");
                        int idCheck = 0;
                        while (verifyRecordTarget == null || idCheck > newRecordTarget.AlternateIdentifiers.Count)
                            verifyRecordTarget = cp.GetPerson(conn, null, newRecordTarget.AlternateIdentifiers[idCheck++], true);

                        if (verifyRecordTarget == null || oldRecordTarget.Id != verifyRecordTarget.Id)
                            throw new ConstraintException("The update request specifies a different subject than the request currently stored");
                    }
                    else
                        verifyRecordTarget = oldRecordTarget;
                    //newRecordTarget.VersionId = verifyRecordTarget.VersionId;
                    newRecordTarget.Id = verifyRecordTarget.Id;

                    // VAlidate classific
                    if (oldHsrEvent.EventClassifier != hsrEvent.EventClassifier &&
                        (hsrEvent.EventClassifier & RegistrationEventType.Register) == RegistrationEventType.None)
                        throw new ConstraintException("Record type mis-match between update data and the data already in the persistence store");

                    if (oldHsrEvent.LanguageCode != hsrEvent.LanguageCode)
                        throw new ConstraintException("Record language mis-match between update data and data already in persistence store. To change the language use a \"replaces\" relationship rather than an update");

                    // Are we performing a component update? If so the only two components we want freshened are the CommentOn (for adding comments)
                    // and the ReasonFor | OlderVersionOf comment for change summaries
                    if ((hsrEvent.EventClassifier & RegistrationEventType.ComponentEvent) != 0)
                    {
                        for (int i = hsrEvent.XmlComponents.Count - 1; i >= 0; i--)
                            if (((hsrEvent.XmlComponents[i].Site as HealthServiceRecordSite).SiteRoleType & (HealthServiceRecordSiteRoleType.CommentOn | HealthServiceRecordSiteRoleType.ReasonFor | HealthServiceRecordSiteRoleType.OlderVersionOf | HealthServiceRecordSiteRoleType.TargetOf)) == 0)
                                hsrEvent.Remove(hsrEvent.XmlComponents[i]);
                    }
                    
                    // Copy over any components that aren't already specified or updated in the new record
                    // Merge the old and new. Sets the update mode appropriately
                    cp.MergePersons(newRecordTarget, oldRecordTarget);

                    // Next we copy this as a replacement of
                    hsrEvent.RemoveAllFromRole(HealthServiceRecordSiteRoleType.SubjectOf);
                    hsrEvent.Add(newRecordTarget, "SUBJ", HealthServiceRecordSiteRoleType.SubjectOf, null);

                    // Begin and update
                    tx = conn.BeginTransaction();

                    var retVal = persister.Persist(conn, tx, storageData as IComponent, true);

                    if (m_configuration.OverridePersistenceMode.HasValue)
                        mode = m_configuration.OverridePersistenceMode.Value;

                    if (mode == DataPersistenceMode.Production)
                    {
                        tx.Commit();
                        tx = null;

                        // Mark conflicts if any are outstanding pointing at the old versionj
                        if (this.m_clientRegistryMerge != null)
                        {
                            var existingConflicts = this.m_clientRegistryMerge.GetConflicts(oldHsrEvent.AlternateIdentifier);
                            if (existingConflicts.Count() > 0)
                            {
                                Trace.TraceInformation("Obsoleting existing conlflicts resolved");
                                this.m_clientRegistryMerge.ObsoleteConflicts(oldHsrEvent.AlternateIdentifier);
                            }
                            this.m_threadPool.QueueUserWorkItem(this.MarkConflictsAsync, retVal);
                        }

                        // Notify register
                        if (this.m_notificationService != null)
                        {

                            if (hsrEvent.Mode == RegistrationEventType.Replace)
                                this.m_notificationService.NotifyDuplicatesResolved(hsrEvent);
                            else
                                this.m_notificationService.NotifyUpdate(hsrEvent);
                        }
                    }
                    else
                        tx.Rollback();


                    return retVal;
                }
                catch (Exception e)
                {
                    if (tx != null)
                        tx.Rollback();
                    throw;
                }
                finally
                {
                    if (tx != null)
                        tx.Dispose();
                    if (conn != null)
                    {
                        DatabasePersistenceService.ConnectionManager.ReleaseConnection(conn);
                        conn = null;
                    }
                }
            }
            else
            {
                Trace.TraceError("Can't find a persistence handler for type '{0}'", storageData.GetType().Name);
                throw new DataException(String.Format("Can't persist type '{0}'", storageData.GetType().Name));
            }
        }

        /// <summary>
        /// Enrich the registration event
        /// </summary>
        internal void EnrichRegistrationEvent(IDbConnection conn, IDbTransaction tx, RegistrationEvent hsrEvent)
        {
            decimal tryDec;
            ISystemConfigurationService configService = ApplicationContext.ConfigurationService;

            // Create a query based on the person 
            Person subject = hsrEvent.FindComponent(HealthServiceRecordSiteRoleType.SubjectOf) as Person;

            if (subject.AlternateIdentifiers.Exists(o => o.Domain == configService.OidRegistrar.GetOid(ClientRegistryOids.CLIENT_CRID).Oid))
                subject = new Person()
                {
                    AlternateIdentifiers = new List<DomainIdentifier>(subject.AlternateIdentifiers.FindAll(o => o.Domain == configService.OidRegistrar.GetOid(ClientRegistryOids.CLIENT_CRID).Oid)),
                    Status = StatusType.Normal
                };
            else if (subject.AlternateIdentifiers.Exists(o => o is AuthorityAssignedDomainIdentifier))
                subject = new Person()
                {
                    AlternateIdentifiers = new List<DomainIdentifier>(subject.AlternateIdentifiers.FindAll(o => o is AuthorityAssignedDomainIdentifier)),
                    Status = StatusType.Normal
                };
            else
                subject = new Person()
                {
                    AlternateIdentifiers = new List<DomainIdentifier>(subject.AlternateIdentifiers),
                    Status = StatusType.Normal
                };

            QueryEvent query = new QueryEvent();
            RegistrationEvent evt = new RegistrationEvent();
            query.Add(evt, "SUBJ", HealthServiceRecordSiteRoleType.SubjectOf, null);
            evt.Status = StatusType.Active | StatusType.Obsolete;
            evt.Add(subject, "SUBJ", HealthServiceRecordSiteRoleType.SubjectOf, null);
            query.Add(new QueryParameters()
            {
                MatchingAlgorithm = MatchAlgorithm.Exact,
                MatchStrength = MatchStrength.Exact
            }, "FLTR", HealthServiceRecordSiteRoleType.FilterOf, null);
            var tRecordIds = QueryRecord(query);
            if (tRecordIds.Length != 1)
                throw new MissingPrimaryKeyException(ApplicationContext.LocaleService.GetString("DBCF004"));
            else if (tRecordIds[0].Domain != configService.OidRegistrar.GetOid(ClientRegistryOids.REGISTRATION_EVENT).Oid)
                throw new MissingPrimaryKeyException(ApplicationContext.LocaleService.GetString("DBCF005"));

            tryDec = Decimal.Parse(tRecordIds[0].Identifier);
            hsrEvent.AlternateIdentifier = tRecordIds[0];
        }

        /// <summary>
        /// Get container
        /// </summary>
        public IContainer GetContainer(MARC.HI.EHRS.SVC.Core.DataTypes.VersionedDomainIdentifier containerId, bool loadFast)
        {
            // Get the persister that will handle the HealthServiceRecord type

            var persister = m_persisters.Values.FirstOrDefault(o => o is IQueryComponentPersister && (o as IQueryComponentPersister).ComponentTypeOid == containerId.Domain);
            if (persister == null)
                persister = GetPersister(typeof(RegistrationEvent));

            ISystemConfigurationService configService = Context.GetService(typeof(ISystemConfigurationService)) as ISystemConfigurationService;

            DbUtil.ClearPersistedCache();

            if (persister != null)
            {
                IDbConnection conn = null;
                try
                {
                    conn = DatabasePersistenceService.ReadOnlyConnectionManager.GetConnection();

                    // Is the record something that we have access to?
                    //if (containerId.Domain != null && !containerId.Domain.Equals(configService.OidRegistrar.GetOid(ClientRegistryOids.REGISTRATION_EVENT).Oid))
                    //    throw new ArgumentException(String.Format("The record OID '{0}' cannot be retrieved by this repository, expecting OID '{1}'",
                    //        containerId.Domain, configService.OidRegistrar.GetOid(ClientRegistryOids.REGISTRATION_EVENT).Oid));
                    if (persister == null)
                        throw new ArgumentException(String.Format("The record type OID '{0}' cannot be retrieved by this repository",
                            containerId.Domain));
                    decimal tryDec = default(decimal);

                    if (Decimal.TryParse(containerId.Identifier, out tryDec))
                        return persister.DePersist(conn, tryDec, null, null, loadFast) as IContainer;
                    else
                        throw new ArgumentException(String.Format("The identifier '{0}' is not a valid identifier for this repository", containerId.Identifier));
                }
                finally
                {
                    if (conn != null)
                        DatabasePersistenceService.ConnectionManager.ReleaseConnection(conn);
                }
            }
            else
            {
                Trace.TraceError("Can't find a persistence handler for type '{0}'", containerId.Domain);
                throw new DataException("Can't persist type HealthServiceRecordEvent");
            }
        }


        #endregion

        #region IUsesHostContext Members

        public IServiceProvider Context
        {
            get { return m_hostContext; }
            set
            {
                ApplicationContext.CurrentContext = value;
                this.m_hostContext = value;
                this.m_clientRegistryMerge = value.GetService(typeof(IClientRegistryMergeService)) as IClientRegistryMergeService;
                this.m_notificationService = value.GetService(typeof(IClientNotificationService)) as IClientNotificationService;
                this.m_clientRegistryConfiguration = value.GetService(typeof(IClientRegistryConfigurationService)) as IClientRegistryConfigurationService;
            }
        }

        #endregion

        #region IDocumentRegistrationService Members

        /// <summary>
        /// Register a record with the document registration service
        /// </summary>
        public bool RegisterRecord(IComponent recordComponent, DataPersistenceMode mode)
        {
            if (m_disposed) throw new ObjectDisposedException("DatabasePersistenceService");

#if DEBUG
            Trace.TraceInformation("This client registration system does not require registration");
#endif

            return true;
        }

        /// <summary>
        /// Query the registration system for records
        /// </summary>
        public VersionedDomainIdentifier[] QueryRecord(IComponent queryParameters)
        {
            if (m_disposed) throw new ObjectDisposedException("DatabasePersistenceService");

            // TODO: Store consent policy override if applicable
            List<VersionedDomainIdentifier> retVal = new List<VersionedDomainIdentifier>(30);

            ISystemConfigurationService configService = this.Context.GetService(typeof(ISystemConfigurationService)) as ISystemConfigurationService;

            // Get the subject of the query
            var queryFilter = (queryParameters as HealthServiceRecordContainer).FindComponent(HealthServiceRecordSiteRoleType.FilterOf) as QueryParameters;

            // Query Filter
            if (queryFilter == null || queryFilter.MatchingAlgorithm == MatchAlgorithm.Unspecified)
                queryFilter = new QueryParameters()
                {
                    MatchStrength = DatabasePersistenceService.ValidationSettings.DefaultMatchStrength,
                    MatchingAlgorithm = DatabasePersistenceService.ValidationSettings.DefaultMatchAlgorithms
                };

            // Subject of the query
            QueryEvent queryEvent = queryParameters as QueryEvent;

            // Connect to the database
            IDbConnection conn = m_configuration.ReadonlyConnectionManager.GetConnection();
            try
            {
                using (IDbCommand cmd = conn.CreateCommand())
                {

                    var subject = queryEvent.FindComponent(HealthServiceRecordSiteRoleType.SubjectOf);
                    string queryFilterString = String.Format("{0}", DbUtil.BuildQueryFilter(subject, this.Context, queryFilter.MatchingAlgorithm == MatchAlgorithm.Exact));
                    cmd.CommandType = CommandType.Text;
                    cmd.CommandText = queryFilterString;

#if DEBUG
                    Trace.TraceInformation(cmd.CommandText);
#endif           
                    try
                    {
                        using (IDataReader rdr = cmd.ExecuteReader())
                        {
                            // Read all results
                            while (rdr.Read())
                            {

                                // Id
                                var id = new VersionedResultIdentifier()
                                {
                                    Domain = GetQueryPersister(subject.GetType()).ComponentTypeOid,
                                    Identifier = Convert.ToString(rdr[0]),
                                    Version = Convert.ToString(rdr[1])
                                };
                                // Add the ID
                                retVal.Add(id);
                                if (retVal.Count % 30 == 29)
                                    retVal.Capacity += 30;
                            }
                        }

                    }
                    catch
                    {
                        Trace.TraceInformation("Query in error: {0}", queryFilterString);
                        throw;
                    }

                }
            }
            catch (Exception e)
            {
                throw new DataException("Query error : " + e.Message, e);
            }
            finally
            {
                m_configuration.ConnectionManager.ReleaseConnection(conn);
            }

            //retVal.Sort((a, b) => b.Identifier.CompareTo(a.Identifier));
#if DEBUG
            Trace.TraceInformation("{0} records returned by function", retVal.Count);
#endif
            return retVal.ToArray();
        }

        #endregion

        /// <summary>
        /// Get the register
        /// </summary>
        internal static IQueryComponentPersister GetQueryPersister(Type forType)
        {
            IComponentPersister pPersister = null;

            if (m_persisters.TryGetValue(forType, out pPersister))
                return pPersister as IQueryComponentPersister;
#if DEBUG
            Trace.TraceError("Can't find register for '{0}'", forType);
#endif
            return null;
        }

        /// <summary>
        /// Dispose
        /// </summary>
        public void Dispose()
        {
            this.m_threadPool.Dispose();
            this.m_disposed = true;

        }
    }
}
