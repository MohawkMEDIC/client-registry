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
                verifiedPerson.BirthOrder,
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
            return new MARC.Everest.RMIM.CA.R020402.MFMI_MT700746CA.RegistrationEvent<MARC.Everest.RMIM.CA.R020402.PRPA_MT101104CA.IdentifiedEntity>();
        }
    }
}
