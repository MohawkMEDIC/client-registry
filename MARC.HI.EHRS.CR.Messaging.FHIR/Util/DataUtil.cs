/**
 * Copyright 2012-2015 Mohawk College of Applied Arts and Technology
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
 * User: Justin
 * Date: 12-7-2015
 */
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
using MARC.HI.EHRS.SVC.Messaging.FHIR;
using MARC.HI.EHRS.CR.Messaging.FHIR.Processors;
using System.Data;

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
        public class ClientRegistryFhirQuery : FhirQuery
        {
            /// <summary>
            /// The filter
            /// </summary>
            public HealthServiceRecordContainer Filter;

        }

        /// <summary>
        /// Query the data store
        /// </summary>
        public static IComponent Register(IComponent storeContainer, DataPersistenceMode mode, List<IResultDetail> details)
        {
            // Get the services
            IDataPersistenceService persistence = ApplicationContext.CurrentContext.GetService(typeof(IDataPersistenceService)) as IDataPersistenceService;
            IDataRegistrationService registration = ApplicationContext.CurrentContext.GetService(typeof(IDataRegistrationService)) as IDataRegistrationService;

            try
            {

                // Sanity check
                if (persistence == null)
                    throw new InvalidOperationException("No persistence service has been configured, registrations cannot continue without this service");
                else if (registration == null)
                    throw new InvalidOperationException("No registration service has been configured, registrations cannot continue without this service");

                // Store
                var storedData = persistence.StoreContainer(storeContainer as IContainer, mode);
                if (storeContainer != null)
                    registration.RegisterRecord((IComponent)storeContainer, mode);

                // Now read and return
                return persistence.GetContainer(storedData, true) as IComponent;
                //return null;
                
            }
            catch (Exception ex)
            {
                Trace.TraceError(ex.ToString());
                details.Add(new PersistenceResultDetail(ResultDetailType.Error, ex.Message, ex));
                throw;
            }
        }

        /// <summary>
        /// Query the data store
        /// </summary>
        public static FhirQueryResult Query(ClientRegistryFhirQuery querySpec, List<IResultDetail> details)
        {
            // Get the services
            IDataPersistenceService persistence = ApplicationContext.CurrentContext.GetService(typeof(IDataPersistenceService)) as IDataPersistenceService;
            IDataRegistrationService registration = ApplicationContext.CurrentContext.GetService(typeof(IDataRegistrationService)) as IDataRegistrationService;
            IQueryPersistenceService queryPersistence = ApplicationContext.CurrentContext.GetService(typeof(IQueryPersistenceService)) as IQueryPersistenceService;

            try
            {

                if(querySpec.Quantity > 100)
                    throw new ConstraintException("Query limit must not exceed 100");

                if (persistence == null)
                    throw new InvalidOperationException("No persistence service has been configured, queries cannot continue without this service");
                else if (registration == null)
                    throw new InvalidOperationException("No registration service has been configured, queries cannot continue without this service");
                
                FhirQueryResult result = new FhirQueryResult();
                result.Query = querySpec;
                result.Issues = new List<DetectedIssue>();
                result.Details = details;
                result.Results = new List<SVC.Messaging.FHIR.Resources.ResourceBase>(querySpec.Quantity);
                
                VersionedDomainIdentifier[] identifiers;

                if (queryPersistence != null && result.Query.QueryId != Guid.Empty)
                {
                    if (!queryPersistence.IsRegistered(result.Query.QueryId.ToString()))
                    {
                        identifiers = registration.QueryRecord(querySpec.Filter);
                        queryPersistence.RegisterQuerySet(querySpec.QueryId.ToString(), identifiers, querySpec.QueryId);
                    }
                    identifiers = queryPersistence.GetQueryResults(result.Query.QueryId.ToString(), result.Query.Start, result.Query.Quantity);
                }
                else // stateless
                {
                    identifiers = registration.QueryRecord(querySpec.Filter);
                    
                    result.TotalResults = identifiers.Length;

                    identifiers = new List<VersionedDomainIdentifier>(identifiers.Skip(result.Query.Start).Take(result.Query.Quantity)).ToArray();
                    if (result.Query.QueryId != Guid.Empty)
                        details.Add(new NotImplementedElementResultDetail(ResultDetailType.Warning, "Stateful queries cannot be executed when a query manager is not configured", null, null));
                }

                // Fetch the records async and convert
                List<VersionedDomainIdentifier> retRecordId = new List<VersionedDomainIdentifier>(100);
                foreach (HealthServiceRecordContainer res in GetRecordsAsync(identifiers, retRecordId, result.Issues, details, querySpec))
                {
                    var resultSubject = res.FindComponent(HealthServiceRecordSiteRoleType.SubjectOf) as HealthServiceRecordContainer ?? res;
                    var processor = FhirMessageProcessorUtil.GetComponentProcessor(resultSubject.GetType());
                    if (processor == null)
                        result.Details.Add(new NotImplementedResultDetail(ResultDetailType.Error, String.Format("Will not include {1}^^^&{2}&ISO in result set, cannot find converter for {0}", resultSubject.GetType().Name, resultSubject.Id, identifiers[0].Domain), null, null));
                    else
                        result.Results.Add(processor.ProcessComponent(resultSubject, details));
                }

                // Sort control?
                // TODO: Support sort control but for now just sort according to confidence then date
                //retVal.Sort((a, b) => b.Id.CompareTo(a.Id)); // Default sort by id

                //if (queryPersistence != null)
                //    result.TotalResults = (int)queryPersistence.QueryResultTotalQuantity(querySpec.QueryId.ToString());
                //else
                //    result.TotalResults = retRecordId.Count(o => o != null);

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
        internal static List<IComponent> GetRecordsAsync(VersionedDomainIdentifier[] recordIds, List<VersionedDomainIdentifier> retRecordId, List<DetectedIssue> issues, List<IResultDetail> dtls, ClientRegistryFhirQuery qd)
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

                        //// Are we disclosing this record?
                        //if (result == null || result.IsMasked)
                        //    lock (syncLock)
                        //        retRecordId.Remove(parm as VersionedDomainIdentifier);

                        // Add issues and details
                        if(mIssue.Count > 0 || mDtls.Count > 0)
                            lock (syncLock)
                            {
                                issues.AddRange(mIssue);
                                dtls.AddRange(mDtls);
                            }
                    }, id
                        );
                // for
                bool didReturn = wtp.WaitOne(new TimeSpan(0,0,1,0), true);

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
        internal static HealthServiceRecordComponent GetRecord(VersionedDomainIdentifier recordId, List<IResultDetail> dtls, List<DetectedIssue> issues, ClientRegistryFhirQuery qd)
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
                var result = persistence.GetContainer(recordId, !qd.IncludeHistory) as HealthServiceRecordContainer;

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
                if (filter is RegistrationEvent)
                    filter = filter.FindComponent(HealthServiceRecordSiteRoleType.SubjectOf) as HealthServiceRecordContainer;

                QueryParameters confidence = new QueryParameters() { Confidence = 1.0f };
                if(subject != null)// We're fetching a patient?
                    confidence = (subject).Confidence(filter as Person);

                //if (confidence.Confidence < qd.MinimumDegreeMatch)
                //    return null;

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


        /// <summary>
        /// Update the container
        /// </summary>
        public static IComponent Update(IComponent storeContainer, String id, DataPersistenceMode mode, List<IResultDetail> dtls)
        {
            // Get the services
            IDataPersistenceService persistence = ApplicationContext.CurrentContext.GetService(typeof(IDataPersistenceService)) as IDataPersistenceService;
            IDataRegistrationService registration = ApplicationContext.CurrentContext.GetService(typeof(IDataRegistrationService)) as IDataRegistrationService;

            try
            {

                // Sanity check
                if (persistence == null)
                    throw new InvalidOperationException("No persistence service has been configured, registrations cannot continue without this service");
                else if (registration == null)
                    throw new InvalidOperationException("No registration service has been configured, registrations cannot continue without this service");

                // Store
                if((storeContainer as HealthServiceRecordContainer).Id == default(decimal))
                    (storeContainer as HealthServiceRecordContainer).Id = decimal.Parse(id);

                var storedData = persistence.UpdateContainer(storeContainer as IContainer, mode);
                if (storeContainer != null)
                    registration.RegisterRecord(storeContainer, mode);

                // Now read and return
                return persistence.GetContainer(storedData, true) as IComponent;
                //return null;

            }
            catch (Exception ex)
            {
                Trace.TraceError(ex.ToString());
                dtls.Add(new PersistenceResultDetail(ResultDetailType.Error, ex.Message, ex));
                throw;
            }
        }
    }
}
