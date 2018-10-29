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
 * Date: 17-10-2012
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MARC.HI.EHRS.SVC.Core.Services;
using NHapi.Base.Util;
using MARC.HI.EHRS.CR.Core.ComponentModel;
using MARC.HI.EHRS.SVC.Core.DataTypes;
using MARC.HI.EHRS.SVC.Core.ComponentModel.Components;
using NHapi.Base.Model;
using MARC.Everest.DataTypes;
using MARC.Everest.DataTypes.Interfaces;
using MARC.HI.EHRS.CR.Core.Services;
using NHapi.Model.V25.Segment;

namespace MARC.HI.EHRS.CR.Messaging.PixPdqv2
{
    /// <summary>
    /// Decomponentization utility
    /// </summary>
    public class DeComponentUtility : IUsesHostContext
    {
        
        #region IUsesHostContext Members

        // Host context
        private IServiceProvider m_context;

        // Localization service
        private ILocalizationService m_locale;

        // Config
        private ISystemConfigurationService m_config;

        /// <summary>
        /// Gets or sets the application context of this component
        /// </summary>
        public IServiceProvider Context
        {
            get { return this.m_context; }
            set
            {
                this.m_context = value;
                this.m_locale = value.GetService(typeof(ILocalizationService)) as ILocalizationService;
                this.m_config = value.GetService(typeof(ISystemConfigurationService)) as ISystemConfigurationService;
            }
        }

        #endregion

        /// <summary>
        /// Create the RSP_K23 mesasge
        /// </summary>
        public NHapi.Model.V25.Message.RSP_K23 CreateRSP_K23(RegistryQueryResult result, List<Everest.Connectors.IResultDetail> dtls)
        {
            // Return value
            var retVal = new NHapi.Model.V25.Message.RSP_K23();

            retVal.MSH.MessageType.MessageStructure.Value = "RSP_K23";
            retVal.MSH.MessageType.TriggerEvent.Value = "K23";

            var qak = retVal.QAK;
            var msa = retVal.MSA;

            qak.QueryTag.Value = result.QueryTag;
            msa.AcknowledgmentCode.Value = "AA";
            if (dtls.Exists(o => o.Type == Everest.Connectors.ResultDetailType.Error))
            {
                qak.QueryResponseStatus.Value = "AE";
                msa.AcknowledgmentCode.Value = "AE";
                foreach (var dtl in dtls)
                {
                    var err = retVal.GetStructure("ERR", retVal.currentReps("ERR")) as NHapi.Model.V25.Segment.ERR;
                    MessageUtil.UpdateERR(err, dtl, Context);
                }
            }
            else if (result.Results == null || result.Results.Count == 0)
            {
                qak.QueryResponseStatus.Value = "NF";
            }
            else
            {
                // Create the pid
                qak.QueryResponseStatus.Value = "OK";
                UpdatePID(result.Results[0] as RegistrationEvent, retVal.QUERY_RESPONSE.PID, true);
            }



            return retVal;
        }

        /// <summary>
        /// Update the specified PID
        /// </summary>
        public void UpdatePID(Core.ComponentModel.RegistrationEvent registrationEvent, NHapi.Model.V25.Segment.PID pid, bool summaryOnly)
        {

            var subject = registrationEvent.FindComponent(SVC.Core.ComponentModel.HealthServiceRecordSiteRoleType.SubjectOf) as Person;
            var aut = registrationEvent.FindComponent(SVC.Core.ComponentModel.HealthServiceRecordSiteRoleType.AuthorOf) as RepositoryDevice;

            // Update time
            pid.LastUpdateDateTime.Time.Value = ((TS)subject.Timestamp).Value;
            if (aut != null)
                pid.LastUpdateFacility.NamespaceID.Value = aut.Jurisdiction;
            
            // Alternate identifiers
            foreach (var altId in subject.AlternateIdentifiers)
            {
                var id = pid.GetPatientIdentifierList(pid.PatientIdentifierListRepetitionsUsed);
                UpdateCX(altId, id);
            }

            // Other identifiers
            foreach (var othId in subject.OtherIdentifiers)
            {
                var id = pid.GetPatientIdentifierList(pid.PatientIdentifierListRepetitionsUsed);
                UpdateCX(othId.Value, id);

                // Correct v3 codes
                if (othId.Key.CodeSystem == "1.3.6.1.4.1.33349.3.98.12")
                    id.IdentifierTypeCode.Value = othId.Key.Code;
                else if (othId.Key.CodeSystem == "2.16.840.1.113883.2.20.3.85")
                    switch (othId.Key.Code)
                    {
                        case "SIN":
                            id.IdentifierTypeCode.Value = "SS";
                            break;
                        case "DL":
                            id.IdentifierTypeCode.Value = othId.Key.Code;
                            break;
                        default:
                            id.IdentifierTypeCode.Value = null;
                            break;
                    }
                else
                    id.IdentifierTypeCode.Value = null;

            }

            // IHE: This first repetition should be null
            if (summaryOnly)
            {
                pid.GetPatientName(0);
                pid.GetPatientName(1).NameTypeCode.Value = "S";
                return;
            }

            // Populate Names
            if(subject.Names != null)
                foreach (var name in subject.Names)
                {
                    var xpn = pid.GetPatientName(pid.PatientNameRepetitionsUsed);
                    UpdateXPN(name, xpn);
                }

            // Birth time
            if(subject.BirthTime != null)
            {
                MARC.Everest.DataTypes.TS ts = new Everest.DataTypes.TS(subject.BirthTime.Value, ReverseLookup(ComponentUtility.TS_PREC_MAP, subject.BirthTime.Precision));
                pid.DateTimeOfBirth.Time.Value = MARC.Everest.Connectors.Util.ToWireFormat(ts);
            }

            // Admin Sex
            if (subject.GenderCode != null)
                pid.AdministrativeSex.Value = subject.GenderCode;

            // Address
            if(subject.Addresses != null)
                foreach (var addr in subject.Addresses)
                {
                    var ad = pid.GetPatientAddress(pid.PatientAddressRepetitionsUsed);
                    UpdateAD(addr, ad);
                }

            // Death
            if (subject.DeceasedTime != null)
            {
                pid.PatientDeathIndicator.Value = "Y";
                MARC.Everest.DataTypes.TS ts = new Everest.DataTypes.TS(subject.DeceasedTime.Value, ReverseLookup(ComponentUtility.TS_PREC_MAP, subject.DeceasedTime.Precision));
                pid.PatientDeathDateAndTime.Time.Value = MARC.Everest.Connectors.Util.ToWireFormat(ts);
            }

            // MB Order
            if (subject.BirthOrder.HasValue)
            {
                pid.MultipleBirthIndicator.Value = "Y";
                if(subject.BirthOrder.Value > 0)
                    pid.BirthOrder.Value = subject.BirthOrder.ToString();
            }

            // Citizenship
            if (subject.Citizenship != null)
                foreach (var cit in subject.Citizenship)
                    if (cit.Status == SVC.Core.ComponentModel.Components.StatusType.Active)
                    {
                        var c = pid.GetCitizenship(pid.CitizenshipRepetitionsUsed);
                        UpdateCE(new CodeValue(cit.CountryCode, this.m_config.OidRegistrar.GetOid("ISO3166-1").Oid), c);
                    }

            if (subject.MaritalStatus != null)
                UpdateCE(subject.MaritalStatus, pid.MaritalStatus);
            // Language
            if(subject.Language != null)
                foreach(var lang in subject.Language)
                    if (lang.Type == LanguageType.Preferred)
                    {
                        UpdateCE(new CodeValue(lang.Language, this.m_config.OidRegistrar.GetOid("ISO639-1").Oid), pid.PrimaryLanguage);
                        break;
                    }

            // Mothers name
            var relations = subject.FindAllComponents(SVC.Core.ComponentModel.HealthServiceRecordSiteRoleType.RepresentitiveOf);
            foreach(var r in relations)
                if (r is PersonalRelationship)
                {
                    var psn = r as PersonalRelationship;
                    if (psn.RelationshipKind != "MTH") continue;
                    
                    if(psn.AlternateIdentifiers != null)
                        foreach (var altid in psn.AlternateIdentifiers)
                        {
                            var id = pid.GetMotherSIdentifier(pid.MotherSIdentifierRepetitionsUsed);
                            UpdateCX(altid, id);
                        }
                    if (psn.LegalName != null)
                        UpdateXPN(psn.LegalName, pid.GetMotherSMaidenName(0));
                }

            // Telecom addresses
            foreach (var tel in subject.TelecomAddresses)
                if (tel.Use == "HP" && tel.Value.StartsWith("tel"))
                    MessageUtil.XTNFromTel(new TEL()
                    {
                        Value = tel.Value,
                        Use = MARC.Everest.Connectors.Util.Convert<SET<CS<TelecommunicationAddressUse>>>(tel.Use),
                        Capabilities = MARC.Everest.Connectors.Util.Convert<SET<CS<TelecommunicationCabability>>>(tel.Capability)
                    }, pid.GetPhoneNumberHome(pid.PhoneNumberHomeRepetitionsUsed));
                else if (tel.Use == "HP")
                    pid.GetPhoneNumberHome(pid.PhoneNumberHomeRepetitionsUsed).EmailAddress.Value = tel.Value;
                else if (tel.Use == "WP" && tel.Value.StartsWith("tel"))
                    MessageUtil.XTNFromTel(new TEL()
                    {
                        Value = tel.Value,
                        Use = MARC.Everest.Connectors.Util.Convert<SET<CS<TelecommunicationAddressUse>>>(tel.Use),
                        Capabilities = MARC.Everest.Connectors.Util.Convert<SET<CS<TelecommunicationCabability>>>(tel.Capability)
                    }, pid.GetPhoneNumberBusiness(pid.PhoneNumberBusinessRepetitionsUsed));
                else if (tel.Use == "WP")
                    pid.GetPhoneNumberBusiness(pid.PhoneNumberBusinessRepetitionsUsed).EmailAddress.Value = tel.Value;

            // Race
            if (subject.Race != null)
            {
                foreach(var rc in subject.Race)
                    this.UpdateCE(rc, pid.GetRace(pid.RaceRepetitionsUsed));
            }
            
            // Ethnic code
            if (subject.EthnicGroup != null)
                foreach (var e in subject.EthnicGroup)
                    this.UpdateCE(e, pid.GetEthnicGroup(pid.EthnicGroupRepetitionsUsed));


            // Place of birth
            if (subject.BirthPlace != null)
                pid.BirthPlace.Value = subject.BirthPlace.Name;
        }

        /// <summary>
        /// Update an AD instance
        /// </summary>
        private void UpdateAD(AddressSet addr, NHapi.Model.V25.Datatype.XAD ad)
        {
            ad.AddressType.Value = ReverseLookup(ComponentUtility.AD_USE_MAP, addr.Use);
            
            foreach (var cmp in addr.Parts)
            {
                string cmpStr = ReverseLookup(ComponentUtility.AD_MAP, cmp.PartType);
                if (String.IsNullOrEmpty(cmpStr)) continue;
                int cmpNo = int.Parse(cmpStr);
                if (ad.Components[cmpNo - 1] is AbstractPrimitive)
                    (ad.Components[cmpNo - 1] as AbstractPrimitive).Value = cmp.AddressValue;
                else if (ad.Components[cmpNo - 1] is NHapi.Model.V25.Datatype.SAD)
                    (ad.Components[cmpNo - 1] as NHapi.Model.V25.Datatype.SAD).StreetOrMailingAddress.Value = cmp.AddressValue;
            }
        }

        /// <summary>
        /// Update a CE
        /// </summary>
        public void UpdateCE(CodeValue codeValue, NHapi.Model.V25.Datatype.CE ce)
        {
            ITerminologyService tservice = Context.GetService(typeof(ITerminologyService)) as ITerminologyService;
            codeValue = tservice.FillInDetails(codeValue);

            ce.Identifier.Value = codeValue.Code;
            ce.NameOfCodingSystem.Value = codeValue.CodeSystemName;
            
        }

        /// <summary>
        /// Update name
        /// </summary>
        public void UpdateXPN(NameSet name, NHapi.Model.V25.Datatype.XPN xpn)
        {
            xpn.NameTypeCode.Value = ReverseLookup(ComponentUtility.XPN_USE_MAP, name.Use);

            // IF SEAGULL!!! NOOO!!!! Hopefully nobody finds this thing
            foreach (var cmp in name.Parts)
            {
                string cmpStr = ReverseLookup(ComponentUtility.XPN_MAP, cmp.Type);
                int cmpNo = int.Parse(cmpStr);

                if (xpn.Components[cmpNo - 1] is AbstractPrimitive)
                {
                    if (cmp.Type == NamePart.NamePartType.Given && (!String.IsNullOrEmpty((xpn.Components[cmpNo - 1] as AbstractPrimitive).Value)))// given is taken so use other segment
                    {
                        if(!String.IsNullOrEmpty(xpn.SecondAndFurtherGivenNamesOrInitialsThereof.Value))
                            xpn.SecondAndFurtherGivenNamesOrInitialsThereof.Value += " ";
                        xpn.SecondAndFurtherGivenNamesOrInitialsThereof.Value += cmp.Value;
                    }
                    else
                        (xpn.Components[cmpNo - 1] as AbstractPrimitive).Value = cmp.Value;
                }
                else if (xpn.Components[cmpNo - 1] is NHapi.Model.V25.Datatype.FN)
                {
                    var fn = xpn.Components[cmpNo - 1] as NHapi.Model.V25.Datatype.FN;
                    if (!String.IsNullOrEmpty(fn.Surname.Value))
                        fn.Surname.Value += "-";
                    fn.Surname.Value += cmp.Value;
                }
            }
        }

        /// <summary>
        /// Reverse lookup dictionary
        /// </summary>
        public K ReverseLookup<K,V>(Dictionary<K, V> dictionary, V value)
        {
            foreach (var kv in dictionary)
                if (kv.Value.Equals(value))
                    return kv.Key;
            return default(K);
        }


        /// <summary>
        /// Update a CX instance
        /// </summary>
        public void UpdateCX(SVC.Core.DataTypes.DomainIdentifier altId, NHapi.Model.V25.Datatype.CX cx)
        {
            // Get oid data
            var oidData = this.m_config.OidRegistrar.FindData(altId.Domain);
            cx.AssigningAuthority.UniversalID.Value = altId.Domain ?? (oidData == null ? null :  oidData.Oid);
            cx.AssigningAuthority.UniversalIDType.Value = "ISO";
            cx.AssigningAuthority.NamespaceID.Value = oidData == null ? altId.AssigningAuthority : oidData.Attributes.Find(o => o.Key.Equals("AssigningAuthorityName")).Value;
            cx.IDNumber.Value = altId.Identifier;

            if (cx.AssigningAuthority.UniversalID.Value == this.m_config.OidRegistrar.GetOid("CR_CID").Oid) // AA
                cx.IdentifierTypeCode.Value = "PI";
            else
                cx.IdentifierTypeCode.Value = "PT";
        }

        /// <summary>
        /// Create RSP_K21 message
        /// </summary>
        internal NHapi.Base.Model.IMessage CreateRSP_K21(RegistryQueryResult result, RegistryQueryRequest filter, List<Everest.Connectors.IResultDetail> dtls)
        {
            // Return value
            var retVal = new NHapi.Model.V25.Message.RSP_K21();

            retVal.MSH.MessageType.MessageStructure.Value = "RSP_K21";

            var qak = retVal.QAK;
            var msa = retVal.MSA;
            var dsc = retVal.DSC;
            var qpd = retVal.QPD;

            qak.QueryTag.Value = result.QueryTag;
            msa.AcknowledgmentCode.Value = "AA";
            if (dtls.Exists(o => o.Type == Everest.Connectors.ResultDetailType.Error))
            {
                qak.QueryResponseStatus.Value = "AE";
                msa.AcknowledgmentCode.Value = "AE";
                foreach (var dtl in dtls)
                {
                    var err = retVal.GetStructure("ERR", retVal.currentReps("ERR")) as NHapi.Model.V25.Segment.ERR;
                    MessageUtil.UpdateERR(err, dtl, Context);
                }
            }
            else if (result.Results == null || result.Results.Count == 0)
            {
                qak.QueryResponseStatus.Value = "NF";
            }
            else
            {
                // Create the pid
                qak.QueryResponseStatus.Value = "OK";
                qak.HitCount.Value = result.TotalResults.ToString();
                qak.ThisPayload.Value = result.Results.Count.ToString();
                //qak.HitsRemaining.Value = (result.TotalResults - result.StartRecordNumber).ToString();
                foreach (RegistrationEvent res in result.Results)
                {
                    var pid = retVal.GetQUERY_RESPONSE(retVal.QUERY_RESPONSERepetitionsUsed);
                    UpdatePID(res, pid.PID, false);

                    var subject = res.FindComponent(SVC.Core.ComponentModel.HealthServiceRecordSiteRoleType.SubjectOf) as Person;
                    var relations = subject.FindAllComponents(SVC.Core.ComponentModel.HealthServiceRecordSiteRoleType.RepresentitiveOf);
                    
                    foreach (var rel in relations)
                        UpdateNK1(rel as PersonalRelationship, pid.GetNK1(pid.NK1RepetitionsUsed));
                    UpdateQRI(res, pid.QRI);
                    pid.PID.SetIDPID.Value = retVal.QUERY_RESPONSERepetitionsUsed.ToString();
                    
                }

                // DSC segment?
                if (result.TotalResults > result.Results.Count)
                {
                    retVal.DSC.ContinuationPointer.Value = result.ContinuationPtr;
                    retVal.DSC.ContinuationStyle.Value = "I";
                }
            }

            // Actual query paramaeters
            var regFilter = filter.QueryRequest.FindComponent(SVC.Core.ComponentModel.HealthServiceRecordSiteRoleType.SubjectOf) as RegistrationEvent;
            var personFilter = regFilter.FindComponent(SVC.Core.ComponentModel.HealthServiceRecordSiteRoleType.SubjectOf) as Person;

            qpd.QueryTag.Value = filter.QueryTag;
            qpd.MessageQueryName.Identifier.Value = "IHE PDQ Query";
            var terser = new Terser(retVal);
            int qpdRep = 0;
            if (personFilter.GenderCode != null)
            {
                terser.Set(String.Format("/QPD-3({0})-1", qpdRep), "@PID.8");
                terser.Set(String.Format("/QPD-3({0})-2", qpdRep++), personFilter.GenderCode);
            }
            if (personFilter.AlternateIdentifiers != null && personFilter.AlternateIdentifiers.Count > 0)
            {
                var altId = personFilter.AlternateIdentifiers[0];

                if (altId.Domain != null)
                {
                    terser.Set(String.Format("/QPD-3({0})-1", qpdRep), "@PID.3.4.2");
                    terser.Set(String.Format("/QPD-3({0})-2", qpdRep++), altId.Domain);
                    terser.Set(String.Format("/QPD-3({0})-1", qpdRep), "@PID.3.4.3");
                    terser.Set(String.Format("/QPD-3({0})-2", qpdRep++), "ISO");
                }
                if (altId.Identifier != null)
                {
                    terser.Set(String.Format("/QPD-3({0})-1", qpdRep), "@PID.3.1");
                    terser.Set(String.Format("/QPD-3({0})-2", qpdRep++), altId.Identifier);
                }
                if (altId.AssigningAuthority != null)
                {
                    terser.Set(String.Format("/QPD-3({0})-1", qpdRep), "@PID.3.4.1");
                    terser.Set(String.Format("/QPD-3({0})-2", qpdRep++), altId.AssigningAuthority);
                }
            }
            if (personFilter.Names != null && personFilter.Names.Count > 0)
            {
                var name = personFilter.Names[0];
                foreach (var pt in name.Parts)
                {
                    string pidNo = ComponentUtility.XPN_MAP.First(o => o.Value == pt.Type).Key;
                    terser.Set(String.Format("/QPD-3({0})-1", qpdRep), String.Format("@PID.5.{0}", pidNo));
                    terser.Set(String.Format("/QPD-3({0})-2", qpdRep++), pt.Value);
                }
                if (name.Use != NameSet.NameSetUse.Search)
                {
                    terser.Set(String.Format("/QPD-3({0})-1", qpdRep), "@PID.5.7");
                    terser.Set(String.Format("/QPD-3({0})-2", qpdRep++), ComponentUtility.XPN_USE_MAP.First(o => o.Value == name.Use).Key);
                }
            }
            if (personFilter.BirthTime != null)
            {
                terser.Set(String.Format("/QPD-3({0})-1", qpdRep), "@PID.7");
                terser.Set(String.Format("/QPD-3({0})-2", qpdRep++), new TS(personFilter.BirthTime.Value).Value);
            }
            if (personFilter.Addresses != null && personFilter.Addresses.Count > 0)
            {
                var addr = personFilter.Addresses[0];
                foreach (var pt in addr.Parts)
                {
                    string pidNo = ComponentUtility.AD_MAP.First(o => o.Value == pt.PartType).Key;
                    terser.Set(String.Format("/QPD-3({0})-1", qpdRep), String.Format("@PID.11.{0}", pidNo));
                    terser.Set(String.Format("/QPD-3({0})-2", qpdRep++), pt.AddressValue);
                }
                if (addr.Use != AddressSet.AddressSetUse.Search)
                {
                    terser.Set(String.Format("/QPD-3({0})-1", qpdRep), "@PID.11.7");
                    terser.Set(String.Format("/QPD-3({0})-2", qpdRep++), ComponentUtility.AD_USE_MAP.First(o => o.Value == addr.Use).Key);
                }
            }
            var ma = personFilter.FindComponent(SVC.Core.ComponentModel.HealthServiceRecordSiteRoleType.RepresentitiveOf) as PersonalRelationship;
            if(ma != null)
            {
                if(ma.LegalName != null)
                {
                    foreach (var pt in ma.LegalName.Parts)
                    {
                        string pidNo = ComponentUtility.XPN_MAP.First(o => o.Value == pt.Type).Key;
                        terser.Set(String.Format("/QPD-3({0})-1", qpdRep), String.Format("@PID.6.{0}", pidNo));
                        terser.Set(String.Format("/QPD-3({0})-2", qpdRep++), pt.Value);
                    }
                }
                if(ma.AlternateIdentifiers != null && ma.AlternateIdentifiers.Count > 0)
                {
                    var altId = ma.AlternateIdentifiers[0];

                    if (altId.Domain != null)
                    {
                        terser.Set(String.Format("/QPD-3({0})-1", qpdRep), "@PID.21.4.2");
                        terser.Set(String.Format("/QPD-3({0})-2", qpdRep++), altId.Domain);
                        terser.Set(String.Format("/QPD-3({0})-1", qpdRep), "@PID.21.4.3");
                        terser.Set(String.Format("/QPD-3({0})-2", qpdRep++), "ISO");
                    }
                    if (altId.Identifier != null)
                    {
                        terser.Set(String.Format("/QPD-3({0})-1", qpdRep), "@PID.21.1");
                        terser.Set(String.Format("/QPD-3({0})-2", qpdRep++), altId.Identifier);
                    }
                    if (altId.AssigningAuthority != null)
                    {
                        terser.Set(String.Format("/QPD-3({0})-1", qpdRep), "@PID.21.4.1");
                        terser.Set(String.Format("/QPD-3({0})-2", qpdRep++), altId.AssigningAuthority);
                    }

                }
            }
            return retVal;
        }

        /// <summary>
        /// Update the NK1 segment
        /// </summary>
        private void UpdateNK1(PersonalRelationship subject, NK1 nk1)
        {
            if (subject.AlternateIdentifiers != null)
                foreach(var altId in subject.AlternateIdentifiers)
                    UpdateCX(altId, nk1.GetNextOfKinAssociatedPartySIdentifiers(nk1.NextOfKinAssociatedPartySIdentifiersRepetitionsUsed));

            // Birth time
            if (subject.BirthTime != null)
            {
                MARC.Everest.DataTypes.TS ts = new Everest.DataTypes.TS(subject.BirthTime.Value, ReverseLookup(ComponentUtility.TS_PREC_MAP, subject.BirthTime.Precision));
                nk1.DateTimeOfBirth.Time.Value = MARC.Everest.Connectors.Util.ToWireFormat(ts);
            }

            if(subject.GenderCode != null)
                nk1.AdministrativeSex.Value = subject.GenderCode;

            if (subject.LegalName != null)
                UpdateXPN(subject.LegalName, nk1.GetName(0));

            if (subject.PerminantAddress != null)
                UpdateAD(subject.PerminantAddress, nk1.GetAddress(0));

            if (subject.RelationshipKind != null)
                nk1.Relationship.Identifier.Value = subject.RelationshipKind;

            if(subject.TelecomAddresses != null)
                foreach(var tel in subject.TelecomAddresses)
                    if (tel.Use == "HP" && tel.Value.StartsWith("tel"))
                        MessageUtil.XTNFromTel(new TEL()
                        {
                            Value = tel.Value,
                            Use = MARC.Everest.Connectors.Util.Convert<SET<CS<TelecommunicationAddressUse>>>(tel.Use),
                            Capabilities = MARC.Everest.Connectors.Util.Convert<SET<CS<TelecommunicationCabability>>>(tel.Capability)
                        }, nk1.GetContactPersonSTelephoneNumber(nk1.ContactPersonSTelephoneNumberRepetitionsUsed));
                    else if (tel.Use == "HP")
                        nk1.GetPhoneNumber(nk1.ContactPersonSTelephoneNumberRepetitionsUsed).EmailAddress.Value = tel.Value;
                    else if (tel.Use == "WP" && tel.Value.StartsWith("tel"))
                        MessageUtil.XTNFromTel(new TEL()
                        {
                            Value = tel.Value,
                            Use = MARC.Everest.Connectors.Util.Convert<SET<CS<TelecommunicationAddressUse>>>(tel.Use),
                            Capabilities = MARC.Everest.Connectors.Util.Convert<SET<CS<TelecommunicationCabability>>>(tel.Capability)
                        }, nk1.GetContactPersonSTelephoneNumber(nk1.ContactPersonSTelephoneNumberRepetitionsUsed));
                    else if (tel.Use == "WP")
                        nk1.GetPhoneNumber(nk1.ContactPersonSTelephoneNumberRepetitionsUsed).EmailAddress.Value = tel.Value;


        }

        /// <summary>
        /// Update QRI
        /// </summary>
        private void UpdateQRI(RegistrationEvent res, NHapi.Model.V25.Segment.QRI qri)
        {
            var subject = res.FindComponent(SVC.Core.ComponentModel.HealthServiceRecordSiteRoleType.SubjectOf) as Person;
            var confidence = subject.FindComponent(SVC.Core.ComponentModel.HealthServiceRecordSiteRoleType.CommentOn | SVC.Core.ComponentModel.HealthServiceRecordSiteRoleType.ComponentOf) as QueryParameters;

            if (confidence != null)
            {
                if (confidence.MatchingAlgorithm == MatchAlgorithm.Soundex)
                    qri.AlgorithmDescriptor.Identifier.Value = "Soundex";
                else if (confidence.MatchingAlgorithm == MatchAlgorithm.Variant)
                    qri.AlgorithmDescriptor.Identifier.Value = "Variant";

                if (confidence.MatchingAlgorithm == MatchAlgorithm.Unspecified) // identifier match
                    qri.GetMatchReasonCode(qri.MatchReasonCodeRepetitionsUsed).Value = "ID";
                else if (confidence.MatchingAlgorithm == MatchAlgorithm.Soundex) // soundex
                    qri.GetMatchReasonCode(qri.MatchReasonCodeRepetitionsUsed).Value = "NP";
                else if (confidence.MatchingAlgorithm == MatchAlgorithm.Exact) // match
                    qri.GetMatchReasonCode(qri.MatchReasonCodeRepetitionsUsed).Value = "NA";
                else if (confidence.MatchingAlgorithm == MatchAlgorithm.Variant) // variant
                    qri.GetMatchReasonCode(qri.MatchReasonCodeRepetitionsUsed).Value = "VAR";

                // Confidence
                qri.CandidateConfidence.Value = confidence.Confidence.ToString("0.##");
            }
        }
    }
}
