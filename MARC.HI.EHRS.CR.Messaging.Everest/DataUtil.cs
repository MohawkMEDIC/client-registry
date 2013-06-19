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
 * Date: 17-10-2012
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MARC.HI.EHRS.SVC.Core.Services;
using MARC.HI.EHRS.SVC.Core.Exceptions;
using MARC.Everest.Connectors;
using MARC.HI.EHRS.CR.Core.ComponentModel;
using MARC.HI.EHRS.SVC.Core.DataTypes;
using System.Data;
using System.Data.Common;
using System.ComponentModel;
using System.Xml.Serialization;
using System.IO;
using System.Threading;
using MARC.Everest.Interfaces;
using System.Xml;
using System.Runtime.Serialization.Formatters.Binary;
using MARC.HI.EHRS.SVC.Core;
using MARC.HI.EHRS.SVC.PolicyEnforcement;
using MARC.HI.EHRS.SVC.Core.ComponentModel;
using MARC.HI.EHRS.SVC.Core.ComponentModel.Components;
using MARC.HI.EHRS.SVC.DecisionSupport;
using MARC.HI.EHRS.SVC.Core.Issues;
using MARC.HI.EHRS.CR.Core.Services;
using MARC.HI.EHRS.CR.Messaging.Everest.MessageReceiver.UV;
using MARC.HI.EHRS.SVC.Subscription.Core.Services;
using System.Diagnostics;


namespace MARC.HI.EHRS.CR.Messaging.Everest
{
    /// <summary>
    /// Data utilities wrap the interaction with the data persistence
    /// </summary>
    public class DataUtil : IUsesHostContext
    {

        /// <summary>
        /// The host context
        /// </summary>
        private IServiceProvider m_context;

        // Notification service
        protected IClientNotificationService m_notificationService;
        // The system configuration service
        protected ISystemConfigurationService m_configService;
        // The policy enforcement service
        protected IPolicyEnforcementService m_policyService;
        // The auditor service
        protected IAuditorService m_auditorService;
        // The document registration service
        protected IDataRegistrationService m_docRegService;
        // The query service
        protected IQueryPersistenceService m_queryService;
        // The decision service
        protected IDecisionSupportService m_decisionService;
        // Data persistence service
        protected IDataPersistenceService m_persistenceService;
        // localization service
        protected ILocalizationService m_localeService;
        // Configuration service
        protected IClientRegistryConfigurationService m_clientRegistryConfigService;
        protected ISubscriptionManagementService m_subscriptionService;
        

        /// <summary>
        /// Sync lock
        /// </summary>
        private object syncLock = new object();

        /// <summary>
        /// Query result data
        /// </summary>
        public struct QueryResultData
        {

            /// <summary>
            /// Identifies the first record number that is to be returned in the set
            /// </summary>
            public int StartRecordNumber { get; set; }

            /// <summary>
            /// Gets or sets the identifier of the query the result set is for
            /// </summary>
            public string QueryId { get; set; }
            /// <summary>
            /// Gets or sets the results for the query
            /// </summary>
            public RegistrationEvent[] Results { get; set; }
            /// <summary>
            /// Gets or sets the total results for the query
            /// </summary>
            public int TotalResults { get; set; }
            /// <summary>
            /// Empty result
            /// </summary>
            public static QueryResultData Empty = new QueryResultData()
                {
                    Results = new RegistrationEvent[] { }
                };
        }

        /// <summary>
        /// Query data structure
        /// </summary>
        [XmlRoot("qd")]
        [Serializable]
        public struct QueryData
        {
            // Target (filter) identifiers for clients
            private List<DomainIdentifier> m_targetIds;


            /// <summary>
            /// True if the query is a summary query
            /// </summary>
            [XmlAttribute]
            public bool IsSummary { get; set; }

            
            /// <summary>
            /// Gets or sets the query id for the query 
            /// </summary>
            [XmlIgnore]
            public string QueryId { get; set; }
            /// <summary>
            /// Gets or sets the originator of the request
            /// </summary>
            [XmlAttribute("orgn")]
            public string Originator { get; set; }
            /// <summary>
            /// If true, include notes in the query results
            /// </summary>
            [XmlAttribute("nt")]
            public bool IncludeNotes { get; set; }
            /// <summary>
            /// If true, include history in the query results
            /// </summary>
            [XmlAttribute("hst")]
            public bool IncludeHistory { get; set; }
            /// <summary>
            /// Specifies the maximum number of query results to return fro mthe ffunction
            /// </summary>
            [XmlAttribute("qty")]
            public int Quantity { get; set; }
            /// <summary>
            /// Represents the original query component that is being used to query
            /// </summary>
            [XmlIgnore]
            public RegistrationEvent QueryRequest { get; set; }
            /// <summary>
            /// The minimum degree of match
            /// </summary>
            [XmlAttribute("minDegreeMatch")]
            public float MinimumDegreeMatch { get; set; }
            /// <summary>
            /// Matching algorithms
            /// </summary>
            [XmlAttribute("matchAlgorithm")]
            public MatchAlgorithm MatchingAlgorithm { get; set; }
            /// <summary>
            /// Original Request
            /// </summary>
            [XmlIgnore]
            public IGraphable OriginalMessageQuery { get; set; }
            /// <summary>
            /// Record Ids to be fetched
            /// </summary>
            [XmlElement("restriction")]
            public List<DomainIdentifier> TargetDomains { get; set; }

            /// <summary>
            /// Represent the QD as string
            /// </summary>
            public override string ToString()
            {
                StringWriter sb = new StringWriter();
                XmlSerializer xs = new XmlSerializer(this.GetType());
                xs.Serialize(sb, this);
                return sb.ToString();
            }

            /// <summary>
            /// Parse XML from the string
            /// </summary>
            internal static QueryData ParseXml(string p)
            {
                StringReader sr = new StringReader(p);
                XmlSerializer xsz = new XmlSerializer(typeof(QueryData));
                QueryData retVal = (QueryData)xsz.Deserialize(sr);
                sr.Close();
                return retVal;
            }
        }

        #region IUsesHostContext Members

        /// <summary>
        /// Gets or sets the host context
        /// </summary>
        public IServiceProvider Context
        {
            get { return m_context; }
            set
            {
                m_context = value;

                if (value == null) return;
                this.m_auditorService = value.GetService(typeof(IAuditorService)) as IAuditorService;
                this.m_configService = value.GetService(typeof(ISystemConfigurationService)) as ISystemConfigurationService;
                this.m_persistenceService = value.GetService(typeof(IDataPersistenceService)) as IDataPersistenceService;
                this.m_decisionService = value.GetService(typeof(IDecisionSupportService)) as IDecisionSupportService;
                this.m_docRegService = value.GetService(typeof(IDataRegistrationService)) as IDataRegistrationService;
                this.m_policyService = value.GetService(typeof(IPolicyEnforcementService)) as IPolicyEnforcementService;
                this.m_queryService = value.GetService(typeof(IQueryPersistenceService)) as IQueryPersistenceService;
                this.m_localeService = value.GetService(typeof(ILocalizationService)) as ILocalizationService;
                this.m_notificationService = value.GetService(typeof(IClientNotificationService)) as IClientNotificationService;
                this.m_clientRegistryConfigService = this.m_context.GetService(typeof(IClientRegistryConfigurationService)) as IClientRegistryConfigurationService; // config
                this.m_subscriptionService = this.m_context.GetService(typeof(ISubscriptionManagementService)) as ISubscriptionManagementService;

            }
        }

        #endregion

        /// <summary>
        /// Register health service record with the data persistence engine
        /// </summary>
        internal virtual VersionedDomainIdentifier Register(RegistrationEvent healthServiceRecord, List<MARC.Everest.Connectors.IResultDetail> dtls, List<DetectedIssue> issues, DataPersistenceMode mode)
        {

            // persistence services

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
                if (this.m_decisionService != null)
                    issues.AddRange(this.m_decisionService.RecordPersisting(healthServiceRecord));

                // Any errors?
                if (issues.Count(o => o.Priority == IssuePriorityType.Error) > 0)
                    dtls.Add(new PersistenceResultDetail(ResultDetailType.Error, "Won't attempt to persist message due to detected issues", null));

                // Return value
                var retVal = this.m_persistenceService.StoreContainer(healthServiceRecord, mode);

                // Audit the creation
                if (this.m_auditorService != null)
                {
                    AuditData auditData = new AuditData(DateTime.Now, ActionType.Create, OutcomeIndicator.Success, EventIdentifierType.ProvisioningEvent, new CodeValue("PRPA_TE101201CA", "HL7 Trigger Events"));
                    UpdateAuditData(healthServiceRecord, auditData, 0);
                    UpdateAuditData(AuditableObjectLifecycle.Creation, new List<VersionedDomainIdentifier>(new VersionedDomainIdentifier[] { retVal }), auditData);
                    this.m_auditorService.SendAudit(auditData);
                }

                // Call the dss
                if (this.m_decisionService != null)
                    this.m_decisionService.RecordPersisted(healthServiceRecord);

                if (this.m_subscriptionService != null)
                    this.m_subscriptionService.PublishContainer(healthServiceRecord);

                // Register the document set if it is a document
                if (retVal != null && this.m_docRegService != null && !this.m_docRegService.RegisterRecord(healthServiceRecord, mode))
                    dtls.Add(new PersistenceResultDetail(ResultDetailType.Warning, "Wasn't able to register event in the event registry, event exists in repository but not in registry. You may not be able to query for this event", null));

                return retVal;
            }
            catch (DuplicateNameException ex) // Already persisted stuff
            {
                Trace.TraceError(ex.ToString());
                // Audit exception
                if (this.m_auditorService != null)
                {
                    AuditData audit = new AuditData(DateTime.Now, ActionType.Create, OutcomeIndicator.EpicFail, EventIdentifierType.ProvisioningEvent, null);
                    UpdateAuditData(healthServiceRecord, audit, 0);
                    this.m_auditorService.SendAudit(audit);
                }
                dtls.Add(new PersistenceResultDetail(ResultDetailType.Error, m_localeService.GetString("DTPE005"), ex));
                issues.Add(new DetectedIssue()
                {
                    Severity = IssueSeverityType.High,
                    Type = IssueType.AlreadyPerformed,
                    Text = ex.Message,
                    Priority = IssuePriorityType.Error
                });
                return null;
            }
            catch (MissingPrimaryKeyException ex) // Already persisted stuff
            {
                Trace.TraceError(ex.ToString());
                // Audit exception
                if (this.m_auditorService != null)
                {
                    AuditData audit = new AuditData(DateTime.Now, ActionType.Create, OutcomeIndicator.EpicFail, EventIdentifierType.ProvisioningEvent, null);
                    UpdateAuditData(healthServiceRecord, audit, 0);
                    this.m_auditorService.SendAudit(audit);
                }
                dtls.Add(new PersistenceResultDetail(ResultDetailType.Error, m_localeService.GetString("DTPE005"), ex));
                issues.Add(new DetectedIssue()
                {
                    Severity = IssueSeverityType.High,
                    Type = IssueType.DetectedIssue,
                    Text = ex.Message,
                    Priority = IssuePriorityType.Error
                });
                return null;
            }
            catch (ConstraintException ex)
            {
                Trace.TraceError(ex.ToString());
                // Audit exception
                if (this.m_auditorService != null)
                {
                    AuditData audit = new AuditData(DateTime.Now, ActionType.Create, OutcomeIndicator.EpicFail, EventIdentifierType.ProvisioningEvent, null);
                    UpdateAuditData(healthServiceRecord, audit, 0);
                    this.m_auditorService.SendAudit(audit);
                }
                dtls.Add(new PersistenceResultDetail(ResultDetailType.Error, m_localeService.GetString("DTPE005"), ex));
                issues.Add(new DetectedIssue()
                {
                    Severity = IssueSeverityType.High,
                    Type = IssueType.DetectedIssue,
                    Text = ex.Message,
                    Priority = IssuePriorityType.Error
                });
                return null;
            }
            catch (DbException ex)
            {
                Trace.TraceError(ex.ToString());
                // Audit exception
                if (this.m_auditorService != null)
                {
                    AuditData audit = new AuditData(DateTime.Now, ActionType.Create, OutcomeIndicator.EpicFail, EventIdentifierType.ProvisioningEvent, null);
                    UpdateAuditData(healthServiceRecord, audit, 0);
                    this.m_auditorService.SendAudit(audit);
                }

                dtls.Add(new PersistenceResultDetail(ResultDetailType.Error, ex.Message, ex));
                return null;
            }
            catch (DataException ex)
            {
                Trace.TraceError(ex.ToString());
                // Audit exception
                if (this.m_auditorService != null)
                {
                    AuditData audit = new AuditData(DateTime.Now, ActionType.Create, OutcomeIndicator.EpicFail, EventIdentifierType.ProvisioningEvent, null);
                    UpdateAuditData(healthServiceRecord, audit, 0);
                    this.m_auditorService.SendAudit(audit);
                }

                dtls.Add(new PersistenceResultDetail(ResultDetailType.Error, ex.Message, ex));
                return null;
            }
            catch (IssueException ex)
            {
                Trace.TraceError(ex.ToString());
                // Audit exception
                if (this.m_auditorService != null)
                {
                    AuditData audit = new AuditData(DateTime.Now, ActionType.Create, OutcomeIndicator.EpicFail, EventIdentifierType.ProvisioningEvent, null);
                    UpdateAuditData(healthServiceRecord, audit, 0);
                    this.m_auditorService.SendAudit(audit);
                }
                issues.Add(ex.Issue);
                return null;
            }
            catch (Exception ex)
            {
                Trace.TraceError(ex.ToString());
                // Audit exception
                if (this.m_auditorService != null)
                {
                    AuditData audit = new AuditData(DateTime.Now, ActionType.Create, OutcomeIndicator.EpicFail, EventIdentifierType.ProvisioningEvent, null);
                    UpdateAuditData(healthServiceRecord, audit, 0);
                    this.m_auditorService.SendAudit(audit);
                }

                dtls.Add(new ResultDetail(ResultDetailType.Error, ex.Message, ex));
                return null;
            }

        }

        /// <summary>
        /// Get record
        /// </summary>
        internal RegistrationEvent GetRecord(VersionedDomainIdentifier recordId, List<IResultDetail> dtls, List<DetectedIssue> issues, QueryData qd)
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

                // Are we interested in any of the history?
                if (!qd.IncludeHistory)
                    result.RemoveAllFromRole(HealthServiceRecordSiteRoleType.OlderVersionOf);
                if (!qd.IncludeNotes)
                {
                    var notes = result.FindAllComponents(HealthServiceRecordSiteRoleType.CommentOn);
                    foreach (var n in notes ?? new List<IComponent>())
                        (n as Annotation).IsMasked = true;
                }

                // Calculate the matching algorithm
                var subject = result.FindComponent(HealthServiceRecordSiteRoleType.SubjectOf) as Person;

                // Remove all but the alternate identifiers specifed in the query
                if (qd.TargetDomains != null && subject != null && qd.TargetDomains.Count > 0)
                {
                    subject.AlternateIdentifiers.RemoveAll(o => !qd.TargetDomains.Exists(t => t.Domain.Equals(o.Domain)));
                    if (subject.AlternateIdentifiers.Count == 0)
                        return null;
                }

                // Filter data for confidence
                var filter = qd.QueryRequest.FindComponent(HealthServiceRecordSiteRoleType.FilterOf);
                if (filter != null)
                    filter = (filter as HealthServiceRecordContainer).FindComponent(HealthServiceRecordSiteRoleType.SubjectOf);
                var confidence = (subject as Person).Confidence(filter as Person);

                if (confidence.Confidence < qd.MinimumDegreeMatch)
                    return null;

                (subject as Person).Add(confidence, "CONF", HealthServiceRecordSiteRoleType.ComponentOf | HealthServiceRecordSiteRoleType.CommentOn, null);
                // Mask
                if (this.m_policyService != null)
                    result = this.m_policyService.ApplyPolicies(qd.QueryRequest, result, issues) as RegistrationEvent;

                return result;
            }
            catch (Exception ex)
            {
                dtls.Add(new PersistenceResultDetail(ResultDetailType.Error, ex.Message, ex));
                return null;
            }
        }

        /// <summary>
        /// Get components from the persistence service
        /// </summary>
        /// <remarks>
        /// Calls are as follows:
        /// <list type="bullet">
        ///     <item></item>
        /// </list>
        /// </remarks>
        internal virtual QueryResultData Get(VersionedDomainIdentifier[] recordIds, List<IResultDetail> dtls, List<DetectedIssue> issues, QueryData qd)
        {

            try
            {

                List<VersionedDomainIdentifier> retRecordId = new List<VersionedDomainIdentifier>(100);
                // Query continuation
                if (this.m_queryService != null && this.m_queryService.IsRegistered(qd.QueryId.ToLower()))
                {
                    throw new Exception(String.Format("The query '{0}' has already been registered. To continue this query use the QUQI_IN000003CA interaction", qd.QueryId));
                }
                else
                {

                    var retVal = GetRecordsAsync(recordIds, retRecordId, issues, dtls, qd);

                    // Get the count of not-included records
                    retVal.RemoveAll(o => o == null);

                    // Persist the query
                    if (this.m_queryService != null)
                        this.m_queryService.RegisterQuerySet(qd.QueryId.ToLower(), recordIds, qd);

                    // Return query data
                    return new QueryResultData()
                    {
                        QueryId = qd.QueryId.ToLower(),
                        Results = retVal.ToArray(),
                        TotalResults = retRecordId.Count(o=>o != null)
                    };

                }

            }
            catch (TimeoutException ex)
            {
                // Audit exception
                if (this.m_auditorService != null)
                {
                    AuditData audit = new AuditData(DateTime.Now, ActionType.Read, OutcomeIndicator.EpicFail, EventIdentifierType.Query, null);
                    UpdateAuditData(qd.QueryRequest, audit, AuditableObjectLifecycle.ReceiptOfDisclosure);
                    this.m_auditorService.SendAudit(audit);
                }

                dtls.Add(new PersistenceResultDetail(ResultDetailType.Error, ex.Message, ex));
                return QueryResultData.Empty;
            }
            catch (DbException ex)
            {
                // Audit exception
                if (this.m_auditorService != null)
                {
                    AuditData audit = new AuditData(DateTime.Now, ActionType.Read, OutcomeIndicator.EpicFail, EventIdentifierType.Query, null);
                    UpdateAuditData(qd.QueryRequest, audit, AuditableObjectLifecycle.ReceiptOfDisclosure);
                    this.m_auditorService.SendAudit(audit);
                }

                dtls.Add(new PersistenceResultDetail(ResultDetailType.Error, ex.Message, ex));
                return QueryResultData.Empty;
            }
            catch (DataException ex)
            {
                // Audit exception
                if (this.m_auditorService != null)
                {
                    AuditData audit = new AuditData(DateTime.Now, ActionType.Read, OutcomeIndicator.EpicFail, EventIdentifierType.ProvisioningEvent, null);
                    UpdateAuditData(qd.QueryRequest, audit, 0);
                    this.m_auditorService.SendAudit(audit);
                }

                dtls.Add(new PersistenceResultDetail(ResultDetailType.Error, ex.Message, ex));
                return QueryResultData.Empty;
            }
            catch (Exception ex)
            {
                // Audit exception
                if (this.m_auditorService != null)
                {
                    AuditData audit = new AuditData(DateTime.Now, ActionType.Read, OutcomeIndicator.EpicFail, EventIdentifierType.Query, null);
                    UpdateAuditData(qd.QueryRequest, audit, AuditableObjectLifecycle.ReceiptOfDisclosure);
                    this.m_auditorService.SendAudit(audit);
                }

                dtls.Add(new ResultDetail(ResultDetailType.Error, ex.Message, ex));
                return QueryResultData.Empty;
            }
        }

        /// <summary>
        /// Get all records asynchronously
        /// </summary>
        /// <param name="recordIds">Record identifiers to retrieve</param>
        /// <param name="retRecordId">An array of record identiifers actually returned</param>
        internal List<RegistrationEvent> GetRecordsAsync(VersionedDomainIdentifier[] recordIds, List<VersionedDomainIdentifier> retRecordId, List<DetectedIssue> issues, List<IResultDetail> dtls, QueryData qd)
        {
            // Decision Support service
            RegistrationEvent[] retVal = new RegistrationEvent[qd.Quantity < recordIds.Length ? qd.Quantity : recordIds.Length];
            retRecordId.AddRange(recordIds);

            List<VersionedDomainIdentifier> recordFetch = new List<VersionedDomainIdentifier>(retVal.Length);
            // Get the number of records to include
            for(int i = 0; i < retVal.Length; i++)
                recordFetch.Add(recordIds[i]);

            int maxWorkerBees = Environment.ProcessorCount,
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
                                List<DetectedIssue> mIssue = new List<DetectedIssue>(10);

                                // DSS Service
                                if (this.m_decisionService != null)
                                    mIssue.AddRange(this.m_decisionService.RetrievingRecord(id));

                                var result = GetRecord(parm as VersionedDomainIdentifier, mDtls, mIssue, qd);

                              
                                // Process result
                                if (result != null)
                                {
                                    // Container has been retrieved
                                    if (this.m_decisionService != null)
                                        mIssue.AddRange(this.m_decisionService.RetrievedRecord(result));

                                    // Add to the results
                                    lock (syncLock)
                                    {
                                        // Add return value
                                        if (retRecordId.IndexOf(parm as VersionedDomainIdentifier) < retVal.Length)
                                            retVal[retRecordId.IndexOf(parm as VersionedDomainIdentifier)] = result;

                                    }
                                }
                                else
                                {
                                    mIssue.Add(new DetectedIssue()
                                    {
                                        Type = IssueType.BusinessConstraintViolation,
                                        Text = String.Format("Record '{1}^^^&{0}&ISO' will not be retrieved", id.Domain, (parm as VersionedDomainIdentifier).Identifier),
                                        MitigatedBy = ManagementType.OtherActionTaken,
                                        Priority = IssuePriorityType.Warning
                                    });
                                }

                                // Are we disclosing this record?
                                if (result == null || result.IsMasked)
                                    lock (syncLock)
                                        retRecordId.Remove(parm as VersionedDomainIdentifier);

                                // Add issues and details
                                lock (syncLock)
                                {
                                    issues.AddRange(mIssue);
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

            var matchingRecords = new List<RegistrationEvent>(retVal);
            matchingRecords.RemoveAll(o => o == null);
            return matchingRecords;
        }

        /// <summary>
        /// Update audit data for disclosure purposes
        /// </summary>
        private void UpdateAuditData(AuditableObjectLifecycle lifeCycle, List<VersionedDomainIdentifier> retRecordId, AuditData audit)
        {
            foreach (var id in retRecordId)
            {
                audit.AuditableObjects.Add(new AuditableObject()
                {
                    LifecycleType = lifeCycle,
                    IDTypeCode = AuditableObjectIdType.ReportNumber,
                    ObjectId = String.Format("{1}^^^&{0}&ISO", id.Domain, id.Identifier),
                    Role = AuditableObjectRole.Report,
                    Type = AuditableObjectType.SystemObject
                });
            }
        }

        
        /// <summary>
        /// Update auditing data
        /// </summary>
        private void UpdateAuditData(RegistrationEvent queryRequest, AuditData audit, AuditableObjectLifecycle? lifeCycle)
        {

            // Add an actor for the current server
            audit.Actors.Add(new AuditActorData()
            {
                ActorRoleCode = new List<CodeValue>() { new CodeValue("RCV", "HL7 Type Code") },
                NetworkAccessPointId = Environment.MachineName,
                UserIsRequestor = false,
                NetworkAccessPointType = NetworkAccessPointType.MachineName
            });

            // Look for policy override information
            var policyOverride = queryRequest.FindComponent(HealthServiceRecordSiteRoleType.ConsentOverrideFor) as PolicyOverride;
            if (policyOverride != null)
                audit.AuditableObjects.Add(new AuditableObject()
                {
                    IDTypeCode = AuditableObjectIdType.ReportNumber,
                    LifecycleType = AuditableObjectLifecycle.Verification,
                    Role = AuditableObjectRole.SecurityResource,
                    Type = AuditableObjectType.SystemObject,
                    ObjectId = String.Format("{1}^^^&{0}&ISO", policyOverride.FormId.Domain, policyOverride.FormId.Identifier)
                });

            // Add a network node
            foreach (IComponent comp in queryRequest.Components)
            {
                // Healthcare participant = actor
                if (comp is HealthcareParticipant)
                {
                    audit.Actors.Add(new AuditActorData()
                    {
                        ActorRoleCode = new List<CodeValue>() { new CodeValue(comp.Site.Name, "HL7 Type Code") },
                        UserIdentifier = String.Format("{1}^^^&{0}&ISO", this.m_configService.OidRegistrar.GetOid("CR_PID").Oid, (comp as HealthcareParticipant).Id.ToString()),
                        UserName = (comp as HealthcareParticipant).LegalName.ToString(),
                        UserIsRequestor = (comp.Site as HealthServiceRecordSite).SiteRoleType == HealthServiceRecordSiteRoleType.AuthorOf,

                    });
                    audit.AuditableObjects.Add(new AuditableObject()
                    {
                        IDTypeCode = AuditableObjectIdType.UserIdentifier,
                        ObjectId = String.Format("{1}^^^&{0}&ISO", this.m_configService.OidRegistrar.GetOid("CR_PID").Oid, (comp as HealthcareParticipant).Id.ToString()),
                        Role = AuditableObjectRole.Provider,
                        Type = AuditableObjectType.Person,
                        LifecycleType = (comp.Site as HealthServiceRecordSite).SiteRoleType == HealthServiceRecordSiteRoleType.AuthorOf ? lifeCycle.Value : default(AuditableObjectLifecycle)
                    });
                }

            }
        }

        /// <summary>
        /// Query (list) the data from the persistence layer
        /// </summary>
        internal virtual QueryResultData Query(QueryData filter, List<IResultDetail> dtls, List<DetectedIssue> issues)
        {

            try
            {

                List<VersionedDomainIdentifier> retRecordId = new List<VersionedDomainIdentifier>(100);
                // Query continuation
                if (this.m_docRegService == null)
                    throw new InvalidOperationException("No record registration service is registered. Querying for records cannot be done unless this service is present");
                else if (this.m_queryService != null && this.m_queryService.IsRegistered(filter.QueryId.ToLower()))
                    throw new Exception(String.Format("The query '{0}' has already been registered. To continue this query use the QUQI_IN000003CA interaction", filter.QueryId));
                else
                {

                    // Query the document registry service
                    var queryFilter = filter.QueryRequest.FindComponent(HealthServiceRecordSiteRoleType.FilterOf); // The outer filter data is usually just parameter control..

                    var recordIds = this.m_docRegService.QueryRecord(queryFilter as HealthServiceRecordComponent);
                    var retVal = GetRecordsAsync(recordIds, retRecordId, issues, dtls, filter);

                    // Sort control?
                    // TODO: Support sort control
                    //retVal.Sort((a, b) => b.Id.CompareTo(a.Id)); // Default sort by id

                    // Persist the query
                    if (this.m_queryService != null)
                        this.m_queryService.RegisterQuerySet(filter.QueryId.ToLower(), recordIds, filter);

                    // Return query data
                    return new QueryResultData()
                    {
                        QueryId = filter.QueryId.ToLower(),
                        Results = retVal.ToArray(),
                        TotalResults = retRecordId.Count(o => o != null)
                    };

                }

            }
            catch (TimeoutException ex)
            {
                // Audit exception
                if (this.m_auditorService != null)
                {
                    AuditData audit = new AuditData(DateTime.Now, ActionType.Read, OutcomeIndicator.EpicFail, EventIdentifierType.Query, null);
                    UpdateAuditData(filter.QueryRequest, audit, AuditableObjectLifecycle.ReceiptOfDisclosure);
                    this.m_auditorService.SendAudit(audit);
                }

                dtls.Add(new PersistenceResultDetail(ResultDetailType.Error, ex.Message, ex));
                throw;
            }
            catch (DbException ex)
            {
                // Audit exception
                if (this.m_auditorService != null)
                {
                    AuditData audit = new AuditData(DateTime.Now, ActionType.Read, OutcomeIndicator.EpicFail, EventIdentifierType.Query, null);
                    UpdateAuditData(filter.QueryRequest, audit, AuditableObjectLifecycle.ReceiptOfDisclosure);
                    this.m_auditorService.SendAudit(audit);
                }

                dtls.Add(new PersistenceResultDetail(ResultDetailType.Error, ex.Message, ex));
                throw;
            }
            catch (DataException ex)
            {
                // Audit exception
                if (this.m_auditorService != null)
                {
                    AuditData audit = new AuditData(DateTime.Now, ActionType.Read, OutcomeIndicator.EpicFail, EventIdentifierType.ProvisioningEvent, null);
                    UpdateAuditData(filter.QueryRequest, audit, 0);
                    this.m_auditorService.SendAudit(audit);
                }

                dtls.Add(new PersistenceResultDetail(ResultDetailType.Error, ex.Message, ex));
                throw;
            }
            catch (Exception ex)
            {
                // Audit exception
                if (this.m_auditorService != null)
                {
                    AuditData audit = new AuditData(DateTime.Now, ActionType.Read, OutcomeIndicator.EpicFail, EventIdentifierType.Query, null);
                    UpdateAuditData(filter.QueryRequest, audit, AuditableObjectLifecycle.ReceiptOfDisclosure);
                    this.m_auditorService.SendAudit(audit);
                }

                dtls.Add(new ResultDetail(ResultDetailType.Error, ex.Message, ex));
                throw;
            }
        }

        /// <summary>
        /// Update a health service record component
        /// </summary>
        internal virtual VersionedDomainIdentifier Update(RegistrationEvent healthServiceRecord, List<IResultDetail> dtls, List<DetectedIssue> issues, DataPersistenceMode mode)
        {
            // persistence services

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
                if (this.m_decisionService != null)
                    issues.AddRange(this.m_decisionService.RecordPersisting(healthServiceRecord));

                // Any errors?
                if (issues.Count(o => o.Priority == IssuePriorityType.Error) > 0)
                    dtls.Add(new PersistenceResultDetail(ResultDetailType.Error, "Won't attempt to persist message due to detected issues", null));

                // Return value
                var retVal = this.m_persistenceService.UpdateContainer(healthServiceRecord, mode);

                // Audit the creation
                if (this.m_auditorService != null)
                {
                    AuditData auditData = new AuditData(DateTime.Now, ActionType.Update, OutcomeIndicator.Success, EventIdentifierType.ProvisioningEvent, new CodeValue("PRPA_TE101204CA", "HL7 Trigger Events"));
                    UpdateAuditData(healthServiceRecord, auditData, 0);
                    UpdateAuditData(AuditableObjectLifecycle.Amendment, new List<VersionedDomainIdentifier>(new VersionedDomainIdentifier[] { retVal }), auditData);
                    this.m_auditorService.SendAudit(auditData);
                }

                // Call the dss
                if (this.m_decisionService != null)
                    this.m_decisionService.RecordPersisted(healthServiceRecord);

                if (this.m_subscriptionService != null)
                    this.m_subscriptionService.PublishContainer(healthServiceRecord);

                // Register the document set if it is a document
                if (retVal != null && this.m_docRegService != null && !this.m_docRegService.RegisterRecord(healthServiceRecord, mode))
                    dtls.Add(new PersistenceResultDetail(ResultDetailType.Warning, "Wasn't able to register event in the event registry, event exists in repository but not in registry. You may not be able to query for this event", null));

                return retVal;
            }
            catch (DuplicateNameException ex) // Already persisted stuff
            {
                Trace.TraceError(ex.ToString());
                // Audit exception
                if (this.m_auditorService != null)
                {
                    AuditData audit = new AuditData(DateTime.Now, ActionType.Create, OutcomeIndicator.EpicFail, EventIdentifierType.ProvisioningEvent, null);
                    UpdateAuditData(healthServiceRecord, audit, 0);
                    this.m_auditorService.SendAudit(audit);
                }
                dtls.Add(new PersistenceResultDetail(ResultDetailType.Error, m_localeService.GetString("DTPE005"), ex));
                issues.Add(new DetectedIssue()
                {
                    Severity = IssueSeverityType.High,
                    Type = IssueType.AlreadyPerformed,
                    Text = ex.Message,
                    Priority = IssuePriorityType.Error
                });
                return null;
            }
            catch (MissingPrimaryKeyException ex) // Already persisted stuff
            {
                Trace.TraceError(ex.ToString());
                // Audit exception
                if (this.m_auditorService != null)
                {
                    AuditData audit = new AuditData(DateTime.Now, ActionType.Create, OutcomeIndicator.EpicFail, EventIdentifierType.ProvisioningEvent, null);
                    UpdateAuditData(healthServiceRecord, audit, 0);
                    this.m_auditorService.SendAudit(audit);
                }
                dtls.Add(new PersistenceResultDetail(ResultDetailType.Error, m_localeService.GetString("DTPE005"), ex));
                issues.Add(new DetectedIssue()
                {
                    Severity = IssueSeverityType.High,
                    Type = IssueType.DetectedIssue,
                    Text = ex.Message,
                    Priority = IssuePriorityType.Error
                });
                return null;
            }
            catch (ConstraintException ex)
            {
                Trace.TraceError(ex.ToString());
                // Audit exception
                if (this.m_auditorService != null)
                {
                    AuditData audit = new AuditData(DateTime.Now, ActionType.Create, OutcomeIndicator.EpicFail, EventIdentifierType.ProvisioningEvent, null);
                    UpdateAuditData(healthServiceRecord, audit, 0);
                    this.m_auditorService.SendAudit(audit);
                }
                dtls.Add(new PersistenceResultDetail(ResultDetailType.Error, m_localeService.GetString("DTPE005"), ex));
                issues.Add(new DetectedIssue()
                {
                    Severity = IssueSeverityType.High,
                    Type = IssueType.DetectedIssue,
                    Text = ex.Message,
                    Priority = IssuePriorityType.Error
                });
                return null;
            }
            catch (DbException ex)
            {
                Trace.TraceError(ex.ToString());
                // Audit exception
                if (this.m_auditorService != null)
                {
                    AuditData audit = new AuditData(DateTime.Now, ActionType.Update, OutcomeIndicator.EpicFail, EventIdentifierType.ProvisioningEvent, null);
                    UpdateAuditData(healthServiceRecord, audit, 0);
                    this.m_auditorService.SendAudit(audit);
                }

                dtls.Add(new PersistenceResultDetail(ResultDetailType.Error, ex.Message, ex));
                return null;
            }
            catch (DataException ex)
            {
                Trace.TraceError(ex.ToString());
                // Audit exception
                if (this.m_auditorService != null)
                {
                    AuditData audit = new AuditData(DateTime.Now, ActionType.Update, OutcomeIndicator.EpicFail, EventIdentifierType.ProvisioningEvent, null);
                    UpdateAuditData(healthServiceRecord, audit, 0);
                    this.m_auditorService.SendAudit(audit);
                }

                dtls.Add(new PersistenceResultDetail(ResultDetailType.Error, ex.Message, ex));
                return null;
            }
            catch (Exception ex)
            {
                // Audit exception
                if (this.m_auditorService != null)
                {
                    AuditData audit = new AuditData(DateTime.Now, ActionType.Update, OutcomeIndicator.EpicFail, EventIdentifierType.ProvisioningEvent, null);
                    UpdateAuditData(healthServiceRecord, audit, 0);
                    this.m_auditorService.SendAudit(audit);
                }

                dtls.Add(new ResultDetail(ResultDetailType.Error, ex.Message, ex));
                return null;
            }
        }
    }
}
