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
using NHapi.Model.V25.Segment;
using NHapi.Base.Model;
using MARC.HI.EHRS.CR.Notification.PixPdq.Configuration;
using NHapi.Model.V25.Message;
using MARC.HI.EHRS.CR.Messaging.PixPdqv2;
using NHapi.Base.Util;
using MARC.HI.EHRS.SVC.Core.Services;
using MARC.Everest.DataTypes;
using MARC.HI.EHRS.CR.Core.ComponentModel;
using MARC.HI.EHRS.SVC.Core.DataTypes;
using System.Diagnostics;
using System.ComponentModel;
using System.Runtime.InteropServices;
using MARC.HI.EHRS.SVC.Core.Timer;
using MARC.HI.EHRS.SVC.Core;
using System.IO;
using System.Reflection;
using System.Xml.Serialization;
using NHapi.Base.Parser;
using MARC.HI.EHRS.CR.Notification.PixPdq.Queue;

namespace MARC.HI.EHRS.CR.Notification.PixPdq
{
    /// <summary>
    /// Patient IDentity XRef source notifier
    /// </summary>
    /// <remarks>NB: Lots of this stuff is copied from DeComponentUtility because that uses 2.5 and this uses 2.3.1 , and it is a tight deadline. I will come back and clean this up</remarks>
    /// TODO: Clean this up
    [Description("Patient Identity XREF Manager")]
    public class PAT_ID_X_REF_MGR : INotifier
    {

        
        #region INotifier Members

        /// <summary>
        /// Gets or sets the target of the notification
        /// </summary>
        public Configuration.TargetConfiguration Target
        {
            get;
            set;
        }

        /// <summary>
        /// Notify the operation
        /// </summary>
        public void Notify(NotificationQueueWorkItem workItem)
        {
            
            // configuration service
            ISystemConfigurationService config = this.Context.GetService(typeof(ISystemConfigurationService)) as ISystemConfigurationService;
            ILocalizationService locale = this.Context.GetService(typeof(ILocalizationService)) as ILocalizationService;

            // Common message bits we need to update
            IMessage notificationMessage = null;
            MSH msh = null;
            PID pid = null;
            EVN evn = null;
            PV1 pv1 = null;
            

            // Identify the work item action
            switch (workItem.Action)
            {
                case MARC.HI.EHRS.CR.Notification.PixPdq.Configuration.ActionType.Create:
                case MARC.HI.EHRS.CR.Notification.PixPdq.Configuration.ActionType.DuplicatesResolved:
                case MARC.HI.EHRS.CR.Notification.PixPdq.Configuration.ActionType.Update:
                    {
                        ADT_A05 message = new ADT_A05();
                        msh = message.MSH;
                        pid = message.PID;
                        evn = message.EVN;
                        pv1 = message.PV1;
                        notificationMessage = message;
                        msh.MessageType.TriggerEvent.Value = "A31";
                        break;
                    }
            }

            // Populate the MSH header first
            this.UpdateMSH(msh, config);

            // Populate the EVN segment
            evn.EventTypeCode.Value = workItem.Event.Mode.ToString();
            evn.RecordedDateTime.Time.Value = (TS)workItem.Event.Timestamp;
            
            // Populate the PID segment
            Person subject = workItem.Event.FindComponent(SVC.Core.ComponentModel.HealthServiceRecordSiteRoleType.SubjectOf) as Person;
            this.UpdatePID(subject, pid, config);
            pv1.PatientClass.Value = "N";

            // Send
            var queueItem = new Hl7MessageQueue.MessageQueueWorkItem(this.Target, notificationMessage);
            if (!queueItem.TrySend())
            {
                Trace.TraceWarning(locale.GetString("NTFW005"));
                Hl7MessageQueue.Current.EnqueueMessageItem(queueItem);
            }
        }

        /// <summary>
        /// Update the PID
        /// </summary>
        /// <param name="subject"></param>
        /// <param name="pid"></param>
        private void UpdatePID(Person subject, PID pid, ISystemConfigurationService config)
        {

            var dec = new DeComponentUtility();

            // Alternate identifiers
            if (subject.AlternateIdentifiers != null)
            {
                //subject.AlternateIdentifiers.RemoveAll(ii => !this.Target.NotificationDomain.Exists(o => o.Domain.Equals(ii.Domain)));
                List<String> alreadyAdded = new List<string>();
                foreach (var altId in subject.AlternateIdentifiers)
                {
                    String idS = String.Format("{0}^{1}", altId.Domain, altId.Identifier);
                    if (!alreadyAdded.Contains(idS))
                    {
                        var id = pid.GetPatientIdentifierList(pid.PatientIdentifierListRepetitionsUsed);
                        this.UpdateCX(altId, id, config);
                        alreadyAdded.Add(idS);
                    }
                }
            }

            // Populate Names
            pid.GetPatientName(0).FamilyName.Surname.Value = " ";
        }


        /// <summary>
        /// Update a CX
        /// </summary>
        private void UpdateCX(SVC.Core.DataTypes.DomainIdentifier altId, NHapi.Model.V25.Datatype.CX cx, ISystemConfigurationService config)
        {
            // Get oid data
            var oidData = config.OidRegistrar.FindData(altId.Domain);
            cx.AssigningAuthority.UniversalID.Value = altId.Domain ?? (oidData == null ? null : oidData.Oid);
            cx.AssigningAuthority.UniversalIDType.Value = "ISO";
            cx.AssigningAuthority.NamespaceID.Value = altId.AssigningAuthority ?? (oidData == null ? null : oidData.Attributes.Find(o => o.Key.Equals("AssigningAuthorityName")).Value);
            cx.IDNumber.Value = altId.Identifier;
        }

        /// <summary>
        /// Update the MSH header
        /// </summary>
        private void UpdateMSH(MSH msh, ISystemConfigurationService config)
        {
            msh.AcceptAcknowledgmentType.Value = "AL";
            msh.DateTimeOfMessage.Time.Value = (TS)DateTime.Now;
            
            msh.MessageControlID.Value = BitConverter.ToInt64(Guid.NewGuid().ToByteArray(), 0).ToString();
            msh.MessageType.MessageStructure.Value = msh.Message.GetType().Name;
            msh.ProcessingID.ProcessingID.Value = "P";

            if (this.Target.DeviceIdentifier.Contains("|"))
            {
                msh.ReceivingApplication.NamespaceID.Value = this.Target.DeviceIdentifier.Split('|')[0];
                msh.ReceivingFacility.NamespaceID.Value = this.Target.DeviceIdentifier.Split('|')[1];
            }
            else
            {
                msh.ReceivingApplication.NamespaceID.Value = this.Target.DeviceIdentifier;
                msh.ReceivingFacility.NamespaceID.Value = config.JurisdictionData.Name;
            }
            msh.SendingApplication.NamespaceID.Value = config.DeviceName;
            msh.SendingFacility.NamespaceID.Value = config.JurisdictionData.Name;
            
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

    }
}
