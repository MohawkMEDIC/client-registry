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

            retVal.EventClassifier = RegistrationEventType.Register;
            retVal.EventType = new CodeValue("REG");
            retVal.Status = StatusType.Completed;

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
            {
                subjectOf.AlternateIdentifiers = new List<DomainIdentifier>();
                foreach (var ii in regRole.Id)
                    if (!ii.IsNull)
                        subjectOf.AlternateIdentifiers.Add(CreateDomainIdentifier(ii));

            }

            // Status code
            if (regRole.StatusCode != null && !regRole.StatusCode.IsNull)
                subjectOf.Status = ConvertStatusCode(regRole.StatusCode, dtls);

            // Effective time
            if (subjectOf.Status == StatusType.Active || (regRole.EffectiveTime == null || regRole.EffectiveTime.NullFlavor != null) && !(bool)controlActEvent.Subject.ContextConductionInd ||
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
                        subjectOf.Add(new ExtendedAttribute()
                        {
                            PropertyPath = String.Format("TelecomAddresses[{0}]", tel.Value),
                            Value = tel.UseablePeriod.Hull,
                            Name = "UsablePeriod"
                        });
                    }
                }
            }

            // Gender
            if (ident.AdministrativeGenderCode != null && !ident.AdministrativeGenderCode.IsNull)
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
                    if (id.NullFlavor == null)
                    {
                        subjectOf.OtherIdentifiers.Add(new KeyValuePair<CodeValue, DomainIdentifier>(
                            CreateCodeValue(id.Code, dtls),
                            CreateDomainIdentifier(id.Id)
                         ));
                        if (id.AssigningIdOrganization != null && id.AssigningIdOrganization.NullFlavor == null)
                        {
                            // Other identifier assigning organization ext
                            if (id.AssigningIdOrganization.Id != null && !id.AssigningIdOrganization.Id.IsNull)
                                subjectOf.Add(new ExtendedAttribute()
                                {
                                    PropertyPath = String.Format("OtherIdentifiers[{0}{1}]", id.Id.Root, id.Id.Extension ),
                                    Value = CreateDomainIdentifier(id.AssigningIdOrganization.Id),
                                    Name = "AssigningIdOrganizationId"
                                });
                            // Other identifier assigning organization name
                            if (id.AssigningIdOrganization.Name != null && !id.AssigningIdOrganization.Name.IsNull)
                                subjectOf.Add(new ExtendedAttribute()
                                {
                                    PropertyPath = String.Format("OtherIdentifiers[{0}{1}]", id.Id.Root, id.Id.Extension),
                                    Value = id.AssigningIdOrganization.Name.ToString(),
                                    Name = "AssigningIdOrganizationName"
                                });

                        }
                    }
            }

            // Languages
            if (ident.LanguageCommunication != null)
            {
                subjectOf.Language = new List<PersonLanguage>();
                foreach (var lang in ident.LanguageCommunication)
                {
                    if (lang == null || lang.NullFlavor != null) continue;

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
            if (ident.PersonalRelationship != null)
                foreach (var psn in ident.PersonalRelationship)
                    if (psn.NullFlavor == null && psn.RelationshipHolder != null &&
                        psn.RelationshipHolder.NullFlavor == null)
                        subjectOf.Add(new PersonalRelationship()
                        {
                            AlternateIdentifiers = new List<DomainIdentifier>() {  CreateDomainIdentifier(psn.RelationshipHolder.Id) },
                            LegalName = CreateNameSet(psn.RelationshipHolder.Name, dtls),
                            RelationshipKind = Util.ToWireFormat(psn.Code),
                            Status = StatusType.Active
                        }, Guid.NewGuid().ToString(), HealthServiceRecordSiteRoleType.RepresentitiveOf, null);

            retVal.Add(subjectOf, "SUBJ", HealthServiceRecordSiteRoleType.SubjectOf,
                subjectOf.AlternateIdentifiers);
            // Error?
            if (dtls.Exists(o => o.Type == ResultDetailType.Error))
                retVal = null;

            return retVal;
        }

        /// <summary>
        /// Create components for the update event
        /// </summary>
        internal RegistrationEvent CreateComponents(MARC.Everest.RMIM.CA.R020402.MFMI_MT700711CA.ControlActEvent<MARC.Everest.RMIM.CA.R020402.PRPA_MT101002CA.IdentifiedEntity> controlActEvent, List<IResultDetail> dtls)
        {
            ITerminologyService termSvc = Context.GetService(typeof(ITerminologyService)) as ITerminologyService;

            // Create return value
            RegistrationEvent retVal = CreateComponents<MARC.Everest.RMIM.CA.R020402.PRPA_MT101002CA.IdentifiedEntity>(controlActEvent, dtls);
            var subject = controlActEvent.Subject.RegistrationRequest;

            retVal.EventClassifier = RegistrationEventType.Register;
            retVal.EventType = new CodeValue("REG");
            retVal.Status = StatusType.Completed;

            // Control act event code
            if (!controlActEvent.Code.Code.Equals(PRPA_IN101204CA.GetTriggerEvent().Code))
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
            {
                subjectOf.AlternateIdentifiers = new List<DomainIdentifier>();
                foreach (var ii in regRole.Id)
                    if (!ii.IsNull)
                        subjectOf.AlternateIdentifiers.Add(CreateDomainIdentifier(ii));

            }

            // Status code
            if (regRole.StatusCode != null && !regRole.StatusCode.IsNull)
                subjectOf.Status = ConvertStatusCode(regRole.StatusCode, dtls);

            // Effective time
            if (subjectOf.Status == StatusType.Active || (regRole.EffectiveTime == null || regRole.EffectiveTime.NullFlavor != null) && !(bool)controlActEvent.Subject.ContextConductionInd ||
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
                    if (!nam.IsNull)
                        subjectOf.Names.Add(CreateNameSet(nam, dtls));
            }

            // Telecoms
            if (ident.Telecom != null)
            {
                subjectOf.TelecomAddresses = new List<SVC.Core.DataTypes.TelecommunicationsAddress>();
                foreach (var tel in ident.Telecom)
                {
                    if (tel.IsNull) continue;

                    subjectOf.TelecomAddresses.Add(new SVC.Core.DataTypes.TelecommunicationsAddress()
                    {
                        Use = Util.ToWireFormat(tel.Use),
                        Value = tel.Value
                    });

                    // Store usable period as an extension as it is not storable here
                    if (tel.UseablePeriod != null && !tel.UseablePeriod.IsNull)
                    {
                        subjectOf.Add(new ExtendedAttribute()
                        {
                            PropertyPath = String.Format("TelecomAddresses[{0}]", tel.Value),
                            Value = tel.UseablePeriod.Hull,
                            Name = "UsablePeriod"
                        });
                    }
                }
            }

            // Gender
            if (ident.AdministrativeGenderCode != null && !ident.AdministrativeGenderCode.IsNull)
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
            if (ident.MultipleBirthInd != null && !ident.MultipleBirthInd.IsNull)
                dtls.Add(new NotImplementedElementResultDetail(ResultDetailType.Warning, "DeceasedInd", this.m_localeService.GetString("MSGW007"), null));
            if (ident.MultipleBirthOrderNumber != null && !ident.MultipleBirthOrderNumber.IsNull)
                subjectOf.BirthOrder = ident.MultipleBirthOrderNumber;

            // Address(es)
            if (ident.Addr != null)
            {
                subjectOf.Addresses = new List<SVC.Core.DataTypes.AddressSet>(ident.Addr.Count);
                foreach (var addr in ident.Addr)
                    if (!addr.IsNull)
                        subjectOf.Addresses.Add(CreateAddressSet(addr, dtls));
            }

            // As other identifiers
            if (ident.AsOtherIDs != null)
            {
                subjectOf.OtherIdentifiers = new List<KeyValuePair<CodeValue, DomainIdentifier>>();
                foreach (var id in ident.AsOtherIDs)
                    if (id.NullFlavor == null)
                    {
                        subjectOf.OtherIdentifiers.Add(new KeyValuePair<CodeValue, DomainIdentifier>(
                            CreateCodeValue(id.Code, dtls),
                            CreateDomainIdentifier(id.Id)
                         ));
                        if(id.AssigningIdOrganization != null && id.AssigningIdOrganization.NullFlavor == null)
                        {
                            // Other identifier assigning organization ext
                            if (id.AssigningIdOrganization.Id != null && !id.AssigningIdOrganization.Id.IsNull)
                                subjectOf.Add(new ExtendedAttribute()
                                {
                                    PropertyPath = String.Format("OtherIdentifiers[{0}{1}]", id.Id.Root, id.Id.Extension),
                                    Value = CreateDomainIdentifier(id.AssigningIdOrganization.Id),
                                    Name = "AssigningIdOrganizationId"
                                });
                            // Other identifier assigning organization name
                            if (id.AssigningIdOrganization.Name != null && !id.AssigningIdOrganization.Name.IsNull)
                                subjectOf.Add(new ExtendedAttribute()
                                {
                                    PropertyPath = String.Format("OtherIdentifiers[{0}{1}]", id.Id.Root, id.Id.Extension),
                                    Value = id.AssigningIdOrganization.Name.ToString(),
                                    Name = "AssigningIdOrganizationName"
                                });

                        }
                    }
            }

            // Languages
            if (ident.LanguageCommunication != null)
            {
                subjectOf.Language = new List<PersonLanguage>();
                foreach (var lang in ident.LanguageCommunication)
                {
                    if (lang == null || lang.NullFlavor != null) continue;

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
            if (ident.PersonalRelationship != null)
                foreach (var psn in ident.PersonalRelationship)
                    if (psn.NullFlavor == null && psn.RelationshipHolder != null &&
                        psn.RelationshipHolder.NullFlavor == null)
                        subjectOf.Add(new PersonalRelationship()
                        {
                            AlternateIdentifiers = new List<DomainIdentifier>() { CreateDomainIdentifier(psn.RelationshipHolder.Id) },
                            LegalName = CreateNameSet(psn.RelationshipHolder.Name, dtls),
                            RelationshipKind = Util.ToWireFormat(psn.Code),
                            Status = StatusType.Active
                        }, Guid.NewGuid().ToString(), HealthServiceRecordSiteRoleType.RepresentitiveOf, null);

            retVal.Add(subjectOf, "SUBJ", HealthServiceRecordSiteRoleType.SubjectOf,
                subjectOf.AlternateIdentifiers);
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



        /// <summary>
        /// Create a query match for find candidates
        /// </summary>
        internal RegistrationEvent CreateQueryMatch(MARC.Everest.RMIM.CA.R020402.MFMI_MT700751CA.ControlActEvent<MARC.Everest.RMIM.CA.R020402.PRPA_MT101103CA.ParameterList> controlActEvent, List<IResultDetail> dtls, ref List<VersionedDomainIdentifier> recordIds)
        {
            ITerminologyService termSvc = Context.GetService(typeof(ITerminologyService)) as ITerminologyService;

            // Details about the query
            if (!controlActEvent.Code.Code.Equals(PRPA_IN101103CA.GetTriggerEvent().Code))
            {
                dtls.Add(new ResultDetail(ResultDetailType.Error, this.m_localeService.GetString("MSGE00C"), null, null));
                return null;
            }

            // REturn value
            RegistrationEvent retVal = CreateComponents<MARC.Everest.RMIM.CA.R020402.PRPA_MT101103CA.ParameterList>(controlActEvent, dtls);
            
            // Filter
            RegistrationEvent filter = new RegistrationEvent();
            retVal.Add(filter, "QRY", HealthServiceRecordSiteRoleType.FilterOf, null);

            // Parameter list validation
            var parameterList = controlActEvent.QueryByParameter.parameterList;
            if(parameterList == null || parameterList.NullFlavor != null)
            {
                dtls.Add(new MandatoryElementMissingResultDetail(ResultDetailType.Error,this.m_localeService.GetString("MSGE04E"), null));
                parameterList = new MARC.Everest.RMIM.CA.R020402.PRPA_MT101103CA.ParameterList();
            }

           
            // Create the actual query
            Person filterPerson = new Person();

            // Discrete record identifiers
            recordIds = new List<VersionedDomainIdentifier>(100);
            filterPerson.AlternateIdentifiers = new List<DomainIdentifier>();
            foreach (var recId in parameterList.ClientId)
                if (recId != null &&
                    recId.NullFlavor == null && recId.Value != null &&
                    !recId.Value.IsNull)
                    filterPerson.AlternateIdentifiers.Add(new VersionedDomainIdentifier()
                    {
                        Domain = recId.Value.Root,
                        Identifier = recId.Value.Extension
                    });


            // Admin gender
            if (parameterList.AdministrativeGender != null &&
                parameterList.AdministrativeGender.NullFlavor == null &&
                !parameterList.AdministrativeGender.Value.IsNull)
                filterPerson.GenderCode = Util.ToWireFormat(parameterList.AdministrativeGender.Value);
            
            // Deceased flags
            if (parameterList.DeceasedTime != null &&
                parameterList.DeceasedTime.NullFlavor == null &&
                parameterList.DeceasedTime.Value != null &&
                !parameterList.DeceasedTime.Value.IsNull)
                filterPerson.DeceasedTime = CreateTimestamp(parameterList.DeceasedTime.Value, dtls);
            else if (parameterList.DeceasedIndicator != null &&
                parameterList.DeceasedIndicator.NullFlavor == null &&
                parameterList.DeceasedIndicator.Value != null &&
                !parameterList.DeceasedIndicator.Value.IsNull)
                filterPerson.DeceasedTime = new TimestampPart();

            // Fathers name, becomes a personal relationship with a name
            if (parameterList.FathersName != null &&
                parameterList.FathersName.NullFlavor == null &&
                parameterList.FathersName.Value != null &&
                !parameterList.FathersName.Value.IsNull)
            {
                filterPerson.Add(new PersonalRelationship()
                {
                    RelationshipKind = Util.ToWireFormat(PersonalRelationshipRoleType.Father),
                    LegalName = CreateNameSet(parameterList.FathersName.Value, dtls),
                    Status = StatusType.Active
                }, "FTH", HealthServiceRecordSiteRoleType.RepresentitiveOf, null);
            }
            
            // Mother's name
            if (parameterList.MothersMaidenName != null &&
                parameterList.MothersMaidenName.NullFlavor == null &&
                parameterList.MothersMaidenName.Value != null &&
                !parameterList.MothersMaidenName.Value.IsNull)
            {
                var rltn = new PersonalRelationship()
                {
                    RelationshipKind = Util.ToWireFormat(PersonalRelationshipRoleType.Mother),
                    LegalName = CreateNameSet(parameterList.FathersName.Value, dtls),
                    Status = StatusType.Active
                };
              
                rltn.LegalName.Use = NameSet.NameSetUse.MaidenName;
                filterPerson.Add(rltn, "MTH", HealthServiceRecordSiteRoleType.RepresentitiveOf, null);
            }

            // Language code
            if (parameterList.LanguageCode != null &&
                parameterList.LanguageCode.NullFlavor == null &&
                parameterList.LanguageCode.Value != null &&
                !parameterList.LanguageCode.Value.IsNull)
            {
                filterPerson.Language = new List<PersonLanguage>();
                var lang = parameterList.LanguageCode.Value;
                
                PersonLanguage pl = new PersonLanguage();

                CodeValue languageCode = CreateCodeValue(lang, dtls);
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
                pl.Type = LanguageType.WrittenAndSpoken;

                // Add
                filterPerson.Language.Add(pl);
            }

            // Mutliple birth
            if (parameterList.MultipleBirthOrderNumber != null &&
                parameterList.MultipleBirthOrderNumber.NullFlavor == null &&
                parameterList.MultipleBirthOrderNumber.Value != null &&
                !parameterList.MultipleBirthOrderNumber.Value.IsNull)
                filterPerson.BirthOrder = parameterList.MultipleBirthOrderNumber.Value;
            else if (parameterList.MultipleBirthIndicator != null &&
                parameterList.MultipleBirthIndicator.NullFlavor == null &&
                parameterList.MultipleBirthIndicator.Value != null &&
                !parameterList.MultipleBirthIndicator.Value.IsNull)
                filterPerson.BirthOrder = -1;

            // Addresses
            foreach (var addr in parameterList.PersonAddress)
                if (addr != null && addr.NullFlavor == null &&
                    addr.Value != null && !addr.Value.IsNull)
                {
                    if (filterPerson.Addresses == null)
                        filterPerson.Addresses = new List<AddressSet>();
                    filterPerson.Addresses.Add(CreateAddressSet(addr.Value, dtls));
                }

            // Personal relationship code (reverse)
            if (parameterList.PersonalRelationshipCode != null &&
                parameterList.PersonalRelationshipCode.NullFlavor == null &&
                parameterList.PersonalRelationshipCode.Value != null &&
                !parameterList.PersonalRelationshipCode.Value.IsNull)
                filterPerson.Add(new PersonalRelationship()
                {
                    RelationshipKind = Util.ToWireFormat(parameterList.PersonalRelationshipCode.Value),
                    Status = StatusType.Active
                }, "RLTN", HealthServiceRecordSiteRoleType.RepresentitiveOf | HealthServiceRecordSiteRoleType.Inverse, null);

            // Birth time
            if (parameterList.PersonBirthtime != null &&
                parameterList.PersonBirthtime.NullFlavor == null &&
                parameterList.PersonBirthtime.Value != null &&
                !parameterList.PersonBirthtime.Value.IsNull)
                filterPerson.BirthTime = CreateTimestamp(parameterList.PersonBirthtime.Value, dtls);

            // Person name
            foreach (var name in parameterList.PersonName)
                if (name != null && name.NullFlavor == null &&
                    name.Value != null && !name.Value.IsNull)
                {
                    if (filterPerson.Names == null)
                        filterPerson.Names = new List<NameSet>();
                    filterPerson.Names.Add(CreateNameSet(name.Value, dtls));
                }

            // Telecoms
            foreach(var tel in parameterList.PersonTelecom)
                if (tel != null && tel.NullFlavor == null &&
                    tel.Value != null && !tel.Value.IsNull)
                {
                    if (filterPerson.TelecomAddresses == null)
                        filterPerson.TelecomAddresses = new List<TelecommunicationsAddress>();
                    filterPerson.TelecomAddresses.Add(new TelecommunicationsAddress()
                    {
                        Use = Util.ToWireFormat(tel.Value.Use),
                        Value = tel.Value.Value
                    });
                }

            // Filter
            filter.Add(filterPerson, "SUBJ", HealthServiceRecordSiteRoleType.SubjectOf, null);
            // Determine if errors exist that prevent the processing of this message
            if (dtls.Count(o => o.Type == ResultDetailType.Error) > 0)
                return null;

            return retVal;
        }


        /// <summary>
        /// Create a query match parameter for the get message
        /// </summary>
        internal RegistrationEvent CreateQueryMatch(MARC.Everest.RMIM.CA.R020402.MFMI_MT700751CA.ControlActEvent<MARC.Everest.RMIM.CA.R020402.PRPA_MT101101CA.ParameterList> controlActEvent, List<IResultDetail> dtls, ref List<VersionedDomainIdentifier> recordIds)
        {
            ITerminologyService termSvc = Context.GetService(typeof(ITerminologyService)) as ITerminologyService;

            // Details about the query
            if (!controlActEvent.Code.Code.Equals(PRPA_IN101105CA.GetTriggerEvent().Code))
            {
                dtls.Add(new ResultDetail(ResultDetailType.Error, this.m_localeService.GetString("MSGE00C"), null, null));
                return null;
            }

            // REturn value
            RegistrationEvent retVal = CreateComponents<MARC.Everest.RMIM.CA.R020402.PRPA_MT101101CA.ParameterList>(controlActEvent, dtls);

            // Filter
            RegistrationEvent filter = new RegistrationEvent();
            retVal.Add(filter, "QRY", HealthServiceRecordSiteRoleType.FilterOf, null);

            // Parameter list validation
            var parameterList = controlActEvent.QueryByParameter.parameterList;
            if (parameterList == null || parameterList.NullFlavor != null)
            {
                dtls.Add(new MandatoryElementMissingResultDetail(ResultDetailType.Error, this.m_localeService.GetString("MSGE04E"), null));
                parameterList = new MARC.Everest.RMIM.CA.R020402.PRPA_MT101101CA.ParameterList();
            }

            // Discrete record identifiers
            recordIds = new List<VersionedDomainIdentifier>(100);

            // Create the actual query
            Person filterPerson = new Person();

            // Alternate identifiers
            filterPerson.AlternateIdentifiers = new List<DomainIdentifier>();
            if (parameterList.ClientIDBus != null &&
                    parameterList.ClientIDBus.NullFlavor == null && parameterList.ClientIDBus.Value != null &&
                    !parameterList.ClientIDBus.Value.IsNull)
                filterPerson.AlternateIdentifiers.Add(new VersionedDomainIdentifier()
                {
                    Domain = parameterList.ClientIDBus.Value.Root,
                    Identifier = parameterList.ClientIDBus.Value.Extension
                });

            // Other identifiers
            filterPerson.OtherIdentifiers = new List<KeyValuePair<CodeValue, DomainIdentifier>>();
            if (parameterList.ClientIDPub != null &&
                    parameterList.ClientIDPub.NullFlavor == null && parameterList.ClientIDPub.Value != null &&
                    !parameterList.ClientIDPub.Value.IsNull)
                filterPerson.OtherIdentifiers.Add(new KeyValuePair<CodeValue,DomainIdentifier>(new CodeValue(), new DomainIdentifier()
                {
                    Domain = parameterList.ClientIDPub.Value.Root,
                    Identifier = parameterList.ClientIDPub.Value.Extension
                }));

            // Filter
            filter.Add(filterPerson, "SUBJ", HealthServiceRecordSiteRoleType.SubjectOf, null);
            // Determine if errors exist that prevent the processing of this message
            if (dtls.Count(o => o.Type == ResultDetailType.Error) > 0)
                return null;

            return retVal;
        }

    }
}
