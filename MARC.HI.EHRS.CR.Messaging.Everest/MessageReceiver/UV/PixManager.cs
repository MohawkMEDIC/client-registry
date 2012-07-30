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
            return new MCCI_IN000002UV01(); 
            // NB: ID must be root / ext pair
            // NB: VersionCode = V3PR1
            // NB: Processing code is P or D depending on what is happening
            // NB: Processing mode is T
            // NB: Accept ack is NE
            // NB: Device does not have an extension
            // NB: Response is CR , CE or CA


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
            request.Receiver[0].Telecom = e.ReceiveEndpoint.ToString();

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


            try
            {
                // Determine if the message is valid
                if (!isValid)
                    throw new MessageValidationException(locale.GetString("MSGE00A"), receivedMessage.Structure);
              
                // Add ack
                response.Acknowledgement.Add(new MARC.Everest.RMIM.UV.NE2008.MCCI_MT100200UV01.Acknowledgement(
                    AcknowledgementType.AcceptAcknowledgementCommitAccept,
                    new MARC.Everest.RMIM.UV.NE2008.MCCI_MT100200UV01.TargetMessage(request.Id)
                ));
            }
            catch (Exception ex)
            {
                Trace.TraceError(ex.ToString());
                dtls.Add(new ResultDetail(ResultDetailType.Error, ex.Message, ex));
                response.Acknowledgement.Add(new MARC.Everest.RMIM.UV.NE2008.MCCI_MT100200UV01.Acknowledgement(
                    AcknowledgementType.AcceptAcknowledgementCommitError,
                    new MARC.Everest.RMIM.UV.NE2008.MCCI_MT100200UV01.TargetMessage(request.Id)
                ));
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