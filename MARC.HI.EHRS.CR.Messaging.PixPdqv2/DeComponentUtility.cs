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
        internal NHapi.Model.V25.Message.RSP_K23 CreateRSP_K23(QueryResultData result, List<Everest.Connectors.IResultDetail> dtls)
        {
            // Return value
            var retVal = new NHapi.Model.V25.Message.RSP_K23();

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
            else if (result.Results == null || result.Results.Length == 0)
            {
                qak.QueryResponseStatus.Value = "NF";
            }
            else
            {
                // Create the pid
                qak.QueryResponseStatus.Value = "OK";
                UpdatePID(result.Results[0], retVal.QUERY_RESPONSE.PID, true);
            }

            return retVal;
        }

        /// <summary>
        /// Update the specified PID
        /// </summary>
        private void UpdatePID(Core.ComponentModel.RegistrationEvent registrationEvent, NHapi.Model.V25.Segment.PID pid, bool summaryOnly)
        {

            var subject = registrationEvent.FindComponent(SVC.Core.ComponentModel.HealthServiceRecordSiteRoleType.SubjectOf) as Person;

            
            // Alternate identifiers
            foreach (var altId in subject.AlternateIdentifiers)
            {
                var id = pid.GetPatientIdentifierList(pid.PatientIdentifierListRepetitionsUsed);
                UpdateCX(altId, id);
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

            // Language
            if(subject.Language != null)
                foreach(var lang in subject.Language)
                    if (lang.Type == LanguageType.Fluency)
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
                (ad.Components[cmpNo-1] as AbstractPrimitive).Value = cmp.AddressValue;
            }
        }

        /// <summary>
        /// Update a CE
        /// </summary>
        private void UpdateCE(CodeValue codeValue, NHapi.Model.V25.Datatype.CE ce)
        {
            ITerminologyService tservice = Context.GetService(typeof(ITerminologyService)) as ITerminologyService;
            codeValue = tservice.FillInDetails(codeValue);

            ce.Identifier.Value = codeValue.Code;
            ce.NameOfCodingSystem.Value = codeValue.CodeSystemName;
            
        }

        /// <summary>
        /// Update name
        /// </summary>
        private void UpdateXPN(NameSet name, NHapi.Model.V25.Datatype.XPN xpn)
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
        private K ReverseLookup<K,V>(Dictionary<K, V> dictionary, V value)
        {
            foreach (var kv in dictionary)
                if (kv.Value.Equals(value))
                    return kv.Key;
            return default(K);
        }


        /// <summary>
        /// Update a CX instance
        /// </summary>
        private void UpdateCX(SVC.Core.DataTypes.DomainIdentifier altId, NHapi.Model.V25.Datatype.CX cx)
        {
            // Get oid data
            var oidData = this.m_config.OidRegistrar.FindData(altId.Domain);
            cx.AssigningAuthority.UniversalID.Value = oidData.Oid;
            cx.AssigningAuthority.UniversalIDType.Value = "ISO";
            cx.AssigningAuthority.NamespaceID.Value = oidData.Attributes.Find(o => o.Key.Equals("AssigningAuthorityName")).Value;
            cx.IDNumber.Value = altId.Identifier;
        }

        /// <summary>
        /// Create RSP_K21 message
        /// </summary>
        internal NHapi.Base.Model.IMessage CreateRSP_K21(QueryResultData result, List<Everest.Connectors.IResultDetail> dtls)
        {
            // Return value
            var retVal = new NHapi.Model.V25.Message.RSP_K21();

            var qak = retVal.QAK;
            var msa = retVal.MSA;
            var dsc = retVal.DSC;

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
            else if (result.Results == null || result.Results.Length == 0)
            {
                qak.QueryResponseStatus.Value = "NF";
            }
            else
            {
                // Create the pid
                qak.QueryResponseStatus.Value = "OK";
                foreach (var res in result.Results)
                {
                    var pid = retVal.GetQUERY_RESPONSE(retVal.QUERY_RESPONSERepetitionsUsed);
                    UpdatePID(res, pid.PID, false);
                    UpdateQRI(res, pid.QRI);
                    pid.PID.SetIDPID.Value = retVal.QUERY_RESPONSERepetitionsUsed.ToString();
                }

                // DSC segment?
                if (result.TotalResults > result.Results.Length)
                {
                    retVal.DSC.ContinuationPointer.Value = result.ContinuationPtr;
                    retVal.DSC.ContinuationStyle.Value = "I";
                }
            }

            return retVal;
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
