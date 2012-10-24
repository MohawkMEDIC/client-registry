using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MARC.HI.EHRS.SVC.Core.Services;
using MARC.HI.EHRS.CR.Core.ComponentModel;
using MARC.Everest.DataTypes;
using MARC.HI.EHRS.SVC.Core.ComponentModel;
using MARC.HI.EHRS.SVC.Core.ComponentModel.Components;
using MARC.Everest.RMIM.UV.NE2008.Vocabulary;
using MARC.Everest.Connectors;
using MARC.HI.EHRS.SVC.Core.DataTypes;
using MARC.Everest.DataTypes.Interfaces;

namespace MARC.HI.EHRS.CR.Messaging.Everest.MessageReceiver.UV
{
    /// <summary>
    /// Universal de-construction component utility
    /// </summary>
    public class UvDeComponentUtil : DeComponentUtil
    {
        /// <summary>
        /// Create the registration event
        /// </summary>
        internal MARC.Everest.RMIM.UV.NE2008.MFMI_MT700711UV01.RegistrationEvent<MARC.Everest.RMIM.UV.NE2008.PRPA_MT201304UV02.Patient, object> CreateRegistrationEventDetail(Core.ComponentModel.RegistrationEvent res, List<MARC.Everest.Connectors.IResultDetail> details)
        {

            // Patient
            var patient = res.FindComponent(SVC.Core.ComponentModel.HealthServiceRecordSiteRoleType.SubjectOf) as Person;

            if(patient == null)
                return new MARC.Everest.RMIM.UV.NE2008.MFMI_MT700711UV01.RegistrationEvent<MARC.Everest.RMIM.UV.NE2008.PRPA_MT201304UV02.Patient,object>() { NullFlavor = NullFlavor.NoInformation };

            var scoper = patient.FindComponent(HealthServiceRecordSiteRoleType.PlaceOfEntry | HealthServiceRecordSiteRoleType.InformantTo) as HealthcareParticipant;

            // Return status
            var retVal = new MARC.Everest.RMIM.UV.NE2008.MFMI_MT700711UV01.RegistrationEvent<MARC.Everest.RMIM.UV.NE2008.PRPA_MT201304UV02.Patient, object>(
                ActStatus.Active,
                new MARC.Everest.RMIM.UV.NE2008.MFMI_MT700711UV01.Subject2<MARC.Everest.RMIM.UV.NE2008.PRPA_MT201304UV02.Patient>(
                    new MARC.Everest.RMIM.UV.NE2008.PRPA_MT201304UV02.Patient(
                        CreateIISet(patient.AlternateIdentifiers, details),
                        ConvertStatus(patient.Status, details),
                        null,
                        CreateOrganization(scoper, details)
                    )
                )
            );

            retVal.Subject1.registeredRole.SetPatientEntityChoiceSubject(CreatePerson(patient, details));
            return retVal;

        }

        /// <summary>
        /// Create the person portion
        /// </summary>
        private MARC.Everest.RMIM.UV.NE2008.PRPA_MT201304UV02.Person CreatePerson(Person patient, List<IResultDetail> details)
        {
            var retVal = new MARC.Everest.RMIM.UV.NE2008.PRPA_MT201304UV02.Person();
            // Patient names
            if (patient.Names != null)
            {
                retVal.Name = new BAG<PN>();
                foreach (var nam in patient.Names)
                    retVal.Name.Add(CreatePN(nam, details));
            }

            // Create AsOtherIds
            retVal.AsOtherIDs = new List<MARC.Everest.RMIM.UV.NE2008.PRPA_MT201303UV02.OtherIDs>();
            if (patient.OtherIdentifiers != null)
                foreach (var othId in patient.OtherIdentifiers)
                {
                    var otherIdentifier = new MARC.Everest.RMIM.UV.NE2008.PRPA_MT201303UV02.OtherIDs(
                        SET<II>.CreateSET(CreateII(othId.Value, details)),
                        RoleStatus.Active,
                        null,
                        null);
                    // Any extensions that apply to this?
                    var propertyPath = String.Format("OtherIdentifiers[{0}{1}]", othId.Value.Domain, othId.Value.Identifier);
                    var othAltId = patient.FindAllExtensions(o => o.Name == "AssigningIdOrganizationExtraId" && o.PropertyPath == propertyPath );
                    var extId = patient.FindAllExtensions(o => o.Name == "AssigningIdOrganizationId" && o.PropertyPath == propertyPath);
                    var extName = patient.FindAllExtensions(o => o.Name == "AssigningIdOrganizationName" && o.PropertyPath == propertyPath);
                    var extCode = patient.FindExtension(o => o.Name == "AssigningIdOrganizationCode" && o.PropertyPath == propertyPath);
                    if(othAltId != null)
                        foreach (var id in othAltId)
                            otherIdentifier.Id.Add(CreateII(id.Value as DomainIdentifier, details));

                    // Any of the extensions that apply to scoping org applied
                    if (extId != null || extName != null || extCode != null)
                    {
                        // Scoping org
                        otherIdentifier.ScopingOrganization = new MARC.Everest.RMIM.UV.NE2008.COCT_MT150002UV01.Organization(
                            null,
                            extCode != null ? CreateCD<String>(extCode.Value as CodeValue, details) : null,
                            null,
                            null
                        );

                        // Extended identifiers (scoping id org id)
                        if (extId != null)
                        {
                            otherIdentifier.ScopingOrganization.Id = new SET<II>();
                            foreach (var ii in extId)
                                otherIdentifier.ScopingOrganization.Id.Add(CreateII(ii.Value as DomainIdentifier, details));
                        }

                        // Extension identifiers for name
                        if (extName != null)
                        {
                            otherIdentifier.ScopingOrganization.Name = new BAG<ON>();
                            foreach (var on in extName)
                                otherIdentifier.ScopingOrganization.Name.Add(new ON(EntityNameUse.Legal, new ENXP[] { new ENXP(on.Value as String) }));
                        }

                    }
                    retVal.AsOtherIDs.Add(otherIdentifier);
                }

            return retVal;

        }

        /// <summary>
        /// Create organization scoper
        /// </summary>
        private MARC.Everest.RMIM.UV.NE2008.COCT_MT150003UV03.Organization CreateOrganization(HealthcareParticipant scoper, List<MARC.Everest.Connectors.IResultDetail> details)
        {
            if (scoper == null) return null;

            PN tName = null;
            if(scoper.LegalName != null)
                tName = CreatePN(scoper.LegalName, details);

            // Basic return value
            var retVal = new MARC.Everest.RMIM.UV.NE2008.COCT_MT150003UV03.Organization(
                CreateIISet(scoper.AlternateIdentifiers, details),
                scoper.Type != null ? CreateCD<String>(scoper.Type, details) : null,
                tName != null ? BAG<ON>.CreateBAG(new ON(tName.Use[0], tName.Part)) : null,
                null
            );

            // Find all representatives
            foreach (HealthcareParticipant rep in scoper.FindAllComponents(HealthServiceRecordSiteRoleType.RepresentitiveOf))
            {
                var cp = new MARC.Everest.RMIM.UV.NE2008.COCT_MT150003UV03.ContactParty(
                    CreateIISet(rep.AlternateIdentifiers, details),
                    rep.Type != null ? CreateCD<String>(rep.Type, details) : null,
                    rep.PrimaryAddress != null ? BAG<AD>.CreateBAG(CreateAD(rep.PrimaryAddress, details)) : null,
                    null,
                    null
                    );

                // Add telecoms
                if (rep.TelecomAddresses != null)
                {
                    cp.Telecom = new BAG<TEL>();
                    foreach (var tel in rep.TelecomAddresses)
                        cp.Telecom.Add(CreateTEL(tel, details));
                }

                // Person info
                if (rep.Classifier == HealthcareParticipant.HealthcareParticipantType.Person && rep.LegalName != null)
                    cp.ContactPerson = new MARC.Everest.RMIM.UV.NE2008.COCT_MT150003UV03.Person(
                        BAG<EN>.CreateBAG(CreatePN(rep.LegalName, details))
                    );
                retVal.ContactParty.Add(cp);
            }

            return retVal;
        }

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
        /// Convert the specified patient record to a registration event
        /// </summary>
        internal MARC.Everest.RMIM.UV.NE2008.MFMI_MT700711UV01.RegistrationEvent<MARC.Everest.RMIM.UV.NE2008.PRPA_MT201310UV02.Patient, object> CreateRegistrationEvent(RegistrationEvent res, List<IResultDetail> details)
        {
            // Patient
            var patient = res.FindComponent(SVC.Core.ComponentModel.HealthServiceRecordSiteRoleType.SubjectOf) as Person;
            
            if (patient == null)
                return new MARC.Everest.RMIM.UV.NE2008.MFMI_MT700711UV01.RegistrationEvent<MARC.Everest.RMIM.UV.NE2008.PRPA_MT201310UV02.Patient,object>() { NullFlavor = NullFlavor.NoInformation };

            var scoper = patient.FindComponent(HealthServiceRecordSiteRoleType.PlaceOfEntry | HealthServiceRecordSiteRoleType.InformantTo) as HealthcareParticipant;
            var queryParameter = patient.FindComponent(HealthServiceRecordSiteRoleType.CommentOn | HealthServiceRecordSiteRoleType.ComponentOf) as QueryParameters;
            var mask = patient.FindComponent(HealthServiceRecordSiteRoleType.FilterOf) as MaskingIndicator;

            // Return status
            var retVal = new MARC.Everest.RMIM.UV.NE2008.MFMI_MT700711UV01.RegistrationEvent<MARC.Everest.RMIM.UV.NE2008.PRPA_MT201310UV02.Patient, object>(
                ActStatus.Active,
                new MARC.Everest.RMIM.UV.NE2008.MFMI_MT700711UV01.Subject2<MARC.Everest.RMIM.UV.NE2008.PRPA_MT201310UV02.Patient>(
                    new MARC.Everest.RMIM.UV.NE2008.PRPA_MT201310UV02.Patient(
                        CreateIISet(patient.AlternateIdentifiers, details),
                        ConvertStatus(patient.Status, details),
                        null,
                        CreateOrganization(scoper, details),
                        null
                    )
                )
            );

            retVal.Subject1.registeredRole.SetPatientEntityChoiceSubject(CreatePersonDetail(patient, details));
            
            
            // Mask
            if (mask != null)
                retVal.Subject1.registeredRole.ConfidentialityCode = new SET<CE<string>>(
                    CreateCD<String>(mask.MaskingCode, details)
                );

            if (patient.VipCode != null)
                retVal.Subject1.registeredRole.VeryImportantPersonCode = CreateCD<String>(patient.VipCode, details);

            // Query observation
            if (queryParameter != null)
            {
                retVal.Subject1.registeredRole.SubjectOf1.Add(new MARC.Everest.RMIM.UV.NE2008.PRPA_MT201310UV02.Subject(
                    new MARC.Everest.RMIM.UV.NE2008.PRPA_MT202310UV02.QueryMatchObservation(
                        new CD<string>(queryParameter.MatchingAlgorithm == MatchAlgorithm.Soundex ? "PHCM" : "PTNM", "2.16.840.1.113883.2.20.5.2"),
                        new INT((int)(queryParameter.Confidence * 100))
                    )
                ));
            }

            return retVal;
        }

        /// <summary>
        /// Create the person object
        /// </summary>
        private MARC.Everest.RMIM.UV.NE2008.PRPA_MT201310UV02.Person CreatePersonDetail(Person patient, List<IResultDetail> details)
        {
            if (patient == null)
                return new MARC.Everest.RMIM.UV.NE2008.PRPA_MT201310UV02.Person() { NullFlavor = NullFlavor.NoInformation };

            var retVal = new MARC.Everest.RMIM.UV.NE2008.PRPA_MT201310UV02.Person();
            var relations = patient.FindAllComponents(HealthServiceRecordSiteRoleType.RepresentitiveOf);
            
            // Names
            if (patient.Names != null)
            {
                retVal.Name = new BAG<PN>();
                foreach (var n in patient.Names)
                    retVal.Name.Add(CreatePN(n, details));
            }

            // Telecoms
            if (patient.TelecomAddresses != null)
            {
                retVal.Telecom = new BAG<TEL>();
                foreach (var t in patient.TelecomAddresses)
                {
                    var tel = CreateTEL(t, details);
                    var use = patient.FindExtension(o=>o.Name == "UsablePeriod" && o.PropertyPath == String.Format("TelecomAddresses[{0}]", t.Value));
                    if(use != null)
                        tel.UseablePeriod = new GTS() { Hull = use.Value as ISetComponent<TS> };
                    retVal.Telecom.Add(tel);
                }
            }

            // Gender
            if(!String.IsNullOrEmpty(patient.GenderCode))
                retVal.AdministrativeGenderCode = Util.Convert<AdministrativeGender>(patient.GenderCode);

            // Birth
            if (patient.BirthTime != null)
                retVal.BirthTime = CreateTS(patient.BirthTime, details);

            // Deceased
            if (patient.DeceasedTime != null)
            {
                retVal.DeceasedInd = true;
                retVal.DeceasedTime = CreateTS(patient.DeceasedTime, details);
            }

            // Multiple birth
            if (patient.BirthOrder.HasValue)
            {
                retVal.MultipleBirthInd = true;
                if (patient.BirthOrder >= 0)
                    retVal.MultipleBirthOrderNumber = patient.BirthOrder.Value;
            }

            // Addresses
            if (patient.Addresses != null)
            {
                retVal.Addr = new BAG<AD>();
                foreach (var adr in patient.Addresses)
                    retVal.Addr.Add(CreateAD(adr, details));
            }

            // Marital status code
            if (patient.MaritalStatus != null)
                retVal.MaritalStatusCode = CreateCD<String>(patient.MaritalStatus, details);

            // Religious affiliation
            if (patient.ReligionCode != null)
                retVal.ReligiousAffiliationCode = CreateCD<String>(patient.ReligionCode, details);

            // Ethnicity
            var eth = patient.FindAllExtensions(o => o.Name == "EthnicGroupCode");
            if (eth != null && eth.Count() > 0)
            {
                retVal.EthnicGroupCode = new SET<CE<string>>();
                foreach (var e in eth)
                    retVal.EthnicGroupCode.Add(CreateCD<string>(e.Value as CodeValue, details));
            }

            // Other identifiers
            if (patient.OtherIdentifiers != null)
            {
                retVal.AsOtherIDs = new List<MARC.Everest.RMIM.UV.NE2008.PRPA_MT201303UV02.OtherIDs>();
                foreach (var id in patient.OtherIdentifiers)
                {
                    // Other identifiers
                    var othId = new MARC.Everest.RMIM.UV.NE2008.PRPA_MT201303UV02.OtherIDs(
                        new SET<II>(CreateII(id.Value, details)),
                        null,
                        null,
                        new MARC.Everest.RMIM.UV.NE2008.COCT_MT150002UV01.Organization());

                    // Create Other identifiers
                    var extAddlId = patient.FindAllExtensions(o => o.Name == "AssigningIdOrganizationExtraId" && o.PropertyPath == String.Format("OtherIdentifiers[{0}{1}]", id.Value.Domain, id.Value.Identifier));
                    if (extAddlId != null)
                        foreach (var extId in extAddlId)
                            othId.Id.Add(CreateII(extId.Value as DomainIdentifier, details));

                    // Scoping and other extendsion
                    var extScopingOrgs = patient.FindAllExtensions(o => o.Name == "AssigningIdOrganizationId" && o.PropertyPath == String.Format("OtherIdentifiers[{0}{1}]", id.Value.Domain, id.Value.Identifier));
                    var extScopingNames = patient.FindAllExtensions(o => o.Name == "AssigningIdOrganizationName" && o.PropertyPath == String.Format("OtherIdentifiers[{0}{1}]", id.Value.Domain, id.Value.Identifier));
                    var extScopingCode = patient.FindExtension(o => o.Name == "AssigningIdOrganizationCode" && o.PropertyPath == String.Format("OtherIdentifiers[{0}{1}]", id.Value.Domain, id.Value.Identifier));
                    if (extScopingOrgs != null)
                    {
                        othId.ScopingOrganization.Id = new SET<II>();
                        foreach (var scpId in extScopingOrgs)
                            othId.ScopingOrganization.Id.Add(CreateII(scpId.Value as DomainIdentifier, details));
                    }
                    if (extScopingNames != null)
                    {
                        othId.ScopingOrganization.Name = new BAG<ON>();
                        foreach (var scpName in extScopingNames)
                            othId.ScopingOrganization.Name.Add(new ON(EntityNameUse.Legal, new ENXP[] { new ENXP(scpName.Value.ToString()) }));
                    }
                    if (extScopingCode != null)
                        othId.ScopingOrganization.Code = CreateCD<String>(extScopingCode.Value as CodeValue, details);

                    retVal.AsOtherIDs.Add(othId);
                }
            }

            // Personal relationships
            if (relations != null)
                foreach (var rel in relations)
                    retVal.PersonalRelationship.Add(CreatePersonalRelationship(rel as PersonalRelationship, details));

            // Citizenships
            if (patient.Citizenship != null)
                foreach (var cit in patient.Citizenship)
                {
                    var citizenRole = new MARC.Everest.RMIM.UV.NE2008.PRPA_MT201303UV02.Citizen(
                        null,
                        cit.EffectiveTime != null ? CreateIVL(cit.EffectiveTime, details) : null,
                        new MARC.Everest.RMIM.UV.NE2008.PRPA_MT201303UV02.Nation(
                            new CD<string>(cit.CountryCode, this.m_configService.OidRegistrar.GetOid("ISO3166-1").Oid),
                            cit.CountryName != null ? new ON(EntityNameUse.Legal, new ENXP[] { new ENXP(cit.CountryName) }) : null
                        )
                    );

                    // Citizenship identifiers
                    var extCitId = patient.FindExtension(o => o.Name == "CitizenshipIds" && o.PropertyPath == String.Format("Citizenship[{0}]", cit.CountryCode));
                    if (extCitId != null)
                        citizenRole.Id = CreateIISet(extCitId.Value as List<DomainIdentifier>, details);
                }

            // Employment
            if(patient.Employment != null)
                foreach (var emp in patient.Employment)
                {
                    var employmentRole = new MARC.Everest.RMIM.UV.NE2008.PRPA_MT201303UV02.Employee();
                    if(emp.Occupation != null)
                        employmentRole.OccupationCode = CreateCD<String>(emp.Occupation, details);
                    if (emp.EffectiveTime != null)
                        employmentRole.EffectiveTime = CreateIVL(emp.EffectiveTime, details);
                    employmentRole.StatusCode = ConvertStatusRole(emp.Status);
                    retVal.AsEmployee.Add(employmentRole);
                }

            // Lanugage of communication
            if(patient.Language != null)
                foreach (var lang in patient.Language)
                {
                    var langRole = new MARC.Everest.RMIM.UV.NE2008.COCT_MT030000UV04.LanguageCommunication();
                    if (!String.IsNullOrEmpty(lang.Language))
                        langRole.LanguageCode = new CE<string>(lang.Language, this.m_configService.OidRegistrar.GetOid("ISO639-1").Oid);
                    else
                        langRole.LanguageCode = new CE<string>() { NullFlavor = NullFlavor.NoInformation };

                    langRole.PreferenceInd = lang.Type == LanguageType.Fluency;

                    retVal.LanguageCommunication.Add(langRole);
                        
                }

            return retVal;
        }

        /// <summary>
        /// Convert role status
        /// </summary>
        private CS<RoleStatus> ConvertStatusRole(StatusType statusType)
        {
            switch (statusType)
            {
                case StatusType.Active:
                    return RoleStatus.Active;
                case StatusType.Cancelled:
                    return RoleStatus.Cancelled;
                case StatusType.Nullified:
                    return RoleStatus.Nullified;
                case StatusType.New:
                    return RoleStatus.Pending;
                case StatusType.Cancelled | StatusType.Active:
                    return RoleStatus.Suspended;
                case StatusType.Cancelled | StatusType.Obsolete:
                    return RoleStatus.Terminated;
                case StatusType.Unknown:
                    return RoleStatus.Normal;
                default:
                    return RoleStatus.Active;

            }
        }

        /// <summary>
        /// Create personal relationship
        /// </summary>
        private MARC.Everest.RMIM.UV.NE2008.PRPA_MT201303UV02.PersonalRelationship CreatePersonalRelationship(PersonalRelationship rel, List<IResultDetail> details)
        {
            var retVal = new MARC.Everest.RMIM.UV.NE2008.PRPA_MT201303UV02.PersonalRelationship();

            
            // Identifier
            retVal.Id = new SET<II>(new II(rel.Id.ToString(), this.m_configService.OidRegistrar.GetOid("CR_PRID").Oid));
            if (!String.IsNullOrEmpty(rel.RelationshipKind))
                retVal.Code = new CE<string>(rel.RelationshipKind, "2.16.840.1.113883.5.111");

            // Effective time
            var efftTsExt = rel.FindExtension(o=>o.Name == "EffectiveTime");
            if (efftTsExt != null)
                retVal.EffectiveTime = CreateIVL(efftTsExt.Value as TimestampSet, details);

            // Relationship holder
            var holder = new MARC.Everest.RMIM.UV.NE2008.COCT_MT030007UV.Person();
            if (rel.GenderCode != null)
                holder.AdministrativeGenderCode = Util.Convert<AdministrativeGender>(rel.GenderCode);

            // Birth
            if (rel.BirthTime != null)
                holder.BirthTime = CreateTS(rel.BirthTime, details);

            holder.Id = CreateIISet(rel.AlternateIdentifiers, details);
            // Name
            if (rel.LegalName != null)
                holder.Name = new BAG<EN>() { CreatePN(rel.LegalName, details) };
            // Address
            if (rel.PerminantAddress != null)
                retVal.Addr = new BAG<AD>() { CreateAD(rel.PerminantAddress, details) };

            // Telecom
            if (rel.TelecomAddresses != null)
            {
                retVal.Telecom = new BAG<TEL>();
                foreach (var tel in rel.TelecomAddresses)
                    retVal.Telecom.Add(CreateTEL(tel, details));
            }

            retVal.SetRelationshipHolder1(holder);

            return retVal;
        }
    }
}
