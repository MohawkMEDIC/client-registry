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

namespace MARC.HI.EHRS.CR.Messaging.PixPdqv2
{
    /// <summary>
    /// Component utility
    /// </summary>
    public class ComponentUtility : IUsesHostContext
    {
        #region IUsesHostContext Members

        // Host context
        private MARC.HI.EHRS.SVC.Core.HostContext m_context;

        // Localization service
        private ILocalizationService m_locale;

        // Config
        private ISystemConfigurationService m_config;

        // XPN Maps
        internal static readonly Dictionary<String, NamePart.NamePartType?> XPN_MAP = new Dictionary<string, NamePart.NamePartType?>()
        {
            { "1",  NamePart.NamePartType.Family },
            { "2", NamePart.NamePartType.Given },
            { "3", NamePart.NamePartType.Given },
            { "4", NamePart.NamePartType.Suffix },
            { "5", NamePart.NamePartType.Prefix },
            { "6", NamePart.NamePartType.Prefix }
        };

        // AD Maps
        internal static readonly Dictionary<String, AddressPart.AddressPartType?> AD_MAP = new Dictionary<string, AddressPart.AddressPartType?>()
        {
            { "1", AddressPart.AddressPartType.StreetAddressLine },
            { "2", AddressPart.AddressPartType.AddressLine },
            { "3", AddressPart.AddressPartType.City },
            { "4", AddressPart.AddressPartType.State },
            { "5", AddressPart.AddressPartType.PostalCode },
            { "6", AddressPart.AddressPartType.Country }
        };

        // XPN Type Maps
        internal static readonly Dictionary<String, NameSet.NameSetUse?> XPN_USE_MAP = new Dictionary<string, NameSet.NameSetUse?>()
        {
            { "A", NameSet.NameSetUse.Pseudonym },
            { "B", NameSet.NameSetUse.OfficialRecord },
            { "C", NameSet.NameSetUse.OfficialRecord },
            { "D", NameSet.NameSetUse.OfficialRecord },
            { "I", NameSet.NameSetUse.License },
            { "L", NameSet.NameSetUse.Legal },
            { "M", NameSet.NameSetUse.MaidenName },
            { "N", NameSet.NameSetUse.Artist },
            { "T", NameSet.NameSetUse.Indigenous }
        };

        // AD Type maps
        internal static readonly Dictionary<String, AddressSet.AddressSetUse?> AD_USE_MAP = new Dictionary<string, AddressSet.AddressSetUse?>()
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
        internal static readonly Dictionary<MARC.Everest.DataTypes.DatePrecision, string> TS_PREC_MAP = new Dictionary<MARC.Everest.DataTypes.DatePrecision, string>()
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
        public SVC.Core.HostContext Context
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
            if (!String.IsNullOrEmpty(addlData.Domain))
                retVal.Domain = addlData.Domain;
            if (!String.IsNullOrEmpty(addlData.Identifier))
            {
                retVal.AssigningAuthority = addlData.Identifier;
                var oid = this.m_config.OidRegistrar.FindData(o => o.Attributes.Exists(k=>k.Key.Equals("AssigningAuthorityName")) && addlData.Identifier.Equals(o.Attributes.Find(k=>k.Key.Equals("AssigningAuthorityName")).Value));
                if (oid == null)
                    retVal.Domain = String.Empty;
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
        internal QueryData CreateQueryComponents(NHapi.Model.V25.Message.QBP_Q21 request, List<IResultDetail> dtls)
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
            else if(qpd.MessageQueryName.Identifier.Value != "IHE PIX Query")
                dtls.Add(new FixedValueMisMatchedResultDetail(qpd.MessageQueryName.Identifier.Value, "IHE PIX Query", false, "QPD^1"));

            // Return value
            QueryData retVal = new QueryData()
            {
                QueryRequest = new RegistrationEvent()
                {
                    EventClassifier = RegistrationEventType.Any,
                    Timestamp = DateTime.Now,
                    AlternateIdentifier = new VersionedDomainIdentifier()
                    {
                        Identifier = msh.MessageControlID.Value
                    }
                },
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

            // Filter data
            RegistrationEvent filter = new RegistrationEvent(){ EventClassifier = RegistrationEventType.Register };
            retVal.QueryRequest.Add(filter, "FLT", HealthServiceRecordSiteRoleType.FilterOf, null);
            Person subjectOf = new Person();
            filter.Add(subjectOf, "SUBJ", HealthServiceRecordSiteRoleType.SubjectOf, null);

            // Get the PID qualifier
            var pid = qpd.GetField(3, 0) as NHapi.Base.Model.Varies;
            if (pid == null)
                dtls.Add(new MandatoryElementMissingResultDetail(ResultDetailType.Error, this.m_locale.GetString("MSGE05F"), "QPD^3"));
            else
            {
                subjectOf.AlternateIdentifiers = new List<DomainIdentifier>();
                var tcx = new NHapi.Model.V25.Datatype.CX(request);
                NHapi.Base.Util.DeepCopy.copy(pid.Data as GenericComposite, tcx);

                var dmn = CreateDomainIdentifier(tcx, dtls);

                if (String.IsNullOrEmpty(dmn.Domain) || !m_config.OidRegistrar.FindData(dmn.Domain).Attributes.Exists(p=>p.Key.Equals("AssigningAuthorityName")))
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
                        dtls.Add(new UnrecognizedTargetDomainResultDetail(this.m_locale));
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


            retVal.Quantity = 100;
            retVal.IsSummary = true;
            retVal.OriginalMessageQueryId = msh.MessageControlID.Value;
            



            if (dtls.Exists(o => o.Type == ResultDetailType.Error))
                return QueryData.Empty;
            return retVal;
        }

        /// <summary>
        /// Create query components for the PDQ message
        /// </summary>
        internal QueryData CreateQueryComponentsPdq(NHapi.Model.V25.Message.QBP_Q21 request, List<IResultDetail> dtls)
        {
            // Validate the segments
            var msh = request.MSH;
            var qpd = request.QPD;
            var rcp = request.RCP;

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
            QueryData retVal = new QueryData()
            {
                QueryRequest = new RegistrationEvent()
                {
                    EventClassifier = RegistrationEventType.Any,
                    Timestamp = DateTime.Now,
                    AlternateIdentifier = new VersionedDomainIdentifier()
                    {
                        Identifier = msh.MessageControlID.Value
                    }
                },
                ResponseMessageType = "RSP_K21"
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

            // Filter data
            RegistrationEvent filter = new RegistrationEvent() { EventClassifier = RegistrationEventType.Register };
            retVal.QueryRequest.Add(filter, "FLT", HealthServiceRecordSiteRoleType.FilterOf, null);
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
                        Regex paramSeg = new Regex(@"@(P[ID][D1]\.?\d+)\.?(\d+)\.?(\d+)");
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
                                    // Alternate identifier
                                    if (altId == null)
                                        altId = new DomainIdentifier();

                                    if (componentNo == "1")
                                        altId.Identifier = qip2;
                                    else if (componentNo == "4")
                                        switch (subComponentNo)
                                        {
                                            case "1":
                                                altId.AssigningAuthority = qip2;
                                                break;
                                            case "2":
                                                altId.Domain = qip2;
                                                break;
                                            case "3":
                                                if (qip2 != "ISO")
                                                {
                                                    dtls.Add(new NotSupportedChoiceResultDetail(ResultDetailType.Error, m_locale.GetString("MSGE05B"), String.Format("QPD^3^{0}", Array.IndexOf(qps, qp) + 1), null));
                                                    return QueryData.Empty;
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
                                case "PID.18":
                                    throw new ArgumentException();
                                    break;
                            }
                        }
                        else
                            throw new ArgumentException("Cannot parse QID");
                    }
                    catch (Exception e)
                    {
                        dtls.Add(new ResultDetail(ResultDetailType.Error, m_locale.GetString("MSGE061"), String.Format("QPD^3^{0}", Array.IndexOf(qps, qp) + 1), null));
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
                filter.Add(new QueryParameters()
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
                        dtls.Add(new UnrecognizedTargetDomainResultDetail(this.m_locale));
                    retVal.TargetDomain.Add(dmn);
                }
            }

            // Get RCP which controls the initial quantity
            retVal.Quantity = 100;
            if (rcp != null)
            {
                if (rcp.QuantityLimitedRequest != null)
                    retVal.Quantity = Int32.Parse(rcp.QuantityLimitedRequest.Quantity.Value);
            }

            // Construct additional query data
            var tag = qpd.GetField(2, 0) as NHapi.Model.V25.Datatype.ST;
            retVal.QueryId = Guid.NewGuid().ToString();
            if (tag != null)
                retVal.QueryTag = tag.Value;
            else
                retVal.QueryTag = retVal.QueryId;

            retVal.IsSummary = true;
            retVal.OriginalMessageQueryId = msh.MessageControlID.Value;

            if (dtls.Exists(o => o.Type == ResultDetailType.Error))
                return QueryData.Empty;
            return retVal;
        }
    }
}
