/**
 * Copyright 2012-2015 Mohawk College of Applied Arts and Technology
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
using MARC.HI.EHRS.CR.Notification.PixPdq.Queue;

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
        private static Object s_syncLock = new object();

        // Notification configuration data
        public static NotificationConfiguration s_configuration;

        /// <summary>
        /// Static ctor
        /// </summary>
        static PixNotifier()
        {
            s_configuration = ConfigurationManager.GetSection("marc.hi.ehrs.cr.notification.pixpdq") as NotificationConfiguration;

        }

        /// <summary>
        /// Create the PIX notifier
        /// </summary>
        public PixNotifier()
        {
            this.m_threadPool = new WaitThreadPool(s_configuration.ConcurrencyLevel);
            //Hl7MessageQueue.Current.Restore();
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
                IDataPersistenceService idps = this.Context.GetService(typeof(IDataPersistenceService)) as IDataPersistenceService;

                if (workItem == null)
                    throw new ArgumentException("workItem");

                var evt = workItem.Event;

                if (idps != null)
                    evt = idps.GetContainer(workItem.Event.AlternateIdentifier, true) as RegistrationEvent;

                var subject = workItem.Event.FindComponent(SVC.Core.ComponentModel.HealthServiceRecordSiteRoleType.SubjectOf) as Person;
                if (subject == null)
                    throw new InvalidOperationException(locale.GetString("NTFE001"));

                // Now determine who will receive updates
                Trace.TraceInformation("Searching for targets for patient notification...");
                List<TargetConfiguration> targets = null;
                lock (s_syncLock)
                {

                    targets = s_configuration.Targets.FindAll(o => o.NotificationDomain.Exists(delegate(NotificationDomainConfiguration dc)
                        {
                            bool action = dc.Actions.Exists(act => (act.Action & workItem.Action) == workItem.Action);
                            bool domain = dc.Domain == "*" || subject.AlternateIdentifiers.Exists(id => id.Domain == dc.Domain);
                            return action && domain;
                        }
                    ));
                }
                Trace.TraceInformation("{0} targets for patient notification found...");

                // Notify the targets
                foreach (var itm in targets)
                {
                    itm.Notifier.Context = this.Context;
                    itm.Notifier.Notify(workItem);
                }

            }
            catch (Exception e)
            {
                Trace.TraceError(e.ToString());
            }
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
            // Dispose notifiers
            foreach (var cnf in s_configuration.Targets)
                if (cnf.Notifier is IDisposable)
                    (cnf.Notifier as IDisposable).Dispose();

            // Flush the queue
            Hl7MessageQueue.Current.Flush();

            if (m_threadPool != null)
                m_threadPool.Dispose();
        }

        #endregion
    }
}
