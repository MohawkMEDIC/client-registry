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
            var scoper = res.FindComponent(HealthServiceRecordSiteRoleType.PlaceOfEntry | HealthServiceRecordSiteRoleType.InformantTo) as HealthcareParticipant;

            if(patient == null)
                return new MARC.Everest.RMIM.UV.NE2008.MFMI_MT700711UV01.RegistrationEvent<MARC.Everest.RMIM.UV.NE2008.PRPA_MT201304UV02.Patient,object>() { NullFlavor = NullFlavor.NoInformation };

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

    }
}
