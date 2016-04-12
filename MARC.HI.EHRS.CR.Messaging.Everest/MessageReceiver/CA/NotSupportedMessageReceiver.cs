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
 * Date: 16-7-2012
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MARC.HI.EHRS.SVC.Messaging.Everest;
using MARC.HI.EHRS.SVC.Core.Services;
using MARC.HI.EHRS.SVC.Core.DataTypes;
using MARC.Everest.RMIM.CA.R020403.Interactions;
using MARC.Everest.RMIM.CA.R020403.Vocabulary;
using MARC.Everest.Connectors;
using MARC.Everest.DataTypes;
using System.Reflection;
using MARC.Everest.Interfaces;
using System.Runtime.InteropServices;
using MARC.Everest.RMIM.CA.R020403.MCCI_MT102001CA;
using MARC.Everest.RMIM.CA.R020403.MCCI_MT002200CA;

namespace MARC.HI.EHRS.CR.Messaging.Everest.MessageReceiver.CA
{
    /// <summary>
    /// Handler for any interaction that is not supported
    /// </summary>
    public class NotSupportedMessageReceiver : IEverestMessageReceiver
    {
        #region IEverestMessageReceiver Members

        /// <summary>
        /// The name of the software
        /// </summary>
        static AssemblyProductAttribute SoftwareName = null;
        /// <summary>
        /// The description of the software
        /// </summary>
        static AssemblyDescriptionAttribute SoftwareDescription = null;
        /// <summary>
        /// Version of the software
        /// </summary>
        static Version SoftwareVersion = null;

        /// <summary>
        /// Static constructor for the not supported exception
        /// </summary>
        static NotSupportedMessageReceiver()
        {
            try
            {
                SoftwareName = Assembly.GetEntryAssembly().GetCustomAttributes(typeof(AssemblyProductAttribute), false)[0] as AssemblyProductAttribute;
                SoftwareDescription = Assembly.GetEntryAssembly().GetCustomAttributes(typeof(AssemblyDescriptionAttribute), false)[0] as AssemblyDescriptionAttribute;
                SoftwareVersion = Assembly.GetEntryAssembly().GetName().Version;
            }
            catch { }
        }

        /// <summary>
        /// Handles a received message
        /// </summary>
        public MARC.Everest.Interfaces.IGraphable HandleMessageReceived(object sender, UnsolicitedDataEventArgs e, MARC.Everest.Connectors.IReceiveResult receivedMessage)
        {

            // audit the error
            IAuditorService auditor = Context.GetService(typeof(IAuditorService)) as IAuditorService;
            AuditData ad = new AuditData(
                DateTime.Now, ActionType.Execute, OutcomeIndicator.EpicFail, EventIdentifierType.ApplicationActivity, new CodeValue(String.Format("{0}", receivedMessage.Structure))
                );
            ad.Actors.AddRange(new List<AuditActorData>(10)
                    { 
                        new AuditActorData() { NetworkAccessPointId = e.ReceiveEndpoint.ToString(), NetworkAccessPointType = NetworkAccessPointType.IPAddress, UserIsRequestor = false },
                        new AuditActorData() { NetworkAccessPointType = NetworkAccessPointType.MachineName, NetworkAccessPointId = Environment.MachineName, UserIsRequestor = false }
                    }
            );
            ad.AuditableObjects.Add(new AuditableObject() { IDTypeCode = AuditableObjectIdType.ReportNumber, LifecycleType = AuditableObjectLifecycle.Verification, ObjectId = (receivedMessage.Structure as IIdentifiable).Id.Root, Role = AuditableObjectRole.Subscriber, Type = AuditableObjectType.SystemObject });
            if(auditor != null)
                auditor.SendAudit(ad);

            IInteraction solicitation = receivedMessage.Structure as IInteraction;

            // get the configuration
            ISystemConfigurationService configService = Context.GetService(typeof(ISystemConfigurationService)) as ISystemConfigurationService;
            
             // construct a generic response
            MCCI_IN000002CA response = new MCCI_IN000002CA(
                Guid.NewGuid(), 
                DateTime.Now,
                new CV<ResponseMode>(ResponseMode.Immediate),
                MCCI_IN000002CA.GetInteractionId(), 
                MCCI_IN000002CA.GetProfileId(),
                ProcessingID.Debugging,
                AcknowledgementCondition.Never,
                null,
                MessageUtil.CreateSender(e.ReceiveEndpoint, configService),
                new Acknowledgement(
                    AcknowledgementType.ApplicationAcknowledgementReject, 
                    new TargetMessage(
                        (receivedMessage.Structure as IIdentifiable).Id
                    )
                )
            );

            // Add a detail
            if (solicitation.InteractionId != null && solicitation.InteractionId.Extension != receivedMessage.Structure.GetType().Name)
                response.Acknowledgement.AcknowledgementDetail.Add(
                    new AcknowledgementDetail(
                        AcknowledgementDetailType.Error,
                        Util.ToWireFormat(MARC.Everest.RMIM.CA.R020402.Vocabulary.AcknowledgementDetailCode.ValueDoesNotMatchFixedValue),
                        String.Format("Interaction ID '{0}' not supported for message type '{1}'", solicitation.InteractionId.Extension, receivedMessage.Structure.GetType().Name),
                        null));
            else
                response.Acknowledgement.AcknowledgementDetail.Add(
                    new AcknowledgementDetail(
                        AcknowledgementDetailType.Error,
                        Util.ToWireFormat(MARC.Everest.RMIM.CA.R020402.Vocabulary.AcknowledgementDetailCode.UnsupportedInteraction),
                        "Cannot process this interaction",
                        null)
                );
            
            // Validation detils
            response.Acknowledgement.AcknowledgementDetail.AddRange(MessageUtil.CreateGenAckDetails(receivedMessage.Details));
                    
            // Populate the receiver
            Sender originalSolicitor = solicitation.GetType().GetProperty("Sender").GetValue(solicitation, null) as Sender;
            Receiver receiver = MessageUtil.CreateReceiver(originalSolicitor);
            response.Receiver = receiver;

            return response;

        }

        #endregion

        #region IUsesHostContext Members

        /// <summary>
        /// Gets or sets the context under which this message handler runs
        /// </summary>
        public IServiceProvider Context
        {
            get;
            set;
        }

        #endregion

        #region ICloneable Members

        /// <summary>
        /// Clone this item
        /// </summary>
        public object Clone()
        {
            return this.MemberwiseClone();
        }

        #endregion
    }
}
