using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MARC.Everest.Connectors;
using MARC.HI.EHRS.SVC.Core.Services;
using MARC.HI.EHRS.SVC.Core.ComponentModel;
using MARC.Everest.DataTypes;
using MARC.Everest.RMIM.CA.R020402.Vocabulary;
using MARC.HI.EHRS.CR.Core.ComponentModel;
using MARC.HI.EHRS.SVC.Core.DataTypes;
using MARC.HI.EHRS.SVC.Core.ComponentModel.Components;

namespace MARC.HI.EHRS.CR.Messaging.Everest.MessageReceiver.CA
{
    /// <summary>
    /// De-Component Utility
    /// </summary>
    public class CaDeComponentUtil : MARC.HI.EHRS.CR.Messaging.Everest.DeComponentUtil
    {

        /// <summary>
        /// Convert status to rolestatus
        /// </summary>
        protected CS<RoleStatus> ConvertStatus(StatusType statusType, List<IResultDetail> dtls)
        {
            switch (statusType)
            {
                case StatusType.Aborted:
                    return RoleStatus.Suspended;
                case StatusType.Active:
                    return RoleStatus.Active;
                case StatusType.Cancelled:
                    return RoleStatus.Cancelled;
                case StatusType.New:
                    return RoleStatus.Pending;
                case StatusType.Nullified:
                    return RoleStatus.Nullified;
                case StatusType.Obsolete:
                    return RoleStatus.Terminated;
                case StatusType.Unknown:
                default:
                    dtls.Add(new VocabularyIssueResultDetail(ResultDetailType.Warning, m_localeService.GetString("MSGE010"), null, null));
                    return new CS<RoleStatus>() { NullFlavor = NullFlavor.Other };
            }
        }

        /// <summary>
        /// Create the person portion of the registration
        /// </summary>
        internal MARC.Everest.RMIM.CA.R020402.PRPA_MT101102CA.Person CreatePerson(Person verifiedPerson, List<IResultDetail> dtls)
        {
            var retVal = new MARC.Everest.RMIM.CA.R020402.PRPA_MT101102CA.Person(
                null,
                null,
                Util.Convert<CV<AdministrativeGender>>(verifiedPerson.GenderCode),
                CreateTS(verifiedPerson.BirthTime, dtls),
                verifiedPerson.DeceasedTime != null,
                CreateTS(verifiedPerson.DeceasedTime, dtls),
                verifiedPerson.BirthOrder.HasValue,
                verifiedPerson.BirthOrder.HasValue && verifiedPerson.BirthOrder >= 0 ? verifiedPerson.BirthOrder : null,
                null,
                null,
                null,
                null);

            // Create names
            retVal.Name = new LIST<PN>();
            if (verifiedPerson.Names != null)
                foreach (var name in verifiedPerson.Names)
                    retVal.Name.Add(CreatePN(name, dtls));
            else
                retVal.Name.NullFlavor = NullFlavor.NoInformation;

            // Create telecoms
            retVal.Telecom = new LIST<TEL>();
            if (verifiedPerson.TelecomAddresses != null)
                foreach (var tel in verifiedPerson.TelecomAddresses)
                    retVal.Telecom.Add(CreateTEL(tel, dtls));
            else
                retVal.Telecom.NullFlavor = NullFlavor.NoInformation;

            // Create addresses
            retVal.Addr = new LIST<AD>();
            if (verifiedPerson.Addresses != null)
                foreach (var addr in verifiedPerson.Addresses)
                    retVal.Addr.Add(CreateAD(addr, dtls));
            else
                retVal.Addr.NullFlavor = NullFlavor.NoInformation;

            // Create AsOtherIds
            retVal.AsOtherIDs = new List<MARC.Everest.RMIM.CA.R020402.PRPA_MT101102CA.OtherIDs>();
            if (verifiedPerson.OtherIdentifiers != null)
                foreach (var othId in verifiedPerson.OtherIdentifiers)
                {
                    var otherIdentifier = new MARC.Everest.RMIM.CA.R020402.PRPA_MT101102CA.OtherIDs(
                        CreateII(othId.Value, dtls),
                        CreateCV<String>(othId.Key, dtls),
                        null);
                    // Any extensions that apply to this?
                    var extId = verifiedPerson.FindExtension(o => o.Name == "AssigningIdOrganizationId" && o.PropertyPath == String.Format("OtherIdentifiers[{0}{1}]", otherIdentifier.Id.Root, otherIdentifier.Id.Extension));
                    var extName = verifiedPerson.FindExtension(o => o.Name == "AssigningIdOrganizationName" && o.PropertyPath == String.Format("OtherIdentifiers[{0}{1}]", otherIdentifier.Id.Root, otherIdentifier.Id.Extension));
                    if (extId != null || extName != null)
                        otherIdentifier.AssigningIdOrganization = new MARC.Everest.RMIM.CA.R020402.PRPA_MT101104CA.IdOrganization(
                            extId != null ? CreateII(extId.Value as DomainIdentifier, dtls) : null,
                            extName != null ? extName.Value as String : null
                        );
                    retVal.AsOtherIDs.Add(otherIdentifier);
                }

            // TODO: Personal Relationships
            // TODO: Language of communication
            return retVal;
        }

        /// <summary>
        /// Create an instance of the identified entity class from the specified person class
        /// </summary>
        internal MARC.Everest.RMIM.CA.R020402.PRPA_MT101102CA.IdentifiedEntity CreateIdentifiedEntity(RegistrationEvent verified, List<IResultDetail> dtls)
        {

            // Get localization service
            ILocalizationService locale = m_context.GetService(typeof(ILocalizationService)) as ILocalizationService;

            // Find the major components
            var person = verified.FindComponent(HealthServiceRecordSiteRoleType.SubjectOf) as Person;

            // Verify
            if (person == null)
            {
                dtls.Add(new NotImplementedResultDetail(ResultDetailType.Error, locale.GetString("DBCF0007"), null, null));
                return new MARC.Everest.RMIM.CA.R020402.PRPA_MT101102CA.IdentifiedEntity() { NullFlavor = NullFlavor.NoInformation };
            }

            var mask = person.FindComponent(HealthServiceRecordSiteRoleType.FilterOf) as MaskingIndicator;

            // Return value
            var retVal = new MARC.Everest.RMIM.CA.R020402.PRPA_MT101102CA.IdentifiedEntity(
                CreateIISet(person.AlternateIdentifiers, dtls),
                ConvertStatus(person.Status, dtls),
                CreateIVL(verified.EffectiveTime, dtls),
                mask == null ? null : CreateCV<x_VeryBasicConfidentialityKind>(mask.MaskingCode, dtls),
                CreatePerson(person, dtls),
                new MARC.Everest.RMIM.CA.R020402.PRPA_MT101104CA.Subject() { NullFlavor = NullFlavor.NotApplicable }
            );

            // Return value
            return retVal;
        }

        /// <summary>
        /// Create registration event 
        /// </summary>
        internal MARC.Everest.RMIM.CA.R020402.MFMI_MT700746CA.RegistrationEvent<MARC.Everest.RMIM.CA.R020402.PRPA_MT101104CA.IdentifiedEntity> CreateRegistrationEvent(RegistrationEvent res, List<IResultDetail> details)
        {
            var retVal = new MARC.Everest.RMIM.CA.R020402.MFMI_MT700746CA.RegistrationEvent<MARC.Everest.RMIM.CA.R020402.PRPA_MT101104CA.IdentifiedEntity>();

            var person = res.FindComponent(HealthServiceRecordSiteRoleType.SubjectOf) as Person;
            var custodialDevice = res.FindComponent(HealthServiceRecordSiteRoleType.PlaceOfRecord | HealthServiceRecordSiteRoleType.ResponsibleFor) as RepositoryDevice;
            var replacement = res.FindAllComponents(HealthServiceRecordSiteRoleType.ReplacementOf);

            // person
            if (person == null)
                retVal.Subject = new MARC.Everest.RMIM.CA.R020402.MFMI_MT700746CA.Subject4<MARC.Everest.RMIM.CA.R020402.PRPA_MT101104CA.IdentifiedEntity>() { NullFlavor = NullFlavor.NoInformation };
            else
                retVal.Subject = new MARC.Everest.RMIM.CA.R020402.MFMI_MT700746CA.Subject4<MARC.Everest.RMIM.CA.R020402.PRPA_MT101104CA.IdentifiedEntity>(CreateRegisteredRole(person, details));

            // custodial device
            if (custodialDevice == null)
                retVal.Custodian = new MARC.Everest.RMIM.CA.R020402.REPC_MT230003CA.Custodian() { NullFlavor = NullFlavor.NoInformation };
            else
                retVal.Custodian = CreateCustodialDevice(custodialDevice, details);

            // Replacement
            foreach (RegistrationEvent replc in replacement)
                retVal.ReplacementOf.Add(new MARC.Everest.RMIM.CA.R020402.MFMI_MT700711CA.ReplacementOf(
                    new MARC.Everest.RMIM.CA.R020402.MFMI_MT700711CA.PriorRegistration(
                        new MARC.Everest.RMIM.CA.R020402.MFMI_MT700711CA.Subject5(
                            new MARC.Everest.RMIM.CA.R020402.MFMI_MT700711CA.PriorRegisteredRole()
                            {
                                Id = CreateII(replc.AlternateIdentifier, details)
                            }
                        )
                    )
                ));

            return retVal;

        }

        /// <summary>
        /// Create registered role
        /// </summary>
        private MARC.Everest.RMIM.CA.R020402.PRPA_MT101104CA.IdentifiedEntity CreateRegisteredRole(Person person, List<IResultDetail> details)
        {

            ISystemConfigurationService configService = Context.GetService(typeof(ISystemConfigurationService)) as ISystemConfigurationService;
            ITerminologyService termService = Context.GetService(typeof(ITerminologyService)) as ITerminologyService;

            var retVal = new MARC.Everest.RMIM.CA.R020402.PRPA_MT101104CA.IdentifiedEntity();

            var relations = person.FindAllComponents(HealthServiceRecordSiteRoleType.RepresentitiveOf);
            var maskingIndicators = person.FindComponent(HealthServiceRecordSiteRoleType.FilterOf) as MaskingIndicator;
            var queryParameter = person.FindComponent(HealthServiceRecordSiteRoleType.CommentOn | HealthServiceRecordSiteRoleType.ComponentOf) as QueryParameters;

            // Masking indicators
            if (maskingIndicators != null)
                retVal.ConfidentialityCode = CreateCV<x_VeryBasicConfidentialityKind>(maskingIndicators.MaskingCode, details);
            else
                retVal.ConfidentialityCode = new CV<x_VeryBasicConfidentialityKind>(x_VeryBasicConfidentialityKind.Normal);

            retVal.Id = CreateIISet(person.AlternateIdentifiers, details);
            retVal.StatusCode = ConvertStatus(person.Status, details);
            
            // Query parameter (i.e. the match strength)
            if(queryParameter != null)
                retVal.SubjectOf = new MARC.Everest.RMIM.CA.R020402.PRPA_MT101104CA.Subject(
                    new MARC.Everest.RMIM.CA.R020402.PRPA_MT101104CA.ObservationEvent(
                        (queryParameter.MatchingAlgorithm & MatchAlgorithm.Soundex) != 0 ? ObservationQueryMatchType.PhoneticMatch : ObservationQueryMatchType.PatternMatch,
                        new REAL(queryParameter.Confidence) { Precision = 2 }
                    )
                );

            object gend = new CV<AdministrativeGender>() { NullFlavor = NullFlavor.NoInformation }; 
            Util.TryFromWireFormat(person.GenderCode, typeof(CV<AdministrativeGender>), out gend);

            // Set the identified person
            retVal.IdentifiedPerson = new MARC.Everest.RMIM.CA.R020402.PRPA_MT101104CA.Person(
                null,
                null,
                gend as CV<AdministrativeGender>, 
                CreateTS(person.BirthTime, details),
                person.DeceasedTime != null,
                CreateTS(person.DeceasedTime, details),
                person.BirthOrder.HasValue ? (BL)true : null,
                person.BirthOrder,
                null,
                null,
                null,
                null);

            // Create names
            retVal.IdentifiedPerson.Name = new LIST<PN>();
            if (person.Names != null)
                foreach (var name in person.Names)
                    retVal.IdentifiedPerson.Name.Add(CreatePN(name, details));
            else
                retVal.IdentifiedPerson.Name.NullFlavor = NullFlavor.NoInformation;

            // Create telecoms
            retVal.IdentifiedPerson.Telecom = new LIST<TEL>();
            if (person.TelecomAddresses != null)
                foreach (var tel in person.TelecomAddresses)
                    retVal.IdentifiedPerson.Telecom.Add(CreateTEL(tel, details));
            else
                retVal.IdentifiedPerson.Telecom.NullFlavor = NullFlavor.NoInformation;

            // Create addresses
            retVal.IdentifiedPerson.Addr = new LIST<AD>();
            if (person.Addresses != null)
                foreach (var addr in person.Addresses)
                    retVal.IdentifiedPerson.Addr.Add(CreateAD(addr, details));
            else
                retVal.IdentifiedPerson.Addr.NullFlavor = NullFlavor.NoInformation;

            // Create AsOtherIds
            retVal.IdentifiedPerson.AsOtherIDs = new List<MARC.Everest.RMIM.CA.R020402.PRPA_MT101104CA.OtherIDs>();
            if (person.OtherIdentifiers != null)
                foreach (var othId in person.OtherIdentifiers)
                {
                    var otherIdentifier = new MARC.Everest.RMIM.CA.R020402.PRPA_MT101104CA.OtherIDs(
                        CreateII(othId.Value, details),
                        CreateCV<String>(othId.Key, details),
                        null);
                    // Any extensions that apply to this?
                    var extId = person.FindExtension(o => o.Name == "AssigningIdOrganizationId" && o.PropertyPath == String.Format("OtherIdentifiers[{0}{1}]", otherIdentifier.Id.Root, otherIdentifier.Id.Extension));
                    var extName = person.FindExtension(o => o.Name == "AssigningIdOrganizationName" && o.PropertyPath == String.Format("OtherIdentifiers[{0}{1}]", otherIdentifier.Id.Root, otherIdentifier.Id.Extension));
                    if (extId != null || extName != null)
                        otherIdentifier.AssigningIdOrganization = new MARC.Everest.RMIM.CA.R020402.PRPA_MT101104CA.IdOrganization(
                            extId != null ? CreateII(extId.Value as DomainIdentifier, details) : null,
                            extName != null ? extName.Value as String : null
                        );
                    retVal.IdentifiedPerson.AsOtherIDs.Add(otherIdentifier);
                }

            // Languages
            if(person.Language != null)
                foreach (var lang in person.Language)
                {
                    // Translate the code
                    var langCode = new CodeValue(lang.Language, configService.OidRegistrar.GetOid("ISO639-1").Oid);

                    if(termService != null)
                        langCode = termService.Translate(langCode, configService.OidRegistrar.GetOid("ISO639-3").Oid);

                    retVal.IdentifiedPerson.LanguageCommunication.Add(new MARC.Everest.RMIM.CA.R020402.PRPA_MT101104CA.LanguageCommunication(
                        CreateCV<String>(langCode, details),
                        lang.Type == LanguageType.Fluency
                        ));
                }

            // Personal Relationships
            if(relations != null)
                foreach (PersonalRelationship relation in relations)
                    if(!String.IsNullOrEmpty(relation.RelationshipKind))
                        retVal.IdentifiedPerson.PersonalRelationship.Add(new MARC.Everest.RMIM.CA.R020402.PRPA_MT101104CA.PersonalRelationship(
                            Util.Convert<PersonalRelationshipRoleType>(relation.RelationshipKind),
                            new MARC.Everest.RMIM.CA.R020402.PRPA_MT101104CA.ParentPerson(
                                CreateII(relation.AlternateIdentifiers[0], details),
                                CreatePN(relation.LegalName, details)
                            )
                        ));

            return retVal;
        }

        /// <summary>
        /// Create custodian device
        /// </summary>
        private MARC.Everest.RMIM.CA.R020402.REPC_MT230003CA.Custodian CreateCustodialDevice(RepositoryDevice custodialDevice, List<IResultDetail> details)
        {
            return new MARC.Everest.RMIM.CA.R020402.REPC_MT230003CA.Custodian(
                new MARC.Everest.RMIM.CA.R020402.COCT_MT090310CA.AssignedDevice(
                    CreateII(custodialDevice.AlternateIdentifier, details),
                    new MARC.Everest.RMIM.CA.R020402.COCT_MT090310CA.Repository(
                        custodialDevice.Name
                    ),
                    new MARC.Everest.RMIM.CA.R020402.COCT_MT090310CA.RepositoryJurisdiction(
                        custodialDevice.Jurisdiction
                    )
                )
            );
        }

        /// <summary>
        /// Create the response registration event
        /// </summary>
        internal MARC.Everest.RMIM.CA.R020402.MFMI_MT700746CA.RegistrationEvent<MARC.Everest.RMIM.CA.R020402.PRPA_MT101106CA.IdentifiedEntity> CreateRegistrationEventDetail(RegistrationEvent res, List<IResultDetail> details)
        {
            var retVal = new MARC.Everest.RMIM.CA.R020402.MFMI_MT700746CA.RegistrationEvent<MARC.Everest.RMIM.CA.R020402.PRPA_MT101106CA.IdentifiedEntity>();

            var person = res.FindComponent(HealthServiceRecordSiteRoleType.SubjectOf) as Person;
            var custodialDevice = res.FindComponent(HealthServiceRecordSiteRoleType.PlaceOfRecord | HealthServiceRecordSiteRoleType.ResponsibleFor) as RepositoryDevice;
            var replacement = res.FindAllComponents(HealthServiceRecordSiteRoleType.ReplacementOf);

            // person
            if (person == null)
                retVal.Subject = new MARC.Everest.RMIM.CA.R020402.MFMI_MT700746CA.Subject4<MARC.Everest.RMIM.CA.R020402.PRPA_MT101106CA.IdentifiedEntity>() { NullFlavor = NullFlavor.NoInformation };
            else
            {
                var regRole = CreateRegisteredRole(person, details);
                retVal.Subject = new MARC.Everest.RMIM.CA.R020402.MFMI_MT700746CA.Subject4<MARC.Everest.RMIM.CA.R020402.PRPA_MT101106CA.IdentifiedEntity>(
                    new MARC.Everest.RMIM.CA.R020402.PRPA_MT101106CA.IdentifiedEntity(
                        regRole.Id,
                        regRole.StatusCode,
                        regRole.EffectiveTime,
                        regRole.ConfidentialityCode,
                        new MARC.Everest.RMIM.CA.R020402.PRPA_MT101106CA.Person(),
                        regRole.SubjectOf
                    ));
                if (regRole.IdentifiedPerson != null && regRole.IdentifiedPerson.NullFlavor == null)
                    foreach (var othId in regRole.IdentifiedPerson.AsOtherIDs)
                        retVal.Subject.registeredRole.IdentifiedPerson.AsOtherIDs.Add(
                            new MARC.Everest.RMIM.CA.R020402.PRPA_MT101106CA.OtherIDs(
                                othId.Id,
                                othId.Code,
                                new MARC.Everest.RMIM.CA.R020402.PRPA_MT101106CA.IdOrganization(
                                    othId.AssigningIdOrganization.Id,
                                    othId.AssigningIdOrganization.Name)
                            ));
            }

            // custodial device
            if (custodialDevice == null)
                retVal.Custodian = new MARC.Everest.RMIM.CA.R020402.REPC_MT230003CA.Custodian() { NullFlavor = NullFlavor.NoInformation };
            else
                retVal.Custodian = CreateCustodialDevice(custodialDevice, details);
            
            // Replacement
            foreach (RegistrationEvent replc in replacement)
                retVal.ReplacementOf.Add(new MARC.Everest.RMIM.CA.R020402.MFMI_MT700711CA.ReplacementOf(
                    new MARC.Everest.RMIM.CA.R020402.MFMI_MT700711CA.PriorRegistration(
                        new MARC.Everest.RMIM.CA.R020402.MFMI_MT700711CA.Subject5(
                            new MARC.Everest.RMIM.CA.R020402.MFMI_MT700711CA.PriorRegisteredRole()
                            {
                                Id = CreateII(replc.AlternateIdentifier, details)
                            }
                        )
                    )
                ));

            return retVal;
        }

        /// <summary>
        /// Create Get Patient details
        /// </summary>
        internal MARC.Everest.RMIM.CA.R020402.MFMI_MT700746CA.RegistrationEvent<MARC.Everest.RMIM.CA.R020402.PRPA_MT101102CA.IdentifiedEntity> CreateRegistrationEventDetailEx(RegistrationEvent res, List<IResultDetail> details)
        {
            var retVal = new MARC.Everest.RMIM.CA.R020402.MFMI_MT700746CA.RegistrationEvent<MARC.Everest.RMIM.CA.R020402.PRPA_MT101102CA.IdentifiedEntity>();

            var person = res.FindComponent(HealthServiceRecordSiteRoleType.SubjectOf) as Person;
            var custodialDevice = res.FindComponent(HealthServiceRecordSiteRoleType.PlaceOfRecord | HealthServiceRecordSiteRoleType.ResponsibleFor) as RepositoryDevice;
            var replacement = res.FindAllComponents(HealthServiceRecordSiteRoleType.ReplacementOf);

            // person
            if (person == null)
                retVal.Subject = new MARC.Everest.RMIM.CA.R020402.MFMI_MT700746CA.Subject4<MARC.Everest.RMIM.CA.R020402.PRPA_MT101102CA.IdentifiedEntity>() { NullFlavor = NullFlavor.NoInformation };
            else
                retVal.Subject = new MARC.Everest.RMIM.CA.R020402.MFMI_MT700746CA.Subject4<MARC.Everest.RMIM.CA.R020402.PRPA_MT101102CA.IdentifiedEntity>(CreateRegisteredRoleAlt(person, details));

            // custodial device
            if (custodialDevice == null)
                retVal.Custodian = new MARC.Everest.RMIM.CA.R020402.REPC_MT230003CA.Custodian() { NullFlavor = NullFlavor.NoInformation };
            else
                retVal.Custodian = CreateCustodialDevice(custodialDevice, details);

            // Replacement
            foreach (RegistrationEvent replc in replacement)
                retVal.ReplacementOf.Add(new MARC.Everest.RMIM.CA.R020402.MFMI_MT700711CA.ReplacementOf(
                    new MARC.Everest.RMIM.CA.R020402.MFMI_MT700711CA.PriorRegistration(
                        new MARC.Everest.RMIM.CA.R020402.MFMI_MT700711CA.Subject5(
                            new MARC.Everest.RMIM.CA.R020402.MFMI_MT700711CA.PriorRegisteredRole()
                            {
                                Id = CreateII(replc.AlternateIdentifier, details)
                            }
                        )
                    )
                ));

            return retVal;
        }

        /// <summary>
        /// Create alternate registered role
        /// </summary>
        private MARC.Everest.RMIM.CA.R020402.PRPA_MT101102CA.IdentifiedEntity CreateRegisteredRoleAlt(Person person, List<IResultDetail> details)
        {
            ISystemConfigurationService configService = Context.GetService(typeof(ISystemConfigurationService)) as ISystemConfigurationService;
            ITerminologyService termService = Context.GetService(typeof(ITerminologyService)) as ITerminologyService;

            var retVal = new MARC.Everest.RMIM.CA.R020402.PRPA_MT101102CA.IdentifiedEntity();

            var relations = person.FindAllComponents(HealthServiceRecordSiteRoleType.RepresentitiveOf);
            var maskingIndicators = person.FindComponent(HealthServiceRecordSiteRoleType.FilterOf) as MaskingIndicator;
            var queryParameter = person.FindComponent(HealthServiceRecordSiteRoleType.CommentOn | HealthServiceRecordSiteRoleType.ComponentOf) as QueryParameters;

            // Masking indicators
            if (maskingIndicators != null)
                retVal.ConfidentialityCode = CreateCV<x_VeryBasicConfidentialityKind>(maskingIndicators.MaskingCode, details);
            else
                retVal.ConfidentialityCode = new CV<x_VeryBasicConfidentialityKind>(x_VeryBasicConfidentialityKind.Normal);

            retVal.Id = CreateIISet(person.AlternateIdentifiers, details);
            retVal.StatusCode = ConvertStatus(person.Status, details);

            // Query parameter (i.e. the match strength)
            if (queryParameter != null)
                retVal.SubjectOf = new MARC.Everest.RMIM.CA.R020402.PRPA_MT101104CA.Subject(
                    new MARC.Everest.RMIM.CA.R020402.PRPA_MT101104CA.ObservationEvent(
                        (queryParameter.MatchingAlgorithm & MatchAlgorithm.Soundex) != 0 ? ObservationQueryMatchType.PhoneticMatch : ObservationQueryMatchType.PatternMatch,
                        new REAL(queryParameter.Confidence) { Precision = 2 }
                    )
                );

            object gend = new CV<AdministrativeGender>() { NullFlavor = NullFlavor.NoInformation };
            Util.TryFromWireFormat(person.GenderCode, typeof(CV<AdministrativeGender>), out gend);

            // Set the identified person
            retVal.IdentifiedPerson = new MARC.Everest.RMIM.CA.R020402.PRPA_MT101102CA.Person(
                null,
                null,
                gend as CV<AdministrativeGender>,
                CreateTS(person.BirthTime, details),
                person.DeceasedTime != null,
                CreateTS(person.DeceasedTime, details),
                person.BirthOrder.HasValue ? (BL)true : null,
                person.BirthOrder,
                null,
                null,
                null,
                null);

            // Create names
            retVal.IdentifiedPerson.Name = new LIST<PN>();
            if (person.Names != null)
                foreach (var name in person.Names)
                    retVal.IdentifiedPerson.Name.Add(CreatePN(name, details));
            else
                retVal.IdentifiedPerson.Name.NullFlavor = NullFlavor.NoInformation;

            // Create telecoms
            retVal.IdentifiedPerson.Telecom = new LIST<TEL>();
            if (person.TelecomAddresses != null)
                foreach (var tel in person.TelecomAddresses)
                    retVal.IdentifiedPerson.Telecom.Add(CreateTEL(tel, details));
            else
                retVal.IdentifiedPerson.Telecom.NullFlavor = NullFlavor.NoInformation;

            // Create addresses
            retVal.IdentifiedPerson.Addr = new LIST<AD>();
            if (person.Addresses != null)
                foreach (var addr in person.Addresses)
                    retVal.IdentifiedPerson.Addr.Add(CreateAD(addr, details));
            else
                retVal.IdentifiedPerson.Addr.NullFlavor = NullFlavor.NoInformation;

            // Create AsOtherIds
            retVal.IdentifiedPerson.AsOtherIDs = new List<MARC.Everest.RMIM.CA.R020402.PRPA_MT101102CA.OtherIDs>();
            if (person.OtherIdentifiers != null)
                foreach (var othId in person.OtherIdentifiers)
                {
                    var otherIdentifier = new MARC.Everest.RMIM.CA.R020402.PRPA_MT101102CA.OtherIDs(
                        CreateII(othId.Value, details),
                        CreateCV<String>(othId.Key, details),
                        null);
                    // Any extensions that apply to this?
                    var extId = person.FindExtension(o => o.Name == "AssigningIdOrganizationId" && o.PropertyPath == String.Format("OtherIdentifiers[{0}{1}]", otherIdentifier.Id.Root, otherIdentifier.Id.Extension));
                    var extName = person.FindExtension(o => o.Name == "AssigningIdOrganizationName" && o.PropertyPath == String.Format("OtherIdentifiers[{0}{1}]", otherIdentifier.Id.Root, otherIdentifier.Id.Extension));
                    if (extId != null || extName != null)
                        otherIdentifier.AssigningIdOrganization = new MARC.Everest.RMIM.CA.R020402.PRPA_MT101104CA.IdOrganization(
                            extId != null ? CreateII(extId.Value as DomainIdentifier, details) : null,
                            extName != null ? extName.Value as String : null
                        );
                    retVal.IdentifiedPerson.AsOtherIDs.Add(otherIdentifier);
                }

            // Languages
            if (person.Language != null)
                foreach (var lang in person.Language)
                {
                    // Translate the code
                    var langCode = new CodeValue(lang.Language, configService.OidRegistrar.GetOid("ISO639-1").Oid);

                    if (termService != null)
                        langCode = termService.Translate(langCode, configService.OidRegistrar.GetOid("ISO639-3").Oid);

                    retVal.IdentifiedPerson.LanguageCommunication.Add(new MARC.Everest.RMIM.CA.R020402.PRPA_MT101104CA.LanguageCommunication(
                        CreateCV<String>(langCode, details),
                        lang.Type == LanguageType.Fluency
                        ));
                }

            // Personal Relationships
            if (relations != null)
                foreach (PersonalRelationship relation in relations)
                    if (!String.IsNullOrEmpty(relation.RelationshipKind))
                        retVal.IdentifiedPerson.PersonalRelationship.Add(new MARC.Everest.RMIM.CA.R020402.PRPA_MT101104CA.PersonalRelationship(
                            Util.Convert<PersonalRelationshipRoleType>(relation.RelationshipKind),
                            new MARC.Everest.RMIM.CA.R020402.PRPA_MT101104CA.ParentPerson(
                                CreateII(relation.AlternateIdentifiers[0], details),
                                CreatePN(relation.LegalName, details)
                            )
                        ));

            return retVal;
        }
    }
}
