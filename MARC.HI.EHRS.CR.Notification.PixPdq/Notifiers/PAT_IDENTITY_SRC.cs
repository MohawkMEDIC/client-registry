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
using NHapi.Model.V231.Segment;
using NHapi.Base.Model;
using MARC.HI.EHRS.CR.Notification.PixPdq.Configuration;
using NHapi.Model.V231.Message;
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
    [Description("Patient Identity Source")]
    public class PAT_IDENTITY_SRC : INotifier
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
            MRG mrg = null;

            // Identify the work item action
            switch (workItem.Action)
            {
                case MARC.HI.EHRS.CR.Notification.PixPdq.Configuration.ActionType.Create:
                    {
                        ADT_A01 message = new ADT_A01();
                        msh = message.MSH;
                        pid = message.PID;
                        evn = message.EVN;
                        pv1 = message.PV1;
                        notificationMessage = message;
                        msh.MessageType.TriggerEvent.Value = "A04";

                        break;
                    }
                case MARC.HI.EHRS.CR.Notification.PixPdq.Configuration.ActionType.DuplicatesResolved:
                    {
                        ADT_A39 message = new ADT_A39();
                        msh = message.MSH;
                        msh.MessageType.TriggerEvent.Value = "A40";
                        pid = message.GetPATIENT(0).PID;
                        evn = message.EVN;
                        pv1 = message.GetPATIENT(0).PV1;
                        mrg = message.GetPATIENT(0).MRG;
                        notificationMessage = message;
                        break;
                    };
                case MARC.HI.EHRS.CR.Notification.PixPdq.Configuration.ActionType.Update:
                    {
                        ADT_A01 message = new ADT_A01();
                        msh = message.MSH;
                        pid = message.PID;
                        evn = message.EVN;
                        pv1 = message.PV1;
                        notificationMessage = message;
                        msh.MessageType.TriggerEvent.Value = "A08";
                        break;
                    }
            }

            // Populate the MSH header first
            this.UpdateMSH(msh, config);

            // Populate the EVN segment
            evn.EventTypeCode.Value = workItem.Event.Mode.ToString();
            evn.RecordedDateTime.TimeOfAnEvent.Value = (TS)workItem.Event.Timestamp;
            
            // Populate the PID segment
            Person subject = workItem.Event.FindComponent(SVC.Core.ComponentModel.HealthServiceRecordSiteRoleType.SubjectOf) as Person;
            this.UpdatePID(subject, pid, config);
            pv1.PatientClass.Value = "I";

            // Populate MRG
            if (mrg != null)
            {
                var registration = this.Context.GetService(typeof(IDataRegistrationService)) as IDataRegistrationService;
                var persistence = this.Context.GetService(typeof(IDataPersistenceService)) as IDataPersistenceService;
                var replacements = subject.FindAllComponents(SVC.Core.ComponentModel.HealthServiceRecordSiteRoleType.ReplacementOf);

                foreach (PersonRegistrationRef rplc in replacements)
                {
                    // First, need to de-persist the identifiers
                    QueryParameters qp = new QueryParameters()
                    {
                        Confidence = 1.0f,
                        MatchingAlgorithm = MatchAlgorithm.Exact,
                        MatchStrength = MatchStrength.Exact
                    };
                    var queryEvent = new QueryEvent();
                    var patientQuery = new RegistrationEvent();
                    queryEvent.Add(qp, "FLT", SVC.Core.ComponentModel.HealthServiceRecordSiteRoleType.FilterOf, null);
                    patientQuery.Add(new Person() { AlternateIdentifiers = rplc.AlternateIdentifiers }, "SUBJ", SVC.Core.ComponentModel.HealthServiceRecordSiteRoleType.SubjectOf, null);
                    queryEvent.Add(patientQuery, "SUBJ", SVC.Core.ComponentModel.HealthServiceRecordSiteRoleType.SubjectOf, null);
                    // Perform the query
                    var patientIdentifiers = registration.QueryRecord(queryEvent);
                    if (patientIdentifiers.Length == 0)
                        throw new InvalidOperationException();
                    var replacedPerson = (persistence.GetContainer(patientIdentifiers[0], true) as RegistrationEvent).FindComponent(SVC.Core.ComponentModel.HealthServiceRecordSiteRoleType.SubjectOf) as Person;

                    foreach (var ii in replacedPerson.AlternateIdentifiers.FindAll(o => this.Target.NotificationDomain.Exists(d => d.Domain == o.Domain)))
                    {
                        var cx = mrg.GetPriorPatientIdentifierList(mrg.PriorAlternatePatientIDRepetitionsUsed);
                        cx.ID.Value = ii.Identifier;
                        if (String.IsNullOrEmpty(ii.AssigningAuthority))
                        {
                            cx.AssigningAuthority.NamespaceID.Value = config.OidRegistrar.FindData(ii.Domain).Attributes.Find(o => o.Key == "AssigningAuthorityName").Value;
                        }
                        else
                            cx.AssigningAuthority.NamespaceID.Value = ii.AssigningAuthority;
                        cx.AssigningAuthority.UniversalID.Value = ii.Domain;
                        cx.AssigningAuthority.UniversalIDType.Value = "ISO";
                    }
                }
            }

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
                if(!this.Target.NotificationDomain.Any(o=>o.Domain == "*"))
                    subject.AlternateIdentifiers.RemoveAll(ii => !this.Target.NotificationDomain.Exists(o => o.Domain.Equals(ii.Domain)));
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
            if (subject.Names != null)
                foreach (var name in subject.Names)
                {
                    var xpn = pid.GetPatientName(pid.PatientNameRepetitionsUsed);
                    this.UpdateXPN(name, xpn);
                }

            // Birth time
            if (subject.BirthTime != null)
            {
                MARC.Everest.DataTypes.TS ts = new Everest.DataTypes.TS(subject.BirthTime.Value, dec.ReverseLookup(ComponentUtility.TS_PREC_MAP, subject.BirthTime.Precision));
                pid.DateTimeOfBirth.TimeOfAnEvent.Value = MARC.Everest.Connectors.Util.ToWireFormat(ts);
            }

            // Admin Sex
            if (subject.GenderCode != null)
                pid.Sex.Value = subject.GenderCode;

            // Address
            if (subject.Addresses != null)
                foreach (var addr in subject.Addresses)
                {
                    var ad = pid.GetPatientAddress(pid.PatientAddressRepetitionsUsed);
                    this.UpdateAD(addr, ad);
                }

            // Death
            if (subject.DeceasedTime != null)
            {
                pid.PatientDeathIndicator.Value = "Y";
                MARC.Everest.DataTypes.TS ts = new Everest.DataTypes.TS(subject.DeceasedTime.Value, dec.ReverseLookup(ComponentUtility.TS_PREC_MAP, subject.DeceasedTime.Precision));
                pid.PatientDeathDateAndTime.TimeOfAnEvent.Value = MARC.Everest.Connectors.Util.ToWireFormat(ts);
            }

            // MB Order
            if (subject.BirthOrder.HasValue)
            {
                pid.MultipleBirthIndicator.Value = "Y";
                pid.BirthOrder.Value = subject.BirthOrder.ToString();
            }

            // Citizenship
            if (subject.Citizenship != null)
                foreach (var cit in subject.Citizenship)
                    if (cit.Status == SVC.Core.ComponentModel.Components.StatusType.Active)
                    {
                        var c = pid.GetCitizenship(pid.CitizenshipRepetitionsUsed);
                        this.UpdateCE(new CodeValue(cit.CountryCode, config.OidRegistrar.GetOid("ISO3166-1").Oid), c);
                    }

            // Language
            if (subject.Language != null)
                foreach (var lang in subject.Language)
                    if (lang.Type == LanguageType.Fluency)
                    {
                        this.UpdateCE(new CodeValue(lang.Language, config.OidRegistrar.GetOid("ISO639-1").Oid), pid.PrimaryLanguage);
                        break;
                    }

            // Mothers name
            var relations = subject.FindAllComponents(SVC.Core.ComponentModel.HealthServiceRecordSiteRoleType.RepresentitiveOf);
            foreach (var r in relations)
                if (r is MARC.HI.EHRS.SVC.Core.ComponentModel.Components.PersonalRelationship)
                {
                    var psn = r as MARC.HI.EHRS.SVC.Core.ComponentModel.Components.PersonalRelationship;
                    if (psn.RelationshipKind != "MTH") continue;

                    if (psn.AlternateIdentifiers != null)
                        foreach (var altid in psn.AlternateIdentifiers)
                        {
                            var id = pid.GetMotherSIdentifier(pid.MotherSIdentifierRepetitionsUsed);
                            UpdateCX(altid, id,config);
                        }
                    if (psn.LegalName != null)
                        UpdateXPN(psn.LegalName, pid.GetMotherSMaidenName(0));
                    break;
                }

            // Telecom addresses
            //if(subject.TelecomAddresses != null)
            //    foreach (var tel in subject.TelecomAddresses)
            //        if (tel.Use == "HP" && tel.Value.StartsWith("tel"))
            //            MessageUtil.XTNFromTel((MARC.Everest.DataTypes.TEL)tel.Value, pid.GetPhoneNumberHome(pid.PhoneNumberHomeRepetitionsUsed));
            //        else if (tel.Use == "HP")
            //            pid.GetPhoneNumberHome(pid.PhoneNumberHomeRepetitionsUsed).EmailAddress.Value = tel.Value;
            //        else if (tel.Use == "WP" && tel.Value.StartsWith("tel"))
            //            MessageUtil.XTNFromTel((MARC.Everest.DataTypes.TEL)tel.Value, pid.GetPhoneNumberBusiness(pid.PhoneNumberBusinessRepetitionsUsed));
            //        else if (tel.Use == "WP")
            //            pid.GetPhoneNumberBusiness(pid.PhoneNumberBusinessRepetitionsUsed).EmailAddress.Value = tel.Value;
   
            
        }

        /// <summary>
        /// Update a CE
        /// </summary>
        private void UpdateCE(CodeValue codeValue, NHapi.Model.V231.Datatype.CE ce)
        {
            ITerminologyService tservice = Context.GetService(typeof(ITerminologyService)) as ITerminologyService;
            codeValue = tservice.FillInDetails(codeValue);

            ce.Identifier.Value = codeValue.Code;
            ce.NameOfCodingSystem.Value = codeValue.CodeSystemName;
        }

        /// <summary>
        /// Update an AD
        /// </summary>
        private void UpdateAD(SVC.Core.DataTypes.AddressSet addr, NHapi.Model.V231.Datatype.XAD ad)
        {
            var dec = new DeComponentUtility() { Context = this.Context };
            ad.AddressType.Value = dec.ReverseLookup(ComponentUtility.AD_USE_MAP, addr.Use);

            foreach (var cmp in addr.Parts)
            {
                string cmpStr = dec.ReverseLookup(ComponentUtility.AD_MAP, cmp.PartType);
                if (String.IsNullOrEmpty(cmpStr)) continue;
                int cmpNo = int.Parse(cmpStr);
                if (ad.Components[cmpNo - 1] is AbstractPrimitive)
                    (ad.Components[cmpNo - 1] as AbstractPrimitive).Value = cmp.AddressValue;
                else if (ad.Components[cmpNo - 1] is NHapi.Model.V25.Datatype.SAD)
                    (ad.Components[cmpNo - 1] as NHapi.Model.V25.Datatype.SAD).StreetOrMailingAddress.Value = cmp.AddressValue;
            }
        }

        /// <summary>
        /// Update an XPN
        /// </summary>
        private void UpdateXPN(SVC.Core.DataTypes.NameSet name, NHapi.Model.V231.Datatype.XPN xpn)
        {
            var dec = new DeComponentUtility() { Context = this.Context };

            xpn.NameTypeCode.Value = dec.ReverseLookup(ComponentUtility.XPN_USE_MAP, name.Use);

            // IF SEAGULL!!! NOOO!!!! Hopefully nobody finds this thing
            foreach (var cmp in name.Parts)
            {
                string cmpStr = dec.ReverseLookup(ComponentUtility.XPN_MAP, cmp.Type);
                int cmpNo = int.Parse(cmpStr);

                if (xpn.Components[cmpNo - 1] is AbstractPrimitive)
                {
                    if (cmp.Type == NamePart.NamePartType.Given && (!String.IsNullOrEmpty((xpn.Components[cmpNo - 1] as AbstractPrimitive).Value)))// given is taken so use other segment
                    {
                        if (!String.IsNullOrEmpty(xpn.MiddleInitialOrName.Value))
                            xpn.MiddleInitialOrName.Value += " ";
                        xpn.MiddleInitialOrName.Value += cmp.Value;
                    }
                    else
                        (xpn.Components[cmpNo - 1] as AbstractPrimitive).Value = cmp.Value;
                }
                else if (xpn.Components[cmpNo - 1] is NHapi.Model.V231.Datatype.FN)
                {
                    var fn = xpn.Components[cmpNo - 1] as NHapi.Model.V231.Datatype.FN;
                    if (!String.IsNullOrEmpty(fn.FamilyName.Value))
                        fn.FamilyName.Value += "-";
                    fn.FamilyName.Value += cmp.Value;
                }
            }
        }

        /// <summary>
        /// Update a CX
        /// </summary>
        private void UpdateCX(SVC.Core.DataTypes.DomainIdentifier altId, NHapi.Model.V231.Datatype.CX cx, ISystemConfigurationService config)
        {
            // Get oid data
            var oidData = config.OidRegistrar.FindData(altId.Domain);
            cx.AssigningAuthority.UniversalID.Value = altId.Domain ?? (oidData == null ? null : oidData.Oid);
            cx.AssigningAuthority.UniversalIDType.Value = "ISO";
            cx.AssigningAuthority.NamespaceID.Value = altId.AssigningAuthority ?? (oidData == null ? null : oidData.Attributes.Find(o => o.Key.Equals("AssigningAuthorityName")).Value);
            cx.ID.Value = altId.Identifier;
        }


        /// <summary>
        /// Update the MSH header
        /// </summary>
        private void UpdateMSH(MSH msh, ISystemConfigurationService config)
        {
            msh.AcceptAcknowledgmentType.Value = "AL";
            msh.DateTimeOfMessage.TimeOfAnEvent.Value = (TS)DateTime.Now;
            
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
