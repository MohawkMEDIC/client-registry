/* 
 * Copyright 2008-2011 Mohawk College of Applied Arts and Technology
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
 * User: Justin Fyfe
 * Date: 08-24-2011
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
using MARC.HI.EHRS.CR.Persistence.Data.Configuration;
using MARC.HI.EHRS.CR.Persistence.Data.ComponentPersister;
using System.Text;

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
    public class DatabasePersistenceService : IDataPersistenceService, IDataRegistrationService
    {
        /// <summary>
        /// Host context for instance
        /// </summary>
        private HostContext m_hostContext;

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
        internal static ConnectionManager ConnectionManager { get { return m_configuration.ConnectionManager; } }

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


        #region IDataPersistenceService Members

        /// <summary>
        /// Store a container
        /// </summary>
        public VersionedDomainIdentifier StoreContainer(System.ComponentModel.IContainer storageData, DataPersistenceMode mode)
        {

            // TODO: do a sanity check, have we already persisted this record?
            if (!ValidationSettings.AllowDuplicateRecords)
            {
                var duplicateQuery = (storageData as ICloneable).Clone() as HealthServiceRecordContainer;
                duplicateQuery.Add(new QueryParameters()
                {
                    MatchStrength = Core.ComponentModel.MatchStrength.Exact,
                    MatchingAlgorithm = MatchAlgorithm.Exact
                });
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

                    if (mode == DataPersistenceMode.Production)
                        tx.Commit();
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

                    // Is there no event identifier ?
                    if(hsrEvent.AlternateIdentifier != null && !Decimal.TryParse(hsrEvent.AlternateIdentifier.Identifier, out tryDec))
                        throw new ArgumentException(String.Format("The identifier '{0}' is not a valid identifier for this repository", hsrEvent.AlternateIdentifier.Identifier));
                    else if (hsrEvent.AlternateIdentifier == null) // The alternate identifier is null ... so we need to look up the registration event to version ... interesting....
                    {
                        // Create a query based on the person 
                        Person subject = hsrEvent.FindComponent(HealthServiceRecordSiteRoleType.SubjectOf) as Person;
                        subject = new Person() {
                            AlternateIdentifiers = new List<DomainIdentifier>(subject.AlternateIdentifiers)
                        };
                        RegistrationEvent query = new RegistrationEvent();
                        query.Status = StatusType.Active;
                        query.Add(subject, "SUBJ", HealthServiceRecordSiteRoleType.SubjectOf, null);
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


                    // Validate and duplicate the components that are to be loaded as part of the new version
                    var oldHsrEvent = GetContainer(hsrEvent.AlternateIdentifier, true) as RegistrationEvent; // Get the old container
                    if(oldHsrEvent == null)
                        throw new MissingPrimaryKeyException(String.Format("Record {1}^^^&{0}&ISO does not exist", hsrEvent.AlternateIdentifier.Domain, hsrEvent.AlternateIdentifier.Identifier));

                    PersonPersister cp = new PersonPersister();
                    
                    // Validate the old record target
                    Person oldRecordTarget = oldHsrEvent.FindComponent(HealthServiceRecordSiteRoleType.SubjectOf) as Person,
                        newRecordTarget = hsrEvent.FindComponent(HealthServiceRecordSiteRoleType.SubjectOf) as Person;
                    Person verifyRecordTarget = null;
                    int idCheck = 0;
                    while(verifyRecordTarget == null || idCheck > newRecordTarget.AlternateIdentifiers.Count)
                        verifyRecordTarget = cp.GetPerson(conn, null, newRecordTarget.AlternateIdentifiers[idCheck++], true);
                    
                    if (verifyRecordTarget == null || oldRecordTarget.Id != verifyRecordTarget.Id)
                        throw new ConstraintException("The update request specifies a different subject than the request currently stored");

                    newRecordTarget.VersionId = verifyRecordTarget.VersionId;
                    newRecordTarget.Id = verifyRecordTarget.Id;

                    // VAlidate classific
                    if (oldHsrEvent.EventClassifier != hsrEvent.EventClassifier &&
                        (hsrEvent.EventClassifier & RegistrationEventType.Register) == RegistrationEventType.None)
                        throw new ConstraintException("Record type mis-match between update data and the data already in the persistence store");

                    if (oldHsrEvent.LanguageCode != hsrEvent.LanguageCode)
                        throw new ConstraintException("Record language mis-match between update data and data already in persistence store. To change the language use a \"replaces\" relationship rather than an update");

                    // Are we performing a component update? If so the only two components we want freshened are the CommentOn (for adding comments)
                    // and the ReasonFor | OlderVersionOf comment for change summaries
                    if((hsrEvent.EventClassifier & RegistrationEventType.ComponentEvent) != 0)
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

                    if (mode == DataPersistenceMode.Production)
                        tx.Commit();
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
        /// Get container
        /// </summary>
        public IContainer GetContainer(MARC.HI.EHRS.SVC.Core.DataTypes.VersionedDomainIdentifier containerId, bool loadFast)
        {
            // Get the persister that will handle the HealthServiceRecord type
            IComponentPersister persister = GetPersister(typeof(RegistrationEvent));
            ISystemConfigurationService configService = Context.GetService(typeof(ISystemConfigurationService)) as ISystemConfigurationService;
            
            DbUtil.ClearPersistedCache();

            if (persister != null)
            {
                IDbConnection conn = null;
                try
                {
                    conn = DatabasePersistenceService.ReadOnlyConnectionManager.GetConnection();

                    // Is the record something that we have access to?
                    if (containerId.Domain != null && !containerId.Domain.Equals(configService.OidRegistrar.GetOid(ClientRegistryOids.REGISTRATION_EVENT).Oid))
                        throw new ArgumentException(String.Format("The record OID '{0}' cannot be retrieved by this repository, expecting OID '{1}'",
                            containerId.Domain, configService.OidRegistrar.GetOid(ClientRegistryOids.REGISTRATION_EVENT).Oid));

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
                Trace.TraceError("Can't find a persistence handler for type 'HealthServiceRecordEvent'");
                throw new DataException("Can't persist type HealthServiceRecordEvent");
            }
        }


        #endregion

        #region IUsesHostContext Members

        public MARC.HI.EHRS.SVC.Core.HostContext Context
        {
            get { return m_hostContext; }
            set
            {
                ApplicationContext.CurrentContext = value;
                this.m_hostContext = value;
            }
        }

        #endregion

        #region IDocumentRegistrationService Members

        /// <summary>
        /// Register a record with the document registration service
        /// </summary>
        public bool RegisterRecord(IComponent recordComponent, DataPersistenceMode mode)
        {
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

            // TODO: Store consent policy override if applicable
            List<VersionedDomainIdentifier> retVal = new List<VersionedDomainIdentifier>(10);
            ISystemConfigurationService configService = this.Context.GetService(typeof(ISystemConfigurationService)) as ISystemConfigurationService;

            if (!(queryParameters is RegistrationEvent))
                throw new ArgumentException("Must inherit from HealthServiceRecordEvent", "queryParameters");

            // Get the subject of the query
            var subjectOfQuery = (queryParameters as HealthServiceRecordContainer).FindComponent(HealthServiceRecordSiteRoleType.SubjectOf) as Person;
            var queryFilter = (queryParameters as HealthServiceRecordContainer).FindComponent(HealthServiceRecordSiteRoleType.FilterOf) as QueryParameters;
            if (subjectOfQuery == null)
                throw new InvalidOperationException();

            // Query Filter
            if (queryFilter == null || queryFilter.MatchingAlgorithm == MatchAlgorithm.Unspecified)
                queryFilter = new QueryParameters()
                {
                    MatchStrength = DatabasePersistenceService.ValidationSettings.DefaultMatchStrength,
                    MatchingAlgorithm = DatabasePersistenceService.ValidationSettings.DefaultMatchAlgorithms
                };


            // Connect to the database
            IDbConnection conn = m_configuration.ReadonlyConnectionManager.GetConnection();
            try
            {
                using (IDbCommand cmd = conn.CreateCommand())
                {

                    for (int i = 0; i < 2; i++)
                    {

                        // Matching?
                        StringBuilder sb = new StringBuilder("SELECT DISTINCT HSR_ID FROM HSR_VRSN_TBL INNER JOIN PSN_VRSN_TBL ON (PSN_VRSN_TBL.REG_VRSN_ID = HSR_VRSN_TBL.HSR_VRSN_ID) WHERE PSN_VRSN_TBL.OBSLT_UTC IS NULL AND PSN_ID IN (");
                        // Identifiers
                        if (subjectOfQuery.AlternateIdentifiers != null && subjectOfQuery.AlternateIdentifiers.Count > 0)
                            sb.AppendFormat("({0}) INTERSECT ", BuildFilterIdentifiers(subjectOfQuery.AlternateIdentifiers));
                        if (subjectOfQuery.Names != null && subjectOfQuery.Names.Count > 0)
                            sb.AppendFormat("({0}) INTERSECT ", BuildFilterNames(subjectOfQuery.Names, i == 1 ? queryFilter : new QueryParameters() { MatchingAlgorithm = MatchAlgorithm.Exact }));
                        else
                            i = 2;
                        if (subjectOfQuery.OtherIdentifiers != null && subjectOfQuery.OtherIdentifiers.Count > 0)
                            sb.AppendFormat("({0}) INTERSECT ", BuildFilterIdentifiers(subjectOfQuery.OtherIdentifiers));
                        // TRIM INTERSECT
                        if (sb.ToString().EndsWith("INTERSECT "))
                            sb.Remove(sb.Length - 10, 10);
                        sb.Append(")");

                        cmd.CommandText = sb.ToString();
                        cmd.CommandType = CommandType.Text;
                        using (IDataReader rdr = cmd.ExecuteReader())
                        {
                            // Read all results
                            while (rdr.Read())
                            {

                                // Id
                                var id = new VersionedResultIdentifier()
                                {
                                    Domain = configService.OidRegistrar.GetOid(ClientRegistryOids.REGISTRATION_EVENT).Oid,
                                    Identifier = Convert.ToString(rdr["hsr_id"])

                                };
                                // Add the ID
                                if (!retVal.Exists(o => o.Identifier == id.Identifier))
                                    retVal.Add(id);

                            }
                        }
                    }
                }
            }
            finally
            {
                m_configuration.ConnectionManager.ReleaseConnection(conn);
            }


            return retVal.ToArray();
        }

        /// <summary>
        /// Build filter identifiers
        /// </summary>
        private object BuildFilterIdentifiers(List<KeyValuePair<CodeValue, DomainIdentifier>> identifiers)
        {
            StringBuilder retVal = new StringBuilder();
            foreach (var id in identifiers)
            {
                retVal.AppendFormat("SELECT PSN_ID FROM GET_PSN_EXTERN('{0}','{1}')", id.Value.Domain.Replace("'", "''"), id.Value.Identifier.Replace("'", "''"), identifiers.Count * 4);
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
                    filterString.AppendFormat("{0}{1}", cmp.Value, cmp == nm.Parts.Last() ? "" : ",");
                    cmpTypeString.AppendFormat("{0}{1}", (decimal)cmp.Type, cmp == nm.Parts.Last() ? "" : ",");
                }

                // Match strength & algorithms
                int desiredMatchLevel = 5;
                bool useVariant = false;
                if (nm.Use == NameSet.NameSetUse.Search)
                {
                    useVariant = (parameters.MatchingAlgorithm & MatchAlgorithm.Variant) != 0;
                    if ((parameters.MatchingAlgorithm & MatchAlgorithm.Soundex) != 0) // no soundex is allowed so exact only
                        desiredMatchLevel = 4;
                }

                retVal.AppendFormat("SELECT PSN_ID FROM FIND_PSN_BY_NAME_SET('{{{0}}}','{{{1}}}', {3}, {4}, {2})",
                    filterString, cmpTypeString, nm.Use == NameSet.NameSetUse.Search ? (object)"NULL" : (decimal)nm.Use, desiredMatchLevel, useVariant);

                if (nm != names.Last())
                    retVal.AppendFormat(" UNION ");
            }
            return retVal.ToString();
        }



        /// <summary>
        /// Build filter on identifiers
        /// </summary>
        private string BuildFilterIdentifiers(List<DomainIdentifier> identifiers)
        {
            StringBuilder retVal = new StringBuilder();
            foreach (var id in identifiers)
            {
                retVal.AppendFormat("SELECT PSN_ID FROM GET_PSN_EXTERN('{0}','{1}')", id.Domain.Replace("'", "''"), id.Identifier.Replace("'", "''"), identifiers.Count * 4);
                if (id != identifiers.Last())
                    retVal.AppendFormat(" UNION ");
            }
            return retVal.ToString();
        }

        #endregion


    }
}
