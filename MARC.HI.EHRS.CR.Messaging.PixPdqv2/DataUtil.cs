/**
 * Copyright 2012-2012 Mohawk College of Applied Arts and Technology
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
 * Date: 17-10-2012
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MARC.HI.EHRS.SVC.Core.Services;
using MARC.HI.EHRS.SVC.DecisionSupport;
using MARC.HI.EHRS.SVC.PolicyEnforcement;
using MARC.HI.EHRS.SVC.Core.DataTypes;
using MARC.Everest.Connectors;
using System.Data.Common;
using System.Data;
using MARC.HI.EHRS.CR.Core.ComponentModel;
using System.Threading;
using MARC.HI.EHRS.SVC.Core.ComponentModel;
using MARC.HI.EHRS.CR.Messaging.HL7.TransportProtocol;
using NHapi.Base.Util;
using System.Net;
using System.IO;
using NHapi.Base.Parser;
using MARC.HI.EHRS.CR.Core.Services;
using MARC.HI.EHRS.SVC.Core.ComponentModel.Components;
using System.Diagnostics;
using MARC.HI.EHRS.SVC.Subscription.Core.Services;

namespace MARC.HI.EHRS.CR.Messaging.PixPdqv2
{
    /// <summary>
    /// Data utility
    /// </summary>
    public class DataUtil : IUsesHostContext
    {

        /// <summary>
        /// Create audit data
        /// </summary>
        public AuditData CreateAuditData(string itiName, ActionType action, OutcomeIndicator outcome, Hl7MessageReceivedEventArgs msgEvent, QueryResultData result)
        {
            // Create the call to the other create audit data message by constructing the list of disclosed identifiers
            List<VersionedDomainIdentifier> vids = new List<VersionedDomainIdentifier>(result.Results.Length);
            foreach (var res in result.Results)
            {
                if (res == null) continue;
                var subj = res.FindComponent(SVC.Core.ComponentModel.HealthServiceRecordSiteRoleType.SubjectOf) as Person;
                if (subj == null)
                    continue;
                vids.Add(new VersionedDomainIdentifier()
                {
                    Domain = this.m_configService.OidRegistrar.GetOid("CR_CID").Oid,
                    Identifier = subj.Id.ToString()
                });
            }

            return CreateAuditData(itiName, action, outcome, msgEvent, vids);
        }

        /// <summary>
        /// Create audit data
        /// </summary>
        internal AuditData CreateAuditData(string itiName, ActionType action, OutcomeIndicator outcome, Hl7MessageReceivedEventArgs msgEvent, List<VersionedDomainIdentifier> identifiers)
        {
            // Audit data
            AuditData retVal = null;

            AuditableObjectLifecycle lifecycle = AuditableObjectLifecycle.Access;

            // Get the config service
            ISystemConfigurationService config = Context.GetService(typeof(ISystemConfigurationService)) as ISystemConfigurationService;

            Terser terser = new Terser(msgEvent.Message);

            // Source and dest
            string sourceData = String.Format("{0}|{1}", terser.Get("/MSH-3"), terser.Get("/MSH-4")),
                destData = String.Format("{0}|{1}",terser.Get("/MSH-5"), terser.Get("/MSH-6"));

            switch (itiName)
            {
                case "ITI-21":
                    {
                        retVal = new AuditData(DateTime.Now, action, outcome, EventIdentifierType.Query, new CodeValue(itiName, "IHE Transactions"));

                        // Audit actor for Patient Identity Source
                        retVal.Actors.Add(new AuditActorData()
                        {
                            UserIsRequestor = true,
                            UserIdentifier = sourceData,
                            ActorRoleCode = new List<CodeValue>() {
                            new  CodeValue("110153", "DCM") { DisplayName = "Source" }
                        },
                            NetworkAccessPointId = msgEvent.SolicitorEndpoint.Host,
                            NetworkAccessPointType = msgEvent.SolicitorEndpoint.HostNameType == UriHostNameType.Dns ? NetworkAccessPointType.MachineName : NetworkAccessPointType.IPAddress
                        });

                        
                        // Add query parameters
                        retVal.AuditableObjects.Add(
                            new AuditableObject()
                            {
                                IDTypeCode = AuditableObjectIdType.Custom,
                                CustomIdTypeCode = new CodeValue(itiName.Replace("-", "")),
                                QueryData = Convert.ToBase64String(CreateMessageSerialized(msgEvent.Message)),
                                Type = AuditableObjectType.SystemObject,
                                Role = AuditableObjectRole.Query,
                                ObjectId = terser.Get("/QPD-2")
                            }
                        );

                        // Audit actor for PDQ
                        retVal.Actors.Add(new AuditActorData()
                        {
                            UserIdentifier = destData,
                            UserIsRequestor = false,
                            ActorRoleCode = new List<CodeValue>() { new CodeValue("110152", "DCM") { DisplayName = "Destination" } },
                            NetworkAccessPointType = NetworkAccessPointType.MachineName,
                            NetworkAccessPointId = Dns.GetHostName()
                        });
                        break;
                    }
                case "ITI-8":
                    {
                        retVal = new AuditData(DateTime.Now, action, outcome, EventIdentifierType.PatientRecord, new CodeValue(itiName, "IHE Transactions"));

                        // Audit actor for Patient Identity Source
                        retVal.Actors.Add(new AuditActorData()
                        {
                            UserIsRequestor = true,
                            UserIdentifier = sourceData,
                            ActorRoleCode = new List<CodeValue>() {
                            new  CodeValue("110153", "DCM") { DisplayName = "Source" }
                        },
                            NetworkAccessPointId = msgEvent.SolicitorEndpoint.Host,
                            NetworkAccessPointType = msgEvent.SolicitorEndpoint.HostNameType == UriHostNameType.Dns ? NetworkAccessPointType.MachineName : NetworkAccessPointType.IPAddress
                        });

                        // Audit actor for PDQ
                        retVal.Actors.Add(new AuditActorData()
                        {
                            UserIdentifier = destData,
                            UserIsRequestor = false,
                            ActorRoleCode = new List<CodeValue>() { new CodeValue("110152", "DCM") { DisplayName = "Destination" } },
                            NetworkAccessPointType = NetworkAccessPointType.MachineName,
                            NetworkAccessPointId = Dns.GetHostName()
                        });
                        break;
                    }
                case "ITI-9":
                    {
                        retVal = new AuditData(DateTime.Now, action, outcome, EventIdentifierType.Query, new CodeValue(itiName, "IHE Transactions"));

                        // Audit actor for Patient Identity Source
                        retVal.Actors.Add(new AuditActorData()
                        {
                            UserIsRequestor = true,
                            UserIdentifier = sourceData,
                            ActorRoleCode = new List<CodeValue>() {
                            new  CodeValue("110153", "DCM") { DisplayName = "Source" }
                        },
                            NetworkAccessPointId = msgEvent.SolicitorEndpoint.Host,
                            NetworkAccessPointType = msgEvent.SolicitorEndpoint.HostNameType == UriHostNameType.Dns ? NetworkAccessPointType.MachineName : NetworkAccessPointType.IPAddress
                        });

                        // Add query parameters
                        retVal.AuditableObjects.Add(
                            new AuditableObject()
                            {
                                IDTypeCode = AuditableObjectIdType.Custom,
                                CustomIdTypeCode = new CodeValue("ITI9"),
                                QueryData = Convert.ToBase64String(CreateMessageSerialized(msgEvent.Message)),
                                Type = AuditableObjectType.SystemObject,
                                Role = AuditableObjectRole.Query,
                                ObjectId = terser.Get("/QPD-2")
                            }
                        );

                        // Audit actor for PDQ
                        retVal.Actors.Add(new AuditActorData()
                        {
                            UserIdentifier = destData,
                            UserIsRequestor = false,
                            ActorRoleCode = new List<CodeValue>() { new CodeValue("110152", "DCM") { DisplayName = "Destination" } },
                            NetworkAccessPointType = NetworkAccessPointType.MachineName,
                            NetworkAccessPointId = Dns.GetHostName()
                        });
                        break;
                    }
            }

            var expDatOid = config.OidRegistrar.GetOid("CR_CID");

            // Audit patients
            foreach (var id in identifiers)
            {
                // Construct the audit object
                AuditableObject aud = new AuditableObject()
                {
                    IDTypeCode = AuditableObjectIdType.PatientNumber,
                    Role = AuditableObjectRole.Patient,
                    Type = AuditableObjectType.Person
                };

                // Lifecycle
                switch (action)
                {
                    case ActionType.Create:
                        aud.LifecycleType = AuditableObjectLifecycle.Creation;
                        break;
                    case ActionType.Delete:
                        aud.LifecycleType = AuditableObjectLifecycle.LogicalDeletion;
                        break;
                    case ActionType.Execute:
                        aud.LifecycleType = AuditableObjectLifecycle.Access;
                        break;
                    case ActionType.Read:
                        aud.LifecycleType = AuditableObjectLifecycle.Disclosure;
                        break;
                    case ActionType.Update:
                        aud.LifecycleType = AuditableObjectLifecycle.Amendment;
                        break;
                }

                aud.ObjectId = String.Format("{1}^^^{2}&{0}&ISO", expDatOid.Oid, id.Identifier, expDatOid.Attributes.Find(o => o.Key == "AssigningAuthorityName").Value);
                retVal.AuditableObjects.Add(aud);
            }


            return retVal;
        }

        /// <summary>
        /// Create a serialized message
        /// </summary>
        private byte[] CreateMessageSerialized(NHapi.Base.Model.IMessage iMessage)
        {
            MemoryStream ms = new MemoryStream();
            string msg = new PipeParser().Encode(iMessage);
            ms.Write(Encoding.ASCII.GetBytes(msg), 0, msg.Length);
            ms.Flush();
            return ms.GetBuffer();
        }

        #region IUsesHostContext Members

        // Host context
        private MARC.HI.EHRS.SVC.Core.HostContext m_context;

        // Services
        private IAuditorService m_auditService; // Auditor
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
        public SVC.Core.HostContext Context
        {
            get { return this.m_context; }
            set
            {
                this.m_context = value;
                this.m_auditService = this.m_context.GetService(typeof(IAuditorService)) as IAuditorService; // Auditor
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

        #endregion


        /// <summary>
        /// Sync lock
        /// </summary>
        private object m_syncLock = new object();

        /// <summary>
        /// Get components from the persistence service
        /// </summary>
        /// <remarks>
        /// Calls are as follows:
        /// <list type="bullet">
        ///     <item></item>
        /// </list>
        /// </remarks>
        internal virtual QueryResultData Get(VersionedDomainIdentifier[] recordIds, List<IResultDetail> dtls, QueryData qd)
        {

            try
            {

                List<VersionedDomainIdentifier> retRecordId = new List<VersionedDomainIdentifier>(100);
                // Query continuation
                if (this.m_queryPersistence != null && this.m_queryPersistence.IsRegistered(qd.QueryId))
                {
                    throw new Exception(String.Format("The query '{0}' has already been registered. To continue this query use the QUQI_IN000003CA interaction", qd.QueryId));
                }
                else
                {

                    var retVal = GetRecordsAsync(recordIds, retRecordId, dtls, qd);

                    // Get the count of not-included records
                    retVal.RemoveAll(o => o == null);

                    // Persist the query
                    if (this.m_queryPersistence != null)
                        this.m_queryPersistence.RegisterQuerySet(qd.QueryId, recordIds, qd);

                    // Return query data
                    return new QueryResultData()
                    {
                        QueryTag = qd.QueryTag,
                        ContinuationPtr = qd.QueryId,
                        Results = retVal.ToArray(),
                        TotalResults = retRecordId.Count
                    };

                }

            }
            catch (TimeoutException ex)
            {
                // Audit exception
                dtls.Add(new PersistenceResultDetail(ResultDetailType.Error, ex.Message, ex));
                return QueryResultData.Empty;
            }
            catch (DbException ex)
            {
                // Audit exception
                dtls.Add(new PersistenceResultDetail(ResultDetailType.Error, ex.Message, ex));
                return QueryResultData.Empty;
            }
            catch (DataException ex)
            {
                // Audit exception
                dtls.Add(new PersistenceResultDetail(ResultDetailType.Error, ex.Message, ex));
                return QueryResultData.Empty;
            }
            catch (Exception ex)
            {
                // Audit exception
                dtls.Add(new ResultDetail(ResultDetailType.Error, ex.Message, ex));
                return QueryResultData.Empty;
            }
        }

        /// <summary>
        /// Get all records asynchronously
        /// </summary>
        /// <param name="recordIds">Record identifiers to retrieve</param>
        /// <param name="retRecordId">An array of record identiifers actually returned</param>
        internal List<RegistrationEvent> GetRecordsAsync(VersionedDomainIdentifier[] recordIds, List<VersionedDomainIdentifier> retRecordId, List<IResultDetail> dtls, QueryData qd)
        {
            // Decision Support service
            RegistrationEvent[] retVal = new RegistrationEvent[qd.Quantity < recordIds.Length ? qd.Quantity : recordIds.Length];
            retRecordId.AddRange(recordIds);

            List<VersionedDomainIdentifier> recordFetch = new List<VersionedDomainIdentifier>(retVal.Length);
            // Get the number of records to include
            for (int i = 0; i < retVal.Length; i++)
                recordFetch.Add(recordIds[i]);

            int maxWorkerBees = Environment.ProcessorCount * 4,
                nResults = 0;
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

                        var result = GetRecord(parm as VersionedDomainIdentifier, mDtls, qd);


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
                // for
                bool didReturn = wtp.WaitOne(20000, true);

                if (!didReturn)
                    throw new TimeoutException("The query could not complete in the specified amount of time");

            }
            finally
            {
                wtp.Dispose();
            }

            return new List<RegistrationEvent>(retVal);
        }

        /// <summary>
        /// Get record
        /// </summary>
        internal RegistrationEvent GetRecord(VersionedDomainIdentifier recordId, List<IResultDetail> dtls, QueryData qd)
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
                var result = this.m_persistenceService.GetContainer(recordId, qd.IsSummary) as RegistrationEvent;
                    
                // Does this result match what we're looking for?
                if (result == null)
                    return null; // next record

                // Calculate the matching algorithm
                var subject = result.FindComponent(HealthServiceRecordSiteRoleType.SubjectOf) as Person;

                // Remove all but the alternate identifiers specifed in the query
                if (qd.TargetDomain != null && subject != null)
                {
                    subject.AlternateIdentifiers.RemoveAll(o => !qd.TargetDomain.Exists(t => t.Domain.Equals(o.Domain)));
                    if(subject.AlternateIdentifiers.Count == 0)
                        return null;
                }

                if (subject != null)
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
                    foreach(var itm in dte)
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
        /// Query the data layer
        /// </summary>
        internal QueryResultData Query(QueryData filter, List<IResultDetail> dtls)
        {
            try
            {

                List<VersionedDomainIdentifier> retRecordId = new List<VersionedDomainIdentifier>(100);
                // Query continuation
                if (this.m_registrationService == null)
                    throw new InvalidOperationException("No record registration service is registered. Querying for records cannot be done unless this service is present");
                else if (this.m_queryPersistence != null && this.m_queryPersistence.IsRegistered(filter.QueryId))
                    throw new Exception(String.Format("The query '{0}' has already been registered. To continue this query use the QUQI_IN000003CA interaction", filter.QueryId));
                else
                {

                    // Query the document registry service
                    // Are we doing a straig get?
                    var queryFilter = filter.QueryRequest.FindComponent(HealthServiceRecordSiteRoleType.FilterOf); // The outer filter data is usually just parameter control..

                    VersionedDomainIdentifier[] recordIds = this.m_registrationService.QueryRecord(queryFilter as HealthServiceRecordComponent);
                    var retVal = GetRecordsAsync(recordIds, retRecordId, dtls, filter);
                    if (retVal.Count == 0 && filter.IsSummary)
                        dtls.Add(new PatientNotFoundResultDetail(this.m_localeService));


                    // Sort control?
                    // TODO: Support sort control
                    //retVal.Sort((a, b) => b.Id.CompareTo(a.Id)); // Default sort by id

                    // Persist the query
                    if (this.m_queryPersistence != null)
                        this.m_queryPersistence.RegisterQuerySet(filter.QueryId, recordIds, filter);

                    // Return query data
                    return new QueryResultData()
                    {
                        QueryTag = filter.QueryTag,
                        ContinuationPtr = filter.QueryId,
                        Results = retVal.ToArray(),
                        TotalResults = recordIds.Length
                    };

                }

            }
            catch (TimeoutException ex)
            {
                dtls.Add(new PersistenceResultDetail(ResultDetailType.Error, ex.Message, ex));
                throw;
            }
            catch (DbException ex)
            {
                dtls.Add(new PersistenceResultDetail(ResultDetailType.Error, ex.Message, ex));
                throw;
            }
            catch (DataException ex)
            {
                dtls.Add(new PersistenceResultDetail(ResultDetailType.Error, ex.Message, ex));
                throw;
            }
            catch (Exception ex)
            {
                dtls.Add(new ResultDetail(ResultDetailType.Error, ex.Message, ex));
                throw;
            }
        }

        /// <summary>
        /// Register the patient
        /// </summary>
        internal VersionedDomainIdentifier Register(RegistrationEvent healthServiceRecord, List<IResultDetail> dtls, DataPersistenceMode mode)
        {
            bool needsReconciliation = false; // true when the record registered needs to be reconciled

            try
            {

                // Can't find persistence
                if (this.m_persistenceService == null)
                {
                    dtls.Add(new PersistenceResultDetail(ResultDetailType.Error, "Couldn't locate an implementation of a PersistenceService object, storage is aborted", null));
                    return null;
                }
                else if (healthServiceRecord == null)
                {
                    dtls.Add(new PersistenceResultDetail(ResultDetailType.Error, "Can't register null health service record data", null));
                    return null;
                }
                else if (dtls.Count(o => o.Type == ResultDetailType.Error) > 0)
                {
                    dtls.Add(new PersistenceResultDetail(ResultDetailType.Error, "Won't attempt to persist invalid message", null));
                    return null;
                }

                // First, IHE is a little different first we have to see if we can match any of the records for cross referencing
                // therefore we do a query, first with identifiers and then without identifiers, 100% match
                VersionedDomainIdentifier[] pid = null;
                if (this.m_clientRegistryConfigService != null)
                {
                    bool isFuzzyMatch = false;
                    // Check if the person exists just via the identifier?
                    // This is important because it is not necessarily a merge but an "update if exists"
                    var subject = healthServiceRecord.FindComponent(SVC.Core.ComponentModel.HealthServiceRecordSiteRoleType.SubjectOf) as Person;
                    QueryParameters qp = new QueryParameters()
                    {
                        Confidence = 1.0f,
                        MatchingAlgorithm = MatchAlgorithm.Exact,
                        MatchStrength = MatchStrength.Exact
                    };

                    var patientQuery = new RegistrationEvent();
                    patientQuery.Add(qp, "FLT", SVC.Core.ComponentModel.HealthServiceRecordSiteRoleType.FilterOf, null);
                    patientQuery.Add(new Person() { AlternateIdentifiers = subject.AlternateIdentifiers }, "SUBJ", SVC.Core.ComponentModel.HealthServiceRecordSiteRoleType.SubjectOf, null);
                    // Perform the query
                    pid = this.m_registrationService.QueryRecord(patientQuery);

                    if (pid.Length == 0)
                    {
                        isFuzzyMatch = true;
                        // No match based on ID, get rid of the identifiers and then match
                        // based on configured criteria
                        patientQuery.RemoveAllFromRole(SVC.Core.ComponentModel.HealthServiceRecordSiteRoleType.SubjectOf);
                        var ssubject = this.m_clientRegistryConfigService.CreateMergeFilter(subject);

                        if (ssubject != null) // Minimum criteria was met
                        {
                            patientQuery.Add(ssubject, "SUBJ", HealthServiceRecordSiteRoleType.SubjectOf, null);
                            pid = this.m_registrationService.QueryRecord(patientQuery); // Try to cross reference again   
                        }
                    }

                    // Did we cross reference a patient?
                    if (pid.Length == 1)
                    {
                        // Add the pid to the list of registration event identifiers
                        healthServiceRecord.AlternateIdentifier = pid[0];
                        VersionedDomainIdentifier vid = null;
                        // Update
                        //   1. If it is a fuzzy match and the automerge is enabled
                        //   2. If it is an exact match with update if exists
                        if (this.m_clientRegistryConfigService.Configuration.Registration.AutoMerge || !isFuzzyMatch && this.m_clientRegistryConfigService.Configuration.Registration.UpdateIfExists)
                        {
                            dtls.Add(new ResultDetail(ResultDetailType.Warning, String.Format(m_localeService.GetString("DTPW002"), pid[0].Domain, pid[0].Identifier), null, null));
                            vid = this.Update(healthServiceRecord, dtls, mode);
                            return vid;
                        }
                        else // candidate match but no auto-merge or update if exists, that means we need to link
                        {
                            needsReconciliation = true;
                            healthServiceRecord.Add(new HealthServiceRecordComponentRef()
                            {
                                AlternateIdentifier = pid[0]
                            }, "MRGT", HealthServiceRecordSiteRoleType.TargetOf | HealthServiceRecordSiteRoleType.AlternateTo, null);
                        }
                    }
                    else if (pid.Length > 1) // Add a warning
                    {
                        dtls.Add(new ResultDetail(ResultDetailType.Warning, m_localeService.GetString("DTPW001"), null, null));
                        // Notify someone that this needs to occur
                        needsReconciliation = true;
                        // Add each pid to a reconciliation reference
                        foreach (var p in pid)
                            healthServiceRecord.Add(new HealthServiceRecordComponentRef()
                            {
                                AlternateIdentifier = p
                            }, Guid.NewGuid().ToString(), HealthServiceRecordSiteRoleType.TargetOf | HealthServiceRecordSiteRoleType.AlternateTo, null);
                    }
                }
                // Call the dss
                if (this.m_decisionSupportService != null)
                    foreach (var iss in this.m_decisionSupportService.RecordPersisting(healthServiceRecord))
                        dtls.Add(new ResultDetail(iss.Priority == SVC.Core.Issues.IssuePriorityType.Error ? ResultDetailType.Error : ResultDetailType.Warning, iss.Text, null, null));

                // Any errors?
                if (dtls.Count(o => o.Type == ResultDetailType.Error) > 0)
                    dtls.Add(new PersistenceResultDetail(ResultDetailType.Error, "Won't attempt to persist message due to detected issues", null));

                // Persist
                var retVal = this.m_persistenceService.StoreContainer(healthServiceRecord, mode);
                retVal.UpdateMode = UpdateModeType.Add;


                // Notify that reconciliation is required
                if (this.m_notificationService != null && needsReconciliation)
                {
                    var list = new List<VersionedDomainIdentifier>(pid) { retVal };
                    this.m_notificationService.NotifyReconciliationRequired(list);
                }

                
                // Call the dss
                if (this.m_decisionSupportService != null)
                    this.m_decisionSupportService.RecordPersisted(healthServiceRecord);
                
                // Call sub
                if (this.m_subscriptionService != null)
                    this.m_subscriptionService.PublishContainer(healthServiceRecord);

                // Register the document set if it is a document
                if (retVal != null && this.m_registrationService != null && !this.m_registrationService.RegisterRecord(healthServiceRecord, mode))
                    dtls.Add(new PersistenceResultDetail(ResultDetailType.Warning, "Wasn't able to register event in the event registry, event exists in repository but not in registry. You may not be able to query for this event", null));

                return retVal;
            }
            catch (DuplicateNameException ex) // Already persisted stuff
            {
                Trace.TraceError(ex.ToString());
                dtls.Add(new PersistenceResultDetail(ResultDetailType.Error, m_localeService.GetString("DTPE005"), ex));
                return null;
            }
            catch (MissingPrimaryKeyException ex) // Already persisted stuff
            {
                Trace.TraceError(ex.ToString());
                dtls.Add(new PersistenceResultDetail(ResultDetailType.Error, m_localeService.GetString("DTPE005"), ex));
                return null;
            }
            catch (ConstraintException ex)
            {
                Trace.TraceError(ex.ToString());
                dtls.Add(new PersistenceResultDetail(ResultDetailType.Error, m_localeService.GetString("DTPE005"), ex));
                return null;
            }
            catch (Exception ex)
            {
                Trace.TraceError(ex.ToString());
                dtls.Add(new ResultDetail(ResultDetailType.Error, ex.Message, ex));
                return null;
            }
        }

        /// <summary>
        /// Update an existing patient record
        /// </summary>
        internal VersionedDomainIdentifier Update(RegistrationEvent healthServiceRecord, List<IResultDetail> dtls, DataPersistenceMode mode)
        {
            try
            {

                // Can't find persistence
                if (this.m_persistenceService == null)
                {
                    dtls.Add(new PersistenceResultDetail(ResultDetailType.Error, "Couldn't locate an implementation of a PersistenceService object, storage is aborted", null));
                    return null;
                }
                else if (healthServiceRecord == null)
                {
                    dtls.Add(new PersistenceResultDetail(ResultDetailType.Error, "Can't register null health service record data", null));
                    return null;
                }
                else if (dtls.Count(o => o.Type == ResultDetailType.Error) > 0)
                {
                    dtls.Add(new PersistenceResultDetail(ResultDetailType.Error, "Won't attempt to persist invalid message", null));
                    return null;
                }

                // Call the dss
                if (this.m_decisionSupportService != null)
                    foreach (var iss in this.m_decisionSupportService.RecordPersisting(healthServiceRecord))
                        dtls.Add(new DetectedIssueResultDetail(iss.Priority == SVC.Core.Issues.IssuePriorityType.Error ? ResultDetailType.Error : ResultDetailType.Warning, iss.Text, (string)null));
                
                // Any errors?
                if (dtls.Count(o => o.Type == ResultDetailType.Error) > 0)
                    dtls.Add(new PersistenceResultDetail(ResultDetailType.Error, "Won't attempt to persist message due to detected issues", null));

                // Persist
                var retVal = this.m_persistenceService.UpdateContainer(healthServiceRecord, mode);

                retVal.UpdateMode = UpdateModeType.Update;

                // Call the dss
                if (this.m_decisionSupportService != null)
                    this.m_decisionSupportService.RecordPersisted(healthServiceRecord);

                // Call sub
                if (this.m_subscriptionService != null)
                    this.m_subscriptionService.PublishContainer(healthServiceRecord);

                // Register the document set if it is a document
                if (retVal != null && this.m_registrationService != null && !this.m_registrationService.RegisterRecord(healthServiceRecord, mode))
                    dtls.Add(new PersistenceResultDetail(ResultDetailType.Warning, "Wasn't able to register event in the event registry, event exists in repository but not in registry. You may not be able to query for this event", null));

                return retVal;
            }
            catch (DuplicateNameException ex) // Already persisted stuff
            {
                dtls.Add(new PersistenceResultDetail(ResultDetailType.Error, m_localeService.GetString("DTPE005"), ex));
                return null;
            }
            catch (MissingPrimaryKeyException ex) // Already persisted stuff
            {
                dtls.Add(new PersistenceResultDetail(ResultDetailType.Error, m_localeService.GetString("DTPE005"), ex));
                return null;
            }
            catch (ConstraintException ex)
            {
                dtls.Add(new PersistenceResultDetail(ResultDetailType.Error, m_localeService.GetString("DTPE005"), ex));
                return null;
            }
            catch (Exception ex)
            {
                dtls.Add(new ResultDetail(ResultDetailType.Error, ex.Message, ex));
                return null;
            }
        }
    }
}
