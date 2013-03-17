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
 * Date: 4-9-2012
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MARC.HI.EHRS.SVC.Messaging.Everest;
using MARC.Everest.Interfaces;
using MARC.Everest.RMIM.CA.R020402.Interactions;
using MARC.HI.EHRS.SVC.Core.Issues;
using MARC.Everest.Connectors;
using System.Diagnostics;
using MARC.Everest.Exceptions;
using MARC.HI.EHRS.SVC.Core.Services;
using MARC.Everest.RMIM.CA.R020402.Vocabulary;
using MARC.Everest.DataTypes;
using MARC.HI.EHRS.SVC.Core.DataTypes;
using MARC.HI.EHRS.SVC.Core.ComponentModel;
using MARC.HI.EHRS.SVC.Core.ComponentModel.Components;
using MARC.HI.EHRS.CR.Core.ComponentModel;
using MARC.Everest.RMIM.CA.R020402.QUQI_MT120008CA;
using MARC.HI.EHRS.CR.Core.Services;

namespace MARC.HI.EHRS.CR.Messaging.Everest.MessageReceiver.CA
{
    /// <summary>
    /// Client registry message receiver
    /// </summary>
    public class ClientRegistryMessageReceiver : IEverestMessageReceiver
    {
        #region IEverestMessageReceiver Members

        /// <summary>
        /// Handle a message that has been received 
        /// </summary>
        public MARC.Everest.Interfaces.IGraphable HandleMessageReceived(object sender, MARC.Everest.Connectors.UnsolicitedDataEventArgs e, MARC.Everest.Connectors.IReceiveResult receivedMessage)
        {
            IGraphable response = null;


            if (receivedMessage.Structure is PRPA_IN101103CA)
                response = HandleFindCandidates(e, receivedMessage);
            else if (receivedMessage.Structure is PRPA_IN101101CA)
                response = HandleGetCandidates(e, receivedMessage);
            else if (receivedMessage.Structure is PRPA_IN101105CA)
                response = HandleGetAlternateIdentifiers(e, receivedMessage);
            else if (receivedMessage.Structure is PRPA_IN101204CA)
                response = HandleRevisePatient(e, receivedMessage);
            else if (receivedMessage.Structure is PRPA_IN101201CA)
                response = HandlePutPatient(e, receivedMessage);

            if (response == null)
                response = new NotSupportedMessageReceiver() { Context = this.Context }.HandleMessageReceived(sender, e, receivedMessage);
            return response;
        }

        /// <summary>
        /// Handle the get candidates message
        /// </summary>
        private IGraphable HandleGetCandidates(UnsolicitedDataEventArgs e, IReceiveResult receivedMessage)
        {
            // Get the core services
            ISystemConfigurationService configService = Context.GetService(typeof(ISystemConfigurationService)) as ISystemConfigurationService;
            List<IResultDetail> dtls = new List<IResultDetail>(receivedMessage.Details);
            List<DetectedIssue> issues = new List<DetectedIssue>(10);

            // Localization service
            ILocalizationService locale = Context.GetService(typeof(ILocalizationService)) as ILocalizationService;


            // Do a basic check and add common validation errors
            MessageUtil.ValidateTransportWrapper(receivedMessage.Structure as IInteraction, configService, dtls);

            // Create the request message
            PRPA_IN101101CA request = receivedMessage.Structure as PRPA_IN101101CA;

            if (request == null)
                return null;

            bool isValid = MessageUtil.IsValid(receivedMessage);

            AuditData audit = null;
            try
            {
                if (!isValid)
                    throw new MessageValidationException(locale.GetString("MSGE00A"), receivedMessage.Structure);

                // set the URI
                request.Receiver.Telecom = e.ReceiveEndpoint.ToString();

                // Create the prototype query structure
                DataUtil dataUtil = new DataUtil() { Context = this.Context };

                // Use the data utility to query for our discharge
                GetCandidatesQueryResponseFactory fact = new GetCandidatesQueryResponseFactory() { Context = this.Context };
                var filter = fact.CreateFilterData(request, dtls);

                if (filter.QueryRequest == null)
                    throw new MessageValidationException(locale.GetString("MSGE00A"), receivedMessage.Structure);

                var results = dataUtil.Query(filter, dtls, issues);

                // Audit
                audit = new MARC.HI.EHRS.CR.Messaging.Everest.MessageReceiver.UV.IheDataUtil() { Context = this.Context }.CreateAuditData(request.controlActEvent.Code.Code,
                    ActionType.Read,
                    OutcomeIndicator.Success,
                    e,
                    receivedMessage,
                    results,
                    filter.QueryRequest.FindComponent(HealthServiceRecordSiteRoleType.AuthorOf) as HealthcareParticipant
                );


                return fact.Create(request, results, dtls, issues);

            }
            catch (Exception ex)
            {
                dtls.Add(new ResultDetail(ResultDetailType.Error, ex.Message, ex.StackTrace, ex));

                // Prepare for audit
                audit = new MARC.HI.EHRS.CR.Messaging.Everest.MessageReceiver.UV.IheDataUtil() { Context = this.Context }.CreateAuditData(request.controlActEvent.Code.Code, ActionType.Read, OutcomeIndicator.EpicFail, e, receivedMessage,
                    new List<VersionedDomainIdentifier>(),
                    null
                );
            }
            finally
            {
                IAuditorService auditService = Context.GetService(typeof(IAuditorService)) as IAuditorService;
                // Audit the event
                if (auditService != null)
                    auditService.SendAudit(audit);

            }

            PRPA_IN101102CA nackResponse = new PRPA_IN101102CA(
                Guid.NewGuid(),
                DateTime.Now,
                ResponseMode.Immediate,
                PRPA_IN101102CA.GetInteractionId(),
                PRPA_IN101102CA.GetProfileId(),
                ProcessingID.Production,
                AcknowledgementCondition.Never,
                MessageUtil.CreateReceiver(request.Sender),
                MessageUtil.CreateSender(e.ReceiveEndpoint, configService),
                new MARC.Everest.RMIM.CA.R020402.MCCI_MT002200CA.Acknowledgement(
                    AcknowledgementType.ApplicationAcknowledgementError,
                    new MARC.Everest.RMIM.CA.R020402.MCCI_MT002200CA.TargetMessage(
                        request.Id)
                )
            );
            nackResponse.Acknowledgement.AcknowledgementDetail = MessageUtil.CreateAckDetails(dtls.ToArray());
            nackResponse.controlActEvent = PRPA_IN101102CA.CreateControlActEvent(
                new II(configService.Custodianship.Id.Domain, Guid.NewGuid().ToString()),
                PRPA_IN101102CA.GetTriggerEvent(),
                new QueryAck(
                    request.controlActEvent.QueryByParameter.QueryId,
                    QueryResponse.ApplicationError,
                    0, 0, 0),
                request.controlActEvent.QueryByParameter
            );
            nackResponse.controlActEvent.LanguageCode = MessageUtil.GetDefaultLanguageCode(this.Context);
            if (issues.Count > 0)
                nackResponse.controlActEvent.SubjectOf.AddRange(MessageUtil.CreateDetectedIssueEventsQuery(issues));
            return nackResponse;
        }

        /// <summary>
        /// Handle the put patient message
        /// </summary>
        private IGraphable HandlePutPatient(MARC.Everest.Connectors.UnsolicitedDataEventArgs e, MARC.Everest.Connectors.IReceiveResult receivedMessage)
        {
            // Setup the lists of details and issues
            List<IResultDetail> dtls = new List<IResultDetail>(receivedMessage.Details);
            List<DetectedIssue> issues = new List<DetectedIssue>(); 

            // System configuration service
            ISystemConfigurationService configService = Context.GetService(typeof(ISystemConfigurationService)) as ISystemConfigurationService;

            // Localization service
            ILocalizationService locale = Context.GetService(typeof(ILocalizationService)) as ILocalizationService;

            // Do basic check and add common validation errors
            MessageUtil.ValidateTransportWrapper(receivedMessage.Structure as IInteraction, configService, dtls);

            // Check the request is valid
            PRPA_IN101201CA request = receivedMessage.Structure as PRPA_IN101201CA;
            if (request == null)
                return null;

            // Determine if the received message was interpreted properly
            bool isValid = MessageUtil.IsValid(receivedMessage);

            try
            {

                if (!isValid)
                    throw new MessageValidationException(locale.GetString("MSGE00A"), receivedMessage.Structure);

                // set the URI
                request.Receiver.Telecom = e.ReceiveEndpoint.ToString();

                // Componentize the message into the data model
                CaComponentUtil compUtil = new CaComponentUtil();
                DeComponentUtil deCompUtil = new DeComponentUtil();
                compUtil.Context = deCompUtil.Context = this.Context;
                RegistrationEvent components = compUtil.CreateComponents(request.controlActEvent, dtls);

                // Componentization fail?
                if(components == null)
                    throw new MessageValidationException(locale.GetString("MSGE00A"), receivedMessage.Structure);

                // Store the message into the data persistence 
                DataUtil dataUtil = new DataUtil() { Context = this.Context };
                VersionedDomainIdentifier vdi = dataUtil.Register(components, dtls, issues, request.ProcessingCode == ProcessingID.Debugging ? DataPersistenceMode.Debugging : DataPersistenceMode.Production);

                // Find the CACT record
                var cact = components.FindComponent(HealthServiceRecordSiteRoleType.ReasonFor | HealthServiceRecordSiteRoleType.OlderVersionOf) as ChangeSummary;

                if (vdi != null)
                {

                    
                    // Registration ID
                    var regReq = request.controlActEvent.Subject.RegistrationRequest.Subject.registeredRole;
                    if(regReq.Id == null)
                        regReq.Id = new SET<II>(II.Comparator);
                    regReq.Id.Add(new II(configService.OidRegistrar.GetOid("CR_CID").Oid, (components.FindComponent(HealthServiceRecordSiteRoleType.SubjectOf) as Person).Id.ToString()));

                    // Create the response
                    PRPA_IN101202CA response = new PRPA_IN101202CA
                    (
                        Guid.NewGuid(),
                        DateTime.Now,
                        ResponseMode.Immediate,
                        PRPA_IN101202CA.GetInteractionId(),
                        PRPA_IN101202CA.GetProfileId(),
                        ProcessingID.Production,
                        AcknowledgementCondition.Never,
                        MessageUtil.CreateReceiver(request.Sender),
                        MessageUtil.CreateSender(e.ReceiveEndpoint, configService),
                        new MARC.Everest.RMIM.CA.R020402.MCCI_MT002200CA.Acknowledgement(
                            AcknowledgementType.AcceptAcknowledgementCommitAccept,
                            new MARC.Everest.RMIM.CA.R020402.MCCI_MT002200CA.TargetMessage(request.Id)
                        )
                    );
                    response.Acknowledgement.AcknowledgementDetail = MessageUtil.CreateAckDetails(dtls.ToArray());

                    // Control act event
                    response.controlActEvent = PRPA_IN101202CA.CreateControlActEvent(
                        new II(cact.AlternateIdentifier.Domain, cact.AlternateIdentifier.Identifier),
                        PRPA_IN101202CA.GetTriggerEvent(),
                        new IVL<TS>(DateTime.Now, new TS() { NullFlavor = NullFlavor.NotApplicable }),
                        null, 
                        MessageUtil.GetDefaultLanguageCode(this.Context),
                        new MARC.Everest.RMIM.CA.R020402.MFMI_MT700726CA.Subject2<MARC.Everest.RMIM.CA.R020402.PRPA_MT101106CA.IdentifiedEntity>(
                            true, 
                            new MARC.Everest.RMIM.CA.R020402.MFMI_MT700726CA.RegistrationEvent<MARC.Everest.RMIM.CA.R020402.PRPA_MT101106CA.IdentifiedEntity>(
                                new MARC.Everest.RMIM.CA.R020402.MFMI_MT700726CA.Subject4<MARC.Everest.RMIM.CA.R020402.PRPA_MT101106CA.IdentifiedEntity>(
                                    new MARC.Everest.RMIM.CA.R020402.PRPA_MT101106CA.IdentifiedEntity(
                                        regReq.Id,
                                        regReq.StatusCode,
                                        regReq.EffectiveTime,
                                        regReq.ConfidentialityCode,
                                        new MARC.Everest.RMIM.CA.R020402.PRPA_MT101106CA.Person(),
                                        new MARC.Everest.RMIM.CA.R020402.PRPA_MT101104CA.Subject() { NullFlavor = NullFlavor.NoInformation }
                                    )
                                ), 
                                request.controlActEvent.Subject.RegistrationRequest.Custodian,
                                null
                            )),
                            null
                        );
                    
                    // AsOtherIDs
                    if (regReq.IdentifiedPerson.AsOtherIDs.Count > 0)
                        foreach (var res in request.controlActEvent.Subject.RegistrationRequest.Subject.registeredRole.IdentifiedPerson.AsOtherIDs)
                            response.controlActEvent.Subject.RegistrationEvent.Subject.registeredRole.IdentifiedPerson.AsOtherIDs.Add(new MARC.Everest.RMIM.CA.R020402.PRPA_MT101106CA.OtherIDs(
                                res.Id,
                                res.Code,
                                new MARC.Everest.RMIM.CA.R020402.PRPA_MT101106CA.IdOrganization(
                                    res.AssigningIdOrganization.Id,
                                    res.AssigningIdOrganization.Name
                                )
                            ));
                    else
                        response.controlActEvent.Subject.RegistrationEvent.Subject.registeredRole.IdentifiedPerson.AsOtherIDs = new List<MARC.Everest.RMIM.CA.R020402.PRPA_MT101106CA.OtherIDs>()
                            {
                                new MARC.Everest.RMIM.CA.R020402.PRPA_MT101106CA.OtherIDs() { NullFlavor = NullFlavor.NoInformation }
                            };

                    // Response
                    response.controlActEvent.LanguageCode = request.controlActEvent.LanguageCode ?? MessageUtil.GetDefaultLanguageCode(this.Context);

                    // Any detected issues
                    if (issues.Count > 0)
                        response.controlActEvent.SubjectOf.AddRange(MessageUtil.CreateDetectedIssueEventsQuery(issues));
                    
                    return response;
                }
                else
                    throw new Exception(locale.GetString("DTPE001"));
 
            }
            catch (Exception ex)
            {
                Trace.TraceError(ex.ToString());
                dtls.Add(new ResultDetail(ResultDetailType.Error, ex.Message, ex));
            }

            PRPA_IN101203CA nackResponse = new PRPA_IN101203CA(
                Guid.NewGuid(),
                DateTime.Now,
                ResponseMode.Immediate,
                PRPA_IN101203CA.GetInteractionId(),
                PRPA_IN101203CA.GetProfileId(),
                ProcessingID.Production,
                AcknowledgementCondition.Never,
                MessageUtil.CreateReceiver(request.Sender),
                MessageUtil.CreateSender(e.ReceiveEndpoint, configService),
                new MARC.Everest.RMIM.CA.R020402.MCCI_MT002200CA.Acknowledgement(
                    AcknowledgementType.ApplicationAcknowledgementError,
                    new MARC.Everest.RMIM.CA.R020402.MCCI_MT002200CA.TargetMessage(request.Id)
                )
            );
            nackResponse.Acknowledgement.AcknowledgementDetail = MessageUtil.CreateAckDetails(dtls.ToArray());
            nackResponse.controlActEvent = PRPA_IN101203CA.CreateControlActEvent(
                new II(configService.Custodianship.Id.Domain, Guid.NewGuid().ToString()),
                PRPA_IN101203CA.GetTriggerEvent(),
                new MARC.Everest.RMIM.CA.R020402.MFMI_MT700726CA.Subject2<MARC.Everest.RMIM.CA.R020402.PRPA_MT101106CA.IdentifiedEntity>(
                    request.controlActEvent.Subject.ContextConductionInd,
                    new MARC.Everest.RMIM.CA.R020402.MFMI_MT700726CA.RegistrationEvent<MARC.Everest.RMIM.CA.R020402.PRPA_MT101106CA.IdentifiedEntity>(
                        new MARC.Everest.RMIM.CA.R020402.MFMI_MT700726CA.Subject4<MARC.Everest.RMIM.CA.R020402.PRPA_MT101106CA.IdentifiedEntity>(
                            new MARC.Everest.RMIM.CA.R020402.PRPA_MT101106CA.IdentifiedEntity(
                                request.controlActEvent.Subject.RegistrationRequest.Subject.registeredRole.Id,
                                request.controlActEvent.Subject.RegistrationRequest.Subject.registeredRole.StatusCode ?? new CS<RoleStatus>() { NullFlavor = NullFlavor.NoInformation },
                                request.controlActEvent.Subject.RegistrationRequest.Subject.registeredRole.EffectiveTime,
                                request.controlActEvent.Subject.RegistrationRequest.Subject.registeredRole.ConfidentialityCode ?? new CV<x_VeryBasicConfidentialityKind>() { NullFlavor = NullFlavor.NoInformation },
                                new MARC.Everest.RMIM.CA.R020402.PRPA_MT101106CA.Person(

                                )
                                {
                                    AsOtherIDs = new List<MARC.Everest.RMIM.CA.R020402.PRPA_MT101106CA.OtherIDs>()
                                },
                                new MARC.Everest.RMIM.CA.R020402.PRPA_MT101104CA.Subject() { NullFlavor = NullFlavor.NoInformation }
                            )
                        ),
                        request.controlActEvent.Subject.RegistrationRequest.Custodian
                    )
                )
            );

            // AsOtherIDs
            if (request.controlActEvent.Subject.RegistrationRequest.Subject.registeredRole.IdentifiedPerson.AsOtherIDs.Count > 0)
                foreach (var res in request.controlActEvent.Subject.RegistrationRequest.Subject.registeredRole.IdentifiedPerson.AsOtherIDs)
                    nackResponse.controlActEvent.Subject.RegistrationEvent.Subject.registeredRole.IdentifiedPerson.AsOtherIDs.Add(new MARC.Everest.RMIM.CA.R020402.PRPA_MT101106CA.OtherIDs(
                        res.Id,
                        res.Code,
                        new MARC.Everest.RMIM.CA.R020402.PRPA_MT101106CA.IdOrganization(
                            res.AssigningIdOrganization.Id,
                            res.AssigningIdOrganization.Name
                        )
                    ));
            else
                nackResponse.controlActEvent.Subject.RegistrationEvent.Subject.registeredRole.IdentifiedPerson.NullFlavor = NullFlavor.NoInformation;

            nackResponse.controlActEvent.LanguageCode = MessageUtil.GetDefaultLanguageCode(this.Context);
            if (issues.Count > 0)
                nackResponse.controlActEvent.SubjectOf.AddRange(MessageUtil.CreateDetectedIssueEventsQuery(issues));
            return nackResponse;


        }

        /// <summary>
        /// Handle the revise patient message
        /// </summary>
        private IGraphable HandleRevisePatient(MARC.Everest.Connectors.UnsolicitedDataEventArgs e, MARC.Everest.Connectors.IReceiveResult receivedMessage)
        {
            // Setup the lists of details and issues
            List<IResultDetail> dtls = new List<IResultDetail>(receivedMessage.Details);
            List<DetectedIssue> issues = new List<DetectedIssue>();

            // System configuration service
            ISystemConfigurationService configService = Context.GetService(typeof(ISystemConfigurationService)) as ISystemConfigurationService;

            // Localization service
            ILocalizationService locale = Context.GetService(typeof(ILocalizationService)) as ILocalizationService;

            // Do basic check and add common validation errors
            MessageUtil.ValidateTransportWrapper(receivedMessage.Structure as IInteraction, configService, dtls);

            // Check the request is valid
            PRPA_IN101204CA request = receivedMessage.Structure as PRPA_IN101204CA;
            if (request == null)
                return null;

            // Determine if the received message was interpreted properly
            bool isValid = MessageUtil.IsValid(receivedMessage);

            try
            {

                if (!isValid)
                    throw new MessageValidationException(locale.GetString("MSGE00A"), receivedMessage.Structure);

                // set the URI
                request.Receiver.Telecom = e.ReceiveEndpoint.ToString();

                // Componentize the message into the data model
                CaComponentUtil compUtil = new CaComponentUtil();
                CaDeComponentUtil deCompUtil = new CaDeComponentUtil();
                compUtil.Context = deCompUtil.Context = this.Context;
                RegistrationEvent components = compUtil.CreateComponents(request.controlActEvent, dtls);

                // Componentization fail?
                if (components == null)
                    throw new MessageValidationException(locale.GetString("MSGE00A"), receivedMessage.Structure);

                // Store the message into the data persistence 
                DataUtil dataUtil = new DataUtil() { Context = this.Context };
                VersionedDomainIdentifier vdi = dataUtil.Update(components, dtls, issues, request.ProcessingCode == ProcessingID.Debugging ? DataPersistenceMode.Debugging : DataPersistenceMode.Production);

                // Find the CACT record
                var cact = components.FindComponent(HealthServiceRecordSiteRoleType.ReasonFor | HealthServiceRecordSiteRoleType.OlderVersionOf) as ChangeSummary;

                if (vdi != null)
                {

                    // Registration Data for update
                    var verified = dataUtil.GetRecord(vdi, dtls, issues, new DataUtil.QueryData() { IsSummary = true, QueryId = Guid.NewGuid().ToString(), QueryRequest = components }) as RegistrationEvent;
                    var verifiedPerson = verified.FindComponent(HealthServiceRecordSiteRoleType.SubjectOf) as Person;

                    // Create the response
                    PRPA_IN101205CA response = new PRPA_IN101205CA
                    (
                        Guid.NewGuid(),
                        DateTime.Now,
                        ResponseMode.Immediate,
                        PRPA_IN101205CA.GetInteractionId(),
                        PRPA_IN101205CA.GetProfileId(),
                        ProcessingID.Production,
                        AcknowledgementCondition.Never,
                        MessageUtil.CreateReceiver(request.Sender),
                        MessageUtil.CreateSender(e.ReceiveEndpoint, configService),
                        new MARC.Everest.RMIM.CA.R020402.MCCI_MT002200CA.Acknowledgement(
                            AcknowledgementType.AcceptAcknowledgementCommitAccept,
                            new MARC.Everest.RMIM.CA.R020402.MCCI_MT002200CA.TargetMessage(request.Id)
                        )
                    );
                    response.Acknowledgement.AcknowledgementDetail = MessageUtil.CreateAckDetails(dtls.ToArray());

                    // Control act event
                    response.controlActEvent = PRPA_IN101205CA.CreateControlActEvent(
                        new II(cact.AlternateIdentifier.Domain, cact.AlternateIdentifier.Identifier),
                        PRPA_IN101202CA.GetTriggerEvent(),
                        new IVL<TS>(DateTime.Now, new TS() { NullFlavor = NullFlavor.NotApplicable }),
                        null,
                        MessageUtil.GetDefaultLanguageCode(this.Context),
                        new MARC.Everest.RMIM.CA.R020402.MFMI_MT700726CA.Subject2<MARC.Everest.RMIM.CA.R020402.PRPA_MT101102CA.IdentifiedEntity>(
                            true,
                            new MARC.Everest.RMIM.CA.R020402.MFMI_MT700726CA.RegistrationEvent<MARC.Everest.RMIM.CA.R020402.PRPA_MT101102CA.IdentifiedEntity>(
                                new MARC.Everest.RMIM.CA.R020402.MFMI_MT700726CA.Subject4<MARC.Everest.RMIM.CA.R020402.PRPA_MT101102CA.IdentifiedEntity>(
                                    deCompUtil.CreateIdentifiedEntity(verified, dtls)
                                ),
                                request.controlActEvent.Subject.RegistrationRequest.Custodian,
                                null
                            )),
                            null
                        );

                    
                    // Response
                    response.controlActEvent.LanguageCode = request.controlActEvent.LanguageCode ?? MessageUtil.GetDefaultLanguageCode(this.Context);

                    // Any detected issues
                    if (issues.Count > 0)
                        response.controlActEvent.SubjectOf.AddRange(MessageUtil.CreateDetectedIssueEventsQuery(issues));

                    return response;
                }
                else
                    throw new Exception(locale.GetString("DTPE001"));

            }
            catch (Exception ex)
            {
                Trace.TraceError(ex.ToString());
                dtls.Add(new ResultDetail(ResultDetailType.Error, ex.Message, ex));
            }

            PRPA_IN101206CA nackResponse = new PRPA_IN101206CA(
                Guid.NewGuid(),
                DateTime.Now,
                ResponseMode.Immediate,
                PRPA_IN101206CA.GetInteractionId(),
                PRPA_IN101206CA.GetProfileId(),
                ProcessingID.Production,
                AcknowledgementCondition.Never,
                MessageUtil.CreateReceiver(request.Sender),
                MessageUtil.CreateSender(e.ReceiveEndpoint, configService),
                new MARC.Everest.RMIM.CA.R020402.MCCI_MT002200CA.Acknowledgement(
                    AcknowledgementType.ApplicationAcknowledgementError,
                    new MARC.Everest.RMIM.CA.R020402.MCCI_MT002200CA.TargetMessage(request.Id)
                )
            );
            nackResponse.Acknowledgement.AcknowledgementDetail = MessageUtil.CreateAckDetails(dtls.ToArray());
            nackResponse.controlActEvent = PRPA_IN101206CA.CreateControlActEvent(
                new II(configService.Custodianship.Id.Domain, Guid.NewGuid().ToString()),
                PRPA_IN101206CA.GetTriggerEvent(),
                new MARC.Everest.RMIM.CA.R020402.MFMI_MT700726CA.Subject2<MARC.Everest.RMIM.CA.R020402.PRPA_MT101102CA.IdentifiedEntity>(
                    request.controlActEvent.Subject.ContextConductionInd,
                    new MARC.Everest.RMIM.CA.R020402.MFMI_MT700726CA.RegistrationEvent<MARC.Everest.RMIM.CA.R020402.PRPA_MT101102CA.IdentifiedEntity>(
                        new MARC.Everest.RMIM.CA.R020402.MFMI_MT700726CA.Subject4<MARC.Everest.RMIM.CA.R020402.PRPA_MT101102CA.IdentifiedEntity>(
                            new MARC.Everest.RMIM.CA.R020402.PRPA_MT101102CA.IdentifiedEntity(
                                request.controlActEvent.Subject.RegistrationRequest.Subject.registeredRole.Id,
                                request.controlActEvent.Subject.RegistrationRequest.Subject.registeredRole.StatusCode ?? new CS<RoleStatus>() { NullFlavor = NullFlavor.NoInformation },
                                request.controlActEvent.Subject.RegistrationRequest.Subject.registeredRole.EffectiveTime,
                                request.controlActEvent.Subject.RegistrationRequest.Subject.registeredRole.ConfidentialityCode ?? new CV<x_VeryBasicConfidentialityKind>() { NullFlavor = NullFlavor.NoInformation },
                                new MARC.Everest.RMIM.CA.R020402.PRPA_MT101102CA.Person(
                                    request.controlActEvent.Subject.RegistrationRequest.Subject.registeredRole.IdentifiedPerson.Name,
                                    request.controlActEvent.Subject.RegistrationRequest.Subject.registeredRole.IdentifiedPerson.Telecom,
                                    request.controlActEvent.Subject.RegistrationRequest.Subject.registeredRole.IdentifiedPerson.AdministrativeGenderCode,
                                    request.controlActEvent.Subject.RegistrationRequest.Subject.registeredRole.IdentifiedPerson.BirthTime,
                                    request.controlActEvent.Subject.RegistrationRequest.Subject.registeredRole.IdentifiedPerson.DeceasedInd,
                                    request.controlActEvent.Subject.RegistrationRequest.Subject.registeredRole.IdentifiedPerson.DeceasedTime,
                                    request.controlActEvent.Subject.RegistrationRequest.Subject.registeredRole.IdentifiedPerson.MultipleBirthInd,
                                    request.controlActEvent.Subject.RegistrationRequest.Subject.registeredRole.IdentifiedPerson.MultipleBirthOrderNumber,
                                    request.controlActEvent.Subject.RegistrationRequest.Subject.registeredRole.IdentifiedPerson.Addr,
                                    null,
                                    null,
                                    null
                                ),
                                new MARC.Everest.RMIM.CA.R020402.PRPA_MT101104CA.Subject() { NullFlavor = NullFlavor.NoInformation }
                            )
                        ),
                        request.controlActEvent.Subject.RegistrationRequest.Custodian
                    )
                )
            );

            // AsOtherIDs
            foreach(var oth in request.controlActEvent.Subject.RegistrationRequest.Subject.registeredRole.IdentifiedPerson.AsOtherIDs)
                nackResponse.controlActEvent.Subject.RegistrationEvent.Subject.registeredRole.IdentifiedPerson.AsOtherIDs.Add(
                    new MARC.Everest.RMIM.CA.R020402.PRPA_MT101102CA.OtherIDs(
                        oth.Id,
                        oth.Code,
                        oth.AssigningIdOrganization));
            nackResponse.controlActEvent.Subject.RegistrationEvent.Subject.registeredRole.IdentifiedPerson.PersonalRelationship = request.controlActEvent.Subject.RegistrationRequest.Subject.registeredRole.IdentifiedPerson.PersonalRelationship;
            nackResponse.controlActEvent.Subject.RegistrationEvent.Subject.registeredRole.IdentifiedPerson.LanguageCommunication = request.controlActEvent.Subject.RegistrationRequest.Subject.registeredRole.IdentifiedPerson.LanguageCommunication;

            nackResponse.controlActEvent.LanguageCode = MessageUtil.GetDefaultLanguageCode(this.Context);
            if (issues.Count > 0)
                nackResponse.controlActEvent.SubjectOf.AddRange(MessageUtil.CreateDetectedIssueEventsQuery(issues));
            return nackResponse;
        }

        /// <summary>
        /// Handle the get alternate identifier message
        /// </summary>
        private IGraphable HandleGetAlternateIdentifiers(MARC.Everest.Connectors.UnsolicitedDataEventArgs e, MARC.Everest.Connectors.IReceiveResult receivedMessage)
        {
            // Get the core services
            ISystemConfigurationService configService = Context.GetService(typeof(ISystemConfigurationService)) as ISystemConfigurationService;
            List<IResultDetail> dtls = new List<IResultDetail>(receivedMessage.Details);
            List<DetectedIssue> issues = new List<DetectedIssue>(10);

            // Localization service
            ILocalizationService locale = Context.GetService(typeof(ILocalizationService)) as ILocalizationService;


            // Do a basic check and add common validation errors
            MessageUtil.ValidateTransportWrapper(receivedMessage.Structure as IInteraction, configService, dtls);

            // Create the request message
            PRPA_IN101105CA request = receivedMessage.Structure as PRPA_IN101105CA;

            if (request == null)
                return null;

            bool isValid = MessageUtil.IsValid(receivedMessage);

            AuditData audit = null;
            try
            {
                if (!isValid)
                    throw new MessageValidationException(locale.GetString("MSGE00A"), receivedMessage.Structure);

                // set the URI
                request.Receiver.Telecom = e.ReceiveEndpoint.ToString();

                // Create the prototype query structure
                DataUtil dataUtil = new DataUtil() { Context = this.Context };

                // Use the data utility to query for our discharge
                GetCandidateAlternateIdentifiersQueryResponseFactory fact = new GetCandidateAlternateIdentifiersQueryResponseFactory() { Context = this.Context };
                var filter = fact.CreateFilterData(request, dtls);

                if (filter.QueryRequest == null)
                    throw new MessageValidationException(locale.GetString("MSGE00A"), receivedMessage.Structure);

                var results = dataUtil.Query(filter, dtls, issues);

                // Audit
                audit = new MARC.HI.EHRS.CR.Messaging.Everest.MessageReceiver.UV.IheDataUtil() { Context = this.Context }.CreateAuditData(request.controlActEvent.Code.Code,
                    ActionType.Read,
                    OutcomeIndicator.Success,
                    e,
                    receivedMessage,
                    results,
                    filter.QueryRequest.FindComponent(HealthServiceRecordSiteRoleType.AuthorOf) as HealthcareParticipant
                );


                return fact.Create(request, results, dtls, issues);

            }
            catch (Exception ex)
            {
                dtls.Add(new ResultDetail(ResultDetailType.Error, ex.Message, ex.StackTrace, ex));
               
                // Prepare for audit
                audit = new MARC.HI.EHRS.CR.Messaging.Everest.MessageReceiver.UV.IheDataUtil() { Context = this.Context }.CreateAuditData(request.controlActEvent.Code.Code, ActionType.Read, OutcomeIndicator.EpicFail, e, receivedMessage,
                    new List<VersionedDomainIdentifier>(),
                    null
                );
            }
            finally
            {
                IAuditorService auditService = Context.GetService(typeof(IAuditorService)) as IAuditorService;
                // Audit the event
                if (auditService != null)
                    auditService.SendAudit(audit);

            }

            PRPA_IN101106CA nackResponse = new PRPA_IN101106CA(
                Guid.NewGuid(),
                DateTime.Now,
                ResponseMode.Immediate,
                PRPA_IN101106CA.GetInteractionId(),
                PRPA_IN101106CA.GetProfileId(),
                ProcessingID.Production,
                AcknowledgementCondition.Never,
                MessageUtil.CreateReceiver(request.Sender),
                MessageUtil.CreateSender(e.ReceiveEndpoint, configService),
                new MARC.Everest.RMIM.CA.R020402.MCCI_MT002200CA.Acknowledgement(
                    AcknowledgementType.ApplicationAcknowledgementError,
                    new MARC.Everest.RMIM.CA.R020402.MCCI_MT002200CA.TargetMessage(
                        request.Id)
                )
            );
            nackResponse.Acknowledgement.AcknowledgementDetail = MessageUtil.CreateAckDetails(dtls.ToArray());
            nackResponse.controlActEvent = PRPA_IN101106CA.CreateControlActEvent(
                new II(configService.Custodianship.Id.Domain, Guid.NewGuid().ToString()),
                PRPA_IN101104CA.GetTriggerEvent(),
                new QueryAck(
                    request.controlActEvent.QueryByParameter.QueryId,
                    QueryResponse.ApplicationError,
                    0, 0, 0),
                request.controlActEvent.QueryByParameter
            );
            nackResponse.controlActEvent.LanguageCode = MessageUtil.GetDefaultLanguageCode(this.Context);
            if (issues.Count > 0)
                nackResponse.controlActEvent.SubjectOf.AddRange(MessageUtil.CreateDetectedIssueEventsQuery(issues));
            return nackResponse;

        }

        /// <summary>
        /// Handle the find candidates message
        /// </summary>
        private IGraphable HandleFindCandidates(MARC.Everest.Connectors.UnsolicitedDataEventArgs e, MARC.Everest.Connectors.IReceiveResult receivedMessage)
        {
            // Get the core services
            ISystemConfigurationService configService = Context.GetService(typeof(ISystemConfigurationService)) as ISystemConfigurationService;
            List<IResultDetail> dtls = new List<IResultDetail>(receivedMessage.Details);
            List<DetectedIssue> issues = new List<DetectedIssue>(10);

            // Localization service
            ILocalizationService locale = Context.GetService(typeof(ILocalizationService)) as ILocalizationService;


            // Do a basic check and add common validation errors
            MessageUtil.ValidateTransportWrapper(receivedMessage.Structure as IInteraction, configService, dtls);

            // Create the request message
            PRPA_IN101103CA request = receivedMessage.Structure as PRPA_IN101103CA;

            if (request == null)
                return null;

            bool isValid = MessageUtil.IsValid(receivedMessage);
            AuditData audit = null;
            try
            {
                if (!isValid)
                    throw new MessageValidationException(locale.GetString("MSGE00A"), receivedMessage.Structure);

                // set the URI
                request.Receiver.Telecom = e.ReceiveEndpoint.ToString();

                // Create the prototype query structure
                DataUtil dataUtil = new DataUtil() { Context = this.Context };

                // Use the data utility to query for our discharge
                FindCandidatesQueryResponseFactory fact = new FindCandidatesQueryResponseFactory() { Context = this.Context };
                var filter = fact.CreateFilterData(request, dtls);

                if (filter.QueryRequest == null)
                        throw new MessageValidationException(locale.GetString("MSGE00A"), receivedMessage.Structure);

                var results = dataUtil.Query(filter, dtls, issues);

                // Audit
                audit = new MARC.HI.EHRS.CR.Messaging.Everest.MessageReceiver.UV.IheDataUtil() { Context = this.Context }.CreateAuditData(request.controlActEvent.Code.Code,
                    ActionType.Read,
                    OutcomeIndicator.Success,
                    e,
                    receivedMessage,
                    results,
                    filter.QueryRequest.FindComponent(HealthServiceRecordSiteRoleType.AuthorOf) as HealthcareParticipant
                );

                return fact.Create(request, results, dtls, issues);


            }
            catch (Exception ex)
            {
                dtls.Add(new ResultDetail(ResultDetailType.Error, ex.Message, ex.StackTrace, ex));

                // Prepare for audit
                audit = new MARC.HI.EHRS.CR.Messaging.Everest.MessageReceiver.UV.IheDataUtil() { Context = this.Context }.CreateAuditData(request.controlActEvent.Code.Code, ActionType.Read, OutcomeIndicator.EpicFail, e, receivedMessage,
                    new List<VersionedDomainIdentifier>(),
                    null
                );
            }
            finally
            {
                IAuditorService auditService = Context.GetService(typeof(IAuditorService)) as IAuditorService;
                // Audit the event
                if (auditService != null)
                    auditService.SendAudit(audit);

            }


            PRPA_IN101104CA nackResponse = new PRPA_IN101104CA(
                Guid.NewGuid(),
                DateTime.Now,
                ResponseMode.Immediate,
                PRPA_IN101104CA.GetInteractionId(),
                PRPA_IN101104CA.GetProfileId(),
                ProcessingID.Production,
                AcknowledgementCondition.Never,
                MessageUtil.CreateReceiver(request.Sender),
                MessageUtil.CreateSender(e.ReceiveEndpoint, configService),
                new MARC.Everest.RMIM.CA.R020402.MCCI_MT002200CA.Acknowledgement(
                    AcknowledgementType.ApplicationAcknowledgementError,
                    new MARC.Everest.RMIM.CA.R020402.MCCI_MT002200CA.TargetMessage(
                        request.Id)
                )
            );
            nackResponse.Acknowledgement.AcknowledgementDetail = MessageUtil.CreateAckDetails(dtls.ToArray());
            nackResponse.controlActEvent = PRPA_IN101104CA.CreateControlActEvent(
                new II(configService.Custodianship.Id.Domain, Guid.NewGuid().ToString()),
                PRPA_IN101104CA.GetTriggerEvent(),
                new QueryAck(
                    request.controlActEvent.QueryByParameter.QueryId,
                    QueryResponse.ApplicationError,
                    0, 0, 0),
                request.controlActEvent.QueryByParameter
            );
            nackResponse.controlActEvent.LanguageCode = MessageUtil.GetDefaultLanguageCode(this.Context);
            if (issues.Count > 0)
                nackResponse.controlActEvent.SubjectOf.AddRange(MessageUtil.CreateDetectedIssueEventsQuery(issues));
            return nackResponse;


        }

        #endregion

        #region IUsesHostContext Members

        /// <summary>
        /// Gets or sets the context of the event
        /// </summary>
        public IServiceProvider Context
        {
            get;
            set;
        }

        #endregion

        #region ICloneable Members

        /// <summary>
        /// Clone this object
        /// </summary>
        public object Clone()
        {
            return this.MemberwiseClone();
        }

        #endregion
    }
}

