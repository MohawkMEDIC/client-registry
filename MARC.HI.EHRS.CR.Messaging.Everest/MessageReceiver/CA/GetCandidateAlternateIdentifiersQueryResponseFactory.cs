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
 * Date: 4-9-2012
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MARC.Everest.RMIM.CA.R020402.Interactions;
using MARC.HI.EHRS.SVC.Core.Services;
using MARC.HI.EHRS.SVC.Core.DataTypes;
using MARC.Everest.Exceptions;
using MARC.HI.EHRS.CR.Core.ComponentModel;
using MARC.Everest.RMIM.CA.R020402.Vocabulary;
using MARC.Everest.Connectors;
using MARC.Everest.DataTypes;

namespace MARC.HI.EHRS.CR.Messaging.Everest.MessageReceiver.CA
{
    /// <summary>
    /// A query response factory for the get candidates message
    /// </summary>
    public class GetCandidateAlternateIdentifiersQueryResponseFactory : IQueryResponseFactory
    {
        #region IQueryResponseFactory Members

        /// <summary>
        /// Gets the type that this response factory creates
        /// </summary>
        public Type CreateType
        {
            get { return typeof(PRPA_IN101106CA); }
        }

        /// <summary>
        /// Creates filter data from the request
        /// </summary>
        public DataUtil.QueryData CreateFilterData(MARC.Everest.Interfaces.IInteraction request, List<MARC.Everest.Connectors.IResultDetail> dtls)
        {
            ILocalizationService locale = Context.GetService(typeof(ILocalizationService)) as ILocalizationService;
            
            // Componentize the message into the data model
            CaComponentUtil compUtil = new CaComponentUtil();
            compUtil.Context = this.Context;
            PRPA_IN101105CA rqst = request as PRPA_IN101105CA;

            List<DomainIdentifier> ids = new List<DomainIdentifier>();
            var queryData = compUtil.CreateQueryMatch(rqst.controlActEvent, dtls, ref ids);

            if (ids == null || queryData == null)
                throw new MessageValidationException(locale.GetString("MSGE00A"), request);

            var filter = MessageUtil.CreateQueryData(rqst.controlActEvent.QueryByParameter, String.Format("{1}^^^&{0}&ISO",
                    rqst.Sender.Device.Id.Root,
                    rqst.Sender.Device.Id.Extension)
                );
            filter.OriginalMessageQuery = request;
            filter.QueryRequest = queryData;
            filter.TargetDomains = ids;
            filter.IncludeHistory = true;

            return filter;
        }

        /// <summary>
        /// Create the interaction
        /// </summary>
        public MARC.Everest.Interfaces.IInteraction Create(MARC.Everest.Interfaces.IInteraction request, DataUtil.QueryResultData results, List<MARC.Everest.Connectors.IResultDetail> details, List<SVC.Core.Issues.DetectedIssue> issues)
        {
            // GEt the config services
            ISystemConfigurationService configService = Context.GetService(typeof(ISystemConfigurationService)) as ISystemConfigurationService;

            List<MARC.Everest.RMIM.CA.R020402.MFMI_MT700746CA.Subject2<MARC.Everest.RMIM.CA.R020402.PRPA_MT101106CA.IdentifiedEntity>> retHl7v3 = new List<MARC.Everest.RMIM.CA.R020402.MFMI_MT700746CA.Subject2<MARC.Everest.RMIM.CA.R020402.PRPA_MT101106CA.IdentifiedEntity>>(results.Results.Count());
            CaDeComponentUtil dCompUtil = new CaDeComponentUtil();
            dCompUtil.Context = this.Context;

            PRPA_IN101105CA rqst = request as PRPA_IN101105CA;

            // Convert results to HL7v3
            foreach (var res in results.Results)
            {
                var retRec = new MARC.Everest.RMIM.CA.R020402.MFMI_MT700746CA.Subject2<MARC.Everest.RMIM.CA.R020402.PRPA_MT101106CA.IdentifiedEntity>(
                    dCompUtil.CreateRegistrationEventDetail(res, details)
                );
                if (retRec.RegistrationEvent  == null)
                    retRec = new MARC.Everest.RMIM.CA.R020402.MFMI_MT700746CA.Subject2<MARC.Everest.RMIM.CA.R020402.PRPA_MT101106CA.IdentifiedEntity>(
                        new MARC.Everest.RMIM.CA.R020402.MFMI_MT700746CA.RegistrationEvent<MARC.Everest.RMIM.CA.R020402.PRPA_MT101106CA.IdentifiedEntity>()
                        {
                            NullFlavor = NullFlavor.NoInformation
                        }
                    );
                retHl7v3.Add(retRec);
            }

            // HACK: Sort by confidence score (if present)
            retHl7v3.Sort((a, b) => b.RegistrationEvent.Subject.registeredRole.SubjectOf.ObservationEvent.Value.CompareTo(a.RegistrationEvent.Subject.registeredRole.SubjectOf.ObservationEvent.Value));

            // Create the response
            PRPA_IN101106CA response = new PRPA_IN101106CA
            (
                Guid.NewGuid(),
                DateTime.Now,
                ResponseMode.Immediate,
                PRPA_IN101106CA.GetInteractionId(),
                PRPA_IN101106CA.GetProfileId(),
                ProcessingID.Production,
                AcknowledgementCondition.Never,
                MessageUtil.CreateReceiver(rqst.Sender),
                MessageUtil.CreateSender(new Uri(rqst.Receiver.Telecom.Value), configService),
                new MARC.Everest.RMIM.CA.R020402.MCCI_MT002200CA.Acknowledgement(
                    details.Count(a => a.Type == ResultDetailType.Error) == 0 ? AcknowledgementType.ApplicationAcknowledgementAccept : AcknowledgementType.ApplicationAcknowledgementError,
                    new MARC.Everest.RMIM.CA.R020402.MCCI_MT002200CA.TargetMessage(request.Id)
                )
            );
            response.Acknowledgement.AcknowledgementDetail = MessageUtil.CreateAckDetails(details.ToArray());
            response.controlActEvent = PRPA_IN101106CA.CreateControlActEvent(
                new II(configService.Custodianship.Id.Domain, Guid.NewGuid().ToString()),
                PRPA_IN101106CA.GetTriggerEvent(),
                new MARC.Everest.RMIM.CA.R020402.QUQI_MT120008CA.QueryAck(
                    rqst.controlActEvent.QueryByParameter.QueryId,
                    results.TotalResults == 0 ? QueryResponse.NoDataFound : (AcknowledgementType)response.Acknowledgement.TypeCode == AcknowledgementType.ApplicationAcknowledgementError ? QueryResponse.ApplicationError : QueryResponse.DataFound,
                    results.TotalResults,
                    results.Results.Length,
                    results.TotalResults - results.Results.Length - results.StartRecordNumber
                ),
                rqst.controlActEvent.QueryByParameter
            );
            response.controlActEvent.LanguageCode = MessageUtil.GetDefaultLanguageCode(this.Context);
            if (issues.Count > 0)
                response.controlActEvent.SubjectOf.AddRange(MessageUtil.CreateDetectedIssueEventsQuery(issues));
            response.controlActEvent.Subject.AddRange(retHl7v3);

            return response;
        }

        #endregion

        #region IUsesHostContext Members

        /// <summary>
        /// Gets or sets the context that this operates within
        /// </summary>
        public IServiceProvider Context
        {
            get;
            set;
        }

        #endregion
    }
}
