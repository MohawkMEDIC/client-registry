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
using MARC.Everest.Connectors;
using MARC.HI.EHRS.SVC.Core.Issues;
using MARC.Everest.Exceptions;
using System.Security;
using MARC.HI.EHRS.SVC.Core.DataTypes;
using MARC.Everest.RMIM.UV.NE2008.Vocabulary;
using MARC.Everest.RMIM.UV.NE2008.MCCI_MT100200UV01;
using MARC.HI.EHRS.CR.Core.Services;
using MARC.Everest.Formatters.XML.ITS1;

namespace MARC.HI.EHRS.CR.Messaging.Everest.MessageReceiver.UV
{
    public class QueryManager : IEverestMessageReceiver
    {
        #region IEverestMessageReceiver Members

        public MARC.Everest.Interfaces.IGraphable HandleMessageReceived(object sender, MARC.Everest.Connectors.UnsolicitedDataEventArgs e, MARC.Everest.Connectors.IReceiveResult receivedMessage)
        {
            IGraphable response = null;
            if (receivedMessage.Structure is QUQI_IN000003UV01)
                response = ProcessQueryContinuation(sender, e, receivedMessage);

            // Last ditch effort to create a response
            if (response == null)
                response = new NotSupportedMessageReceiver() { Context = this.Context }.HandleMessageReceived(sender, e, receivedMessage);

            // return the response
            return response;
        }

        /// <summary>
        /// Process a query continuation
        /// </summary>
        private IGraphable ProcessQueryContinuation(object sender, MARC.Everest.Connectors.UnsolicitedDataEventArgs e, MARC.Everest.Connectors.IReceiveResult receivedMessage)
        {
            // GEt the core services needed for this operation
            IAuditorService auditService = Context.GetService(typeof(IAuditorService)) as IAuditorService;
            ISystemConfigurationService configService = Context.GetService(typeof(ISystemConfigurationService)) as ISystemConfigurationService;
            IMessagePersistenceService msgPersistenceService = Context.GetService(typeof(IMessagePersistenceService)) as IMessagePersistenceService;
            IClientRegistryDataService dataSvc = Context.GetService(typeof(IClientRegistryDataService)) as IClientRegistryDataService;

            List<IResultDetail> dtls = new List<IResultDetail>(receivedMessage.Details);
            List<DetectedIssue> issues = new List<DetectedIssue>(10);

            // Validate transport wrapper
            MessageUtil.ValidateTransportWrapper(receivedMessage.Structure as IInteraction, configService, dtls);

            // Check that the request can be processed
            QUQI_IN000003UV01 request = receivedMessage.Structure as QUQI_IN000003UV01;
            if (request == null)
                return null;

            // Determine if the message structure was interpreted properly
            bool isValid = MessageUtil.IsValid(receivedMessage);

            // Try to process
            try
            {

                // set the URI
                if (request.Receiver.Count == 0)
                    request.Receiver.Add(new MARC.Everest.RMIM.UV.NE2008.MCCI_MT100200UV01.Receiver());
                request.Receiver[0].Telecom = e.ReceiveEndpoint.ToString();

                if (!isValid)
                    throw new MessageValidationException("Cannot process invalid message", request);
                else if (msgPersistenceService == null)
                    throw new InvalidOperationException("Cannot perform query continuation on v3 messages without Message persistence turned on");

                string queryId = String.Format("{1}^^^&{0}&ISO", request.controlActProcess.QueryContinuation.QueryId.Root, request.controlActProcess.QueryContinuation.QueryId.Extension);

                RegistryQueryRequest queryData = new RegistryQueryRequest()
                {
                    QueryId = String.Format("{1}^^^&{0}&ISO", request.controlActProcess.QueryContinuation.QueryId.Root, request.controlActProcess.QueryContinuation.QueryId.Extension),
                    Originator = String.Format("{1}^^^&{0}&ISO", request.Sender.Device.Id.First.Root, request.Sender.Device.Id.First.Extension),
                    Offset = (int)request.controlActProcess.QueryContinuation.StartResultNumber,
                    Limit = (int)request.controlActProcess.QueryContinuation.ContinuationQuantity,
                    IsContinue = true,
                    IsSummary = true
                };

                var result = dataSvc.Query(queryData);
                dtls.AddRange(result.Details);

                // Original request
                using (XmlIts1Formatter fmtr = new XmlIts1Formatter() { ValidateConformance = false })
                {
                    fmtr.GraphAides.Add(new MARC.Everest.Formatters.XML.Datatypes.R1.Formatter() { CompatibilityMode = MARC.Everest.Formatters.XML.Datatypes.R1.DatatypeFormatterCompatibilityMode.Universal });
                    fmtr.Settings = MARC.Everest.Formatters.XML.ITS1.SettingsType.DefaultMultiprocessor;

                    var originalRequest = fmtr.Parse(msgPersistenceService.GetMessage(result.OriginalRequestId));

                    if (originalRequest.Structure == null)
                        throw new InvalidOperationException("Cannot deserialize the original request");
                    // Ensure we can even create the required response type
                    IQueryResponseFactory responseFactory = QueryResponseFactoryUtil.GetResponseFactory(originalRequest.Structure.GetType());
                    if (responseFactory == null)
                        throw new NotImplementedException("Cannot determine how to respond to this interaction");
                    responseFactory.Context = this.Context;


                    return responseFactory.Create(
                        originalRequest.Structure as IInteraction,
                        result, dtls
                    );

                }

            }
            catch (Exception ex)
            {
                dtls.Add(new ResultDetail(ResultDetailType.Error, ex.Message, ex.StackTrace, ex));
                var nackResponse = new MCCI_IN000002UV01(
                    Guid.NewGuid(),
                    DateTime.Now,
                    MCCI_IN000002UV01.GetInteractionId(),
                    ProcessingID.Production,
                    "T",
                    MessageUtil.CreateReceiver(request.Sender),
                    MessageUtil.CreateSenderUv(e.ReceiveEndpoint, configService)
                )
                {
                    Acknowledgement = new List<MARC.Everest.RMIM.UV.NE2008.MCCI_MT100200UV01.Acknowledgement>() {
                        new Acknowledgement(
                            AcknowledgementType.ApplicationAcknowledgementError,
                            new TargetMessage(request.Id)
                        )
                    }
                };
                nackResponse.Acknowledgement[0].AcknowledgementDetail.AddRange(MessageUtil.CreateAckDetailsUv(dtls.ToArray()));
                return nackResponse;
            } // catch

                    
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
