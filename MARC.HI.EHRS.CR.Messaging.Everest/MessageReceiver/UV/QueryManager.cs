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
 * Date: 20-7-2012
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
            IQueryPersistenceService queryService = Context.GetService(typeof(IQueryPersistenceService)) as IQueryPersistenceService;
            IMessagePersistenceService msgPersistenceService = Context.GetService(typeof(IMessagePersistenceService)) as IMessagePersistenceService;

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
                DataUtil datUtil = new DataUtil();

                // set the URI
                if (request.Receiver.Count == 0)
                    request.Receiver.Add(new MARC.Everest.RMIM.UV.NE2008.MCCI_MT100200UV01.Receiver());
                request.Receiver[0].Telecom = e.ReceiveEndpoint.ToString();

                if (queryService == null)
                    throw new InvalidOperationException("No query persistence service is registered with this service");
                else if (msgPersistenceService == null)
                    throw new InvalidOperationException("No message persistence service is registered with this service");
                else if (!isValid)
                    throw new MessageValidationException("Cannot process invalid message", request);

                string queryId = String.Format("{1}^^^&{0}&ISO", request.controlActProcess.QueryContinuation.QueryId.Root, request.controlActProcess.QueryContinuation.QueryId.Extension);

                // Determine if we can process the message
                if (!queryService.IsRegistered(queryId.ToLower()))
                {
                    dtls.Add(new PersistenceResultDetail(ResultDetailType.Error,
                        String.Format("The query '{0}' has not been registered with the query service", queryId),
                        null));
                    throw new ArgumentException("Cannot continue query due to errors");
                }

                // Continue the Query 
                var recordIds = queryService.GetQueryResults(queryId.ToLower(),
                    (int)request.controlActProcess.QueryContinuation.StartResultNumber,
                    (int)request.controlActProcess.QueryContinuation.ContinuationQuantity
                );
                var qd = (MARC.HI.EHRS.CR.Messaging.Everest.DataUtil.QueryData)queryService.GetQueryTag(queryId.ToLower());

                // Rules for Query Continuation
                // 1. The Query Continuation must come from the originating system
                if (request.Sender.Device == null || request.Sender.Device.Id == null ||
                    request.Sender.Device.Id.IsNull || request.Sender.Device.Id.IsEmpty ||
                    !String.Format("{1}^^^&{0}&ISO",
                    request.Sender.Device.Id.First.Root,
                    request.Sender.Device.Id.First.Extension).Equals(qd.Originator))
                {
                    dtls.Add(new UnrecognizedSenderResultDetail(request.Sender));
                    throw new SecurityException("Cannot display results");
                }
                // 2. The original conversation that was used to fetch the original result set must be available
                IGraphable originalRequest = qd.OriginalMessageQuery;
                if (originalRequest == null)
                    throw new InvalidOperationException("Cannot find the original query message in the message persistence store");


                // Ensure we can even create the required response type
                IQueryResponseFactory responseFactory = QueryResponseFactoryUtil.GetResponseFactory(originalRequest.GetType());
                if (responseFactory == null)
                    throw new NotImplementedException("Cannot determine how to respond to this interaction");
                responseFactory.Context = this.Context;
                // Assign the query request from the original data
                qd.QueryRequest = responseFactory.CreateFilterData(originalRequest as IInteraction, dtls).QueryRequest;
                qd.Quantity = (int)request.controlActProcess.QueryContinuation.ContinuationQuantity;

                // De-persist the records
                DataUtil du = new DataUtil();
                du.Context = this.Context;
                var records = du.GetRecordsAsync(recordIds, new List<VersionedDomainIdentifier>(), issues, dtls, qd);
                responseFactory.Context = this.Context;

                return responseFactory.Create(
                    originalRequest as IInteraction,
                    new DataUtil.QueryResultData()
                    {
                        Results = records.ToArray(),
                        TotalResults = (int)queryService.QueryResultTotalQuantity(queryId.ToLower()),
                        QueryId = queryId,
                        StartRecordNumber = (int)request.controlActProcess.QueryContinuation.StartResultNumber
                    },
                    dtls,
                    issues
                );
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
