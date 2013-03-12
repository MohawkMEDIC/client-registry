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
 * Date: 21-8-2012
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MARC.HI.EHRS.CR.Core.Services;
using MARC.HI.EHRS.CR.Notification.PixPdq.Configuration;
using System.Configuration;
using MARC.Everest.Threading;
using System.Diagnostics;
using MARC.HI.EHRS.CR.Core.ComponentModel;
using MARC.HI.EHRS.SVC.Core.Services;
using MARC.Everest.Interfaces;
using MARC.Everest.Connectors.WCF;
using MARC.Everest.Formatters.XML.ITS1;
using MARC.Everest.Formatters.XML.Datatypes.R1;
using MARC.Everest.RMIM.UV.NE2008.Interactions;
using MARC.Everest.RMIM.UV.NE2008.Vocabulary;
using MARC.Everest.Connectors;

namespace MARC.HI.EHRS.CR.Notification.PixPdq
{
    /// <summary>
    /// Represents a client notification service that serves PIXv3 notifications
    /// </summary>
    public class PixNotifier : IClientNotificationService, IDisposable
    {
        #region IClientNotificationService Members

        // Wait threading pool
        private WaitThreadPool m_threadPool ;

        // Sync lock
        private Object m_syncLock = new object();

        // Notification configuration data
        private NotificationConfiguration m_configuration;

        /// <summary>
        /// Create the PIX notifier
        /// </summary>
        public PixNotifier()
        {
            this.m_configuration = ConfigurationManager.GetSection("marc.hi.ehrs.cr.notification.pixpdq") as NotificationConfiguration;
            this.m_threadPool = new WaitThreadPool(this.m_configuration.ConcurrencyLevel);
        }

        /// <summary>
        /// Internal notification logic
        /// </summary>
        private void NotifyInternal(object state)
        {
            // Get the state
            try
            {
                NotificationQueueWorkItem workItem = state as NotificationQueueWorkItem;
                ILocalizationService locale = this.Context.GetService(typeof(ILocalizationService)) as ILocalizationService;

                if (workItem == null)
                    throw new ArgumentException("workItem");

                var subject = workItem.Event.FindComponent(SVC.Core.ComponentModel.HealthServiceRecordSiteRoleType.SubjectOf) as Person;
                if (subject == null)
                    throw new InvalidOperationException(locale.GetString("NTFE001"));

                // Now determine who will receive updates
                List<TargetConfiguration> targets = null;
                lock (this.m_syncLock)
                {

                    targets = this.m_configuration.Targets.FindAll(o => o.NotificationDomain.Exists(delegate(NotificationDomainConfiguration dc)
                        {
                            bool action = dc.Actions.Exists(act => (act.Action & workItem.Action) == workItem.Action);
                            bool domain = subject.AlternateIdentifiers.Exists(id => id.Domain == dc.Domain);
                            return action && domain;
                        }
                    ));
                }


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
                foreach (var t in targets)
                    using (WcfClientConnector wcfClient = new WcfClientConnector(t.ConnectionString))
                    {
                        wcfClient.Formatter = formatter;
                        wcfClient.Open();
                        
                        // Build the message
                        Trace.TraceInformation("Sending notification to '{0}'...", t.Name);
                        IInteraction notification = msgUtil.CreateMessage(workItem.Event, t.ActAs == TargetActorType.PAT_IDENTITY_X_REF_MGR ? ActionType.Update : workItem.Action, t);

                        // Send it
                        var sendResult = wcfClient.Send(notification);
                        if (sendResult.Code != Everest.Connectors.ResultCode.Accepted &&
                            sendResult.Code != Everest.Connectors.ResultCode.AcceptedNonConformant)
                        {
                            Trace.TraceWarning(string.Format(locale.GetString("NTFW002"), t.Name));
                            DumpResultDetails(sendResult.Details);
                            continue;
                        }

                        // Receive the response
                        var rcvResult = wcfClient.Receive(sendResult);
                        if (rcvResult.Code != Everest.Connectors.ResultCode.Accepted &&
                            rcvResult.Code != Everest.Connectors.ResultCode.AcceptedNonConformant)
                        {
                            Trace.TraceWarning(string.Format(locale.GetString("NTFW003"), t.Name));
                            DumpResultDetails(rcvResult.Details);
                            continue;
                        }

                        // Get structure
                        var response = rcvResult.Structure as MCCI_IN000002UV01;
                        if (response == null)
                        {
                            Trace.TraceWarning(string.Format(locale.GetString("NTFW003"), t.Name));
                            continue;
                        }

                        if(response.Acknowledgement.Count == 0 ||
                            response.Acknowledgement[0].TypeCode != AcknowledgementType.AcceptAcknowledgementCommitAccept)
                        {
                            Trace.TraceWarning(string.Format(locale.GetString("NTFW004"), t.Name));
                            continue;
                        }
                            

                        // Close the connector and continue
                        wcfClient.Close();
                        
                    }

            }
            catch (Exception e)
            {
                Trace.TraceError(e.ToString());
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


        /// <summary>
        /// Notify that an update has occurred
        /// </summary>
        public void NotifyUpdate(Core.ComponentModel.RegistrationEvent evt)
        {
            this.m_threadPool.QueueUserWorkItem(NotifyInternal, new NotificationQueueWorkItem(evt, ActionType.Update));
        }

        /// <summary>
        /// Notify that a registration has occurred
        /// </summary>
        public void NotifyRegister(Core.ComponentModel.RegistrationEvent evt)
        {
            this.m_threadPool.QueueUserWorkItem(NotifyInternal, new NotificationQueueWorkItem(evt, ActionType.Create));
        }

        /// <summary>
        /// Notify that a reconciliation is requried
        /// </summary>
        /// <remarks>Not supported by this registry</remarks>
        public void NotifyReconciliationRequired(IEnumerable<SVC.Core.DataTypes.VersionedDomainIdentifier> candidates)
        {
            Trace.TraceInformation("Notification of reconciliation is not supported by the PIX notifier");
            Trace.Indent();
            foreach (var itm in candidates)
                Trace.TraceInformation("{1}^^^&{0}&ISO", itm.Domain, itm.Identifier);
            Trace.Unindent();
            return; // this is not supported
        }

        /// <summary>
        /// Notify that duplicates are resolved
        /// </summary>
        public void NotifyDuplicatesResolved(Core.ComponentModel.RegistrationEvent evt)
        {
            this.m_threadPool.QueueUserWorkItem(NotifyInternal, new NotificationQueueWorkItem(evt, ActionType.DuplicatesResolved));
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

        #region IDisposable Members

        /// <summary>
        /// Dispose the object
        /// </summary>
        public void Dispose()
        {
            if (m_threadPool != null)
                m_threadPool.Dispose();
        }

        #endregion
    }
}
