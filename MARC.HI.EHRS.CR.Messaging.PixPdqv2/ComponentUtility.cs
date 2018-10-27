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
 * Date: 5-12-2012
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MARC.HI.EHRS.SVC.Core.Services;
using MARC.HI.EHRS.SVC.Core.DataTypes;
using MARC.Everest.Connectors;
using MARC.HI.EHRS.CR.Core.ComponentModel;
using MARC.HI.EHRS.SVC.Core.ComponentModel;
using NHapi.Base.Model;
using NHapi.Base.Util;
using MARC.HI.EHRS.SVC.Core.ComponentModel.Components;
using System.Text.RegularExpressions;
using MARC.HI.EHRS.CR.Core;
using System.Diagnostics;
using MARC.HI.EHRS.CR.Core.Services;
using MARC.HI.EHRS.CR.Core.Data;
using NHapi.Model.V231.Segment;
using NHapi.Base.Parser;

namespace MARC.HI.EHRS.CR.Messaging.PixPdqv2
{
    /// <summary>
    /// Component utility
    /// </summary>
    public class ComponentUtility : IUsesHostContext
    {
        #region IUsesHostContext Members

        // Host context
        private IServiceProvider m_context;

        // Localization service
        private ILocalizationService m_locale;

        // Config
        private ISystemConfigurationService m_config;

        // XPN Maps
        public static readonly Dictionary<String, NamePart.NamePartType?> XPN_MAP = new Dictionary<string, NamePart.NamePartType?>()
        {
            { "1",  NamePart.NamePartType.Family },
            { "2", NamePart.NamePartType.Given },
            { "3", NamePart.NamePartType.Given },
            { "4", NamePart.NamePartType.Suffix },
            { "5", NamePart.NamePartType.Prefix },
            { "6", NamePart.NamePartType.Prefix }
        };

        // AD Maps
        public static readonly Dictionary<String, AddressPart.AddressPartType?> AD_MAP = new Dictionary<string, AddressPart.AddressPartType?>()
        {
            { "1", AddressPart.AddressPartType.AddressLine },
            { "2", AddressPart.AddressPartType.AdditionalLocator },
            { "3", AddressPart.AddressPartType.City },
            { "4", AddressPart.AddressPartType.State },
            { "5", AddressPart.AddressPartType.PostalCode },
            { "6", AddressPart.AddressPartType.Country },
            { "8", AddressPart.AddressPartType.DeliveryInstallationQualifier },
            { "9", AddressPart.AddressPartType.County },
            { "10", AddressPart.AddressPartType.CensusTract }
        };

        // XPN Type Maps
        public static readonly Dictionary<String, NameSet.NameSetUse?> XPN_USE_MAP = new Dictionary<string, NameSet.NameSetUse?>()
        {
            { "A", NameSet.NameSetUse.Pseudonym },
            { "B", NameSet.NameSetUse.OfficialRecord },
            { "C", NameSet.NameSetUse.OfficialRecord },
            { "D", NameSet.NameSetUse.OfficialRecord },
            { "I", NameSet.NameSetUse.License },
            { "L", NameSet.NameSetUse.Legal },
            { "M", NameSet.NameSetUse.MaidenName },
            { "N", NameSet.NameSetUse.Artist },
            { "T", NameSet.NameSetUse.Indigenous },
            { String.Empty, NameSet.NameSetUse.Search }
        };

        // AD Type maps
        public static readonly Dictionary<String, AddressSet.AddressSetUse?> AD_USE_MAP = new Dictionary<string, AddressSet.AddressSetUse?>()
        {
            { "BA", AddressSet.AddressSetUse.BadAddress },
            { "C", AddressSet.AddressSetUse.TemporaryAddress },
            { "B", AddressSet.AddressSetUse.WorkPlace },
            { "H", AddressSet.AddressSetUse.HomeAddress },
            { "L", AddressSet.AddressSetUse.PrimaryHome },
            { "M",  AddressSet.AddressSetUse.PostalAddress },
            { "O", AddressSet.AddressSetUse.Public },
            { "P", AddressSet.AddressSetUse.Direct }
        };
        
        // TS Maps
        public static readonly Dictionary<MARC.Everest.DataTypes.DatePrecision, string> TS_PREC_MAP = new Dictionary<MARC.Everest.DataTypes.DatePrecision, string>()
                { 
                       { MARC.Everest.DataTypes.DatePrecision.Day, "D" },
                       { MARC.Everest.DataTypes.DatePrecision.Full, "F" },
                       { MARC.Everest.DataTypes.DatePrecision.Hour, "H" },
                       { MARC.Everest.DataTypes.DatePrecision.Minute, "m" },
                       { MARC.Everest.DataTypes.DatePrecision.Month, "M" }, 
                       { MARC.Everest.DataTypes.DatePrecision.Second, "S" },
                       { MARC.Everest.DataTypes.DatePrecision.Year, "Y" }
                };

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
        /// Create domain identifier
        /// </summary>
        internal SVC.Core.DataTypes.DomainIdentifier CreateDomainIdentifier(NHapi.Model.V25.Datatype.HD id, List<Everest.Connectors.IResultDetail> dtls)
        {
            if (id == null)
                return null;

            DomainIdentifier retVal = new DomainIdentifier();
            if (!String.IsNullOrEmpty(id.NamespaceID.Value))
                retVal.Identifier = id.NamespaceID.Value;
            if (!String.IsNullOrEmpty(id.UniversalID.Value))
                retVal.Domain = id.UniversalID.Value;
            if (!String.IsNullOrEmpty(id.UniversalIDType.Value) && id.UniversalIDType.Value != "ISO")
                dtls.Add(new NotImplementedResultDetail(ResultDetailType.Warning, m_locale.GetString("MSGW016"), null, null));
            if (!String.IsNullOrEmpty(retVal.Domain) && !MARC.Everest.DataTypes.II.IsValidOidFlavor(new MARC.Everest.DataTypes.II(retVal.Domain)))
                dtls.Add(new FormalConstraintViolationResultDetail(ResultDetailType.Error, m_locale.GetString("MSGE05B"), null, null));
            return retVal;
        }

        /// <summary>
        /// Create domain identifier
        /// </summary>
        internal DomainIdentifier CreateDomainIdentifier(NHapi.Model.V231.Datatype.HD id, List<IResultDetail> dtls)
        {
            DomainIdentifier retVal = new DomainIdentifier();
            if (!String.IsNullOrEmpty(id.NamespaceID.Value))
                retVal.Identifier = id.NamespaceID.Value;
            if (!String.IsNullOrEmpty(id.UniversalID.Value))
                retVal.Domain = id.UniversalID.Value;
            if (!String.IsNullOrEmpty(id.UniversalIDType.Value) && id.UniversalIDType.Value != "ISO")
                dtls.Add(new NotImplementedResultDetail(ResultDetailType.Warning, m_locale.GetString("MSGW016"), null, null));
            if (!String.IsNullOrEmpty(retVal.Domain) && !MARC.Everest.DataTypes.II.IsValidOidFlavor(new MARC.Everest.DataTypes.II(retVal.Domain)))
                dtls.Add(new FormalConstraintViolationResultDetail(ResultDetailType.Error, m_locale.GetString("MSGE05B"), null, null));
            return retVal;
        }

        /// <summary>
        /// Create domain identifier from a cx
        /// </summary>
        /// <param name="rid"></param>
        /// <param name="dtls"></param>
        /// <returns></returns>
        private DomainIdentifier CreateDomainIdentifier(NHapi.Model.V25.Datatype.CX id, List<IResultDetail> dtls)
        {
            DomainIdentifier retVal = new DomainIdentifier();
            if (!String.IsNullOrEmpty(id.IDNumber.Value))
                retVal.Identifier = id.IDNumber.Value;

            // Assigning authority
            var addlData = CreateDomainIdentifier(id.AssigningAuthority, dtls);
            if (addlData == null)
            {
                dtls.Add(new RequiredElementMissingResultDetail(ResultDetailType.Error, m_locale.GetString("MSGE06F"), null));
                return retVal;
            }
            if (!String.IsNullOrEmpty(addlData.Domain))
                retVal.Domain = addlData.Domain;
            if (!String.IsNullOrEmpty(addlData.Identifier))
            {
                retVal.AssigningAuthority = addlData.Identifier;
                var oid = this.m_config.OidRegistrar.FindData(o => o.Attributes.Exists(k=>k.Key.Equals("AssigningAuthorityName")) && addlData.Identifier.Equals(o.Attributes.Find(k=>k.Key.Equals("AssigningAuthorityName")).Value));
                if (oid == null && String.IsNullOrEmpty(retVal.Domain))
                    retVal.Domain = String.Empty;
                else if (oid == null)
                    ;
                else if (String.IsNullOrEmpty(retVal.Domain))
                    retVal.Domain = oid.Oid;
                else if (retVal.Domain != oid.Oid)
                    dtls.Add(new FixedValueMisMatchedResultDetail(retVal.Domain, oid.Oid, false, "CX.3"));

            }

            return retVal;
        }

        /// <summary>
        /// Create domain identifier from a cx
        /// </summary>
        /// <param name="rid"></param>
        /// <param name="dtls"></param>
        /// <returns></returns>
        private DomainIdentifier CreateDomainIdentifier(NHapi.Model.V231.Datatype.CX id, List<IResultDetail> dtls)
        {
            DomainIdentifier retVal = new DomainIdentifier();
            if (!String.IsNullOrEmpty(id.ID.Value))
                retVal.Identifier = id.ID.Value;

            // Assigning authority
            var addlData = CreateDomainIdentifier(id.AssigningAuthority, dtls);
            if (!String.IsNullOrEmpty(addlData.Domain))
                retVal.Domain = addlData.Domain;
            if (!String.IsNullOrEmpty(addlData.Identifier))
            {
                retVal.AssigningAuthority = addlData.Identifier;
                var oid = this.m_config.OidRegistrar.FindData(o => o.Attributes.Exists(k=>k.Key.Equals("AssigningAuthorityName")) && addlData.Identifier.Equals(o.Attributes.Find(k=>k.Key.Equals("AssigningAuthorityName")).Value));
                if (oid == null)
                    ;
                else if (String.IsNullOrEmpty(retVal.Domain))
                    retVal.Domain = oid.Oid;
                else if (retVal.Domain != oid.Oid)
                    dtls.Add(new FixedValueMisMatchedResultDetail(retVal.Domain, oid.Oid, false, "CX.3"));

            }

            return retVal;
        }

        /// <summary>
        /// Create query components for the specified request
        /// </summary>
        internal RegistryQueryRequest CreateQueryComponents(NHapi.Model.V25.Message.QBP_Q21 request, List<IResultDetail> dtls)
        {
            // Validate the segments
            var msh = request.MSH;
            var qpd = request.QPD;
            var rcp = request.RCP;

            // Message header validation code
            if (msh == null)
                dtls.Add(new MandatoryElementMissingResultDetail(ResultDetailType.Error, this.m_locale.GetString("MSGE05C"), null));
            else if(msh.MessageType.MessageStructure.Value != "QBP_Q21")
                dtls.Add(new FixedValueMisMatchedResultDetail(msh.MessageType.MessageStructure.Value, "QBP_Q21", false, "MSH^9"));

            // Query Parameter Definition QPD code
            if(qpd == null)
                dtls.Add(new MandatoryElementMissingResultDetail(ResultDetailType.Error, this.m_locale.GetString("MSGE05B"), null));
            //else if(qpd.MessageQueryName.Identifier.Value != "IHE PIX Query")
            //    dtls.Add(new FixedValueMisMatchedResultDetail(qpd.MessageQueryName.Identifier.Value, "IHE PIX Query", false, "QPD^1"));

            // Return value
            RegistryQueryRequest retVal = new RegistryQueryRequest()
            {
                QueryRequest = new QueryEvent()
                {
                    Timestamp = DateTime.Now,
                },
                Limit = 1,
                Offset = 0,
                ResponseMessageType = "RSP_K23"
            };

            // Add the author (null author role) to the return value (policy enforcement doesn't freak out)
            retVal.QueryRequest.Add(new HealthcareParticipant()
            {
                Classifier = HealthcareParticipant.HealthcareParticipantType.Organization | HealthcareParticipant.HealthcareParticipantType.Person,
                AlternateIdentifiers = new List<DomainIdentifier>() {
                    CreateDomainIdentifier(msh.SendingFacility, dtls)
                },
                LegalName = new NameSet()
                {
                    Parts = new List<NamePart>() {
                        new NamePart() { 
                            Type = NamePart.NamePartType.None,
                            Value = msh.SendingFacility.NamespaceID.Value
                        }
                    }
                }
            });


            // Registration event which is the informant to the 
            retVal.QueryRequest.Add(new RegistrationEvent()
            {
                EventClassifier = RegistrationEventType.Any,
                AlternateIdentifier = new VersionedDomainIdentifier()
                {
                    Identifier = msh.MessageControlID.Value
                }
            }, "RSN", HealthServiceRecordSiteRoleType.ReasonFor, null);

            // Filter data
            RegistrationEvent filter = new RegistrationEvent(){ EventClassifier = RegistrationEventType.Query };
            retVal.QueryRequest.Add(filter, "FLT", HealthServiceRecordSiteRoleType.SubjectOf, null);
            Person subjectOf = new Person();
            filter.Add(subjectOf, "SUBJ", HealthServiceRecordSiteRoleType.SubjectOf, null);

            // Get the PID qualifier
            var pid = qpd.GetField(3, 0) as NHapi.Base.Model.Varies;
            if (pid == null || pid.Data == null)
                dtls.Add(new MandatoryElementMissingResultDetail(ResultDetailType.Error, this.m_locale.GetString("MSGE05F"), "QPD^3"));
            else
            {
                subjectOf.AlternateIdentifiers = new List<DomainIdentifier>();
                var tcx = new NHapi.Model.V25.Datatype.CX(request);
                NHapi.Base.Util.DeepCopy.copy(pid.Data as GenericComposite, tcx);

                var dmn = CreateDomainIdentifier(tcx, dtls);

                if (String.IsNullOrEmpty(dmn.Domain) || m_config.OidRegistrar.FindData(dmn.Domain) == null || !m_config.OidRegistrar.FindData(dmn.Domain).Attributes.Exists(p=>p.Key.Equals("AssigningAuthorityName")))
                    dtls.Add(new UnrecognizedPatientDomainResultDetail(this.m_locale));
                subjectOf.AlternateIdentifiers.Add(dmn);
                
            }

            // Return domains
            var retDomain = qpd.GetField(4);
            foreach(Varies rd in retDomain)
            {
                var rid = new NHapi.Model.V25.Datatype.CX(request);
                DeepCopy.copy(rd.Data as GenericComposite, rid);
                if (rid != null)
                {
                    if (retVal.TargetDomain == null) retVal.TargetDomain = new List<DomainIdentifier>();
                    var dmn = CreateDomainIdentifier(rid, dtls);
                    if (String.IsNullOrEmpty(dmn.Domain) || m_config.OidRegistrar.FindData(dmn.Domain) == null || !m_config.OidRegistrar.FindData(dmn.Domain).Attributes.Exists(p=>p.Key.Equals("AssigningAuthorityName")))
                        dtls.Add(new UnrecognizedTargetDomainResultDetail(this.m_locale, "QPD^1^4^"));
                    retVal.TargetDomain.Add(dmn);
                }
            }

            // Construct additional query data
            var tag = qpd.GetField(2, 0) as NHapi.Model.V25.Datatype.ST;
            retVal.QueryId = Guid.NewGuid().ToString();
            if (tag != null)
                retVal.QueryTag = tag.Value;
            else
                retVal.QueryTag = retVal.QueryId;


            retVal.Limit = 100;
            retVal.IsSummary = true;
            retVal.OriginalMessageQueryId = msh.MessageControlID.Value;
            
            if (dtls.Exists(o => o.Type == ResultDetailType.Error))
                return null;

            return retVal;
        }

        /// <summary>
        /// Create query components for the PDQ message
        /// </summary>
        internal RegistryQueryRequest CreateQueryComponentsPdq(NHapi.Model.V25.Message.QBP_Q21 request, List<IResultDetail> dtls)
        {
            // Validate the segments
            var msh = request.MSH;
            var qpd = request.QPD;
            var rcp = request.RCP;
            var dsc = request.DSC;

            // Message header validation code
            if (msh == null)
                dtls.Add(new MandatoryElementMissingResultDetail(ResultDetailType.Error, this.m_locale.GetString("MSGE05C"), null));
            else if (msh.MessageType.MessageStructure.Value != "QBP_Q21")
                dtls.Add(new FixedValueMisMatchedResultDetail(msh.MessageType.MessageStructure.Value, "QBP_Q21", false, "MSH^9"));

            // Query Parameter Definition QPD code
            if (qpd == null)
                dtls.Add(new MandatoryElementMissingResultDetail(ResultDetailType.Error, this.m_locale.GetString("MSGE05B"), null));
            else if (qpd.MessageQueryName.Identifier.Value != "IHE PDQ Query")
                dtls.Add(new FixedValueMisMatchedResultDetail(qpd.MessageQueryName.Identifier.Value, "IHE PDQ Query", false, "QPD^1"));

            
            // Return value
            RegistryQueryRequest retVal = new RegistryQueryRequest()
            {
                QueryRequest = new QueryEvent()
                {
                    Timestamp = DateTime.Now,
                },
                ResponseMessageType = "RSP_K21",
                Originator = String.Format("{0}|{1}", msh.SendingApplication.NamespaceID.Value, msh.SendingFacility.NamespaceID.Value)
            };

            // Reason for querying
            retVal.QueryRequest.Add(new RegistrationEvent()
            {
                EventClassifier = RegistrationEventType.Any,
                Timestamp = DateTime.Now,
                AlternateIdentifier = new VersionedDomainIdentifier()
                {
                    Identifier = msh.MessageControlID.Value
                }
            }, "RSON", HealthServiceRecordSiteRoleType.ReasonFor, null);

            // Add the author (null author role) to the return value (policy enforcement doesn't freak out)
            retVal.QueryRequest.Add(new HealthcareParticipant()
            {
                Classifier = HealthcareParticipant.HealthcareParticipantType.Organization | HealthcareParticipant.HealthcareParticipantType.Person,
                AlternateIdentifiers = new List<DomainIdentifier>() {
                    CreateDomainIdentifier(msh.SendingFacility, dtls)
                },
                LegalName = new NameSet()
                {
                    Parts = new List<NamePart>() {
                        new NamePart() { 
                            Type = NamePart.NamePartType.None,
                            Value = msh.SendingFacility.NamespaceID.Value
                        }
                    }
                }
            });

            // Filter data
            RegistrationEvent filter = new RegistrationEvent() { EventClassifier = RegistrationEventType.Query };
            retVal.QueryRequest.Add(filter, "FLT", HealthServiceRecordSiteRoleType.SubjectOf, null);
            Person subjectOf = new Person();
            filter.Add(subjectOf, "SUBJ", HealthServiceRecordSiteRoleType.SubjectOf, null);

            // Get the PID qualifier
            var qps = qpd.GetField(3);
            if (qps  == null)
                dtls.Add(new MandatoryElementMissingResultDetail(ResultDetailType.Error, this.m_locale.GetString("MSGE05F"), "QPD^3"));
            else
            {

                DomainIdentifier altId = null;
                NameSet name = null;
                AddressSet address = null;
                NameSet mothersName = null;
                DomainIdentifier mothersId = null;

                // Query parameter
                foreach (var qp in qps)
                {
                    var qpvar = qp as Varies;
                    if (qpvar == null)
                        dtls.Add(new ResultDetail(ResultDetailType.Error, m_locale.GetString("MSGE060"), String.Format("QPD^3^{0}", Array.IndexOf(qps, qp) + 1), null));
                    var qpcomp = qpvar.Data as GenericComposite;
                    if (qpvar == null)
                        dtls.Add(new ResultDetail(ResultDetailType.Error, m_locale.GetString("MSGE060"), String.Format("QPD^3^{0}", Array.IndexOf(qps, qp) + 1), null));

                    // Try to parse the parameters
                    try
                    {
                        string qip1 = ((qpcomp.Components[0] as Varies).Data).ToString(),
                            qip2 = ((qpcomp.Components[1] as Varies).Data).ToString();
                        
                        // Get the name of the components to be queried
                        Regex paramSeg = new Regex(@"@(P[IV][D1]\.?\d+)\.?(\d+)?\.?(\d+)?");
                        var match = paramSeg.Match(qip1);
                        if (match.Success)
                        {
                            // Extract data
                            string segmentName = match.Groups[1].Value,
                                componentNo = String.Empty,
                                subComponentNo = String.Empty;
                            if (match.Groups.Count > 2)
                                componentNo = match.Groups[2].Value;
                            if (match.Groups.Count > 3)
                                subComponentNo = match.Groups[3].Value;

                            // Determine the segment
                            switch (segmentName)
                            {
                                case "PID.3":
                                case "PID.18":
                                    // Alternate identifier
                                    if (altId == null)
                                        altId = new DomainIdentifier();

                                    if (componentNo == "1")
                                        altId.Identifier = qip2;
                                    else if (componentNo == "4")
                                        switch (subComponentNo)
                                        {
                                            case "1":
                                            case "":
                                                altId.AssigningAuthority = qip2;
                                                break;
                                            case "2":
                                                altId.Domain = qip2;
                                                break;
                                            case "3":
                                                if (qip2 != "ISO")
                                                {
                                                    dtls.Add(new NotSupportedChoiceResultDetail(ResultDetailType.Error, m_locale.GetString("MSGE05B"), String.Format("QPD^3^{0}", Array.IndexOf(qps, qp) + 1), null));
                                                    return null;
                                                }
                                                break;
                                        }
                                    else
                                        throw new ArgumentException();
                                    break;
                                case "PID.21":
                                    // Alternate identifier
                                    if (mothersId == null)
                                        mothersId = new DomainIdentifier();

                                    if (componentNo == "1")
                                        mothersId.Identifier = qip2;
                                    else if (componentNo == "4")
                                        switch (subComponentNo)
                                        {
                                            case "1":
                                            case "":
                                                mothersId.AssigningAuthority = qip2;
                                                break;
                                            case "2":
                                                mothersId.Domain = qip2;
                                                break;
                                            case "3":
                                                if (qip2 != "ISO")
                                                {
                                                    dtls.Add(new NotSupportedChoiceResultDetail(ResultDetailType.Error, m_locale.GetString("MSGE05B"), String.Format("QPD^3^{0}", Array.IndexOf(qps, qp) + 1), null));
                                                    return null;
                                                }
                                                break;
                                        }
                                    else
                                        throw new ArgumentException();
                                    break;
                                case "PID.5":
                                    // Name
                                    if (name == null)
                                        name = new NameSet() { Use = NameSet.NameSetUse.Search, Parts = new List<NamePart>() };

                                    // Naming part? 
                                    if (componentNo.CompareTo("7") < 0)
                                    {
                                        NamePart np = new NamePart() { Value = qip2 };
                                        NamePart.NamePartType? typ = null;
                                        if (XPN_MAP.TryGetValue(componentNo, out typ))
                                            np.Type = typ.Value;
                                        else
                                            np.Type = NamePart.NamePartType.None;
                                        name.Parts.Add(np);
                                    }
                                    else if (componentNo.Equals("7"))
                                    {
                                        NameSet.NameSetUse? use = null;
                                        if (XPN_USE_MAP.TryGetValue(qip2, out use))
                                            name.Use = use.Value;
                                        else
                                            dtls.Add(new NotSupportedChoiceResultDetail(ResultDetailType.Warning, m_locale.GetString("MSGW017"), String.Format("QPD^3^{0}", Array.IndexOf(qps, qp) + 1), null)); 
                                    }
                                    else
                                        throw new ArgumentException();
                                    break;
                                case "PID.6":
                                    // Mothers
                                    if (mothersName == null)
                                        mothersName = new NameSet() { Use = NameSet.NameSetUse.Search, Parts = new List<NamePart>() };

                                    // Naming part? 
                                    if (componentNo.CompareTo("7") < 0)
                                    {
                                        NamePart np = new NamePart() { Value = qip2 };
                                        NamePart.NamePartType? typ = null;
                                        if (XPN_MAP.TryGetValue(componentNo, out typ))
                                            np.Type = typ.Value;
                                        else
                                            np.Type = NamePart.NamePartType.None;
                                        mothersName.Parts.Add(np);
                                    }
                                    else if (componentNo.Equals("7"))
                                    {
                                        NameSet.NameSetUse? use = null;
                                        if (XPN_USE_MAP.TryGetValue(qip2, out use))
                                            mothersName.Use = use.Value;
                                        else
                                            dtls.Add(new NotSupportedChoiceResultDetail(ResultDetailType.Warning, m_locale.GetString("MSGW017"), String.Format("QPD^3^{0}", Array.IndexOf(qps, qp) + 1), null));
                                    }
                                    else
                                        throw new ArgumentException();
                                    break;

                                case "PID.7":
                                    // Birth
                                    object ts = null;
                                    if (MARC.Everest.Connectors.Util.TryFromWireFormat(qip2, typeof(MARC.Everest.DataTypes.TS), out ts))
                                    {
                                        var ets = ts as MARC.Everest.DataTypes.TS;
                                        subjectOf.BirthTime = new TimestampPart(TimestampPart.TimestampPartType.Standlone, ets.DateValue, TS_PREC_MAP[ets.DateValuePrecision.Value]);
                                    }
                                    else
                                        throw new ArgumentException();
                                    break;
                                case "PID.8":
                                    // Admin sex
                                    subjectOf.GenderCode = qip2.ToLower().Equals("m") ? "M" : qip2.ToLower().Equals("f") ? "F" : "U";
                                    break;
                                case "PID.11":
                                    // address
                                    if (address == null)
                                        address = new AddressSet() { Use = AddressSet.AddressSetUse.Search, Parts = new List<AddressPart>() };

                                    // Naming part? 
                                    if (componentNo.CompareTo("7") < 0)
                                    {
                                        AddressPart np = new AddressPart() { AddressValue = qip2 };
                                        AddressPart.AddressPartType? typ = null;
                                        if (AD_MAP.TryGetValue(componentNo, out typ))
                                            np.PartType = typ.Value;
                                        else
                                            np.PartType = AddressPart.AddressPartType.StreetAddressLine;
                                        address.Parts.Add(np);
                                    }
                                    else if (componentNo.Equals("7"))
                                    {
                                        AddressSet.AddressSetUse? use = null;
                                        if (AD_USE_MAP.TryGetValue(qip2, out use))
                                            address.Use = use.Value;
                                        else
                                            dtls.Add(new NotSupportedChoiceResultDetail(ResultDetailType.Warning, m_locale.GetString("MSGW018"), String.Format("QPD^3^{0}", Array.IndexOf(qps, qp) + 1), null));
                                    }
                                    else
                                        throw new ArgumentException();
                                    break;
                                case "PID.13":
                                     // Naming part? 
                                    if (componentNo.CompareTo("9") < 0)
                                    {
                                        subjectOf.TelecomAddresses.Add(new TelecommunicationsAddress()
                                        {
                                            Value = qip2.ToLower()
                                        });
                                    }
                                    break;
                                case "PID.33": // last update time
                                    {
                                        
                                        var tsqip = (MARC.Everest.DataTypes.TS)qip2;
                                        filter.EffectiveTime = new TimestampSet();
                                        if (tsqip.DateValuePrecision < MARC.Everest.DataTypes.DatePrecision.Second)
                                        {
                                            var ivlqip = tsqip.ToIVL();
                                            filter.EffectiveTime.Parts.Add(new TimestampPart(TimestampPart.TimestampPartType.LowBound, ivlqip.Low.DateValue, TS_PREC_MAP[MARC.Everest.DataTypes.DatePrecision.Full]));
                                            filter.EffectiveTime.Parts.Add(new TimestampPart(TimestampPart.TimestampPartType.HighBound, ivlqip.High.DateValue, TS_PREC_MAP[MARC.Everest.DataTypes.DatePrecision.Full]));
                                        }
                                        else
                                            filter.EffectiveTime.Parts.Add(new TimestampPart(TimestampPart.TimestampPartType.Standlone, tsqip.DateValue, TS_PREC_MAP[tsqip.DateValuePrecision.Value]));
                                        break;
                                    }
                                default:
                                    throw new ArgumentException("Could not understand query parameter");
                            }
                        }
                        else
                            throw new ArgumentException("Cannot parse QID");
                    }
                    catch (Exception e)
                    {
                        dtls.Add(new ResultDetail(ResultDetailType.Error, m_locale.GetString("MSGE061"), String.Format("QPD^1^3^{0}", Array.IndexOf(qps, qp) + 1), null));
                    }
                }

                // Add data
                if (altId != null)
                {
                    // Fill out the altId
                    NHapi.Model.V25.Datatype.CX tcx = new NHapi.Model.V25.Datatype.CX(request);
                    tcx.AssigningAuthority.NamespaceID.Value = altId.AssigningAuthority;
                    tcx.AssigningAuthority.UniversalID.Value = altId.Domain;
                    tcx.AssigningAuthority.UniversalIDType.Value = "ISO";
                    tcx.IDNumber.Value = altId.Identifier;
                    subjectOf.AlternateIdentifiers = new List<DomainIdentifier>() { CreateDomainIdentifier(tcx, dtls) };
                }
                if (name != null)
                    subjectOf.Names = new List<NameSet>() { name };
                if(address != null)
                    subjectOf.Addresses = new List<AddressSet>(){ address };
                if (mothersName != null || mothersId != null)
                {
                    PersonalRelationship prs = new PersonalRelationship() { RelationshipKind = "MTH" };

                    // Fill out the altId
                    if (mothersId != null)
                    {
                        NHapi.Model.V25.Datatype.CX tcx = new NHapi.Model.V25.Datatype.CX(request);
                        tcx.AssigningAuthority.NamespaceID.Value = mothersId.AssigningAuthority;
                        tcx.AssigningAuthority.UniversalID.Value = mothersId.Domain;
                        tcx.AssigningAuthority.UniversalIDType.Value = "ISO";
                        tcx.IDNumber.Value = mothersId.Identifier;
                        prs.AlternateIdentifiers = new List<DomainIdentifier>() { CreateDomainIdentifier(tcx, dtls) };
                    }
                    if (mothersName != null)
                        prs.LegalName = mothersName;
                    subjectOf.Add(prs, "PRS", HealthServiceRecordSiteRoleType.RepresentitiveOf, null);
                }
            }

            // Get the algorithm name
            Varies str = qpd.GetField(4, 0) as Varies,
                algo = qpd.GetField(5, 0) as Varies;
            if (algo != null && !String.IsNullOrEmpty(algo.Data.ToString()))
            {
                MatchAlgorithm algorithm = MatchAlgorithm.Exact;
                if(!Enum.TryParse(algo.Data.ToString(), out algorithm))
                    dtls.Add(new ResultDetail(ResultDetailType.Error, m_locale.GetString("MSGE062"), "QPD^4", null));
                float conf = (float)(Double.Parse(str.Data.ToString()) / 100.0d);
                // Add QP
                retVal.QueryRequest.Add(new QueryParameters()
                {
                    MatchingAlgorithm = algorithm,
                    MatchStrength = MatchStrength.Strong,
                }, "FLT", HealthServiceRecordSiteRoleType.FilterOf, null);
                retVal.MinimumDegreeMatch = conf;
            }

            // Return domains
            var retDomain = qpd.GetField(8);
            foreach (Varies rd in retDomain)
            {
                var rid = new NHapi.Model.V25.Datatype.CX(request);
                DeepCopy.copy(rd.Data as GenericComposite, rid);
                if (rid != null)
                {
                    if (retVal.TargetDomain == null) retVal.TargetDomain = new List<DomainIdentifier>();
                    var dmn = CreateDomainIdentifier(rid, dtls);
                    if (String.IsNullOrEmpty(dmn.Domain) || m_config.OidRegistrar.FindData(dmn.Domain) == null || !m_config.OidRegistrar.FindData(dmn.Domain).Attributes.Exists(p => p.Key.Equals("AssigningAuthorityName")))
                        dtls.Add(new UnrecognizedTargetDomainResultDetail(this.m_locale, "QPD^1^4^"));
                    retVal.TargetDomain.Add(dmn);
                }
            }

            // Get RCP which controls the initial quantity
            retVal.Limit = 100;
            if (rcp != null)
            {
                if (!String.IsNullOrEmpty(rcp.QuantityLimitedRequest.Quantity.Value))
                    retVal.Limit = Int32.Parse(rcp.QuantityLimitedRequest.Quantity.Value);
                 
            }

            // Construct additional query data
            var tag = qpd.GetField(2, 0) as NHapi.Model.V25.Datatype.ST;

            if (!string.IsNullOrEmpty(dsc.ContinuationPointer.Value))
            {
                retVal.QueryId = dsc.ContinuationPointer.Value;
                retVal.IsContinue = true;
                retVal.Offset = -1;
            }
            else
                retVal.QueryId = Guid.NewGuid().ToString();

            if (tag != null)
                retVal.QueryTag = tag.Value;
            else
                retVal.QueryTag = retVal.QueryId;

            
            retVal.IsSummary = false;
            retVal.OriginalMessageQueryId = msh.MessageControlID.Value;

            if (dtls.Exists(o => o.Type == ResultDetailType.Error))
            {
                Trace.TraceError("{0} problems mapping message:", dtls.Count);
                foreach (var itm in dtls)
                    Trace.TraceError($"\t{itm.Type} : {itm.Message}");
                return null;
            }

            return retVal;
        }

        /// <summary>
        /// Create components for the patient structure
        /// </summary>
        internal RegistrationEvent CreateComponents(NHapi.Model.V231.Message.ADT_A01 request, List<IResultDetail> dtls)
        {
            // Registration event
            RegistrationEvent retVal = new RegistrationEvent()
            {
                EventClassifier = RegistrationEventType.Register,
                EventType = new CodeValue(request.MSH.MessageType.TriggerEvent.Value),
                Status = StatusType.Completed,
                LanguageCode = m_config.JurisdictionData.DefaultLanguageCode
            };

            var aautString = String.Format("{0}|{1}", request.MSH.SendingApplication.NamespaceID.Value, request.MSH.SendingFacility.NamespaceID.Value); // sending application
            var aaut = this.m_config.OidRegistrar.FindData(o => o.Attributes.Exists(a => a.Key == "AssigningDevFacility" && a.Value == aautString));
            if (aaut == null)
                Trace.TraceWarning("No assigning authority for {0} found", aautString);

            var evn = request.EVN;
            var pid = request.PID; // get the pid segment
            var pd1 = request.PD1; // PD1 segment

            if (!String.IsNullOrEmpty(evn.RecordedDateTime.TimeOfAnEvent.Value))
                retVal.EffectiveTime = new TimestampSet() { Parts = new List<TimestampPart>() { CreateTimestampPart(evn.RecordedDateTime, dtls) } };
            else
                retVal.EffectiveTime = new TimestampSet() { Parts = new List<TimestampPart>() { new TimestampPart() { PartType = TimestampPart.TimestampPartType.LowBound, Value = DateTime.Now, Precision = "F" } } };

            Person subject = this.CreatePerson(pid, pd1, dtls, aaut);

            // Add to subject
            retVal.Add(subject, "SUBJ", HealthServiceRecordSiteRoleType.SubjectOf, null);

            // Add author information (facility)
            RepositoryDevice dev = new RepositoryDevice();
            dev.Name = request.MSH.SendingApplication.NamespaceID.Value;
            dev.Jurisdiction = request.MSH.SendingFacility.NamespaceID.Value; 
            dev.AlternateIdentifier = this.CreateDomainIdentifier(request.MSH.SendingApplication, dtls);
            if (String.IsNullOrEmpty(dev.AlternateIdentifier.Domain))
                dev.AlternateIdentifier.Domain = this.m_config.OidRegistrar.GetOid("V2_SEND_FAC_ID").Oid;
            retVal.Add(dev, "AUT", HealthServiceRecordSiteRoleType.AuthorOf, null);

            // Next of kin segment
            for(int i = 0; i < request.NK1RepetitionsUsed; i++)
            {
                var nk1 = request.GetNK1(i);
                if (PipeParser.Encode(nk1, new EncodingCharacters('|', "^~\\&")) == "NK1") // HACK: Empty segment for some reason on v2.3.1
                    continue;
                PersonalRelationship relative = this.CreatePersonalRelationship(nk1, dtls, aaut);
                subject.Add(relative, Guid.NewGuid().ToString(), HealthServiceRecordSiteRoleType.RepresentitiveOf, null);
            }

            if (dtls.Exists(o => o.Type == ResultDetailType.Error))
                return null;
            return retVal;

        }

        /// <summary>
        /// Create a personal relationship
        /// </summary>
        private PersonalRelationship CreatePersonalRelationship(NK1 nk1, List<IResultDetail> dtls, OidRegistrar.OidData aaut)
        {
            var retVal = new PersonalRelationship();

            // Person identifier
            retVal.AlternateIdentifiers = nk1.GetNextOfKinAssociatedPartySIdentifiers().Select(o => this.CreateDomainIdentifier(o, dtls)).ToList();

            // type of relation
            if(!String.IsNullOrEmpty(nk1.Relationship.Identifier.Value))
                retVal.RelationshipKind = Util.ToWireFormat(nk1.Relationship.Identifier.Value);

            retVal.Status = StatusType.Active;
            
            // Relationship holder
            if (nk1.Sex.Value != null)
                retVal.GenderCode = nk1.Sex.Value;
            if (!String.IsNullOrEmpty(nk1.DateTimeOfBirth.TimeOfAnEvent.Value))
                retVal.BirthTime = CreateTimestampPart(nk1.DateTimeOfBirth, dtls);

            if (nk1.NameRepetitionsUsed > 0)
                retVal.LegalName = CreateNameSet(nk1.GetName().FirstOrDefault(o => o.NameTypeCode.Value == "OR") ?? nk1.GetName().FirstOrDefault(), dtls);

            // Person address
            if (nk1.AddressRepetitionsUsed > 0)
                retVal.PerminantAddress = CreateAddressSet(nk1.GetAddress().FirstOrDefault(o => o.AddressType.Value == "H") ?? nk1.GetAddress().FirstOrDefault(), dtls);

            // Telephone
            if (nk1.PhoneNumberRepetitionsUsed > 0)
            {
                retVal.TelecomAddresses = new List<TelecommunicationsAddress>();
                foreach (var tel in nk1.GetPhoneNumber())
                    if (String.IsNullOrEmpty(tel.EmailAddress.Value))
                    {
                        var tca = MessageUtil.TelFromXTN(tel);
                        tca.Use = "HP";
                        retVal.TelecomAddresses.Add(tca);
                    }
                    else
                        retVal.TelecomAddresses.Add(new TelecommunicationsAddress()
                        {
                            Capability = "data text",
                            Use = "HP",
                            Value = String.Format("mailto:{0}", tel.EmailAddress)
                        });

            }

            return retVal;
        }

        /// <summary>
        /// Creat the person
        /// </summary>
        private Person CreatePerson(NHapi.Model.V231.Segment.PID pid, NHapi.Model.V231.Segment.PD1 pd1, List<IResultDetail> dtls, MARC.HI.EHRS.SVC.Core.DataTypes.OidRegistrar.OidData aaut)
        {
            var subject = new Person() { Status = StatusType.Active, Timestamp = DateTime.Now };
            subject.RoleCode = PersonRole.PAT;
            // TODO: Effective Time

            subject.AlternateIdentifiers = new List<DomainIdentifier>();

            // Pri Patient Identifier
            if (!String.IsNullOrEmpty(pid.PatientID.ID.Value))
                subject.AlternateIdentifiers.Add(CreateDomainIdentifier(pid.PatientID, aaut, dtls));
            if (pid.PatientIdentifierListRepetitionsUsed > 0)
            {
                // TODO: If the aaut matches a PID auth here then it is the primary
                // and can be the only one that results in a create.
                for (int i = 0; i < pid.PatientIdentifierListRepetitionsUsed; i++)
                {
                    var cx = pid.GetPatientIdentifierList(i);
                    if (cx.IdentifierTypeCode == null || cx.IdentifierTypeCode.Value == null || cx.IdentifierTypeCode.Value == "PI" || cx.IdentifierTypeCode.Value == "PT" || cx.IdentifierTypeCode.Value == "MR")
                    { 

                        Trace.TraceInformation("Adding {0}^^^&{1}&ISO to altIds", cx.ID.Value, cx.AssigningAuthority.UniversalID.Value);
                        subject.AlternateIdentifiers.Add(CreateDomainIdentifier(pid.GetPatientIdentifierList(i), aaut, dtls));
                    }
                    else
                    {
                        if (subject.OtherIdentifiers == null)
                            subject.OtherIdentifiers = new List<KeyValuePair<CodeValue, DomainIdentifier>>();
                        subject.OtherIdentifiers.Add(new KeyValuePair<CodeValue, DomainIdentifier>(
                            new CodeValue() { CodeSystem = "1.3.6.1.4.1.33349.3.98.12", Code = cx.IdentifierTypeCode.Value },
                            CreateDomainIdentifier(pid.GetPatientIdentifierList(i), aaut, dtls)
                        ));
                    }
                }
            }
            else
                dtls.Add(new MandatoryElementMissingResultDetail(ResultDetailType.Error, this.m_locale.GetString("MSGE063"), "PID^3"));
            // Alt patient identifiers
            if (pid.AlternatePatientIDPIDRepetitionsUsed > 0)
                for (int i = 0; i < pid.AlternatePatientIDPIDRepetitionsUsed; i++)
                    subject.AlternateIdentifiers.Add(CreateDomainIdentifier(pid.GetAlternatePatientIDPID(i), dtls));

            // Patient's name
            if (pid.PatientNameRepetitionsUsed > 0)
            {
                subject.Names = new List<NameSet>();
                foreach (var xpn in pid.GetPatientName())
                    subject.Names.Add(CreateNameSet(xpn, dtls));
            }

            // Patient's mother's identifiers
            if (pid.MotherSIdentifierRepetitionsUsed > 0 || pid.MotherSMaidenNameRepetitionsUsed > 0)
            {
                PersonalRelationship relation = new PersonalRelationship() { RelationshipKind = "MTH" };
                relation.AlternateIdentifiers = new List<DomainIdentifier>();
                foreach (var mid in pid.GetMotherSIdentifier())
                    relation.AlternateIdentifiers.Add(CreateDomainIdentifier(mid, dtls));
                foreach (var mname in pid.GetMotherSMaidenName())
                    relation.LegalName = CreateNameSet(mname, dtls);
                subject.Add(relation, "MTH", HealthServiceRecordSiteRoleType.RepresentitiveOf, null);
            }

            // DOB
            if (!String.IsNullOrEmpty(pid.DateTimeOfBirth.TimeOfAnEvent.Value))
                subject.BirthTime = CreateTimestampPart(pid.DateTimeOfBirth, dtls);

            // SEX
            if (!String.IsNullOrEmpty(pid.Sex.Value))
                subject.GenderCode = pid.Sex.Value;

            // Alias name
            if (pid.PatientAliasRepetitionsUsed > 0)
            {
                if (subject.Names != null) subject.Names = new List<NameSet>();
                foreach (var xpn in pid.GetPatientAlias())
                    subject.Names.Add(CreateNameSet(xpn, dtls));
            }

            // Race code
            if (pid.RaceRepetitionsUsed > 0)
            {
                subject.Race = new List<CodeValue>();
                foreach (var rce in pid.GetRace())
                    subject.Race.Add(CreateCodeValue(rce, dtls));
            }

            // Address
            if (pid.PatientAddressRepetitionsUsed > 0)
            {
                subject.Addresses = new List<AddressSet>();
                foreach (var xad in pid.GetPatientAddress())
                    subject.Addresses.Add(CreateAddressSet(xad, dtls));
            }

            // Telephone
            if (pid.PhoneNumberHomeRepetitionsUsed > 0)
            {
                subject.TelecomAddresses = new List<TelecommunicationsAddress>();
                foreach (var tel in pid.GetPhoneNumberHome())
                    if (String.IsNullOrEmpty(tel.EmailAddress.Value))
                    {
                        var tca = MessageUtil.TelFromXTN(tel);
                        tca.Use = "HP";
                        subject.TelecomAddresses.Add(tca);
                    }
                    else
                        subject.TelecomAddresses.Add(new TelecommunicationsAddress()
                        {
                            Capability = "data text",
                            Use = "HP",
                            Value = String.Format("mailto:{0}", tel.EmailAddress)
                        });
                    
            }

            // Business Home
            if (pid.PhoneNumberBusinessRepetitionsUsed > 0)
            {
                if (subject.TelecomAddresses == null)
                    subject.TelecomAddresses = new List<TelecommunicationsAddress>();
                foreach (var tel in pid.GetPhoneNumberBusiness())
                    if (String.IsNullOrEmpty(tel.EmailAddress.Value))
                    {
                        var tca = MessageUtil.TelFromXTN(tel);
                        tca.Use = "WP";
                        subject.TelecomAddresses.Add(tca);
                    }
                    else
                        subject.TelecomAddresses.Add(new TelecommunicationsAddress()
                        {
                            Use = "WP",
                            Capability = "data text",
                            Value = String.Format("mailto:{0}", tel.EmailAddress)
                        });
            }

            // Primary language
            if (!String.IsNullOrEmpty(pid.PrimaryLanguage.Identifier.Value))
            {
                var langCd = CreateCodeValue(pid.PrimaryLanguage, dtls);
                if (langCd.CodeSystem == null)
                    langCd.CodeSystem = this.m_config.OidRegistrar.GetOid("ISO639-1").Oid;
                else if (langCd.CodeSystem != this.m_config.OidRegistrar.GetOid("ISO639-1").Oid)
                {
                    var termSvc = this.m_context.GetService(typeof(ITerminologyService)) as ITerminologyService;
                    langCd = termSvc.Translate(langCd, this.m_config.OidRegistrar.GetOid("ISO639-1").Oid);
                }
                if (langCd.CodeSystem != this.m_config.OidRegistrar.GetOid("ISO639-1").Oid)
                    dtls.Add(new VocabularyIssueResultDetail(ResultDetailType.Error, this.m_locale.GetString("MSGE04C"), "PID^15", null));
                subject.Language = new List<PersonLanguage>();
                subject.Language.Add(new PersonLanguage()
                {
                    Language = langCd.Code,
                    Type = LanguageType.Preferred
                });
            }

            // Marital Status
            if (!String.IsNullOrEmpty(pid.MaritalStatus.Identifier.Value))
                subject.MaritalStatus = CreateCodeValue(pid.MaritalStatus, dtls);

            // Religion
            if (!String.IsNullOrEmpty(pid.Religion.Identifier.Value))
                subject.ReligionCode = CreateCodeValue(pid.Religion, dtls);

            // SSN Number
            if (!String.IsNullOrEmpty(pid.SSNNumberPatient.Value))
            {
                subject.OtherIdentifiers = new List<KeyValuePair<CodeValue, DomainIdentifier>>();
                subject.OtherIdentifiers.Add(new KeyValuePair<CodeValue, DomainIdentifier>(
                    new CodeValue() { CodeSystem = "2.16.840.1.113883.2.20.3.85", Code = "SIN" },
                    new DomainIdentifier() { Domain = this.m_config.OidRegistrar.GetOid("SSN").Oid, Identifier = pid.SSNNumberPatient.Value }));
            }

            // License Number
            if (!String.IsNullOrEmpty(pid.DriverSLicenseNumberPatient.DriverSLicenseNumber.Value))
                subject.OtherIdentifiers.Add(new KeyValuePair<CodeValue, DomainIdentifier>(
                    new CodeValue() { CodeSystem = "2.16.840.1.113883.2.20.3.85", Code = "DL" },
                    new DomainIdentifier() { Domain = this.m_config.OidRegistrar.GetOid(String.Format("DL-{0}", pid.DriverSLicenseNumberPatient.IssuingStateProvinceCountry)).Oid, Identifier = pid.DriverSLicenseNumberPatient.DriverSLicenseNumber.Value }
                ));

            // MBO
            if (!String.IsNullOrEmpty(pid.BirthOrder.Value))
                subject.BirthOrder = Convert.ToInt32(pid.BirthOrder.Value);
            else if (!String.IsNullOrEmpty(pid.MultipleBirthIndicator.Value))
                subject.BirthOrder = -1;
            // Citizenship
            if (pid.CitizenshipRepetitionsUsed > 0)
            {
                subject.Citizenship = new List<Citizenship>();
                foreach (var cit in pid.GetCitizenship())
                {
                    var cv = CreateCodeValue(cit, dtls);
                    subject.Citizenship.Add(new Citizenship()
                    {
                        CountryCode = cv.Code,
                        CountryName = cv.DisplayName
                    });
                }
            }

            // Death
            if (!String.IsNullOrEmpty(pid.PatientDeathDateAndTime.TimeOfAnEvent.Value))
                subject.DeceasedTime = CreateTimestampPart(pid.PatientDeathDateAndTime, dtls);

            // Ethnic group
            if (pid.EthnicGroupRepetitionsUsed > 0)
                foreach (var eth in pid.GetEthnicGroup())
                    subject.EthnicGroup.Add(this.CreateCodeValue(eth, dtls));

            // Birthplace
            if (!String.IsNullOrEmpty(pid.BirthPlace.Value))
                subject.Add(new Place()
                {
                    Name = pid.BirthPlace.Value
                }, "BRTH");

            this.MarkScopedId(subject, aaut, dtls);
            

            return subject;
        }

        /// <summary>
        /// Marks identifiers for which the sender has assigning authority
        /// </summary>
        private void MarkScopedId(Person subject, OidRegistrar.OidData aaut, List<IResultDetail> dtls)
        {
            // Scoper
            if (aaut != null)
            {
                List<DomainIdentifier> scopedIds = subject.AlternateIdentifiers.FindAll(o => aaut.Oid == o.Domain);
                if (scopedIds == null || scopedIds.Count == 0)
                {
                    Trace.TraceError("No OIDs found matching assigning authority OID {0}", aaut.Oid);
                    dtls.Add(new FormalConstraintViolationResultDetail(ResultDetailType.Error, this.m_locale.GetString("MSGE078"), null, null));
                }
                else
                {
                    foreach (var scopedId in scopedIds)
                    {
                        var scopedIdIndex = subject.AlternateIdentifiers.IndexOf(scopedId);
                        subject.AlternateIdentifiers[scopedIdIndex] = new AuthorityAssignedDomainIdentifier()
                        {
                            Assigner = new RepositoryDevice() { AlternateIdentifier = new DomainIdentifier() { Domain = aaut.Oid }, Name = aaut.Name },
                            Domain = scopedId.Domain,
                            Identifier = scopedId.Identifier,
                            AssigningAuthority = scopedId.AssigningAuthority
                        };
                    }
                }
            }
            else
            {
                dtls.Add(new MandatoryElementMissingResultDetail(ResultDetailType.Error, m_locale.GetString("MSGE078"), null));
            }

        }


        /// <summary>
        /// Create address set
        /// </summary>
        private AddressSet CreateAddressSet(NHapi.Model.V231.Datatype.XAD xad, List<IResultDetail> dtls)
        {
            AddressSet retVal = new AddressSet();
            AddressSet.AddressSetUse? use = null;
            if (String.IsNullOrEmpty(xad.AddressType.Value) || !AD_USE_MAP.TryGetValue(xad.AddressType.Value, out use))
                retVal.Use = AddressSet.AddressSetUse.Search;
            else
                retVal.Use = use.Value;
            // Components 
            for(int i = 0; i < xad.Components.Length; i++)
            {
                var pt = xad.Components[i];
                AddressPart.AddressPartType? type = null;
                if (AD_MAP.TryGetValue((i + 1).ToString(), out type))
                {
                    if (pt is AbstractPrimitive)
                    {
                        string value = (pt as AbstractPrimitive).Value;
                        if (!String.IsNullOrEmpty(value))
                            retVal.Parts.Add(new AddressPart() { AddressValue = value, PartType = type.Value });
                    }
                }
            }

            return retVal;
        }

        /// <summary>
        /// Create a code value
        /// </summary>
        private CodeValue CreateCodeValue(NHapi.Model.V231.Datatype.CE rce, List<IResultDetail> dtls)
        {
            var retVal = new CodeValue();

            if (!String.IsNullOrEmpty(rce.Identifier.Value))
                retVal.Code = rce.Identifier.Value;
            if (!String.IsNullOrEmpty(rce.Text.Value))
                retVal.DisplayName = rce.Text.Value;
            if (!String.IsNullOrEmpty(rce.NameOfCodingSystem.Value))
            {
                var oid = this.m_config.OidRegistrar.FindData(o => o.Attributes.Exists(a => a.Key == "HL70396Name" && a.Value == rce.NameOfCodingSystem.Value));
                if (oid != null)
                    retVal.CodeSystem = oid.Oid;
                else
                    dtls.Add(new VocabularyIssueResultDetail(ResultDetailType.Error, this.m_locale.GetString("MSGE070"), "CE.3", null));
            }
            return retVal;
        }

        /// <summary>
        /// Create datetime
        /// </summary>
        private TimestampPart CreateTimestampPart(NHapi.Model.V231.Datatype.TS ts, List<IResultDetail> dtls)
        {
            // Birth
            object mts = null;
            if (MARC.Everest.Connectors.Util.TryFromWireFormat(ts.TimeOfAnEvent.Value, typeof(MARC.Everest.DataTypes.TS), out mts))
            {
                var ets = mts as MARC.Everest.DataTypes.TS;
                string precision;
                if(TS_PREC_MAP.TryGetValue(ets.DateValuePrecision.Value, out precision))
                    return new TimestampPart(TimestampPart.TimestampPartType.Standlone, ets.DateValue, precision);
                return new TimestampPart(TimestampPart.TimestampPartType.Standlone, ets.DateValue, "F");
            }
            else
                throw new ArgumentException();
        }

        /// <summary>
        /// Create nameset
        /// </summary>
        private NameSet CreateNameSet(NHapi.Model.V231.Datatype.XPN xpn, List<IResultDetail> dtls)
        {
            NameSet patientName = new NameSet() { Parts = new List<NamePart>() };
            MARC.HI.EHRS.SVC.Core.DataTypes.NameSet.NameSetUse? tUse = null;
            if (!String.IsNullOrEmpty(xpn.NameTypeCode.Value) && !XPN_USE_MAP.TryGetValue(xpn.NameTypeCode.Value, out tUse))
                tUse = MARC.HI.EHRS.SVC.Core.DataTypes.NameSet.NameSetUse.Search;
            patientName.Use = tUse.HasValue ? tUse.Value : NameSet.NameSetUse.Search;
            // Add components
            if (xpn.FamilyLastName != null)
                foreach (NHapi.Model.V231.Datatype.ST pn in xpn.FamilyLastName.Components)
                    if(!String.IsNullOrEmpty(pn.Value))
                        patientName.Parts.Add(new NamePart() { Value = pn.Value, Type = NamePart.NamePartType.Family });
            if (!String.IsNullOrEmpty(xpn.GivenName.Value))
                patientName.Parts.Add(new NamePart() { Value = xpn.GivenName.Value, Type = NamePart.NamePartType.Given });
            if (!String.IsNullOrEmpty(xpn.MiddleInitialOrName.Value))
                patientName.Parts.Add(new NamePart() { Value = xpn.MiddleInitialOrName.Value, Type = NamePart.NamePartType.Given });
            return patientName;
        }

        /// <summary>
        /// Creates a domain identifier using the specified assigning authority name if present
        /// </summary>
        private DomainIdentifier CreateDomainIdentifier(NHapi.Model.V231.Datatype.CX id, OidRegistrar.OidData aaut, List<IResultDetail> dtls)
        {
            var retVal = CreateDomainIdentifier(id, dtls);
            // Assigning authority validation
            if (String.IsNullOrEmpty(retVal.AssigningAuthority) && String.IsNullOrEmpty(retVal.Domain)) // No aaut, so populate from config
            {
                if (aaut == null)
                    dtls.Add(new MandatoryElementMissingResultDetail(ResultDetailType.Error, m_locale.GetString("MSGE06F"), null));
                else
                {
                    if (!aaut.Attributes.Exists(k => k.Key == "AutoFillCX4" && k.Value.ToLower() == "true"))
                        dtls.Add(new MandatoryElementMissingResultDetail(ResultDetailType.Error, m_locale.GetString("MSGE06F"), null));
                    else
                    {
                        retVal.AssigningAuthority = aaut.Attributes.Find(o => o.Key == "AssigningAuthorityName").Value;
                        retVal.Domain = aaut.Oid;
                    }
                }
            }
            return retVal;
        }

        /// <summary>
        /// Create components
        /// </summary>
        internal RegistrationEvent CreateComponents(NHapi.Model.V231.Message.ADT_A39 request, List<IResultDetail> dtls)
        {
            // Registration event
            RegistrationEvent retVal = new RegistrationEvent()
            {
                EventClassifier = RegistrationEventType.Register,
                Mode = RegistrationEventType.Replace,
                EventType = new CodeValue(request.MSH.MessageType.TriggerEvent.Value),
                Status = StatusType.Completed,
                LanguageCode = m_config.JurisdictionData.DefaultLanguageCode
            };

            var evn = request.EVN;
            var aautString = String.Format("{0}|{1}", request.MSH.SendingApplication.NamespaceID.Value, request.MSH.SendingFacility.NamespaceID.Value); // sending application
            var aaut = this.m_config.OidRegistrar.FindData(o => o.Attributes.Exists(a => a.Key == "AssigningDevFacility" && a.Value == aautString));
            if (aaut == null)
                Trace.TraceWarning("No assigning authority for {0} found", aautString);


            // Can be multiple patients
            for (int i = 0; i < request.PATIENTRepetitionsUsed; i++)
            {
                var patient = request.GetPATIENT(i);
                var pid = patient.PID; // get the pid segment
                var pd1 = patient.PD1;
                
                if (!String.IsNullOrEmpty(evn.RecordedDateTime.TimeOfAnEvent.Value))
                    retVal.EffectiveTime = new TimestampSet() { Parts = new List<TimestampPart>() { CreateTimestampPart(evn.RecordedDateTime, dtls) } };
                else
                    retVal.EffectiveTime = new TimestampSet() { Parts = new List<TimestampPart>() { new TimestampPart() { PartType = TimestampPart.TimestampPartType.LowBound, Value = DateTime.Now, Precision = "F" } } };

                Person subject = this.CreatePerson(pid, pd1, dtls, aaut);

                // Merge
                if (patient.MRG.GetPriorPatientIdentifierList().Length > 0)
                {
                    var re = new PersonRegistrationRef()
                    {
                        AlternateIdentifiers = new List<DomainIdentifier>()
                    };

                    foreach (var id in patient.MRG.GetPriorPatientIdentifierList())
                        re.AlternateIdentifiers.Add(this.CreateDomainIdentifier(id, dtls));

                    subject.Add(re, Guid.NewGuid().ToString(), HealthServiceRecordSiteRoleType.ReplacementOf, null);
                    (re.Site as HealthServiceRecordSite).IsSymbolic = true;
                }

                // Add to subject
                retVal.Add(subject, "SUBJ", HealthServiceRecordSiteRoleType.SubjectOf, null);

                break;
            }

            // Add author information (facility)
            RepositoryDevice dev = new RepositoryDevice();
            dev.Name = request.MSH.SendingApplication.NamespaceID.Value;
            dev.Jurisdiction = request.MSH.SendingFacility.NamespaceID.Value;
            dev.AlternateIdentifier = this.CreateDomainIdentifier(request.MSH.SendingFacility, dtls);
            if (String.IsNullOrEmpty(dev.AlternateIdentifier.Domain))
                dev.AlternateIdentifier.Domain = this.m_config.OidRegistrar.GetOid("V2_SEND_FAC_ID").Oid;
            retVal.Add(dev, "AUT", HealthServiceRecordSiteRoleType.AuthorOf, null);

            if (dtls.Exists(o => o.Type == ResultDetailType.Error))
                return null;

            return retVal;
        }
    }
}
