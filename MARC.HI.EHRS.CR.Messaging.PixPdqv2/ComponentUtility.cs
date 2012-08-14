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
                dtls.Add(new FixedValueMisMatchedResultDetail(qpd.MessageQueryName.Identifier.Value, "IHE PIX Query", "QPD^1"));

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
                    throw new ResultDetailException(new UnrecognizedPatientDomainResultDetail(this.m_locale));
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
                    if (String.IsNullOrEmpty(dmn.Domain) || !m_config.OidRegistrar.FindData(dmn.Domain).Attributes.Exists(p=>p.Key.Equals("AssigningAuthorityName")))
                        throw new ResultDetailException(new UnrecognizedTargetDomainResultDetail(this.m_locale));
                    retVal.TargetDomain.Add(dmn);
                }
            }

            // Construct additional query data
            var tag = qpd.GetField(2, 0) as NHapi.Model.V25.Datatype.ST;
            if (tag != null)
                retVal.QueryTag = tag.Value;
            else
                retVal.QueryTag = Guid.NewGuid().ToString();


            retVal.Quantity = 100;
            retVal.IsSummary = true;
            retVal.OriginalMessageQueryId = msh.MessageControlID.Value;
            



            if (dtls.Exists(o => o.Type == ResultDetailType.Error))
                return QueryData.Empty;
            return retVal;
        }

       
    }
}
