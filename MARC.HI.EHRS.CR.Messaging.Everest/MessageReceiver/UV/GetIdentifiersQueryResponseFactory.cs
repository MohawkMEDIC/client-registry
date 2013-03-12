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
using MARC.Everest.RMIM.UV.NE2008.Interactions;
using MARC.HI.EHRS.SVC.Core.Services;
using MARC.HI.EHRS.SVC.Core.DataTypes;
using MARC.Everest.Exceptions;
using MARC.Everest.RMIM.UV.NE2008.Vocabulary;
using MARC.Everest.Connectors;
using MARC.Everest.DataTypes;

namespace MARC.HI.EHRS.CR.Messaging.Everest.MessageReceiver.UV
{
    /// <summary>
    /// Query response factory
    /// </summary>
    public class GetIdentifiersQueryResponseFactory : IQueryResponseFactory
    {
        #region IQueryResponseFactory Members

        /// <summary>
        /// Get the type of message this query creates
        /// </summary>
        public Type CreateType
        {
            get { return typeof(PRPA_IN201310UV02); }
        }

        /// <summary>
        /// Create filter data
        /// </summary>
        public DataUtil.QueryData CreateFilterData(MARC.Everest.Interfaces.IInteraction request, List<MARC.Everest.Connectors.IResultDetail> dtls)
        {
            ILocalizationService locale = Context.GetService(typeof(ILocalizationService)) as ILocalizationService;
            ISystemConfigurationService config = Context.GetService(typeof(ISystemConfigurationService)) as ISystemConfigurationService;

            // Componentize the message into the data model
            UvComponentUtil compUtil = new UvComponentUtil();
            compUtil.Context = this.Context;
            PRPA_IN201309UV02 rqst = request as PRPA_IN201309UV02;

            List<DomainIdentifier> ids = new List<DomainIdentifier>();
            var queryData = compUtil.CreateQueryMatch(rqst.controlActProcess, dtls, ref ids);

            
            if (ids == null || queryData == null)
                throw new MessageValidationException(locale.GetString("MSGE00A"), request);

            var filter = new DataUtil.QueryData()
            {
                QueryId = String.Format("{1}^^^&{0}&ISO", rqst.controlActProcess.queryByParameter.QueryId.Root, rqst.controlActProcess.queryByParameter.QueryId.Extension),
                IncludeHistory = false,
                IncludeNotes = false,
                Quantity = 100,
                Originator = String.Format("^^^&{0}&ISO",
                    rqst.Sender.Device.Id[0].Root),
                OriginalMessageQuery = request,
                QueryRequest = queryData,
                TargetDomains = ids,
                IsSummary = true
            };

            // Ensure that the target domains are understood by this service
            if(filter.TargetDomains != null)
                foreach(var id in filter.TargetDomains)
                    if (String.IsNullOrEmpty(id.Domain) || config.OidRegistrar.FindData(id.Domain) == null || !config.OidRegistrar.FindData(id.Domain).Attributes.Exists(p => p.Key.Equals("AssigningAuthorityName")))
                        dtls.Add(new UnrecognizedTargetDomainResultDetail(locale, id.Domain));
            return filter;
        }

        /// <summary>
        /// Create the response message
        /// </summary>
        public MARC.Everest.Interfaces.IInteraction Create(MARC.Everest.Interfaces.IInteraction request, DataUtil.QueryResultData results, List<MARC.Everest.Connectors.IResultDetail> details, List<SVC.Core.Issues.DetectedIssue> issues)
        {
            // GEt the config services
            ISystemConfigurationService configService = Context.GetService(typeof(ISystemConfigurationService)) as ISystemConfigurationService;
            ILocalizationService localeService = Context.GetService(typeof(ILocalizationService)) as ILocalizationService;

            var retHl7v3 =  new List<MARC.Everest.RMIM.UV.NE2008.MFMI_MT700711UV01.Subject1<MARC.Everest.RMIM.UV.NE2008.PRPA_MT201304UV02.Patient,object>>(results.Results.Length);

            UvDeComponentUtil dCompUtil = new UvDeComponentUtil();
            dCompUtil.Context = this.Context;

            PRPA_IN201309UV02 rqst = request as PRPA_IN201309UV02;

            // Convert results to HL7v3
            foreach (var res in results.Results)
            {
                var retRec = new MARC.Everest.RMIM.UV.NE2008.MFMI_MT700711UV01.Subject1<MARC.Everest.RMIM.UV.NE2008.PRPA_MT201304UV02.Patient,object>(
                    false,
                    dCompUtil.CreateRegistrationEventDetail(res, details)
                );
                if (retRec.RegistrationEvent == null)
                    retRec = new MARC.Everest.RMIM.UV.NE2008.MFMI_MT700711UV01.Subject1<MARC.Everest.RMIM.UV.NE2008.PRPA_MT201304UV02.Patient,object>(
                        false,
                        new MARC.Everest.RMIM.UV.NE2008.MFMI_MT700711UV01.RegistrationEvent<MARC.Everest.RMIM.UV.NE2008.PRPA_MT201304UV02.Patient,object>() { NullFlavor = NullFlavor.NoInformation }
                    );
                retHl7v3.Add(retRec);
            }
            
            if(retHl7v3.Count > 1)
                details.Add(new InsufficientRepetitionsResultDetail(ResultDetailType.Warning, localeService.GetString("MSGE06E"), null));

            // Create the response
            PRPA_IN201310UV02 response = new PRPA_IN201310UV02
            (
                Guid.NewGuid(),
                DateTime.Now,
                PRPA_IN201310UV02.GetInteractionId(),
                ProcessingID.Production,
                "T",
                AcknowledgementCondition.Never,
                MessageUtil.CreateReceiver(rqst.Sender),
                MessageUtil.CreateSenderUv(new Uri(rqst.Receiver[0].Telecom.Value), configService),
                null
            )
            {
                Acknowledgement = new List<MARC.Everest.RMIM.UV.NE2008.MCCI_MT100300UV01.Acknowledgement>() {
                    new MARC.Everest.RMIM.UV.NE2008.MCCI_MT100300UV01.Acknowledgement(
                        details.Count(a => a.Type == ResultDetailType.Error) == 0 ? AcknowledgementType.ApplicationAcknowledgementAccept : AcknowledgementType.ApplicationAcknowledgementError,
                        new MARC.Everest.RMIM.UV.NE2008.MCCI_MT100200UV01.TargetMessage(request.Id)
                    )
                }
            };
           
            response.controlActProcess = new MARC.Everest.RMIM.UV.NE2008.MFMI_MT700711UV01.ControlActProcess<MARC.Everest.RMIM.UV.NE2008.PRPA_MT201307UV02.QueryByParameter,MARC.Everest.RMIM.UV.NE2008.PRPA_MT201304UV02.Patient,object>("EVN")
            {
                Id = SET<II>.CreateSET(new II(configService.Custodianship.Id.Domain, Guid.NewGuid().ToString())),
                Code = new CD<string>(PRPA_IN201310UV02.GetTriggerEvent().Code, PRPA_IN201310UV02.GetTriggerEvent().CodeSystem),
                QueryAck = new MARC.Everest.RMIM.UV.NE2008.QUQI_MT120001UV01.QueryAck(
                    rqst.controlActProcess.queryByParameter.QueryId,
                    "complete",
                    (AcknowledgementType)response.Acknowledgement[0].TypeCode == AcknowledgementType.ApplicationAcknowledgementError ? QueryResponse.ApplicationError : results.TotalResults == 0 ? QueryResponse.NoDataFound : QueryResponse.DataFound,
                    results.TotalResults,
                    results.Results.Length,
                    results.TotalResults - results.Results.Length - results.StartRecordNumber
                ),
                queryByParameter = rqst.controlActProcess.queryByParameter
            };
                        
            response.controlActProcess.LanguageCode = MessageUtil.GetDefaultLanguageCode(this.Context);
            if(retHl7v3.Count > 0)
                response.controlActProcess.Subject.Add(retHl7v3[0]);
            return response;
        }

        #endregion

        #region IUsesHostContext Members

        /// <summary>
        /// GEts or sets the host context
        /// </summary>
        public IServiceProvider Context
        {
            get;
            set;
        }

        #endregion
    }
}
