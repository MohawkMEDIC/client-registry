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
using MARC.HI.EHRS.CR.Core.Services;

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
            public QueryEvent Filter;

        }

        /// <summary>
        /// Query the data store
        /// </summary>
        public static IComponent Register(IComponent storeContainer, DataPersistenceMode mode, List<IResultDetail> details)
        {
            IClientRegistryDataService dataService = ApplicationContext.CurrentContext.GetService(typeof(IClientRegistryDataService)) as IClientRegistryDataService;

            try
            {

                // Sanity check
                if (dataService == null)
                    throw new InvalidOperationException("No persistence service has been configured, registrations cannot continue without this service");

                // Store
                var result = dataService.Register(storeContainer as RegistrationEvent, mode);
                details.AddRange(result.Details);

                if (result == null || result.VersionId == null)
                    throw new Exception(ApplicationContext.LocalizationService.GetString("DTPE001"));
                
                // Now read and return
                return dataService.Get(
                    new VersionedDomainIdentifier[] { result.VersionId },
                    new RegistryQueryRequest()
                    {
                        IsContinue = false, 
                        IsSummary = true,
                        Limit = 1,
                        Offset = 0, 
                        QueryId = Guid.NewGuid().ToString()
                    }
                ).Results?.FirstOrDefault() as IComponent;
                
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
            IClientRegistryDataService dataService = ApplicationContext.CurrentContext.GetService(typeof(IClientRegistryDataService)) as IClientRegistryDataService;
            IQueryPersistenceService queryService = ApplicationContext.CurrentContext.GetService(typeof(IQueryPersistenceService)) as IQueryPersistenceService;
            try
            {

                if(querySpec.Quantity > 100)
                    throw new ConstraintException("Query limit must not exceed 100");

                if (dataService == null)
                    throw new InvalidOperationException("No persistence service has been configured, queries cannot continue without this service");
                
                FhirQueryResult result = new FhirQueryResult();
                result.Query = querySpec;
                result.Issues = new List<DetectedIssue>();
                result.Details = details;
                result.Results = new List<SVC.Messaging.FHIR.Resources.ResourceBase>(querySpec.Quantity);
                
                VersionedDomainIdentifier[] identifiers;

                RegistryQueryRequest queryRequest = new RegistryQueryRequest()
                {
                    QueryRequest = querySpec.Filter,
                    QueryId = querySpec.QueryId != Guid.Empty ? querySpec.QueryId.ToString() : null,
                    QueryTag = querySpec.QueryId != Guid.Empty ? querySpec.QueryId.ToString(): null,
                    IsSummary = !querySpec.IncludeHistory,
                    Offset = querySpec.Start,
                    Limit = querySpec.Quantity
                };
   

                // Is this a continue?
                queryRequest.IsContinue = (!String.IsNullOrEmpty(queryRequest.QueryId) && queryRequest.Offset > 0);

                var dataResults = dataService.Query(queryRequest);
                details.AddRange(dataResults.Details);

                result.TotalResults = dataResults.TotalResults;

                // Fetch the results
                foreach (HealthServiceRecordContainer res in dataResults.Results)
                {
                    if (res == null)
                    {
                        continue;
                    }

                    var resultSubject = res.FindComponent(HealthServiceRecordSiteRoleType.SubjectOf) as HealthServiceRecordContainer ?? res;
                    var processor = FhirMessageProcessorUtil.GetComponentProcessor(resultSubject.GetType());
                    if (processor == null)
                        result.Details.Add(new NotImplementedResultDetail(ResultDetailType.Error, String.Format("Will not include {1}^^^&{2}&ISO in result set, cannot find converter for {0}", resultSubject.GetType().Name, resultSubject.Id, ApplicationContext.ConfigurationService.OidRegistrar.GetOid("CR_CID").Oid), null, null));
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
        /// Update the container
        /// </summary>
        public static IComponent Update(IComponent storeContainer, String id, DataPersistenceMode mode, List<IResultDetail> dtls)
        {
            IClientRegistryDataService dataService = ApplicationContext.CurrentContext.GetService(typeof(IClientRegistryDataService)) as IClientRegistryDataService;

            try
            {
                // Sanity check
                if (dataService == null)
                    throw new InvalidOperationException("No persistence service has been configured, registrations cannot continue without this service");

                // Store
                var result = dataService.Update(storeContainer as RegistrationEvent, mode);
                dtls.AddRange(result.Details);

                if (result == null || result.VersionId == null)
                    throw new Exception(ApplicationContext.LocalizationService.GetString("DTPE001"));
                
                // Now read and return
                return dataService.Get(
                    new VersionedDomainIdentifier[] { result.VersionId },
                    new RegistryQueryRequest()
                    {
                        IsContinue = false,
                        IsSummary = true,
                        Limit = 1,
                        Offset = 0,
                        QueryId = Guid.NewGuid().ToString()
                    }
                ).Results?.FirstOrDefault() as IComponent;

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
