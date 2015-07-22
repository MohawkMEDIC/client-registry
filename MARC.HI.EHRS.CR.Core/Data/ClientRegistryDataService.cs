using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MARC.HI.EHRS.CR.Core.Services;
using System.Data.Common;
using MARC.Everest.Connectors;
using System.Data;
using MARC.HI.EHRS.SVC.Core.DataTypes;
using MARC.HI.EHRS.SVC.Core.Services;
using MARC.HI.EHRS.SVC.PolicyEnforcement;
using MARC.HI.EHRS.SVC.DecisionSupport;
using MARC.HI.EHRS.SVC.Subscription.Core.Services;
using System.Security;
using MARC.HI.EHRS.SVC.Core.ComponentModel;
using MARC.HI.EHRS.CR.Core.ComponentModel;
using System.Threading;

namespace MARC.HI.EHRS.CR.Core.Data
{
    /// <summary>
    /// Client registry data service implementation
    /// </summary>
    public class ClientRegistryDataService : IClientRegistryDataService, IUsesHostContext
    {

        // Sync lock
        private Object m_syncLock = new object();

        // The service context
        private IServiceProvider m_context;

        // Services
        private IDataPersistenceService m_persistenceService; // Persistence
        private IDataRegistrationService m_registrationService; // Registration
        private IDecisionSupportService m_decisionSupportService; // DSS service
        private IPolicyEnforcementService m_policyService; // policy service
        private IQueryPersistenceService m_queryPersistence; // qp service
        private ILocalizationService m_localeService; // locale
        private IClientNotificationService m_notificationService; // client notification service
        private ISystemConfigurationService m_configService; // config service
        private IClientRegistryConfigurationService m_clientRegistryConfigService;
        private ISubscriptionManagementService m_subscriptionService;

        /// <summary>
        /// Gets or sets the context of the host
        /// </summary>
        public IServiceProvider Context
        {
            get { return this.m_context; }
            set
            {
                this.m_context = value;
                this.m_persistenceService = this.m_context.GetService(typeof(IDataPersistenceService)) as IDataPersistenceService; // Persistence
                this.m_registrationService = this.m_context.GetService(typeof(IDataRegistrationService)) as IDataRegistrationService; // Registration
                this.m_decisionSupportService = this.m_context.GetService(typeof(IDecisionSupportService)) as IDecisionSupportService; // DSS service
                this.m_policyService = this.m_context.GetService(typeof(IPolicyEnforcementService)) as IPolicyEnforcementService; // policy service
                this.m_queryPersistence = this.m_context.GetService(typeof(IQueryPersistenceService)) as IQueryPersistenceService; // qp service
                this.m_localeService = this.m_context.GetService(typeof(ILocalizationService)) as ILocalizationService; // locale service
                this.m_notificationService = this.m_context.GetService(typeof(IClientNotificationService)) as IClientNotificationService; // notification service
                this.m_configService = this.m_context.GetService(typeof(ISystemConfigurationService)) as ISystemConfigurationService; // config service
                this.m_clientRegistryConfigService = this.m_context.GetService(typeof(IClientRegistryConfigurationService)) as IClientRegistryConfigurationService; // config
                this.m_subscriptionService = this.m_context.GetService(typeof(ISubscriptionManagementService)) as ISubscriptionManagementService;
            }
        }


        #region Utility Functions

        /// <summary>
        /// Get record
        /// </summary>
        private RegistrationEvent GetRecord(VersionedDomainIdentifier recordId, List<IResultDetail> dtls, RegistryQueryRequest qd)
        {
            try
            {
                // Can't find persistence
                if (this.m_persistenceService == null)
                {
                    dtls.Add(new PersistenceResultDetail(ResultDetailType.Error, "Couldn't locate an implementation of a PersistenceService object, storage is aborted", null));
                    throw new Exception("Cannot de-persist records");
                }


                // Read the record from the DB
                var result = this.m_persistenceService.GetContainer(recordId, true) as RegistrationEvent;

                // Does this result match what we're looking for?
                if (result == null)
                    return null; // next record

                // Calculate the matching algorithm
                var subject = result.FindComponent(HealthServiceRecordSiteRoleType.SubjectOf) as Person;

                // Remove all but the alternate identifiers specifed in the query
                if (qd.TargetDomain != null && subject != null)
                {
                    subject.AlternateIdentifiers.RemoveAll(o => !qd.TargetDomain.Exists(t => t.Domain.Equals(o.Domain)));
                    if (subject.AlternateIdentifiers.Count == 0)
                        return null;
                }

                if (subject != null && qd.QueryRequest != null)
                {
                    var filter = qd.QueryRequest.FindComponent(HealthServiceRecordSiteRoleType.FilterOf);
                    if (filter != null)
                        filter = (filter as HealthServiceRecordContainer).FindComponent(HealthServiceRecordSiteRoleType.SubjectOf);
                    var confidence = (subject as Person).Confidence(filter as Person);

                    if (confidence.Confidence < qd.MinimumDegreeMatch)
                        return null;

                    (subject as Person).Add(confidence, "CONF", HealthServiceRecordSiteRoleType.ComponentOf | HealthServiceRecordSiteRoleType.CommentOn, null);
                }

                // Mask
                if (this.m_policyService != null)
                {
                    var dte = new List<SVC.Core.Issues.DetectedIssue>();
                    result = this.m_policyService.ApplyPolicies(qd.QueryRequest, result, dte) as RegistrationEvent;
                    foreach (var itm in dte)
                        dtls.Add(new DetectedIssueResultDetail(
                                    ResultDetailType.Warning,
                                    itm.Text,
                                    (string)null));
                }

                return result;
            }
            catch (Exception ex)
            {
                dtls.Add(new PersistenceResultDetail(ResultDetailType.Error, ex.Message, ex));
                return null;
            }
        }

        /// <summary>
        /// Get records asynchronously 
        /// </summary>
        private List<ComponentModel.RegistrationEvent> GetRecordsAsync(VersionedDomainIdentifier[] recordIds, List<VersionedDomainIdentifier> retRecordId, RegistryQueryRequest qd, List<IResultDetail> dtls)
        {
            // Decision Support service
            RegistrationEvent[] retVal = new RegistrationEvent[qd.Limit < recordIds.Length ? qd.Limit : recordIds.Length];
            retRecordId.AddRange(recordIds);

            List<VersionedDomainIdentifier> recordFetch = new List<VersionedDomainIdentifier>(retVal.Length);
            // Get the number of records to include
            for (int i = 0; i < retVal.Length; i++)
                recordFetch.Add(recordIds[i]);

            int maxWorkerBees = recordFetch.Count < Environment.ProcessorCount * 2 ? recordFetch.Count : Environment.ProcessorCount * 2;

            //List<Thread> workerBees = new List<Thread>(maxWorkerBees);  // Worker bees
            var wtp = new MARC.Everest.Threading.WaitThreadPool(maxWorkerBees);
            try
            {

                //// Get components
                foreach (var id in recordFetch)
                    wtp.QueueUserWorkItem((WaitCallback)delegate(object parm)
                    {
                        List<IResultDetail> mDtls = new List<IResultDetail>(10);

                        // DSS Service
                        if (this.m_decisionSupportService != null)
                            foreach (var itm in this.m_decisionSupportService.RetrievingRecord(id))
                                dtls.Add(new DetectedIssueResultDetail(
                                    itm.Priority == SVC.Core.Issues.IssuePriorityType.Error ? ResultDetailType.Error : itm.Priority == SVC.Core.Issues.IssuePriorityType.Warning ? ResultDetailType.Warning : ResultDetailType.Information,
                                    itm.Text,
                                    (string)null));

                        var result = this.GetRecord(parm as VersionedDomainIdentifier, mDtls, qd);


                        // Process result
                        if (result != null)
                        {
                            // DSS Service
                            if (this.m_decisionSupportService != null)
                                foreach (var itm in this.m_decisionSupportService.RetrievedRecord(result))
                                    dtls.Add(new DetectedIssueResultDetail(
                                        itm.Priority == SVC.Core.Issues.IssuePriorityType.Error ? ResultDetailType.Error : itm.Priority == SVC.Core.Issues.IssuePriorityType.Warning ? ResultDetailType.Warning : ResultDetailType.Information,
                                        itm.Text,
                                        (String)null));

                            // Add to the results
                            lock (this.m_syncLock)
                            {
                                // Add return value
                                if (retRecordId.IndexOf(parm as VersionedDomainIdentifier) < retVal.Length)
                                    retVal[retRecordId.IndexOf(parm as VersionedDomainIdentifier)] = result;

                            }
                        }
                        else
                            dtls.Add(new DetectedIssueResultDetail(
                                ResultDetailType.Warning,
                                String.Format("Record '{1}^^^&{0}&ISO' will not be retrieved", id.Domain, (parm as VersionedDomainIdentifier).Identifier),
                                (string)null));

                        // Are we disclosing this record?
                        if (result == null || result.IsMasked)
                            lock (m_syncLock)
                                retRecordId.Remove(parm as VersionedDomainIdentifier);

                        // Add issues and details
                        lock (m_syncLock)
                        {
                            dtls.AddRange(mDtls);
                        }
                    }, id
                        );

                // Wait for return
                // TODO: Move this to a configuration parameter
                bool didReturn = wtp.WaitOne(50000, true);

                if (!didReturn)
                    throw new TimeoutException("The query could not complete in the specified amount of time");

            }
            finally
            {
                wtp.Dispose();
            }

            return new List<RegistrationEvent>(retVal);
        }



        #endregion 

        /// <summary>
        /// Query for a series of results which match the query parameter
        /// </summary>
        public RegistryQueryResult Query(RegistryQueryRequest query)
        {
            RegistryQueryResult retVal = new RegistryQueryResult();

            try
            {
                List<VersionedDomainIdentifier> returnedRecordIdentifiers = new List<VersionedDomainIdentifier>(100);

                // Query continuation?
                if (this.m_registrationService == null)
                    throw new InvalidOperationException("No record registration service is registered. Querying for records cannot be done unless this service is configured");
                else if (query.IsContinue) // Continuation
                {
                    // Sanity check, ensure the query exists!
                    if (this.m_queryPersistence == null || !this.m_queryPersistence.IsRegistered(query.QueryId))
                    {
                        retVal.Details.Add(new ValidationResultDetail(ResultDetailType.Error, String.Format("The query {0} has not been registered", query.QueryId), null, null));
                        throw new InvalidOperationException("Cannot conitnue query due to errors");
                    } // sanity check

                    // Validate the sender
                    RegistryQueryRequest queryTag = (RegistryQueryRequest)this.m_queryPersistence.GetQueryTag(query.QueryId);
                    if (query.Originator != queryTag.Originator)
                    {
                        retVal.Details.Add(new UnrecognizedSenderResultDetail(new DomainIdentifier() { Domain = query.Originator }));
                        throw new SecurityException("Sender mismatch");
                    }

                    // Return value
                    retVal.Results = this.GetRecordsAsync(this.m_queryPersistence.GetQueryResults(query.QueryId, -1, query.Limit), returnedRecordIdentifiers, query, retVal.Details);

                    // Return continued query
                    retVal.TotalResults = (int)this.m_queryPersistence.QueryResultTotalQuantity(query.QueryId);

                }
                else if (this.m_queryPersistence != null && this.m_queryPersistence.IsRegistered(query.QueryId) && !query.IsContinue)
                    throw new Exception(String.Format("The query '{0}' has already been registered. To continue this query use the appropriate interaction", query.QueryId));
                else
                {
                    // Query the registry service
                    var queryFilter = query.QueryRequest.FindComponent(SVC.Core.ComponentModel.HealthServiceRecordSiteRoleType.FilterOf); // The outer filter is usually just parameter control

                    VersionedDomainIdentifier[] recordIds = this.m_registrationService.QueryRecord(queryFilter as HealthServiceRecordComponent);
                    retVal.Results = this.GetRecordsAsync(recordIds, returnedRecordIdentifiers, query, retVal.Details);
                    if (retVal.Results.Count == 0 && query.IsSummary)
                        retVal.Details.Add(new PatientNotFoundResultDetail(this.m_localeService));

                    // Sort control? TODO: Support sort control

                    // Persist the query
                    if (this.m_queryPersistence != null && recordIds.Length > query.Limit)
                    {
                        this.m_queryPersistence.RegisterQuerySet(query.QueryId, recordIds, query);
                        this.m_queryPersistence.GetQueryResults(query.QueryId, query.Offset, query.Limit);
                    }

                    retVal.TotalResults = recordIds.Length;

                }

                retVal.ContinuationPtr = query.QueryId;
                retVal.QueryTag = query.QueryTag;

                return retVal;
            }
            catch (TimeoutException ex)
            {
                retVal.Details.Add(new PersistenceResultDetail(Everest.Connectors.ResultDetailType.Error, ex.Message, ex));
                throw;
            }
            catch (DbException ex)
            {
                retVal.Details.Add(new PersistenceResultDetail(ResultDetailType.Error, ex.Message, ex));
                throw;
            }
            catch (DataException ex)
            {
                retVal.Details.Add(new PersistenceResultDetail(ResultDetailType.Error, ex.Message, ex));
                throw;
            }
            catch (Exception ex)
            {
                retVal.Details.Add(new ResultDetail(ResultDetailType.Error, ex.Message, ex));
                throw;
            }
        }

        public RegistryStoreResult Register(ComponentModel.RegistrationEvent evt, SVC.Core.Services.DataPersistenceMode mode)
        {
            throw new NotImplementedException();
        }

        public RegistryStoreResult Update(ComponentModel.RegistrationEvent evt, SVC.Core.Services.DataPersistenceMode mode)
        {
            throw new NotImplementedException();
        }

        public RegistryStoreResult Merge(ComponentModel.RegistrationEvent mergeEvent, SVC.Core.Services.DataPersistenceMode mode)
        {
            throw new NotImplementedException();
        }

        public RegistryStoreResult UnMerge(ComponentModel.RegistrationEvent evt, SVC.Core.Services.DataPersistenceMode mode)
        {
            throw new NotImplementedException();
        }
    }
}
