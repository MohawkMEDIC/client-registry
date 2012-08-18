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

namespace MARC.HI.EHRS.CR.Notification.PixPdq
{
    /// <summary>
    /// Represents a client notification service that serves PIXv3 notifications
    /// </summary>
    public class PixNotifier : IClientNotificationService, IDisposable
    {
        #region IClientNotificationService Members

        // Wait threading pool
        private WaitThreadPool m_threadPool = new WaitThreadPool();

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

        }

        /// <summary>
        /// Internal notification logic
        /// </summary>
        private void NotifyInternal(object state)
        {
            // Get the state
            NotificationQueueWorkItem workItem = state as NotificationQueueWorkItem;

            if (workItem == null)
                throw new ArgumentException("workItem");

            var subject = workItem.Event.FindComponent(SVC.Core.ComponentModel.HealthServiceRecordSiteRoleType.SubjectOf) as Person;
            if(subject == null)
                throw new InvalidOperationException("Cannot find subject for notification");

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

            foreach (var t in targets)
            {
                Trace.TraceInformation("Sending notification to '{0}'...", t.Name);
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
        public SVC.Core.HostContext Context
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
