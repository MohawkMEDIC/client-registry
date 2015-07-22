/**
 * Copyright 2013-2013 Mohawk College of Applied Arts and Technology
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
 * Date: 6-2-2013
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MARC.HI.EHRS.SVC.Core.Services;
using MARC.HI.EHRS.SVC.DecisionSupport;
using MARC.HI.EHRS.SVC.PolicyEnforcement;
using MARC.HI.EHRS.SVC.Core.DataTypes;
using MARC.Everest.Connectors;
using System.Data.Common;
using System.Data;
using MARC.HI.EHRS.CR.Core.ComponentModel;
using System.Threading;
using MARC.HI.EHRS.SVC.Core.ComponentModel;
using MARC.HI.EHRS.CR.Messaging.HL7.TransportProtocol;
using NHapi.Base.Util;
using System.Net;
using System.IO;
using NHapi.Base.Parser;
using MARC.HI.EHRS.CR.Core.Services;
using MARC.HI.EHRS.SVC.Core.ComponentModel.Components;
using System.Diagnostics;
using MARC.HI.EHRS.SVC.Subscription.Core.Services;
using System.Security;
using MARC.HI.EHRS.CR.Core.Data;

namespace MARC.HI.EHRS.CR.Messaging.PixPdqv2
{
    /// <summary>
    /// Data utility
    /// </summary>
    public class DataUtil : IUsesHostContext
    {

        

        #region IUsesHostContext Members

        // Host context
        private IServiceProvider m_context;

        // Services
        private IAuditorService m_auditService; // Auditor
        private IDataPersistenceService m_persistenceService; // Persistence
        private IDataRegistrationService m_registrationService; // Registration
        private IDecisionSupportService m_decisionSupportService; // DSS service
        private IPolicyEnforcementService m_policyService; // policy service
        private IQueryPersistenceService m_queryPersistence; // qp service
        private ILocalizationService m_localeService; // locale
        private IClientNotificationService m_notificationService; // client notification service
        private ISystemConfigurationService m_configService; // config service
        private IClientRegistryConfigurationService m_clientRegistryConfigService;
        private ISubscriptionManagementService m_subscriptionService;

        /// <summary>
        /// Gets or sets the context of the host
        /// </summary>
        public IServiceProvider Context
        {
            get { return this.m_context; }
            set
            {
                this.m_context = value;
                this.m_auditService = this.m_context.GetService(typeof(IAuditorService)) as IAuditorService; // Auditor
                this.m_persistenceService = this.m_context.GetService(typeof(IDataPersistenceService)) as IDataPersistenceService; // Persistence
                this.m_registrationService = this.m_context.GetService(typeof(IDataRegistrationService)) as IDataRegistrationService; // Registration
                this.m_decisionSupportService = this.m_context.GetService(typeof(IDecisionSupportService)) as IDecisionSupportService; // DSS service
                this.m_policyService = this.m_context.GetService(typeof(IPolicyEnforcementService)) as IPolicyEnforcementService; // policy service
                this.m_queryPersistence = this.m_context.GetService(typeof(IQueryPersistenceService)) as IQueryPersistenceService; // qp service
                this.m_localeService = this.m_context.GetService(typeof(ILocalizationService)) as ILocalizationService; // locale service
                this.m_notificationService = this.m_context.GetService(typeof(IClientNotificationService)) as IClientNotificationService; // notification service
                this.m_configService = this.m_context.GetService(typeof(ISystemConfigurationService)) as ISystemConfigurationService; // config service
                this.m_clientRegistryConfigService = this.m_context.GetService(typeof(IClientRegistryConfigurationService)) as IClientRegistryConfigurationService; // config
                this.m_subscriptionService = this.m_context.GetService(typeof(ISubscriptionManagementService)) as ISubscriptionManagementService;
            }
        }

        #endregion


        /// <summary>
        /// Sync lock
        /// </summary>
        private object m_syncLock = new object();
      
        /// <summary>
        /// Register the patient
        /// </summary>
        internal VersionedDomainIdentifier Register(RegistrationEvent healthServiceRecord, List<IResultDetail> dtls, DataPersistenceMode mode)
        {
          
            try
            {

                // Can't find persistence
                if (this.m_persistenceService == null)
                {
                    dtls.Add(new PersistenceResultDetail(ResultDetailType.Error, "Couldn't locate an implementation of a PersistenceService object, storage is aborted", null));
                    return null;
                }
                else if (healthServiceRecord == null)
                {
                    dtls.Add(new PersistenceResultDetail(ResultDetailType.Error, "Can't register null health service record data", null));
                    return null;
                }
                else if (dtls.Count(o => o.Type == ResultDetailType.Error) > 0)
                {
                    dtls.Add(new PersistenceResultDetail(ResultDetailType.Error, "Won't attempt to persist invalid message", null));
                    return null;
                }

                
                // Call the dss
                if (this.m_decisionSupportService != null)
                    foreach (var iss in this.m_decisionSupportService.RecordPersisting(healthServiceRecord))
                        dtls.Add(new ResultDetail(iss.Priority == SVC.Core.Issues.IssuePriorityType.Error ? ResultDetailType.Error : ResultDetailType.Warning, iss.Text, null, null));

                // Any errors?
                if (dtls.Count(o => o.Type == ResultDetailType.Error) > 0)
                    dtls.Add(new PersistenceResultDetail(ResultDetailType.Error, "Won't attempt to persist message due to detected issues", null));

                // Persist
                var retVal = this.m_persistenceService.StoreContainer(healthServiceRecord, mode);
                retVal.UpdateMode = UpdateModeType.Add;

                // Call the dss
                if (this.m_decisionSupportService != null)
                    this.m_decisionSupportService.RecordPersisted(healthServiceRecord);
                
                // Call sub
                if (this.m_subscriptionService != null)
                    this.m_subscriptionService.PublishContainer(healthServiceRecord);

                // Register the document set if it is a document
                if (retVal != null && this.m_registrationService != null && !this.m_registrationService.RegisterRecord(healthServiceRecord, mode))
                    dtls.Add(new PersistenceResultDetail(ResultDetailType.Warning, "Wasn't able to register event in the event registry, event exists in repository but not in registry. You may not be able to query for this event", null));

                return retVal;
            }
            catch (DuplicateNameException ex) // Already persisted stuff
            {
                Trace.TraceError(ex.ToString());
                dtls.Add(new PersistenceResultDetail(ResultDetailType.Error, m_localeService.GetString("DTPE005"), ex));
                return null;
            }
            catch (MissingPrimaryKeyException ex) // Already persisted stuff
            {
                Trace.TraceError(ex.ToString());
                dtls.Add(new PersistenceResultDetail(ResultDetailType.Error, m_localeService.GetString("DTPE005"), ex));
                return null;
            }
            catch (ConstraintException ex)
            {
                Trace.TraceError(ex.ToString());
                dtls.Add(new PersistenceResultDetail(ResultDetailType.Error, m_localeService.GetString("DTPE005"), ex));
                return null;
            }
            catch (Exception ex)
            {
                Trace.TraceError(ex.ToString());
                dtls.Add(new ResultDetail(ResultDetailType.Error, ex.Message, ex));
                return null;
            }
        }

        /// <summary>
        /// Update an existing patient record
        /// </summary>
        internal VersionedDomainIdentifier Update(RegistrationEvent healthServiceRecord, List<IResultDetail> dtls, DataPersistenceMode mode)
        {
            try
            {

                // Can't find persistence
                if (this.m_persistenceService == null)
                {
                    dtls.Add(new PersistenceResultDetail(ResultDetailType.Error, "Couldn't locate an implementation of a PersistenceService object, storage is aborted", null));
                    return null;
                }
                else if (healthServiceRecord == null)
                {
                    dtls.Add(new PersistenceResultDetail(ResultDetailType.Error, "Can't register null health service record data", null));
                    return null;
                }
                else if (dtls.Count(o => o.Type == ResultDetailType.Error) > 0)
                {
                    dtls.Add(new PersistenceResultDetail(ResultDetailType.Error, "Won't attempt to persist invalid message", null));
                    return null;
                }

                // Call the dss
                if (this.m_decisionSupportService != null)
                    foreach (var iss in this.m_decisionSupportService.RecordPersisting(healthServiceRecord))
                        dtls.Add(new DetectedIssueResultDetail(iss.Priority == SVC.Core.Issues.IssuePriorityType.Error ? ResultDetailType.Error : ResultDetailType.Warning, iss.Text, (string)null));
                
                // Any errors?
                if (dtls.Count(o => o.Type == ResultDetailType.Error) > 0)
                    dtls.Add(new PersistenceResultDetail(ResultDetailType.Error, "Won't attempt to persist message due to detected issues", null));

                // Persist
                var retVal = this.m_persistenceService.UpdateContainer(healthServiceRecord, mode);

                retVal.UpdateMode = UpdateModeType.Update;

                // Call the dss
                if (this.m_decisionSupportService != null)
                    this.m_decisionSupportService.RecordPersisted(healthServiceRecord);

                // Call sub
                if (this.m_subscriptionService != null)
                    this.m_subscriptionService.PublishContainer(healthServiceRecord);

                // Register the document set if it is a document
                if (retVal != null && this.m_registrationService != null && !this.m_registrationService.RegisterRecord(healthServiceRecord, mode))
                    dtls.Add(new PersistenceResultDetail(ResultDetailType.Warning, "Wasn't able to register event in the event registry, event exists in repository but not in registry. You may not be able to query for this event", null));

                return retVal;
            }
            catch (DuplicateNameException ex) // Already persisted stuff
            {
                Trace.TraceError(ex.ToString());
                dtls.Add(new PersistenceResultDetail(ResultDetailType.Error, m_localeService.GetString("DTPE005"), ex));
                return null;
            }
            catch (MissingPrimaryKeyException ex) // Already persisted stuff
            {
                Trace.TraceError(ex.ToString());
                dtls.Add(new PersistenceResultDetail(ResultDetailType.Error, m_localeService.GetString("DTPE005"), ex));
                return null;
            }
            catch (ConstraintException ex)
            {
                Trace.TraceError(ex.ToString());
                dtls.Add(new PersistenceResultDetail(ResultDetailType.Error, m_localeService.GetString("DTPE005"), ex));
                return null;
            }
            catch (Exception ex)
            {
                dtls.Add(new ResultDetail(ResultDetailType.Error, ex.Message, ex));
                return null;
            }
        }
    }
}
