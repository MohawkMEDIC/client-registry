using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.Specialized;
using System.ComponentModel;
using MARC.HI.EHRS.SVC.Core.Services;
using System.Diagnostics;
using MARC.HI.EHRS.SVC.Core.DataTypes;
using System.Threading;
using MARC.Everest.Connectors;
using MARC.HI.EHRS.SVC.Core.Issues;
using MARC.HI.EHRS.SVC.DecisionSupport;
using MARC.HI.EHRS.SVC.Core.ComponentModel;
using MARC.HI.EHRS.CR.Core.ComponentModel;
using MARC.HI.EHRS.SVC.PolicyEnforcement;

namespace MARC.HI.EHRS.CR.Messaging.FHIR.Util
{
    /// <summary>
    /// Data utility
    /// </summary>
    public static class DataUtil
    {

        /// <summary>
        /// Internal query structure
        /// </summary>
        public class FhirQuery
        {
            /// <summary>
            /// FHIR query
            /// </summary>
            public FhirQuery()
            {
                this.ActualParameters = new NameValueCollection();
                this.Filter = null;
                this.QueryId = Guid.Empty;
                this.IncludeHistory = false;
                this.MinimumDegreeMatch = 1.0f;
                this.TargetDomains = new List<DomainIdentifier>();
                this.Start = 0;
                this.Quantity = 25;
            }

            /// <summary>
            /// Get the actual parameters that could be serviced
            /// </summary>
            public NameValueCollection ActualParameters;

            /// <summary>
            /// The filter
            /// </summary>
            public HealthServiceRecordContainer Filter;

            /// <summary>
            /// Identifies the query identifier
            /// </summary>
            public Guid QueryId;

            /// <summary>
            /// True if the query is merely a sumary
            /// </summary>
            public bool IncludeHistory;

            /// <summary>
            /// Gets or sets the target domains
            /// </summary>
            public List<DomainIdentifier> TargetDomains;

            /// <summary>
            /// Minimum degree natcg
            /// </summary>
            public float MinimumDegreeMatch;

            /// <summary>
            /// Start result
            /// </summary>
            public int Start;

            /// <summary>
            /// The Quantity
            /// </summary>
            public int Quantity;

        }

        /// <summary>
        /// Query results
        /// </summary>
        public class FhirQueryResult
        {
            /// <summary>
            /// The query the result is servicing
            /// </summary>
            public FhirQuery Query;

            /// <summary>
            /// Gets the results
            /// </summary>
            public List<IComponent> Results;

            /// <summary>
            /// Business violations
            /// </summary>
            public List<DetectedIssue> Issues;

            /// <summary>
            /// Gets the total results
            /// </summary>
            public int TotalResults;



        }

        /// <summary>
        /// Query the data store
        /// </summary>
        public static FhirQueryResult Query(FhirQuery querySpec, List<IResultDetail> details)
        {
            // Get the services
            IDataPersistenceService persistence = ApplicationContext.CurrentContext.GetService(typeof(IDataPersistenceService)) as IDataPersistenceService;
            IDataRegistrationService registration = ApplicationContext.CurrentContext.GetService(typeof(IDataRegistrationService)) as IDataRegistrationService;
            IQueryPersistenceService queryPersistence = ApplicationContext.CurrentContext.GetService(typeof(IQueryPersistenceService)) as IQueryPersistenceService;

            try
            {
                if (persistence == null)
                    throw new InvalidOperationException("No persistence service has been configured, queries cannot continue without this service");
                else if (registration == null)
                    throw new InvalidOperationException("No registration service has been configured, queries cannot continue without this service");

                FhirQueryResult result = new FhirQueryResult();
                result.Query = querySpec;
                result.Issues = new List<DetectedIssue>();

                // Perform the query
                var identifiers = registration.QueryRecord(querySpec.Filter);

                // Fetch the records async
                List<VersionedDomainIdentifier> retRecordId = new List<VersionedDomainIdentifier>(100);

                result.Results = GetRecordsAsync(identifiers, retRecordId, result.Issues, details, querySpec);

                // Sort control?
                // TODO: Support sort control
                //retVal.Sort((a, b) => b.Id.CompareTo(a.Id)); // Default sort by id

                // Persist the query
                if (queryPersistence != null)
                    queryPersistence.RegisterQuerySet(querySpec.QueryId.ToString(), identifiers, querySpec.QueryId);
                // Return query data
                result.TotalResults = retRecordId.Count(o => o != null);
                return result;
            }
            catch (Exception ex)
            {
                Trace.TraceError(ex.ToString());
                details.Add(new PersistenceResultDetail(ResultDetailType.Error, ex.Message, ex));
                throw;
            }
        }

        /// <summary>
        /// Get all records asynchronously
        /// </summary>
        /// <param name="recordIds">Record identifiers to retrieve</param>
        /// <param name="retRecordId">An array of record identiifers actually returned</param>
        internal static List<IComponent> GetRecordsAsync(VersionedDomainIdentifier[] recordIds, List<VersionedDomainIdentifier> retRecordId, List<DetectedIssue> issues, List<IResultDetail> dtls, FhirQuery qd)
        {

            IDecisionSupportService decisionSupport = ApplicationContext.CurrentContext.GetService(typeof(IDecisionSupportService)) as IDecisionSupportService;
            Object syncLock = new object();

            // Decision Support service
            IComponent[] retVal = new IComponent[qd.Quantity < recordIds.Length ? qd.Quantity : recordIds.Length];
            retRecordId.AddRange(recordIds);

            List<VersionedDomainIdentifier> recordFetch = new List<VersionedDomainIdentifier>(retVal.Length);
            // Get the number of records to include
            for (int i = 0; i < retVal.Length; i++)
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
                        if (decisionSupport != null)
                            mIssue.AddRange(decisionSupport.RetrievingRecord(id));

                        var result = GetRecord(parm as VersionedDomainIdentifier, mDtls, mIssue, qd) ;


                        // Process result
                        if (result != null)
                        {
                            // Container has been retrieved
                            if (decisionSupport != null)
                                mIssue.AddRange(decisionSupport.RetrievedRecord(result as HealthServiceRecordComponent));

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

            var matchingRecords = new List<IComponent>(retVal);
            matchingRecords.RemoveAll(o => o == null);
            return matchingRecords;
        }


        /// <summary>
        /// Get record
        /// </summary>
        internal static HealthServiceRecordComponent GetRecord(VersionedDomainIdentifier recordId, List<IResultDetail> dtls, List<DetectedIssue> issues, FhirQuery qd)
        {
            try
            {
                IDataPersistenceService persistence = ApplicationContext.CurrentContext.GetService(typeof(IDataPersistenceService)) as IDataPersistenceService;
                IPolicyEnforcementService policyService = ApplicationContext.CurrentContext.GetService(typeof(IPolicyEnforcementService)) as IPolicyEnforcementService;

                // Can't find persistence
                if (persistence == null)
                {
                    dtls.Add(new PersistenceResultDetail(ResultDetailType.Error, "Couldn't locate an implementation of a PersistenceService object, storage is aborted", null));
                    throw new Exception("Cannot de-persist records");
                }


                // Read the record from the DB
                var result = persistence.GetContainer(recordId, qd.IncludeHistory) as HealthServiceRecordContainer;

                // Does this result match what we're looking for?
                if (result == null)
                    return null; // next record

                // Are we interested in any of the history?
                if (!qd.IncludeHistory)
                    result.RemoveAllFromRole(HealthServiceRecordSiteRoleType.OlderVersionOf);

                // Calculate the matching algorithm
                var subject = result.FindComponent(HealthServiceRecordSiteRoleType.SubjectOf) as Person;
                if (subject == null)
                    subject = result as Person;

                // Remove all but the alternate identifiers specifed in the query
                if (qd.TargetDomains != null && subject != null && qd.TargetDomains.Count > 0)
                {
                    subject.AlternateIdentifiers.RemoveAll(o => !qd.TargetDomains.Exists(t => t.Domain.Equals(o.Domain)));
                    if (subject.AlternateIdentifiers.Count == 0)
                        return null;
                }

                // Filter data for confidence
                var filter = qd.Filter;

                QueryParameters confidence = new QueryParameters() { Confidence = 1.0f };
                if(subject != null)// We're fetching a patient?
                    confidence = (subject).Confidence(filter as Person);

                if (confidence.Confidence < qd.MinimumDegreeMatch)
                    return null;

                (subject as Person).Add(confidence, "CONF", HealthServiceRecordSiteRoleType.ComponentOf | HealthServiceRecordSiteRoleType.CommentOn, null);
                // Mask
                if (policyService != null)
                    result = policyService.ApplyPolicies(qd.Filter, result, issues) as RegistrationEvent;

                return result;
            }
            catch (Exception ex)
            {
                dtls.Add(new PersistenceResultDetail(ResultDetailType.Error, ex.Message, ex));
                return null;
            }
        }
    }
}
