using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MARC.HI.EHRS.CR.Core.Services;
using MARC.Everest.Threading;
using System.Diagnostics;
using MARC.HI.EHRS.SVC.Core.DataTypes;
using MARC.Everest.Connectors;

namespace MARC.HI.EHRS.CR.Notification.PixPdqv2
{
    /// <summary>
    /// Represents a PIX noficiation handler for HL7v2
    /// </summary>
    public class PixNotifier : IClientNotificationService, IDisposable
    {
        #region IClientNotificationService Members

        // Wait threading pool
        private WaitThreadPool m_threadPool;

        // Sync lock
        private static Object s_syncLock = new object();

        // Notification configuration data
        public static NotificationConfiguration s_configuration;

        /// <summary>
        /// Static ctor
        /// </summary>
        static PixNotifier()
        {
            s_configuration = ConfigurationManager.GetSection("MARC.HI.EHRS.CR.Notification.PixPdqv3") as NotificationConfiguration;

        }

        /// <summary>
        /// Create the PIX notifier
        /// </summary>
        public PixNotifier()
        {
            this.m_threadPool = new WaitThreadPool(s_configuration.ConcurrencyLevel);
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
                List<TargetConfiguration> targets = null;
                lock (s_syncLock)
                {

                    targets = s_configuration.Targets.FindAll(o => o.NotificationDomain.Exists(delegate(NotificationDomainConfiguration dc)
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
                        IInteraction notification = msgUtil.CreateMessage(evt, t.ActAs == TargetActorType.PAT_IDENTITY_X_REF_MGR ? ActionType.Update : workItem.Action, t);

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

                        if (response.Acknowledgement.Count == 0 ||
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
        /// Gets or sets the operational context
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
            if(this.m_threadPool != null)
                this.m_threadPool.Dispose();
        }

        #endregion
    }
}
