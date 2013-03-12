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
 * Date: 17-9-2012
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MARC.HI.EHRS.SVC.Messaging.Everest;
using MARC.Everest.RMIM.UV.NE2008.Interactions;
using MARC.Everest.Interfaces;
using MARC.Everest.Connectors;
using MARC.HI.EHRS.SVC.Core.Issues;
using MARC.HI.EHRS.SVC.Core.Services;
using MARC.Everest.RMIM.UV.NE2008.Vocabulary;
using MARC.Everest.DataTypes;
using MARC.HI.EHRS.SVC.Core.DataTypes;
using MARC.Everest.Exceptions;
using System.Diagnostics;
using MARC.HI.EHRS.SVC.Core.ComponentModel.Components;

namespace MARC.HI.EHRS.CR.Messaging.Everest.MessageReceiver.UV
{
    public class PdqSupplier : IEverestMessageReceiver
    {
        #region IEverestMessageReceiver Members

        /// <summary>
        /// Handle a PDQ message 
        /// </summary>
        public MARC.Everest.Interfaces.IGraphable HandleMessageReceived(object sender, MARC.Everest.Connectors.UnsolicitedDataEventArgs e, MARC.Everest.Connectors.IReceiveResult receivedMessage)
        {
            IGraphable response = null;

            if (receivedMessage.Structure is PRPA_IN201305UV02) // Activates the patient record
                response = HandleQueryPatientDemographics(e, receivedMessage);
            
            if(response == null)
            {
                var msgr = new NotSupportedMessageReceiver();
                msgr.Context = this.Context;
                response = msgr.HandleMessageReceived(sender, e, receivedMessage);
            }

            return response;
        }

        /// <summary>
        /// Handle a query for patient demographic data
        /// </summary>
        private IGraphable HandleQueryPatientDemographics(MARC.Everest.Connectors.UnsolicitedDataEventArgs e, MARC.Everest.Connectors.IReceiveResult receivedMessage)
        {
            // Setup the lists of details and issues
            List<IResultDetail> dtls = new List<IResultDetail>(receivedMessage.Details);
            List<DetectedIssue> issues = new List<DetectedIssue>();

            // System configuration service
            ISystemConfigurationService configService = Context.GetService(typeof(ISystemConfigurationService)) as ISystemConfigurationService;

            // Localization service
            ILocalizationService locale = Context.GetService(typeof(ILocalizationService)) as ILocalizationService;

            // Do basic check and add common validation errors
            MessageUtil.ValidateTransportWrapperUv(receivedMessage.Structure as IInteraction, configService, dtls);

            // Check the request is valid
            var request = receivedMessage.Structure as PRPA_IN201305UV02;
            if (request == null)
                return null;

            // Determine if the received message was interpreted properly
            bool isValid = MessageUtil.IsValid(receivedMessage);

            // set the URI
            if (request.controlActProcess != null)
                request.controlActProcess.Code = request.controlActProcess.Code ?? Util.Convert<CD<String>>(PRPA_IN201302UV02.GetTriggerEvent());
            if (request.Receiver.Count > 0)
                request.Receiver[0].Telecom = request.Receiver[0].Telecom ?? e.ReceiveEndpoint.ToString();


            // Construct the acknowledgment
            var response = new PRPA_IN201306UV02(
                new II(configService.OidRegistrar.GetOid("CR_MSGID").Oid, Guid.NewGuid().ToString()),
                DateTime.Now,
                PRPA_IN201306UV02.GetInteractionId(),
                request.ProcessingCode,
                request.ProcessingModeCode,
                AcknowledgementCondition.Never,
                MessageUtil.CreateReceiver(request.Sender),
                MessageUtil.CreateSenderUv(e.ReceiveEndpoint, configService),
                null
            );


            // Create the support classes
            AuditData audit = null;

            IheDataUtil dataUtil = new IheDataUtil() { Context = this.Context };

            // Try to execute the record
            try
            {
                // Determine if the message is valid
                if (!isValid)
                    throw new MessageValidationException(locale.GetString("MSGE00A"), receivedMessage.Structure);

                // Construct the canonical data structure
                PatientDemographicsQueryResponseFactory fact = new PatientDemographicsQueryResponseFactory() { Context = this.Context };
                DataUtil.QueryData filter = fact.CreateFilterData(request, dtls);

                if (filter.QueryRequest == null)
                    throw new MessageValidationException(locale.GetString("MSGE00A"), receivedMessage.Structure);

                // Query
                var results = dataUtil.Query(filter, dtls, issues);


                // Prepare for audit
                audit = dataUtil.CreateAuditData("ITI-47",
                    ActionType.Execute,
                    dtls.Exists(r => r.Type == ResultDetailType.Error) ? OutcomeIndicator.MinorFail : OutcomeIndicator.Success,
                    e,
                    receivedMessage,
                    results,
                    filter.QueryRequest.FindComponent(SVC.Core.ComponentModel.HealthServiceRecordSiteRoleType.AuthorOf) as HealthcareParticipant
                );


                response = fact.Create(request, results, dtls, issues) as PRPA_IN201306UV02;
            }
            catch (Exception ex)
            {
                Trace.TraceError(ex.ToString());

                // Prepare for audit
                audit = dataUtil.CreateAuditData("ITI-47", ActionType.Execute, OutcomeIndicator.EpicFail, e, receivedMessage,
                    new List<VersionedDomainIdentifier>(),
                    null
                );

                dtls.Add(new ResultDetail(ResultDetailType.Error, ex.Message, ex));
                response.Acknowledgement.Add(new MARC.Everest.RMIM.UV.NE2008.MCCI_MT100300UV01.Acknowledgement(
                    AcknowledgementType.AcceptAcknowledgementCommitError,
                    new MARC.Everest.RMIM.UV.NE2008.MCCI_MT100200UV01.TargetMessage(request.Id)
                ));
            }
            finally
            {
                IAuditorService auditService = Context.GetService(typeof(IAuditorService)) as IAuditorService;
                if (auditService != null)
                    auditService.SendAudit(audit);
            }

            // Common response parameters
            response.ProfileId = new SET<II>(MCCI_IN000002UV01.GetProfileId());
            response.VersionCode = HL7StandardVersionCode.Version3_Prerelease1;
            response.AcceptAckCode = AcknowledgementCondition.Never;
            response.Acknowledgement[0].AcknowledgementDetail.AddRange(MessageUtil.CreateAckDetailsUv(dtls.ToArray()));
            response.Acknowledgement[0].AcknowledgementDetail.AddRange(MessageUtil.CreateAckDetailsUv(issues.ToArray()));
            return response;
        }

        #endregion

        #region IUsesHostContext Members

        /// <summary>
        /// Gets or sets the context for the receiver
        /// </summary>
        public IServiceProvider Context
        {
            get;
            set;
        }


        #endregion

        #region ICloneable Members

        public object Clone()
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
