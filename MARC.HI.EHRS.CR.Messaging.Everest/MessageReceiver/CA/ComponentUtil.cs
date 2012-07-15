using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MARC.HI.EHRS.CR.Core.ComponentModel;
using MARC.HI.EHRS.SVC.Core.Services;
using MARC.Everest.Connectors;
using MARC.Everest.RMIM.CA.R020402.Interactions;
using MARC.HI.EHRS.SVC.Core.ComponentModel.Components;
using MARC.HI.EHRS.SVC.Core.ComponentModel;
using MARC.Everest.RMIM.CA.R020402.Vocabulary;
using MARC.Everest.DataTypes;
using MARC.HI.EHRS.SVC.Core.DataTypes;

namespace MARC.HI.EHRS.CR.Messaging.Everest
{
    /// <summary>
    /// Component utility
    /// </summary>
    public partial class ComponentUtil
    {

        /// <summary>
        /// Create a registration event for a registration event
        /// </summary>
        internal RegistrationEvent CreateComponents(MARC.Everest.RMIM.CA.R020402.MFMI_MT700711CA.ControlActEvent<MARC.Everest.RMIM.CA.R020402.PRPA_MT101001CA.IdentifiedEntity> controlActEvent, List<IResultDetail> dtls)
        {
            ITerminologyService termSvc = Context.GetService(typeof(ITerminologyService)) as ITerminologyService;

            // Create return value
            RegistrationEvent retVal = CreateComponents<MARC.Everest.RMIM.CA.R020402.PRPA_MT101001CA.IdentifiedEntity>(controlActEvent, dtls);
            var subject = controlActEvent.Subject.RegistrationRequest;

            // Control act event code
            if (!controlActEvent.Code.Code.Equals(PRPA_IN101201CA.GetTriggerEvent().Code))
            {
                dtls.Add(new ResultDetail(ResultDetailType.Error, this.m_localeService.GetString("MSGE00C"), null, null));
                return null;
            }

            if (retVal == null) return null;

            // Create the subject
            Person subjectOf = new Person();

            // Validate
            if (subject.NullFlavor != null)
                dtls.Add(new MandatoryElementMissingResultDetail(ResultDetailType.Error, this.m_localeService.GetString("MSGE003"), null));

            // Custodian of the record
            if (subject.Custodian == null || subject.Custodian.NullFlavor != null ||
                subject.Custodian.AssignedDevice == null || subject.Custodian.AssignedDevice.NullFlavor != null)
                dtls.Add(new MandatoryElementMissingResultDetail(ResultDetailType.Error, this.m_localeService.GetString("MSGE00B"), null));
            else
            {
                retVal.Add(CreateRepositoryDevice(subject.Custodian.AssignedDevice, dtls), "CST",
                    HealthServiceRecordSiteRoleType.PlaceOfRecord | HealthServiceRecordSiteRoleType.ResponsibleFor,
                    new List<SVC.Core.DataTypes.DomainIdentifier>()
                    {
                        CreateDomainIdentifier(subject.Custodian.AssignedDevice.Id)
                    });
            }

            // Replacement of?
            foreach (var rplc in subject.ReplacementOf)
                if (rplc.NullFlavor == null &&
                    rplc.PriorRegistration != null && rplc.PriorRegistration.NullFlavor == null &&
                    rplc.PriorRegistration.Subject != null && rplc.PriorRegistration.Subject.NullFlavor == null &&
                    rplc.PriorRegistration.Subject.PriorRegisteredRole != null && rplc.PriorRegistration.Subject.PriorRegisteredRole.NullFlavor == null)
                    subjectOf.Add(new HealthServiceRecordComponentRef()
                    {
                        AlternateIdentifier = CreateDomainIdentifier(rplc.PriorRegistration.Subject.PriorRegisteredRole.Id)
                    }, Guid.NewGuid().ToString(), HealthServiceRecordSiteRoleType.ReplacementOf,
                    new List<SVC.Core.DataTypes.DomainIdentifier>() {
                        CreateDomainIdentifier(rplc.PriorRegistration.Subject.PriorRegisteredRole.Id)
                    });

            // Process additional data
            var regRole = subject.Subject.registeredRole;

            // Any alternate ids?
            if (regRole.Id != null && !regRole.Id.IsNull)
                foreach (var ii in regRole.Id)
                    if(!ii.IsNull)
                        subjectOf.AlternateIdentifiers.Add(CreateDomainIdentifier(ii));
            
            // Status code
            if (regRole.StatusCode != null && !regRole.StatusCode.IsNull)
                subjectOf.Status = ConvertStatusCode(regRole.StatusCode, dtls);

            // Effective time
            if ((regRole.EffectiveTime == null || regRole.EffectiveTime.NullFlavor != null) && !(bool)controlActEvent.Subject.ContextConductionInd ||
                retVal.EffectiveTime == null && (bool)controlActEvent.Subject.ContextConductionInd)
            {
                dtls.Add(new RequiredElementMissingResultDetail(ResultDetailType.Warning, this.m_localeService.GetString("MSGW005"), null));
                retVal.EffectiveTime = CreateTimestamp(new IVL<TS>(DateTime.Now, new TS() { NullFlavor = NullFlavor.NotApplicable }), dtls);
            }
            else
                retVal.EffectiveTime = CreateTimestamp(regRole.EffectiveTime, dtls);

            // Masking indicator
            if (regRole.ConfidentialityCode != null && !regRole.ConfidentialityCode.IsNull)
                subjectOf.Add(new MaskingIndicator()
                {
                    MaskingCode = CreateCodeValue(regRole.ConfidentialityCode, dtls)
                }, "MSK", HealthServiceRecordSiteRoleType.FilterOf, null);

            // Identified entity check
            var ident = regRole.IdentifiedPerson;
            if (ident == null || ident.NullFlavor != null)
            {
                dtls.Add(new MandatoryElementMissingResultDetail(ResultDetailType.Error, this.m_localeService.GetString("MSGE012"), null));
                return null;
            }

            // Names
            if (ident.Name != null)
            {
                subjectOf.Names = new List<SVC.Core.DataTypes.NameSet>();
                foreach (var nam in ident.Name)
                    if(!nam.IsNull)
                        subjectOf.Names.Add(CreateNameSet(nam, dtls));
            }

            // Telecoms
            if (ident.Telecom != null)
            {
                subjectOf.TelecomAddresses = new List<SVC.Core.DataTypes.TelecommunicationsAddress>();
                foreach(var tel in ident.Telecom)
                {
                    if(tel.IsNull) continue;

                    subjectOf.TelecomAddresses.Add(new SVC.Core.DataTypes.TelecommunicationsAddress()
                    {
                        Use = Util.ToWireFormat(tel.Use),
                        Value = tel.Value
                    });

                    // Store usable period as an extension as it is not storable here
                    if (tel.UseablePeriod != null && !tel.UseablePeriod.IsNull)
                    {
                        if (tel.UseablePeriod.Hull is IVL<TS>)
                            subjectOf.TelecomAddresses.Last().UsablePeriod = CreateTimestamp(tel.UseablePeriod.Hull as IVL<TS>, dtls);
                        else
                            subjectOf.Add(new ExtendedAttribute()
                            {
                                PropertyPath = String.Format("TelecomAddresses[{0}]", subjectOf.TelecomAddresses.Count - 1),
                                Value = tel.UseablePeriod.Hull,
                                Name = "UsablePeriod"
                            });
                    }
                }
            }

            // Gender
            if (ident.AdministrativeGenderCode != null && ident.AdministrativeGenderCode.IsNull)
                subjectOf.GenderCode = Util.ToWireFormat(ident.AdministrativeGenderCode);

            // Birth
            if (ident.BirthTime != null && !ident.BirthTime.IsNull)
                subjectOf.BirthTime = CreateTimestamp(ident.BirthTime, dtls);

            // Deceased
            if (ident.DeceasedInd != null && !ident.DeceasedInd.IsNull)
                dtls.Add(new NotImplementedElementResultDetail(ResultDetailType.Warning, "DeceasedInd", this.m_localeService.GetString("MSGW006"), null));
            if (ident.DeceasedTime != null && !ident.DeceasedTime.IsNull)
                subjectOf.DeceasedTime = CreateTimestamp(ident.DeceasedTime, dtls);

            // Multiple Birth
            if(ident.MultipleBirthInd != null && !ident.MultipleBirthInd.IsNull)
                dtls.Add(new NotImplementedElementResultDetail(ResultDetailType.Warning, "DeceasedInd", this.m_localeService.GetString("MSGW007"), null));
            if (ident.MultipleBirthOrderNumber != null && !ident.MultipleBirthOrderNumber.IsNull)
                subjectOf.BirthOrder = ident.MultipleBirthOrderNumber;

            // Address(es)
            if (ident.Addr != null)
            {
                subjectOf.Addresses = new List<SVC.Core.DataTypes.AddressSet>(ident.Addr.Count);
                foreach (var addr in ident.Addr)
                    if(!addr.IsNull)
                        subjectOf.Addresses.Add(CreateAddressSet(addr, dtls));
            }

            // As other identifiers
            if (ident.AsOtherIDs != null)
            {
                subjectOf.OtherIdentifiers = new List<KeyValuePair<CodeValue, DomainIdentifier>>();
                foreach (var id in ident.AsOtherIDs)
                    if (id.NullFlavor != null)
                        subjectOf.OtherIdentifiers.Add(new KeyValuePair<CodeValue, DomainIdentifier>(
                            CreateCodeValue(id.Code, dtls),
                            CreateDomainIdentifier(id.Id)
                         ));
            }

            // Languages
            if (ident.LanguageCommunication != null)
            {
                subjectOf.Language = new List<PersonLanguage>();
                foreach (var lang in ident.LanguageCommunication)
                {
                    if (lang.NullFlavor != null) continue;

                    PersonLanguage pl = new PersonLanguage();

                    CodeValue languageCode = CreateCodeValue(lang.LanguageCode, dtls);
                    // Default ISO 639-3
                    languageCode.CodeSystem = languageCode.CodeSystem ?? "2.16.840.1.113883.6.121";

                    // Validate the language code
                    if (languageCode.CodeSystem != "2.16.840.1.113883.6.121" &&
                        languageCode.CodeSystem != "2.16.840.1.113883.6.99")
                        dtls.Add(new VocabularyIssueResultDetail(ResultDetailType.Error, this.m_localeService.GetString("MSGE04B"), null));

                    // Translate the language code
                    if (languageCode.CodeSystem == "2.16.840.1.113883.6.121") // we need to translate
                        languageCode = termSvc.Translate(languageCode, "2.16.840.1.113883.6.99");

                    if (languageCode == null)
                        dtls.Add(new VocabularyIssueResultDetail(ResultDetailType.Error, this.m_localeService.GetString("MSGE04C"), null, null));
                    else
                        pl.Language = languageCode.Code;
                    
                    // Preferred? 
                    if ((bool)lang.PreferenceInd)
                        pl.Type = LanguageType.Fluency;
                    else
                        pl.Type = LanguageType.WrittenAndSpoken;

                    // Add
                    subjectOf.Language.Add(pl);
                }
            }

            // Personal relationship
            // TODO: 
            // Error?
            if (dtls.Exists(o => o.Type == ResultDetailType.Error))
                retVal = null;

            return retVal;
        }

        

        /// <summary>
        /// Convert status code
        /// </summary>
        private StatusType ConvertStatusCode(MARC.Everest.DataTypes.CS<RoleStatus> status, List<IResultDetail> dtls)
        {

            if(status.Code.IsAlternateCodeSpecified)
            {
                dtls.Add(new VocabularyIssueResultDetail(ResultDetailType.Error, m_localeService.GetString("MSGE010"), null, null));
                return StatusType.Unknown;
            }

            // Status
            switch ((RoleStatus)status)
            {
                case RoleStatus.Active:
                    return StatusType.Active;
                case RoleStatus.Cancelled:
                    return StatusType.Cancelled;
                case RoleStatus.Nullified:
                    return StatusType.Nullified;
                case RoleStatus.Pending:
                    return StatusType.New;
                case RoleStatus.Suspended:
                    return StatusType.Aborted;
                case RoleStatus.Terminated:
                    return StatusType.Obsolete;
                case RoleStatus.Normal:
                    dtls.Add(new VocabularyIssueResultDetail(ResultDetailType.Error, m_localeService.GetString("MSGE011"), null, null));
                    return StatusType.Unknown;
                default:
                    dtls.Add(new VocabularyIssueResultDetail(ResultDetailType.Error, m_localeService.GetString("MSGE010"), null, null));
                    return StatusType.Unknown;
            }
        }

        /// <summary>
        /// Create Repository Device
        /// </summary>
        private RepositoryDevice CreateRepositoryDevice(MARC.Everest.RMIM.CA.R020402.COCT_MT090310CA.AssignedDevice assignedDevice, List<IResultDetail> dtls)
        {

            RepositoryDevice retVal = new RepositoryDevice();

            // Identifier for the device
            if (assignedDevice.Id == null || assignedDevice.Id.IsNull)
                dtls.Add(new MandatoryElementMissingResultDetail(ResultDetailType.Error, m_localeService.GetString("MSGE00D"), null));
            else
                retVal.AlternateIdentifier = CreateDomainIdentifier(assignedDevice.Id);

            // Repository jurisdiction
            if (assignedDevice.RepresentedRepositoryJurisdiction == null || assignedDevice.RepresentedRepositoryJurisdiction.NullFlavor != null ||
                assignedDevice.RepresentedRepositoryJurisdiction.Name == null || assignedDevice.RepresentedRepositoryJurisdiction.Name.IsNull)
                dtls.Add(new MandatoryElementMissingResultDetail(ResultDetailType.Error, m_localeService.GetString("MSGE00E"), null));
            else
                retVal.Jurisdiction = assignedDevice.RepresentedRepositoryJurisdiction.Name;

            // Assigned repository
            if (assignedDevice.AssignedRepository == null || assignedDevice.AssignedRepository.NullFlavor != null ||
                assignedDevice.AssignedRepository.Name == null || assignedDevice.AssignedRepository.Name.IsNull)
                dtls.Add(new MandatoryElementMissingResultDetail(ResultDetailType.Error, m_localeService.GetString("MSGE00F"), null));
            else
                retVal.Name = assignedDevice.AssignedRepository.Name;

            return retVal;
        }
    }
}
