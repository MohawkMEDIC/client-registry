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
using MARC.Everest.RMIM.CA.R020402.Interactions;
using MARC.Everest.Interfaces;
using MARC.HI.EHRS.SVC.Core.Services;
using MARC.Everest.Connectors;
using MARC.HI.EHRS.SVC.Core.Issues;
using MARC.Everest.RMIM.CA.R020402.Vocabulary;
using MARC.Everest.RMIM.CA.R020402.MCCI_MT002200CA;
using MARC.Everest.Exceptions;
using System.Security;
using MARC.HI.EHRS.SVC.Core.DataTypes;

namespace MARC.HI.EHRS.CR.Messaging.Everest.MessageReceiver.CA
{
    public class ContinueQueryMessageReceiver : IEverestMessageReceiver
    {
        #region IEverestMessageReceiver Members

        /// <summary>
        /// Handle a message being received
        /// </summary>
        public MARC.Everest.Interfaces.IGraphable HandleMessageReceived(object sender, MARC.Everest.Connectors.UnsolicitedDataEventArgs e, MARC.Everest.Connectors.IReceiveResult receivedMessage)
        {
            IGraphable response = null;
            if (receivedMessage.Structure is QUQI_IN000003CA)
                response = ProcessQueryContinuation(sender, e, receivedMessage);

            // Last ditch effort to create a response
            if (response == null)
                response = new NotSupportedMessageReceiver() { Context = this.Context }.HandleMessageReceived(sender, e, receivedMessage);

            // return the response
            return response;

        }

        /// <summary>
        /// Process query continuation
        /// </summary>
        private IGraphable ProcessQueryContinuation(object sender, UnsolicitedDataEventArgs e, MARC.Everest.Connectors.IReceiveResult receivedMessage)
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
            QUQI_IN000003CA request = receivedMessage.Structure as QUQI_IN000003CA;
            if (request == null)
                return null;

            // Determine if the message structure was interpreted properly
            bool isValid = MessageUtil.IsValid(receivedMessage);
            
            // Try to process
            try
            {
                DataUtil datUtil = new DataUtil();

                // set the URI
                request.Receiver.Telecom = e.ReceiveEndpoint.ToString();

                if (queryService == null)
                    throw new InvalidOperationException("No query persistence service is registered with this service");
                else if(msgPersistenceService == null)
                    throw new InvalidOperationException("No message persistence service is registered with this service");
                else if (!isValid)
                    throw new MessageValidationException("Cannot process invalid message", request);

                string queryId = String.Format("{1}^^^&{0}&ISO", request.controlActEvent.QueryContinuation.QueryId.Root, request.controlActEvent.QueryContinuation.QueryId.Extension);

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
                    (int)request.controlActEvent.QueryContinuation.StartResultNumber,
                    (int)request.controlActEvent.QueryContinuation.ContinuationQuantity
                );
                var qd = (MARC.HI.EHRS.CR.Messaging.Everest.DataUtil.QueryData)queryService.GetQueryTag(queryId.ToLower());

                // Rules for Query Continuation
                // 1. The Query Continuation must come from the originating system
                if (!String.Format("{1}^^^&{0}&ISO",
                    request.Sender.Device.Id.Root,
                    request.Sender.Device.Id.Extension).Equals(qd.Originator))
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
                qd.Quantity = (int)request.controlActEvent.QueryContinuation.ContinuationQuantity;

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
                        StartRecordNumber = (int)request.controlActEvent.QueryContinuation.StartResultNumber
                    },
                    dtls,
                    issues
                );
            }
            catch (Exception ex)
            {
                dtls.Add(new ResultDetail(ResultDetailType.Error, ex.Message, ex.StackTrace, ex));
                var nackResponse = new MCCI_IN000002CA(
                    Guid.NewGuid(),
                    DateTime.Now,
                    ResponseMode.Immediate,
                    MCCI_IN000002CA.GetInteractionId(),
                    MCCI_IN000002CA.GetProfileId(),
                    request.ProcessingCode,
                    AcknowledgementCondition.Never,
                    MessageUtil.CreateReceiver(request.Sender),
                    MessageUtil.CreateSender(e.ReceiveEndpoint, configService),
                    new Acknowledgement(
                        AcknowledgementType.ApplicationAcknowledgementError,
                        new TargetMessage(request.Id)
                    )
                );
                nackResponse.Acknowledgement.AcknowledgementDetail.AddRange(MessageUtil.CreateGenAckDetails(dtls.ToArray()));
                return nackResponse;
            } // catch

                    
                    
        }

        
        #endregion

        #region IUsesHostContext Members

        /// <summary>
        /// Gets or sets the context of this message handler
        /// </summary>
        public IServiceProvider Context
        {
            get;
            set;
        }

        #endregion

        #region ICloneable Members

        /// <summary>
        /// Clone the object
        /// </summary>
        public object Clone()
        {
            return this.MemberwiseClone();
        }

        #endregion
    }
}
