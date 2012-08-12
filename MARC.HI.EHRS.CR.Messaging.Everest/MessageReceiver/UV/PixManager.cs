﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MARC.HI.EHRS.SVC.Messaging.Everest;
using MARC.Everest.RMIM.UV.NE2008.Interactions;
using MARC.Everest.Interfaces;
using MARC.HI.EHRS.SVC.Core.Services;
using MARC.Everest.DataTypes;
using MARC.Everest.RMIM.UV.NE2008.Vocabulary;
using MARC.Everest.Connectors;
using MARC.HI.EHRS.SVC.Core.Issues;
using MARC.Everest.Exceptions;
using System.Diagnostics;
using MARC.HI.EHRS.SVC.Core.DataTypes;
using MARC.HI.EHRS.SVC.Core.ComponentModel.Components;

namespace MARC.HI.EHRS.CR.Messaging.Everest.MessageReceiver.UV
{
    class PixManager : IEverestMessageReceiver
    {
        #region IEverestMessageReceiver Members

        public MARC.Everest.Interfaces.IGraphable HandleMessageReceived(object sender, MARC.Everest.Connectors.UnsolicitedDataEventArgs e, MARC.Everest.Connectors.IReceiveResult receivedMessage)
        {
            IGraphable response = null;

            if (receivedMessage.Structure is PRPA_IN201301UV02) // Activates the patient record
                response = HandlePatientRegistryRecordAdded(e, receivedMessage);
            else if (receivedMessage.Structure is PRPA_IN201302UV02) // Revises the patient record
                response = HandlePatientRegistryRecordRevised(e, receivedMessage);
            else if (receivedMessage.Structure is PRPA_IN201304UV02)
                response = HandlePatientRegistryDuplicatesResolved(e, receivedMessage);
            else
            {
                var msgr = new NotSupportedMessageReceiver();
                msgr.Context = this.Context;
                response = msgr.HandleMessageReceived(sender, e, receivedMessage);
            }

            return response;
        }

        private IGraphable HandlePatientRegistryDuplicatesResolved(MARC.Everest.Connectors.UnsolicitedDataEventArgs e, MARC.Everest.Connectors.IReceiveResult receivedMessage)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Handles PRPA_IN201302UV02 ITI-44
        /// </summary>
        private IGraphable HandlePatientRegistryRecordRevised(MARC.Everest.Connectors.UnsolicitedDataEventArgs e, MARC.Everest.Connectors.IReceiveResult receivedMessage)
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
            var request = receivedMessage.Structure as PRPA_IN201302UV02;
            if (request == null)
                return null;

            // Determine if the received message was interpreted properly
            bool isValid = MessageUtil.IsValid(receivedMessage);

            // set the URI
            request.controlActProcess.Code = request.controlActProcess.Code ?? Util.Convert<CD<String>>(PRPA_IN201302UV02.GetTriggerEvent());
            request.Receiver[0].Telecom = request.Receiver[0].Telecom ?? e.ReceiveEndpoint.ToString();

            // Construct the acknowledgment
            var response = new MCCI_IN000002UV01(
                new II(configService.OidRegistrar.GetOid("CR_MSGID").Oid, Guid.NewGuid().ToString()),
                DateTime.Now,
                MCCI_IN000002UV01.GetInteractionId(),
                request.ProcessingCode,
                request.ProcessingModeCode,
                MessageUtil.CreateReceiver(request.Sender),
                MessageUtil.CreateSenderUv(e.ReceiveEndpoint, configService)
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
                UvComponentUtil cu = new UvComponentUtil() { Context = this.Context };
                var data = cu.CreateComponents(request.controlActProcess, dtls);

                // Componentization fail?
                if (data == null)
                    throw new MessageValidationException(locale.GetString("MSGE00A"), receivedMessage.Structure);

                // Store 
                var vid = dataUtil.Update(data, dtls, issues, request.ProcessingCode == ProcessingID.Debugging ? DataPersistenceMode.Debugging : DataPersistenceMode.Production);

                if (vid == null)
                    throw new Exception(locale.GetString("DTPE001"));

                // Prepare for audit
                audit = dataUtil.CreateAuditData("ITI-44",
                    vid.UpdateMode == UpdateModeType.Update ? ActionType.Update : ActionType.Create,
                    dtls.Exists(r => r.Type == ResultDetailType.Error) ? OutcomeIndicator.MinorFail : OutcomeIndicator.Success,
                    e,
                    receivedMessage,
                    new List<VersionedDomainIdentifier>() { vid },
                    data.FindComponent(SVC.Core.ComponentModel.HealthServiceRecordSiteRoleType.AuthorOf) as HealthcareParticipant
                );

                // Add ack
                response.Acknowledgement.Add(new MARC.Everest.RMIM.UV.NE2008.MCCI_MT100200UV01.Acknowledgement(
                    AcknowledgementType.AcceptAcknowledgementCommitAccept,
                    new MARC.Everest.RMIM.UV.NE2008.MCCI_MT100200UV01.TargetMessage(request.Id)
                ));
            }
            catch (Exception ex)
            {
                Trace.TraceError(ex.ToString());

                // Prepare for audit
                audit = dataUtil.CreateAuditData("ITI-44", ActionType.Create, OutcomeIndicator.EpicFail, e, receivedMessage,
                    new List<VersionedDomainIdentifier>(),
                    null
                );

                dtls.Add(new ResultDetail(ResultDetailType.Error, ex.Message, ex));
                response.Acknowledgement.Add(new MARC.Everest.RMIM.UV.NE2008.MCCI_MT100200UV01.Acknowledgement(
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

        /// <summary>
        /// Handles PRPA_IN201301UV02 ITI-44
        /// </summary>
        private IGraphable HandlePatientRegistryRecordAdded(MARC.Everest.Connectors.UnsolicitedDataEventArgs e, MARC.Everest.Connectors.IReceiveResult receivedMessage)
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
            var request = receivedMessage.Structure as PRPA_IN201301UV02;
            if (request == null)
                return null;
                        
            // Determine if the received message was interpreted properly
            bool isValid = MessageUtil.IsValid(receivedMessage);

            // set the URI
            request.controlActProcess.Code = request.controlActProcess.Code ?? Util.Convert<CD<String>>(PRPA_IN201301UV02.GetTriggerEvent());
            request.Receiver[0].Telecom = request.Receiver[0].Telecom ?? e.ReceiveEndpoint.ToString();

            // Construct the acknowledgment
            var response = new MCCI_IN000002UV01(
                new II(configService.OidRegistrar.GetOid("CR_MSGID").Oid, Guid.NewGuid().ToString()),
                DateTime.Now,
                MCCI_IN000002UV01.GetInteractionId(),
                request.ProcessingCode,
                request.ProcessingModeCode,
                MessageUtil.CreateReceiver(request.Sender),
                MessageUtil.CreateSenderUv(e.ReceiveEndpoint, configService)
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
                UvComponentUtil cu = new UvComponentUtil() { Context = this.Context };
                var data = cu.CreateComponents(request.controlActProcess, dtls);
                
                // Componentization fail?
                if (data == null || dataUtil.ValidateIdentifiers(data, dtls))
                    throw new MessageValidationException(locale.GetString("MSGE00A"), receivedMessage.Structure);


                // Store 
                var vid = dataUtil.Register(data, dtls, issues, request.ProcessingCode == ProcessingID.Debugging ? DataPersistenceMode.Debugging : DataPersistenceMode.Production);

                if (vid == null)
                    throw new Exception(locale.GetString("DTPE001"));

                // Prepare for audit
                audit = dataUtil.CreateAuditData("ITI-44",
                    vid.UpdateMode == UpdateModeType.Update ? ActionType.Update : ActionType.Create,
                    dtls.Exists(r => r.Type == ResultDetailType.Error) ? OutcomeIndicator.MinorFail : OutcomeIndicator.Success,
                    e,
                    receivedMessage,
                    new List<VersionedDomainIdentifier>() { vid },
                    data.FindComponent(SVC.Core.ComponentModel.HealthServiceRecordSiteRoleType.AuthorOf) as HealthcareParticipant
                );

                // Add ack
                response.Acknowledgement.Add(new MARC.Everest.RMIM.UV.NE2008.MCCI_MT100200UV01.Acknowledgement(
                    AcknowledgementType.AcceptAcknowledgementCommitAccept,
                    new MARC.Everest.RMIM.UV.NE2008.MCCI_MT100200UV01.TargetMessage(request.Id)
                ));
            }
            catch (Exception ex)
            {
                Trace.TraceError(ex.ToString());

                // Prepare for audit
                audit = dataUtil.CreateAuditData("ITI-44", ActionType.Create, OutcomeIndicator.EpicFail, e, receivedMessage,
                    new List<VersionedDomainIdentifier>(),
                    null
                );

                dtls.Add(new ResultDetail(ResultDetailType.Error, ex.Message, ex));
                response.Acknowledgement.Add(new MARC.Everest.RMIM.UV.NE2008.MCCI_MT100200UV01.Acknowledgement(
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
        public SVC.Core.HostContext Context
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
