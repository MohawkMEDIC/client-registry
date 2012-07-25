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
    /// A query response factory for the find candidates message
    /// </summary>
    public class FindCandidatesQueryResponseFactory : IQueryResponseFactory
    {
        #region IQueryResponseFactory Members

        /// <summary>
        /// Gets the type that this response factory creates
        /// </summary>
        public Type CreateType
        {
            get { return typeof(PRPA_IN101104CA); }
        }

        /// <summary>
        /// Creates filter data from the request
        /// </summary>
        public DataUtil.QueryData CreateFilterData(MARC.Everest.Interfaces.IInteraction request, List<MARC.Everest.Connectors.IResultDetail> dtls)
        {
            ILocalizationService locale = Context.GetService(typeof(ILocalizationService)) as ILocalizationService;
            
            // Componentize the message into the data model
            ComponentUtil compUtil = new ComponentUtil();
            compUtil.Context = this.Context;
            PRPA_IN101103CA rqst = request as PRPA_IN101103CA;

            List<VersionedDomainIdentifier> ids = new List<VersionedDomainIdentifier>();
            var queryData = compUtil.CreateQueryMatch(rqst.controlActEvent, dtls, ref ids);

            if (ids == null || queryData == null)
                throw new MessageValidationException(locale.GetString("MSG00A"), request);

            var filter = MessageUtil.CreateQueryData(rqst.controlActEvent.QueryByParameter, String.Format("{0}@{1}",
                    rqst.Sender.Device.Id.Root,
                    rqst.Sender.Device.Id.Extension)
                );
            filter.OriginalMessageQueryId = request.Id.Root;
            filter.QueryRequest = queryData;
            filter.RecordIds = ids;

            return filter;
        }

        /// <summary>
        /// Create the interaction
        /// </summary>
        public MARC.Everest.Interfaces.IInteraction Create(MARC.Everest.Interfaces.IInteraction request, DataUtil.QueryResultData results, List<MARC.Everest.Connectors.IResultDetail> details, List<SVC.Core.Issues.DetectedIssue> issues)
        {
            // GEt the config services
            ISystemConfigurationService configService = Context.GetService(typeof(ISystemConfigurationService)) as ISystemConfigurationService;

            List<MARC.Everest.RMIM.CA.R020402.MFMI_MT700746CA.Subject2<MARC.Everest.RMIM.CA.R020402.PRPA_MT101104CA.IdentifiedEntity>> retHl7v3 = new List<MARC.Everest.RMIM.CA.R020402.MFMI_MT700746CA.Subject2<MARC.Everest.RMIM.CA.R020402.PRPA_MT101104CA.IdentifiedEntity>>(results.Results.Count());
            CaDeComponentUtil dCompUtil = new CaDeComponentUtil();
            dCompUtil.Context = this.Context;

            PRPA_IN101103CA rqst = request as PRPA_IN101103CA;

            // Convert results to HL7v3
            foreach (var res in results.Results)
            {
                var retRec = new MARC.Everest.RMIM.CA.R020402.MFMI_MT700746CA.Subject2<MARC.Everest.RMIM.CA.R020402.PRPA_MT101104CA.IdentifiedEntity>(
                    dCompUtil.CreateRegistrationEvent(res, details)
                );
                if (retRec.RegistrationEvent  == null)
                    retRec = new MARC.Everest.RMIM.CA.R020402.MFMI_MT700746CA.Subject2<MARC.Everest.RMIM.CA.R020402.PRPA_MT101104CA.IdentifiedEntity>(
                        new MARC.Everest.RMIM.CA.R020402.MFMI_MT700746CA.RegistrationEvent<MARC.Everest.RMIM.CA.R020402.PRPA_MT101104CA.IdentifiedEntity>()
                        {
                            NullFlavor = NullFlavor.NoInformation
                        }
                    );
                retHl7v3.Add(retRec);
            }

            // Create the response
            PRPA_IN101104CA response = new PRPA_IN101104CA
            (
                Guid.NewGuid(),
                DateTime.Now,
                ResponseMode.Immediate,
                PRPA_IN101104CA.GetInteractionId(),
                PRPA_IN101104CA.GetProfileId(),
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
            response.controlActEvent = PRPA_IN101104CA.CreateControlActEvent(
                new II(configService.Custodianship.Id.Domain, Guid.NewGuid().ToString()),
                PRPA_IN101104CA.GetTriggerEvent(),
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
        public SVC.Core.HostContext Context
        {
            get;
            set;
        }

        #endregion
    }
}
