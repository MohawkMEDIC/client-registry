/**
 * Copyright 2015-2015 Mohawk College of Applied Arts and Technology
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
using MARC.HI.EHRS.CR.Core.ComponentModel;
using MARC.HI.EHRS.SVC.Core.Services;
using MARC.Everest.Connectors;
using MARC.Everest.RMIM.CA.R020403.Interactions;
using MARC.HI.EHRS.SVC.Core.ComponentModel.Components;
using MARC.HI.EHRS.SVC.Core.ComponentModel;
using MARC.Everest.RMIM.CA.R020403.Vocabulary;
using MARC.Everest.DataTypes;
using MARC.HI.EHRS.SVC.Core.DataTypes;
using System.ComponentModel;

namespace MARC.HI.EHRS.CR.Messaging.Everest
{
    /// <summary>
    /// Component utility
    /// </summary>
    public partial class CaComponentUtil : ComponentUtil
    {

        /// <summary>
        /// Create a registration event for a registration event
        /// </summary>
        internal RegistrationEvent CreateComponents(MARC.Everest.RMIM.CA.R020403.MFMI_MT700711CA.ControlActEvent<MARC.Everest.RMIM.CA.R020403.PRPA_MT101001CA.IdentifiedEntity> controlActEvent, List<IResultDetail> dtls)
        {
            ITerminologyService termSvc = Context.GetService(typeof(ITerminologyService)) as ITerminologyService;
            ISystemConfigurationService config = Context.GetService(typeof(ISystemConfigurationService)) as ISystemConfigurationService;

            // Create return value
            RegistrationEvent retVal = CreateComponents<MARC.Everest.RMIM.CA.R020403.PRPA_MT101001CA.IdentifiedEntity>(controlActEvent, dtls);
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
            subjectOf.RoleCode = PersonRole.PAT;

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
                        CreateDomainIdentifier(subject.Custodian.AssignedDevice.Id, dtls)
                    });
            }

            // Replacement of?
            foreach (var rplc in subject.ReplacementOf)
                if (rplc.NullFlavor == null &&
                    rplc.PriorRegistration != null && rplc.PriorRegistration.NullFlavor == null &&
                    rplc.PriorRegistration.Subject != null && rplc.PriorRegistration.Subject.NullFlavor == null &&
                    rplc.PriorRegistration.Subject.PriorRegisteredRole != null && rplc.PriorRegistration.Subject.PriorRegisteredRole.NullFlavor == null)
                    subjectOf.Add(new PersonRegistrationRef()
                    {
                        AlternateIdentifiers = new List<DomainIdentifier>() { CreateDomainIdentifier(rplc.PriorRegistration.Subject.PriorRegisteredRole.Id, dtls) }
                    }, Guid.NewGuid().ToString(), HealthServiceRecordSiteRoleType.ReplacementOf,
                    new List<SVC.Core.DataTypes.DomainIdentifier>() {
                        CreateDomainIdentifier(rplc.PriorRegistration.Subject.PriorRegisteredRole.Id, dtls)
                    });

            // Process additional data
            var regRole = subject.Subject.registeredRole;

            // Any alternate ids?
            if (regRole.Id != null && !regRole.Id.IsNull)
            {
                subjectOf.AlternateIdentifiers = new List<DomainIdentifier>();
                foreach (var ii in regRole.Id)
                    if (!ii.IsNull)
                        subjectOf.AlternateIdentifiers.Add(CreateDomainIdentifier(ii, dtls));

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
                subjectOf.Names = new List<SVC.Core.DataTypes.NameSet>(ident.Name.Count);
                foreach (var nam in ident.Name)
                    if(!nam.IsNull)
                        subjectOf.Names.Add(CreateNameSet(nam, dtls));
            }

            // Telecoms
            if (ident.Telecom != null)
            {
                subjectOf.TelecomAddresses = new List<SVC.Core.DataTypes.TelecommunicationsAddress>(ident.Telecom.Count);
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
                subjectOf.OtherIdentifiers = new List<KeyValuePair<CodeValue, DomainIdentifier>>(ident.AsOtherIDs.Count);
                foreach (var id in ident.AsOtherIDs)
                    if (id.NullFlavor == null)
                    {
                        subjectOf.OtherIdentifiers.Add(new KeyValuePair<CodeValue, DomainIdentifier>(
                            CreateCodeValue(id.Code, dtls),
                            CreateDomainIdentifier(id.Id, dtls)
                         ));
                        if (id.AssigningIdOrganization != null && id.AssigningIdOrganization.NullFlavor == null)
                        {
                            // Other identifier assigning organization ext
                            if (id.AssigningIdOrganization.Id != null && !id.AssigningIdOrganization.Id.IsNull)
                                subjectOf.Add(new ExtendedAttribute()
                                {
                                    PropertyPath = String.Format("OtherIdentifiers[{0}{1}]", id.Id.Root, id.Id.Extension ),
                                    Value = CreateDomainIdentifier(id.AssigningIdOrganization.Id, dtls),
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
                subjectOf.Language = new List<PersonLanguage>(ident.LanguageCommunication.Count);
                foreach (var lang in ident.LanguageCommunication)
                {
                    if (lang == null || lang.NullFlavor != null) continue;

                    PersonLanguage pl = new PersonLanguage();

                    CodeValue languageCode = CreateCodeValue(lang.LanguageCode, dtls);
                    // Default ISO 639-3
                    languageCode.CodeSystem = languageCode.CodeSystem ?? config.OidRegistrar.GetOid("ISO639-3").Oid;

                    // Validate the language code
                    if (languageCode.CodeSystem != config.OidRegistrar.GetOid("ISO639-3").Oid &&
                        languageCode.CodeSystem != config.OidRegistrar.GetOid("ISO639-1").Oid)
                        dtls.Add(new VocabularyIssueResultDetail(ResultDetailType.Error, this.m_localeService.GetString("MSGE04B"), null));

                    // Translate the language code
                    if (languageCode.CodeSystem == config.OidRegistrar.GetOid("ISO639-3").Oid) // we need to translate
                        languageCode = termSvc.Translate(languageCode, config.OidRegistrar.GetOid("ISO639-1").Oid);

                    if (languageCode == null)
                        dtls.Add(new VocabularyIssueResultDetail(ResultDetailType.Error, this.m_localeService.GetString("MSGE04C"), null, null));
                    else
                        pl.Language = languageCode.Code;

                    pl.Type = 0;
                    // Preferred? 
                    if ((bool)lang.PreferenceInd)
                        pl.Type = LanguageType.Preferred;
                    
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
                            AlternateIdentifiers = new List<DomainIdentifier>(ident.PersonalRelationship.Count) {  CreateDomainIdentifier(psn.RelationshipHolder.Id, dtls) },
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
        internal RegistrationEvent CreateComponents(MARC.Everest.RMIM.CA.R020403.MFMI_MT700711CA.ControlActEvent<MARC.Everest.RMIM.CA.R020403.PRPA_MT101002CA.IdentifiedEntity> controlActEvent, List<IResultDetail> dtls)
        {
            ITerminologyService termSvc = Context.GetService(typeof(ITerminologyService)) as ITerminologyService;
            ISystemConfigurationService config = Context.GetService(typeof(ISystemConfigurationService)) as ISystemConfigurationService;

            // Create return value
            RegistrationEvent retVal = CreateComponents<MARC.Everest.RMIM.CA.R020403.PRPA_MT101002CA.IdentifiedEntity>(controlActEvent, dtls);
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
            subjectOf.RoleCode = PersonRole.PAT;

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
                        CreateDomainIdentifier(subject.Custodian.AssignedDevice.Id, dtls)
                    });
            }
            
            // Replacement of?
            foreach (var rplc in subject.ReplacementOf)
                if (rplc.NullFlavor == null &&
                    rplc.PriorRegistration != null && rplc.PriorRegistration.NullFlavor == null &&
                    rplc.PriorRegistration.Subject != null && rplc.PriorRegistration.Subject.NullFlavor == null &&
                    rplc.PriorRegistration.Subject.PriorRegisteredRole != null && rplc.PriorRegistration.Subject.PriorRegisteredRole.NullFlavor == null)
                    subjectOf.Add(new PersonRegistrationRef()
                    {
                        AlternateIdentifiers = new List<DomainIdentifier>() { CreateDomainIdentifier(rplc.PriorRegistration.Subject.PriorRegisteredRole.Id, dtls) }
                    }, Guid.NewGuid().ToString(), HealthServiceRecordSiteRoleType.ReplacementOf,
                    new List<SVC.Core.DataTypes.DomainIdentifier>() {
                        CreateDomainIdentifier(rplc.PriorRegistration.Subject.PriorRegisteredRole.Id, dtls)
                    });

            // Process additional data
            var regRole = subject.Subject.registeredRole;

            // Any alternate ids?
            if (regRole.Id != null && !regRole.Id.IsNull)
            {
                subjectOf.AlternateIdentifiers = new List<DomainIdentifier>(regRole.Id.Count);
                foreach (var ii in regRole.Id)
                    if (!ii.IsNull)
                        subjectOf.AlternateIdentifiers.Add(CreateDomainIdentifier(ii, dtls));

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
                subjectOf.Names = new List<SVC.Core.DataTypes.NameSet>(ident.Name.Count);
                foreach (var nam in ident.Name)
                    if (!nam.IsNull)
                        subjectOf.Names.Add(CreateNameSet(nam, dtls));
            }

            // Telecoms
            if (ident.Telecom != null)
            {
                subjectOf.TelecomAddresses = new List<SVC.Core.DataTypes.TelecommunicationsAddress>(ident.Telecom.Count);
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
                subjectOf.OtherIdentifiers = new List<KeyValuePair<CodeValue, DomainIdentifier>>(ident.AsOtherIDs.Count);
                foreach (var id in ident.AsOtherIDs)
                    if (id.NullFlavor == null)
                    {
                        subjectOf.OtherIdentifiers.Add(new KeyValuePair<CodeValue, DomainIdentifier>(
                            CreateCodeValue(id.Code, dtls),
                            CreateDomainIdentifier(id.Id, dtls)
                         ));
                        if(id.AssigningIdOrganization != null && id.AssigningIdOrganization.NullFlavor == null)
                        {
                            // Other identifier assigning organization ext
                            if (id.AssigningIdOrganization.Id != null && !id.AssigningIdOrganization.Id.IsNull)
                                subjectOf.Add(new ExtendedAttribute()
                                {
                                    PropertyPath = String.Format("OtherIdentifiers[{0}{1}]", id.Id.Root, id.Id.Extension),
                                    Value = CreateDomainIdentifier(id.AssigningIdOrganization.Id, dtls),
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
                subjectOf.Language = new List<PersonLanguage>(ident.LanguageCommunication.Count);
                foreach (var lang in ident.LanguageCommunication)
                {
                    if (lang == null || lang.NullFlavor != null) continue;

                    PersonLanguage pl = new PersonLanguage();

                    CodeValue languageCode = CreateCodeValue(lang.LanguageCode, dtls);
                    // Default ISO 639-3
                    languageCode.CodeSystem = languageCode.CodeSystem ?? config.OidRegistrar.GetOid("ISO639-3").Oid;

                    // Validate the language code
                    if (languageCode.CodeSystem != config.OidRegistrar.GetOid("ISO639-3").Oid &&
                        languageCode.CodeSystem != config.OidRegistrar.GetOid("ISO639-1").Oid)
                        dtls.Add(new VocabularyIssueResultDetail(ResultDetailType.Error, this.m_localeService.GetString("MSGE04B"), null));

                    // Translate the language code
                    if (languageCode.CodeSystem == config.OidRegistrar.GetOid("ISO639-3").Oid) // we need to translate
                        languageCode = termSvc.Translate(languageCode, config.OidRegistrar.GetOid("ISO639-1").Oid);

                    if (languageCode == null)
                        dtls.Add(new VocabularyIssueResultDetail(ResultDetailType.Error, this.m_localeService.GetString("MSGE04C"), null, null));
                    else
                        pl.Language = languageCode.Code;
                    
                    // Preferred? 
                    if ((bool)lang.PreferenceInd)
                        pl.Type = LanguageType.Preferred;

                    
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
                            AlternateIdentifiers = new List<DomainIdentifier>() { CreateDomainIdentifier(psn.RelationshipHolder.Id, dtls) },
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
        /// Create components generic function to be used on the ControlActEvent of a message
        /// </summary>
        protected RegistrationEvent CreateComponents<T>(MARC.Everest.RMIM.CA.R020403.MFMI_MT700711CA.ControlActEvent<T> controlActEvent, List<IResultDetail> dtls)
        {
            // Get services
            ITerminologyService term = Context.GetService(typeof(ITerminologyService)) as ITerminologyService;
            ISystemConfigurationService config = Context.GetService(typeof(ISystemConfigurationService)) as ISystemConfigurationService;

            RegistrationEvent retVal = new RegistrationEvent();
            retVal.Context = this.Context;

            // All items here are "completed" so do a proper transform
            retVal.Status = StatusType.Completed;

            // Language code
            if (controlActEvent.LanguageCode == null || controlActEvent.LanguageCode.IsNull)
            {
                dtls.Add(new ResultDetail(ResultDetailType.Warning, this.m_localeService.GetString("MSGE002"), null, null));
                retVal.LanguageCode = config.JurisdictionData.DefaultLanguageCode;
            }
            else
            {
                // By default the language codes used by the SHR is ISO 639-1 
                // However the code used in the messaging is ISO 639-3 so we 
                // have to convert
                var iso6393code = CreateCodeValue(controlActEvent.LanguageCode, dtls);
                if (iso6393code.CodeSystem != config.OidRegistrar.GetOid("ISO639-3").Oid &&
                    iso6393code.CodeSystem != config.OidRegistrar.GetOid("ISO639-1").Oid)
                    dtls.Add(new VocabularyIssueResultDetail(ResultDetailType.Error, this.m_localeService.GetString("MSGE04B"), null));

                // Translate the language code
                if (iso6393code.CodeSystem == config.OidRegistrar.GetOid("ISO639-3").Oid) // we need to translate
                    iso6393code = term.Translate(iso6393code, config.OidRegistrar.GetOid("ISO639-1").Oid);

                if (iso6393code == null)
                    dtls.Add(new VocabularyIssueResultDetail(ResultDetailType.Error, this.m_localeService.GetString("MSGE04C"), null, null));
                else
                    retVal.LanguageCode = iso6393code.Code;
            }

            // Prepare a change summary (ie: the act)
            // All events store a copy of their cact as the "reason" for the change
            ChangeSummary changeSummary = new ChangeSummary();
            changeSummary.ChangeType = CreateCodeValue<String>(controlActEvent.Code, dtls);
            changeSummary.Status = StatusType.Completed;
            changeSummary.Timestamp = DateTime.Now;
            changeSummary.LanguageCode = retVal.LanguageCode;

            if (controlActEvent.EffectiveTime == null || controlActEvent.EffectiveTime.IsNull)
                dtls.Add(new MandatoryElementMissingResultDetail(ResultDetailType.Error, this.m_localeService.GetString("MSGE001"), "//urn:hl7-org:v3#controlActEvent"));
            else
                changeSummary.EffectiveTime = CreateTimestamp(controlActEvent.EffectiveTime, dtls);

            if (controlActEvent.ReasonCode != null)
                changeSummary.Add(new Reason()
                {
                    ReasonType = CreateCodeValue<String>(controlActEvent.ReasonCode, dtls)
                }, "RSN", HealthServiceRecordSiteRoleType.ReasonFor, null);
            retVal.Add(changeSummary, "CHANGE", HealthServiceRecordSiteRoleType.ReasonFor | HealthServiceRecordSiteRoleType.OlderVersionOf, null);
            (changeSummary.Site as HealthServiceRecordSite).IsSymbolic = true; // this link adds no real value to the parent's data


            // Author
            HealthcareParticipant aut = null;

            // author
            if (controlActEvent.Author == null || controlActEvent.Author.NullFlavor != null)
                dtls.Add(new ResultDetail(ResultDetailType.Error, this.m_localeService.GetString("MSGE004"), null, null));
            else
            {
                if (controlActEvent.Author.Time == null || controlActEvent.Author.Time.IsNull)
                    dtls.Add(new MandatoryElementMissingResultDetail(ResultDetailType.Error, this.m_localeService.GetString("MSGE005"), null));
                else
                {
                    retVal.Timestamp = (DateTime)controlActEvent.Author.Time;
                    changeSummary.Timestamp = (DateTime)controlActEvent.Author.Time;
                }

                if (controlActEvent.Author.AuthorPerson is MARC.Everest.RMIM.CA.R020403.COCT_MT090102CA.AssignedEntity)
                    aut = CreateParticipantComponent(controlActEvent.Author.AuthorPerson as MARC.Everest.RMIM.CA.R020403.COCT_MT090102CA.AssignedEntity, dtls);
                else if (controlActEvent.Author.AuthorPerson is MARC.Everest.RMIM.CA.R020403.COCT_MT090502CA.AssignedEntity)
                    aut = CreateParticipantComponent(controlActEvent.Author.AuthorPerson as MARC.Everest.RMIM.CA.R020403.COCT_MT090502CA.AssignedEntity, dtls);
                else
                    dtls.Add(new ResultDetail(ResultDetailType.Error, this.m_localeService.GetString("MSGE006"), null, null));

                if (aut != null)
                {
                    changeSummary.Add(aut, "AUT", HealthServiceRecordSiteRoleType.AuthorOf, aut.AlternateIdentifiers);
                    if ((bool)controlActEvent.Subject.ContextConductionInd)
                        retVal.Add(aut.Clone() as IComponent, "AUT", HealthServiceRecordSiteRoleType.AuthorOf, aut.AlternateIdentifiers);

                    // Assign as RSP?
                    if (controlActEvent.ResponsibleParty == null || controlActEvent.ResponsibleParty.NullFlavor != null)
                    {
                        changeSummary.Add(aut.Clone() as IComponent, "RSP", HealthServiceRecordSiteRoleType.ResponsibleFor, aut.AlternateIdentifiers);
                        if ((bool)controlActEvent.Subject.ContextConductionInd)
                            retVal.Add(aut.Clone() as IComponent, "RSP", HealthServiceRecordSiteRoleType.ResponsibleFor, aut.AlternateIdentifiers);
                    }

                    // Assign as DE?
                    if (controlActEvent.DataEnterer == null || controlActEvent.DataEnterer.NullFlavor != null)
                    {
                        changeSummary.Add(aut.Clone() as IComponent, "DTE", HealthServiceRecordSiteRoleType.EntererOf, aut.AlternateIdentifiers);
                        if ((bool)controlActEvent.Subject.ContextConductionInd)
                            retVal.Add(aut.Clone() as IComponent, "DTE", HealthServiceRecordSiteRoleType.EntererOf, aut.AlternateIdentifiers);
                    }

                }
                else
                    dtls.Add(new ResultDetail(ResultDetailType.Error, this.m_localeService.GetString("MSGE004"), null, null));
            }

            // data enterer
            if (controlActEvent.DataEnterer != null && controlActEvent.DataEnterer.NullFlavor == null)
            {
                HealthcareParticipant ptcpt = null;
                if (controlActEvent.DataEnterer.EntererChoice is MARC.Everest.RMIM.CA.R020403.COCT_MT090502CA.AssignedEntity)
                    ptcpt = CreateParticipantComponent(controlActEvent.DataEnterer.EntererChoice as MARC.Everest.RMIM.CA.R020403.COCT_MT090502CA.AssignedEntity, dtls);
                else if (controlActEvent.DataEnterer.EntererChoice is MARC.Everest.RMIM.CA.R020403.COCT_MT090102CA.AssignedEntity)
                    ptcpt = CreateParticipantComponent(controlActEvent.DataEnterer.EntererChoice as MARC.Everest.RMIM.CA.R020403.COCT_MT090102CA.AssignedEntity, dtls);
                else
                    dtls.Add(new ResultDetail(ResultDetailType.Warning, this.m_localeService.GetString("MSGW001"), null, null));

                if (ptcpt != null)
                {
                    changeSummary.Add(ptcpt, "DTE", HealthServiceRecordSiteRoleType.EntererOf, ptcpt.AlternateIdentifiers);
                    if ((bool)controlActEvent.Subject.ContextConductionInd)
                        retVal.Add(ptcpt.Clone() as IComponent, "DTE", HealthServiceRecordSiteRoleType.EntererOf, ptcpt.AlternateIdentifiers);
                }
                else
                    dtls.Add(new ResultDetail(ResultDetailType.Warning, this.m_localeService.GetString("MSGW002"), null, null));
            }

            // responsible party
            if (controlActEvent.ResponsibleParty != null && controlActEvent.ResponsibleParty.NullFlavor == null)
            {
                HealthcareParticipant ptcpt = CreateParticipantComponent(controlActEvent.ResponsibleParty.AssignedEntity, dtls);
                if (ptcpt != null)
                {
                    changeSummary.Add(ptcpt, "RSP", HealthServiceRecordSiteRoleType.ResponsibleFor, ptcpt.AlternateIdentifiers);
                    if ((bool)controlActEvent.Subject.ContextConductionInd)
                        retVal.Add(ptcpt.Clone() as IComponent, "RSP", HealthServiceRecordSiteRoleType.ResponsibleFor, ptcpt.AlternateIdentifiers);
                }
                else
                    dtls.Add(new ResultDetail(ResultDetailType.Error, this.m_localeService.GetString("MSGE007"), null, null));
            }

            // location
            if (controlActEvent.Location != null && controlActEvent.Location.NullFlavor == null)
            {
                Place loc = CreateLocationComponent(controlActEvent.Location.ServiceDeliveryLocation, dtls);
                if (loc != null)
                {
                    changeSummary.Add(loc, "LOC", HealthServiceRecordSiteRoleType.PlaceOfRecord, loc.AlternateIdentifiers);
                    if ((bool)controlActEvent.Subject.ContextConductionInd)
                        retVal.Add(loc.Clone() as IComponent, "LOC", HealthServiceRecordSiteRoleType.PlaceOfRecord, loc.AlternateIdentifiers);
                }
                else
                    dtls.Add(new ResultDetail(ResultDetailType.Warning, this.m_localeService.GetString("MSGW003"), null, null));
            }

            // data entry location
            if (controlActEvent.DataEntryLocation != null && controlActEvent.DataEntryLocation.NullFlavor == null)
            {
                Place loc = CreateLocationComponent(controlActEvent.DataEntryLocation.ServiceDeliveryLocation, dtls);
                if (loc != null)
                {
                    changeSummary.Add(loc, "DTL", HealthServiceRecordSiteRoleType.PlaceOfEntry, loc.AlternateIdentifiers);
                    if ((bool)controlActEvent.Subject.ContextConductionInd)
                        retVal.Add(loc.Clone() as IComponent, "DTL", HealthServiceRecordSiteRoleType.PlaceOfEntry, loc.AlternateIdentifiers);
                }
                else
                    dtls.Add(new ResultDetail(ResultDetailType.Warning, this.m_localeService.GetString("MSGW004"), null, null));
            }


            return retVal;
        }

        /// <summary>
        /// Create components generic function to be used on the ControlActEvent of a message
        /// </summary>
        protected RegistrationEvent CreateComponents<T>(MARC.Everest.RMIM.CA.R020403.MFMI_MT700751CA.ControlActEvent<T> controlActEvent, List<IResultDetail> dtls)
        {
            // Get services
            ITerminologyService term = Context.GetService(typeof(ITerminologyService)) as ITerminologyService;
            ISystemConfigurationService config = Context.GetService(typeof(ISystemConfigurationService)) as ISystemConfigurationService;

            RegistrationEvent retVal = new RegistrationEvent();
            retVal.Context = this.Context;

            // All items here are "completed" so do a proper transform
            retVal.Status = StatusType.Completed;

            // Language code
            if (controlActEvent.LanguageCode == null || controlActEvent.LanguageCode.IsNull)
            {
                dtls.Add(new ResultDetail(ResultDetailType.Warning, this.m_localeService.GetString("MSGE002"), null, null));
                retVal.LanguageCode = config.JurisdictionData.DefaultLanguageCode;
            }
            else
            {
                // By default the language codes used by the SHR is ISO 639-1 
                // However the code used in the messaging is ISO 639-3 so we 
                // have to convert
                var iso6393code = CreateCodeValue(controlActEvent.LanguageCode, dtls);
                if (iso6393code.CodeSystem != config.OidRegistrar.GetOid("ISO639-3").Oid &&
                    iso6393code.CodeSystem != config.OidRegistrar.GetOid("ISO639-1").Oid)
                    dtls.Add(new VocabularyIssueResultDetail(ResultDetailType.Error, this.m_localeService.GetString("MSGE04B"), null));

                // Translate the language code
                if (iso6393code.CodeSystem == config.OidRegistrar.GetOid("ISO639-3").Oid) // we need to translate
                    iso6393code = term.Translate(iso6393code, config.OidRegistrar.GetOid("ISO639-1").Oid);

                if (iso6393code == null)
                    dtls.Add(new VocabularyIssueResultDetail(ResultDetailType.Error, this.m_localeService.GetString("MSGE04C"), null, null));
                else
                    retVal.LanguageCode = iso6393code.Code;
            }

            // Prepare a change summary (ie: the act)
            // All events store a copy of their cact as the "reason" for the change
            ChangeSummary changeSummary = new ChangeSummary();
            changeSummary.ChangeType = CreateCodeValue<String>(controlActEvent.Code, dtls);
            changeSummary.Status = StatusType.Completed;
            changeSummary.Timestamp = DateTime.Now;
            changeSummary.LanguageCode = retVal.LanguageCode;

            if (controlActEvent.EffectiveTime == null || controlActEvent.EffectiveTime.IsNull)
                dtls.Add(new MandatoryElementMissingResultDetail(ResultDetailType.Error, this.m_localeService.GetString("MSGE001"), "//urn:hl7-org:v3#controlActEvent"));
            else
                changeSummary.EffectiveTime = CreateTimestamp(controlActEvent.EffectiveTime, dtls);

            if (controlActEvent.ReasonCode != null)
                changeSummary.Add(new Reason()
                {
                    ReasonType = CreateCodeValue<String>(controlActEvent.ReasonCode, dtls)
                }, "RSN", HealthServiceRecordSiteRoleType.ReasonFor, null);
            retVal.Add(changeSummary, "CHANGE", HealthServiceRecordSiteRoleType.ReasonFor | HealthServiceRecordSiteRoleType.OlderVersionOf, null);
            (changeSummary.Site as HealthServiceRecordSite).IsSymbolic = true; // this link adds no real value to the parent's data


            // Author
            HealthcareParticipant aut = null;

            // author
            if (controlActEvent.Author == null || controlActEvent.Author.NullFlavor != null)
                dtls.Add(new ResultDetail(ResultDetailType.Error, this.m_localeService.GetString("MSGE004"), null, null));
            else
            {
                if (controlActEvent.Author.Time == null || controlActEvent.Author.Time.IsNull)
                    dtls.Add(new MandatoryElementMissingResultDetail(ResultDetailType.Error, this.m_localeService.GetString("MSGE005"), null));
                else
                    retVal.Timestamp = (DateTime)controlActEvent.Author.Time;

                HealthcareParticipant ptcpt = null;

                if (controlActEvent.Author.AuthorPerson is MARC.Everest.RMIM.CA.R020403.COCT_MT090102CA.AssignedEntity)
                    ptcpt = CreateParticipantComponent(controlActEvent.Author.AuthorPerson as MARC.Everest.RMIM.CA.R020403.COCT_MT090102CA.AssignedEntity, dtls);
                else if (controlActEvent.Author.AuthorPerson is MARC.Everest.RMIM.CA.R020403.COCT_MT090502CA.AssignedEntity)
                    ptcpt = CreateParticipantComponent(controlActEvent.Author.AuthorPerson as MARC.Everest.RMIM.CA.R020403.COCT_MT090502CA.AssignedEntity, dtls);
                else
                    dtls.Add(new ResultDetail(ResultDetailType.Error, this.m_localeService.GetString("MSGE006"), null, null));

                if (ptcpt != null)
                    retVal.Add(ptcpt, "AUT", HealthServiceRecordSiteRoleType.AuthorOf, ptcpt.AlternateIdentifiers);
                else
                    dtls.Add(new ResultDetail(ResultDetailType.Error, this.m_localeService.GetString("MSGE004"), null, null));
            }

            // data enterer
            if (controlActEvent.DataEnterer != null && controlActEvent.DataEnterer.NullFlavor == null)
            {
                HealthcareParticipant ptcpt = null;
                if (controlActEvent.DataEnterer.EntererChoice is MARC.Everest.RMIM.CA.R020403.COCT_MT090502CA.AssignedEntity)
                    ptcpt = CreateParticipantComponent(controlActEvent.DataEnterer.EntererChoice as MARC.Everest.RMIM.CA.R020403.COCT_MT090502CA.AssignedEntity, dtls);
                else if (controlActEvent.DataEnterer.EntererChoice is MARC.Everest.RMIM.CA.R020403.COCT_MT090102CA.AssignedEntity)
                    ptcpt = CreateParticipantComponent(controlActEvent.DataEnterer.EntererChoice as MARC.Everest.RMIM.CA.R020403.COCT_MT090102CA.AssignedEntity, dtls);
                else
                    dtls.Add(new ResultDetail(ResultDetailType.Warning, this.m_localeService.GetString("MSGW001"), null, null));
                if (ptcpt != null)
                    retVal.Add(ptcpt, "DTE", HealthServiceRecordSiteRoleType.EntererOf, ptcpt.AlternateIdentifiers);
                else
                    dtls.Add(new ResultDetail(ResultDetailType.Warning, this.m_localeService.GetString("MSGW002"), null, null));
            }

            // responsible party
            if (controlActEvent.ResponsibleParty != null && controlActEvent.ResponsibleParty.NullFlavor == null)
            {
                HealthcareParticipant ptcpt = CreateParticipantComponent(controlActEvent.ResponsibleParty.AssignedEntity, dtls);
                if (ptcpt != null)
                    retVal.Add(ptcpt, "RSP", HealthServiceRecordSiteRoleType.ResponsibleFor, ptcpt.AlternateIdentifiers);
                else
                    dtls.Add(new ResultDetail(ResultDetailType.Error, this.m_localeService.GetString("MSGE007"), null, null));
            }

            // location
            if (controlActEvent.Location != null && controlActEvent.Location.NullFlavor == null)
            {
                Place loc = CreateLocationComponent(controlActEvent.Location.ServiceDeliveryLocation, dtls);
                if (loc != null)
                    retVal.Add(loc, "LOC", HealthServiceRecordSiteRoleType.PlaceOfOccurence, loc.AlternateIdentifiers);
                else
                    dtls.Add(new ResultDetail(ResultDetailType.Warning, this.m_localeService.GetString("MSGW003"), null, null));
            }

            // data entry location
            if (controlActEvent.DataEntryLocation != null && controlActEvent.DataEntryLocation.NullFlavor == null)
            {
                Place loc = CreateLocationComponent(controlActEvent.DataEntryLocation.ServiceDeliveryLocation, dtls);
                if (loc != null)
                    retVal.Add(loc, "DTL", HealthServiceRecordSiteRoleType.PlaceOfEntry, loc.AlternateIdentifiers);
                else
                    dtls.Add(new ResultDetail(ResultDetailType.Warning, this.m_localeService.GetString("MSGW004"), null, null));
            }

            return retVal;
        }


        

        /// <summary>
        /// Create a query match for find candidates
        /// </summary>
        internal QueryEvent CreateQueryMatch(MARC.Everest.RMIM.CA.R020403.MFMI_MT700751CA.ControlActEvent<MARC.Everest.RMIM.CA.R020403.PRPA_MT101103CA.ParameterList> controlActEvent, List<IResultDetail> dtls, ref List<DomainIdentifier> recordIds)
        {
            ITerminologyService termSvc = Context.GetService(typeof(ITerminologyService)) as ITerminologyService;
            ISystemConfigurationService config = Context.GetService(typeof(ISystemConfigurationService)) as ISystemConfigurationService;

            // Details about the query
            if (!controlActEvent.Code.Code.Equals(PRPA_IN101103CA.GetTriggerEvent().Code))
            {
                dtls.Add(new ResultDetail(ResultDetailType.Error, this.m_localeService.GetString("MSGE00C"), null, null));
                return null;
            }

            // REturn value
            QueryEvent retVal = new QueryEvent() { Timestamp = DateTime.Now };
            RegistrationEvent reasonFor = CreateComponents<MARC.Everest.RMIM.CA.R020403.PRPA_MT101103CA.ParameterList>(controlActEvent, dtls);
            retVal.Add(reasonFor, "RSON", HealthServiceRecordSiteRoleType.ReasonFor, null);
            // Filter
            RegistrationEvent filter = new RegistrationEvent();
            retVal.Add(filter, "QRY", HealthServiceRecordSiteRoleType.SubjectOf, null);

            // Parameter list validation
            var parameterList = controlActEvent.QueryByParameter.parameterList;
            if(parameterList == null || parameterList.NullFlavor != null)
            {
                dtls.Add(new MandatoryElementMissingResultDetail(ResultDetailType.Error,this.m_localeService.GetString("MSGE04E"), null));
                parameterList = new MARC.Everest.RMIM.CA.R020403.PRPA_MT101103CA.ParameterList();
            }

           
            // Create the actual query
            Person filterPerson = new Person();

            // Discrete record identifiers
            recordIds = new List<DomainIdentifier>(100);
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
                    RelationshipKind = Util.ToWireFormat("FTH"),
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
                    RelationshipKind = Util.ToWireFormat("MTH"),
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
                languageCode.CodeSystem = languageCode.CodeSystem ?? config.OidRegistrar.GetOid("ISO639-3").Oid;

                // Validate the language code
                if (languageCode.CodeSystem != config.OidRegistrar.GetOid("ISO639-3").Oid &&
                    languageCode.CodeSystem != config.OidRegistrar.GetOid("ISO639-1").Oid)
                    dtls.Add(new VocabularyIssueResultDetail(ResultDetailType.Error, this.m_localeService.GetString("MSGE04B"), null));

                // Translate the language code
                if (languageCode.CodeSystem == config.OidRegistrar.GetOid("ISO639-3").Oid) // we need to translate
                    languageCode = termSvc.Translate(languageCode, config.OidRegistrar.GetOid("ISO639-1").Oid);

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
        internal QueryEvent CreateQueryMatch(MARC.Everest.RMIM.CA.R020403.MFMI_MT700751CA.ControlActEvent<MARC.Everest.RMIM.CA.R020403.PRPA_MT101101CA.ParameterList> controlActEvent, List<IResultDetail> dtls, ref List<DomainIdentifier> recordIds)
        {
            ITerminologyService termSvc = Context.GetService(typeof(ITerminologyService)) as ITerminologyService;

            // Details about the query
            if (!controlActEvent.Code.Code.Equals(PRPA_IN101105CA.GetTriggerEvent().Code) && 
                !controlActEvent.Code.Code.Equals(PRPA_IN101101CA.GetTriggerEvent().Code))
            {
                dtls.Add(new ResultDetail(ResultDetailType.Error, this.m_localeService.GetString("MSGE00C"), null, null));
                return null;
            }

            // REturn value
            QueryEvent retVal = new QueryEvent() { Timestamp = DateTime.Now };
            RegistrationEvent reasonFor = CreateComponents<MARC.Everest.RMIM.CA.R020403.PRPA_MT101101CA.ParameterList>(controlActEvent, dtls);
            retVal.Add(reasonFor, "RSON", HealthServiceRecordSiteRoleType.ReasonFor, null);

            // Filter
            RegistrationEvent filter = new RegistrationEvent();
            retVal.Add(filter, "QRY", HealthServiceRecordSiteRoleType.SubjectOf, null);

            // Parameter list validation
            var parameterList = controlActEvent.QueryByParameter.parameterList;
            if (parameterList == null || parameterList.NullFlavor != null)
            {
                dtls.Add(new MandatoryElementMissingResultDetail(ResultDetailType.Error, this.m_localeService.GetString("MSGE04E"), null));
                parameterList = new MARC.Everest.RMIM.CA.R020403.PRPA_MT101101CA.ParameterList();
            }

            // Discrete record identifiers
            recordIds = new List<DomainIdentifier>(100);

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
