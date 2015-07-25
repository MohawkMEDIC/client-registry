/**
 * Copyright 2015-2015 Mohawk College of Applied Arts and Technology
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
using MARC.HI.EHRS.CR.Core.ComponentModel;
using MARC.HI.EHRS.CR.Core.Services;

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
            else if (receivedMessage.Structure is PRPA_IN201309UV02)
                response = PatientRegistryGetIdentifiers(e, receivedMessage);
            else
            {
                var msgr = new NotSupportedMessageReceiver();
                msgr.Context = this.Context;
                response = msgr.HandleMessageReceived(sender, e, receivedMessage);
            }

            return response;
        }

        /// <summary>
        /// Patient registry get identifiers query
        /// </summary>
        /// <param name="e"></param>
        /// <param name="receivedMessage"></param>
        /// <returns></returns>
        private IGraphable PatientRegistryGetIdentifiers(UnsolicitedDataEventArgs e, IReceiveResult receivedMessage)
        {
            // Setup the lists of details and issues
            List<IResultDetail> dtls = new List<IResultDetail>(receivedMessage.Details);

            // System configuration service
            ISystemConfigurationService configService = Context.GetService(typeof(ISystemConfigurationService)) as ISystemConfigurationService;

            // Localization service
            ILocalizationService locale = Context.GetService(typeof(ILocalizationService)) as ILocalizationService;
            
            // Data Service
            IClientRegistryDataService dataSvc = Context.GetService(typeof(IClientRegistryDataService)) as IClientRegistryDataService;

            // Do basic check and add common validation errors
            MessageUtil.ValidateTransportWrapperUv(receivedMessage.Structure as IInteraction, configService, dtls);

            // Check the request is valid
            var request = receivedMessage.Structure as PRPA_IN201309UV02;
            if (request == null)
                return null;

            // Determine if the received message was interpreted properly
            bool isValid = MessageUtil.IsValid(receivedMessage);

            // set the URI
            if (request.controlActProcess != null)
                request.controlActProcess.Code = request.controlActProcess.Code ?? Util.Convert<CD<String>>(PRPA_IN201302UV02.GetTriggerEvent());
            if(request.Receiver.Count > 0)
                request.Receiver[0].Telecom = request.Receiver[0].Telecom ?? e.ReceiveEndpoint.ToString();


            // Construct the acknowledgment
            var response = new PRPA_IN201310UV02(
                new II(configService.OidRegistrar.GetOid("CR_MSGID").Oid, Guid.NewGuid().ToString()),
                DateTime.Now,
                PRPA_IN201310UV02.GetInteractionId(),
                request.ProcessingCode,
                request.ProcessingModeCode,
                AcknowledgementCondition.Never,
                MessageUtil.CreateReceiver(request.Sender),
                MessageUtil.CreateSenderUv(e.ReceiveEndpoint, configService),
                null
            );


            // Create the support classes
            AuditData audit = null;

            IheAuditUtil dataUtil = new IheAuditUtil() { Context = this.Context };
            
            // Try to execute the record
            try
            {
                // Determine if the message is valid
                if (!isValid)
                    throw new MessageValidationException(locale.GetString("MSGE00A"), receivedMessage.Structure);

                // Construct the canonical data structure
                GetIdentifiersQueryResponseFactory fact = new GetIdentifiersQueryResponseFactory() { Context = this.Context };
                RegistryQueryRequest filter = fact.CreateFilterData(request, dtls);

                if (filter.QueryRequest == null)
                    throw new MessageValidationException(locale.GetString("MSGE00A"), receivedMessage.Structure);
                
                // Query
                var results = dataSvc.Query(filter);
                dtls.AddRange(results.Details);

                // Prepare for audit
                audit = dataUtil.CreateAuditData("ITI-45",
                    ActionType.Execute,
                    dtls.Exists(r => r.Type == ResultDetailType.Error) ? OutcomeIndicator.MinorFail : OutcomeIndicator.Success,
                    e,
                    receivedMessage,
                    results,
                    filter.QueryRequest.FindComponent(SVC.Core.ComponentModel.HealthServiceRecordSiteRoleType.AuthorOf) as HealthcareParticipant
                );


                response = fact.Create(request, results, dtls) as PRPA_IN201310UV02;
            }
            catch (Exception ex)
            {
                Trace.TraceError(ex.ToString());

                // Prepare for audit
                audit = dataUtil.CreateAuditData("ITI-45", ActionType.Execute, OutcomeIndicator.EpicFail, e, receivedMessage,
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
            return response;
        }

        /// <summary>
        /// Handle duplicates resolved message
        /// </summary>
        private IGraphable HandlePatientRegistryDuplicatesResolved(MARC.Everest.Connectors.UnsolicitedDataEventArgs e, MARC.Everest.Connectors.IReceiveResult receivedMessage)
        {
            // Setup the lists of details and issues
            List<IResultDetail> dtls = new List<IResultDetail>(receivedMessage.Details);
            
            // System configuration service
            ISystemConfigurationService configService = Context.GetService(typeof(ISystemConfigurationService)) as ISystemConfigurationService;

            // Localization service
            ILocalizationService locale = Context.GetService(typeof(ILocalizationService)) as ILocalizationService;

            // Data Service
            IClientRegistryDataService dataSvc = Context.GetService(typeof(IClientRegistryDataService)) as IClientRegistryDataService;

            // Do basic check and add common validation errors
            MessageUtil.ValidateTransportWrapperUv(receivedMessage.Structure as IInteraction, configService, dtls);

            // Check the request is valid
            var request = receivedMessage.Structure as PRPA_IN201304UV02;
            if (request == null)
                return null;

            // Determine if the received message was interpreted properly
            bool isValid = MessageUtil.IsValid(receivedMessage);

            // set the URI
            if(request.controlActProcess != null)
                request.controlActProcess.Code = request.controlActProcess.Code ?? Util.Convert<CD<String>>(PRPA_IN201302UV02.GetTriggerEvent());
            if (request.Receiver.Count > 0)
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
            List<AuditData> audits = new List<AuditData>();

            IheAuditUtil dataUtil = new IheAuditUtil() { Context = this.Context };

            // Try to execute the record
            try
            {
                // Determine if the message is valid
                if (!isValid)
                    throw new MessageValidationException(locale.GetString("MSGE00A"), receivedMessage.Structure);

                // Construct the canonical data structure
                UvComponentUtil cu = new UvComponentUtil() { Context = this.Context };
                RegistrationEvent data = cu.CreateComponents(request.controlActProcess, dtls);

                // Componentization fail?
                if (data == null || !dataUtil.ValidateIdentifiers(data, dtls))
                    throw new MessageValidationException(locale.GetString("MSGE00A"), receivedMessage.Structure);

                // Store 
                var result = dataSvc.Merge(data, request.ProcessingCode != ProcessingID.Production ? DataPersistenceMode.Debugging : DataPersistenceMode.Production);

                if(result == null || result.VersionId == null)
                    throw new Exception(locale.GetString("DTPE001"));

                dtls.AddRange(result.Details);
               
                // prepare the delete audit
                var person = data.FindComponent(SVC.Core.ComponentModel.HealthServiceRecordSiteRoleType.SubjectOf) as Person;
                var replc = person.FindAllComponents(SVC.Core.ComponentModel.HealthServiceRecordSiteRoleType.ReplacementOf);

                foreach (PersonRegistrationRef rplc in replc)
                    audits.Add(dataUtil.CreateAuditData("ITI-44",
                        ActionType.Delete,
                        dtls.Exists(r => r.Type == ResultDetailType.Error) ? OutcomeIndicator.MinorFail : OutcomeIndicator.Success,
                        e,
                        receivedMessage,
                        new List<VersionedDomainIdentifier>() {
                            new VersionedDomainIdentifier()
                            {
                                Domain = rplc.AlternateIdentifiers[0].Domain,
                                Identifier = rplc.AlternateIdentifiers[0].Identifier
                            }
                        },
                        data.FindComponent(SVC.Core.ComponentModel.HealthServiceRecordSiteRoleType.AuthorOf) as HealthcareParticipant));
            

                // Prepare for audit
                audits.Add(dataUtil.CreateAuditData("ITI-44",
                    ActionType.Update,
                    dtls.Exists(r => r.Type == ResultDetailType.Error) ? OutcomeIndicator.MinorFail : OutcomeIndicator.Success,
                    e,
                    receivedMessage,
                    new List<VersionedDomainIdentifier>() { result.VersionId },
                    data.FindComponent(SVC.Core.ComponentModel.HealthServiceRecordSiteRoleType.AuthorOf) as HealthcareParticipant
                ));

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
                audits.Add(dataUtil.CreateAuditData("ITI-44", ActionType.Create, OutcomeIndicator.EpicFail, e, receivedMessage,
                    new List<VersionedDomainIdentifier>(),
                    null
                ));

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
                    foreach(var aud in audits)
                        auditService.SendAudit(aud);
            }

            // Common response parameters
            response.ProfileId = new SET<II>(MCCI_IN000002UV01.GetProfileId());
            response.VersionCode = HL7StandardVersionCode.Version3_Prerelease1;
            response.AcceptAckCode = AcknowledgementCondition.Never;
            response.Acknowledgement[0].AcknowledgementDetail.AddRange(MessageUtil.CreateAckDetailsUv(dtls.ToArray()));
            return response;
        }

        /// <summary>
        /// Handles PRPA_IN201302UV02 ITI-44
        /// </summary>
        private IGraphable HandlePatientRegistryRecordRevised(MARC.Everest.Connectors.UnsolicitedDataEventArgs e, MARC.Everest.Connectors.IReceiveResult receivedMessage)
        {
            // Setup the lists of details and issues
            List<IResultDetail> dtls = new List<IResultDetail>(receivedMessage.Details);

            // System configuration service
            ISystemConfigurationService configService = Context.GetService(typeof(ISystemConfigurationService)) as ISystemConfigurationService;

            // Localization service
            ILocalizationService locale = Context.GetService(typeof(ILocalizationService)) as ILocalizationService;

            // Data Service
            IClientRegistryDataService dataSvc = Context.GetService(typeof(IClientRegistryDataService)) as IClientRegistryDataService;

            // Do basic check and add common validation errors
            MessageUtil.ValidateTransportWrapperUv(receivedMessage.Structure as IInteraction, configService, dtls);

            // Check the request is valid
            var request = receivedMessage.Structure as PRPA_IN201302UV02;
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
            IheAuditUtil dataUtil = new IheAuditUtil() { Context = this.Context };

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
                if (data == null || !dataUtil.ValidateIdentifiers(data, dtls))
                    throw new MessageValidationException(locale.GetString("MSGE00A"), receivedMessage.Structure);

                // Store 
                var result = dataSvc.Update(data, request.ProcessingCode != ProcessingID.Production ? DataPersistenceMode.Debugging : DataPersistenceMode.Production);

                if (result == null || result.VersionId == null)
                    throw new Exception(locale.GetString("DTPE001"));
                
                dtls.AddRange(result.Details);

                // Prepare for audit
                audit = dataUtil.CreateAuditData("ITI-44",
                    result.VersionId.UpdateMode == UpdateModeType.Update ? ActionType.Update : ActionType.Create,
                    dtls.Exists(r => r.Type == ResultDetailType.Error) ? OutcomeIndicator.MinorFail : OutcomeIndicator.Success,
                    e,
                    receivedMessage,
                    new List<VersionedDomainIdentifier>() { result.VersionId },
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
                audit = dataUtil.CreateAuditData("ITI-44", ActionType.Update, OutcomeIndicator.EpicFail, e, receivedMessage,
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
            return response;


        }

        /// <summary>
        /// Handles PRPA_IN201301UV02 ITI-44
        /// </summary>
        private IGraphable HandlePatientRegistryRecordAdded(MARC.Everest.Connectors.UnsolicitedDataEventArgs e, MARC.Everest.Connectors.IReceiveResult receivedMessage)
        {

            // Setup the lists of details and issues
            List<IResultDetail> dtls = new List<IResultDetail>(receivedMessage.Details);

            // System configuration service
            ISystemConfigurationService configService = Context.GetService(typeof(ISystemConfigurationService)) as ISystemConfigurationService;

            // Localization service
            ILocalizationService locale = Context.GetService(typeof(ILocalizationService)) as ILocalizationService;

            // Data Service
            IClientRegistryDataService dataSvc = Context.GetService(typeof(IClientRegistryDataService)) as IClientRegistryDataService;

            // Do basic check and add common validation errors
            MessageUtil.ValidateTransportWrapperUv(receivedMessage.Structure as IInteraction, configService, dtls);
            
            // Check the request is valid
            var request = receivedMessage.Structure as PRPA_IN201301UV02;
            if (request == null)
                return null;
                        
            // Determine if the received message was interpreted properly
            bool isValid = MessageUtil.IsValid(receivedMessage);

            // set the URI
            if (request.controlActProcess != null)
                request.controlActProcess.Code = request.controlActProcess.Code ?? Util.Convert<CD<String>>(PRPA_IN201301UV02.GetTriggerEvent());
            if (request.Receiver.Count > 0)
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
            IheAuditUtil dataUtil = new IheAuditUtil() { Context = this.Context };

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
                if (data == null || !dataUtil.ValidateIdentifiers(data, dtls))
                    throw new MessageValidationException(locale.GetString("MSGE00A"), receivedMessage.Structure);


                // Store 
                var result = dataSvc.Register(data, request.ProcessingCode != ProcessingID.Production ? DataPersistenceMode.Debugging : DataPersistenceMode.Production);


                if (result == null || result.VersionId == null)
                    throw new Exception(locale.GetString("DTPE001"));

                dtls.AddRange(result.Details);

              
                // Prepare for audit
                audit = dataUtil.CreateAuditData("ITI-44",
                    result.VersionId.UpdateMode == UpdateModeType.Update ? ActionType.Update : ActionType.Create,
                    dtls.Exists(r => r.Type == ResultDetailType.Error) ? OutcomeIndicator.MinorFail : OutcomeIndicator.Success,
                    e,
                    receivedMessage,
                    new List<VersionedDomainIdentifier>() { result.VersionId },
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
