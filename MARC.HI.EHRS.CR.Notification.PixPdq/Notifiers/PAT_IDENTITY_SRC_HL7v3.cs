/**
 * Copyright 2014-2014 Mohawk College of Applied Arts and Technology
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
 * Date: 20-2-2014
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using MARC.Everest.Formatters.XML.ITS1;
using MARC.Everest.Formatters.XML.Datatypes.R1;
using MARC.HI.EHRS.CR.Notification.PixPdq.Configuration;
using MARC.Everest.Connectors.WCF;
using System.Diagnostics;
using MARC.Everest.Interfaces;
using MARC.Everest.RMIM.UV.NE2008.Vocabulary;
using MARC.Everest.RMIM.UV.NE2008.Interactions;
using MARC.Everest.Connectors;
using MARC.HI.EHRS.SVC.Core.Services;

namespace MARC.HI.EHRS.CR.Notification.PixPdq.Notifiers
{
    /// <summary>
    /// Patient Identity Source HL7v3
    /// </summary>
    [Description("Patient Identity Source HL7v3")]
    public class PAT_IDENTITY_SRC_HL7v3 : INotifier
    {
        #region INotifier Members

        /// <summary>
        /// Gets or sets the target
        /// </summary>
        public TargetConfiguration Target
        {
            get;
            set;
        }

        /// <summary>
        /// Notify 
        /// </summary>
        /// <param name="workItem"></param>
        public void Notify(NotificationQueueWorkItem workItem)
        {
            ILocalizationService locale = this.Context.GetService(typeof(ILocalizationService)) as ILocalizationService;

            // Create a message utility
            MessageUtility msgUtil = new MessageUtility() { Context = this.Context };

            // Create the EV formatters
            XmlIts1Formatter formatter = new XmlIts1Formatter()
            {
                ValidateConformance = false
            };
            formatter.GraphAides.Add(new DatatypeFormatter()
            {
                ValidateConformance = false
            });

            // Iterate through the targets attempting to notify each one
            using (WcfClientConnector wcfClient = new WcfClientConnector(Target.ConnectionString))
            {
                wcfClient.Formatter = formatter;
                wcfClient.Open();

                // Build the message
                Trace.TraceInformation("Sending notification to '{0}'...", this.Target.Name);
                IInteraction notification = msgUtil.CreateMessage(workItem.Event, workItem.Action, this.Target);

                // Send it
                var sendResult = wcfClient.Send(notification);
                if (sendResult.Code != Everest.Connectors.ResultCode.Accepted &&
                    sendResult.Code != Everest.Connectors.ResultCode.AcceptedNonConformant)
                {
                    Trace.TraceWarning(string.Format(locale.GetString("NTFW002"), this.Target.Name));
                    DumpResultDetails(sendResult.Details);
                    return;
                }

                // Receive the response
                var rcvResult = wcfClient.Receive(sendResult);
                if (rcvResult.Code != Everest.Connectors.ResultCode.Accepted &&
                    rcvResult.Code != Everest.Connectors.ResultCode.AcceptedNonConformant)
                {
                    Trace.TraceWarning(string.Format(locale.GetString("NTFW003"), this.Target.Name));
                    DumpResultDetails(rcvResult.Details);
                    return;
                }

                // Get structure
                var response = rcvResult.Structure as MCCI_IN000002UV01;
                if (response == null)
                {
                    Trace.TraceWarning(string.Format(locale.GetString("NTFW003"), this.Target.Name));
                    return;
                }

                if (response.Acknowledgement.Count == 0 ||
                    response.Acknowledgement[0].TypeCode != AcknowledgementType.AcceptAcknowledgementCommitAccept)
                {
                    Trace.TraceWarning(string.Format(locale.GetString("NTFW004"), this.Target.Name));
                    return;
                }


                // Close the connector and continue
                wcfClient.Close();

            }

        }


        /// <summary>
        /// Dump result details
        /// </summary>
        private void DumpResultDetails(IEnumerable<IResultDetail> dtls)
        {
            foreach (var itm in dtls)
                Trace.TraceWarning("{0} : {1} at {2}", itm.Type, itm.Message, itm.Location);
        }

        #endregion

        #region IUsesHostContext Members

        /// <summary>
        /// Gets or sets the context
        /// </summary>
        public IServiceProvider Context
        {
            get;
            set;
        }

        #endregion

        /// <summary>
        /// Initialize
        /// </summary>
        public void Initialize()
        {
            Trace.TraceInformation("Initializing PAT_IDENTITY_SRC_HL7v3 for {0}", this.Target.Name);
        }
    }
}
