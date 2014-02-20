/**
 * Copyright 2013-2013 Mohawk College of Applied Arts and Technology
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
 * Date: 19-2-2013
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MARC.HI.EHRS.SVC.Core.Services;
using MARC.HI.EHRS.CR.Notification.PixPdq.Configuration;
using MARC.Everest.RMIM.UV.NE2008.Interactions;
using MARC.Everest.RMIM.UV.NE2008.Vocabulary;
using MARC.Everest.DataTypes;
using System.Reflection;
using MARC.HI.EHRS.CR.Core.ComponentModel;
using MARC.HI.EHRS.SVC.Core.DataTypes;
using MARC.HI.EHRS.SVC.Core.ComponentModel.Components;
using System.Diagnostics;

namespace MARC.HI.EHRS.CR.Notification.PixPdq
{
    /// <summary>
    /// Message utility class
    /// </summary>
    public partial class MessageUtility : IUsesHostContext
    {
        #region IUsesHostContext Members

        /// <summary>
        /// The name of the software
        /// </summary>
        static AssemblyProductAttribute SoftwareName = null;
        /// <summary>
        /// The description of the software
        /// </summary>
        static AssemblyDescriptionAttribute SoftwareDescription = null;
        /// <summary>
        /// Version of the software
        /// </summary>
        static Version SoftwareVersion = null;

        /// <summary>
        /// Static constructor for the not supported exception
        /// </summary>
        static MessageUtility()
        {
            SoftwareName = Assembly.GetEntryAssembly().GetCustomAttributes(typeof(AssemblyProductAttribute), false)[0] as AssemblyProductAttribute;
            SoftwareDescription = Assembly.GetEntryAssembly().GetCustomAttributes(typeof(AssemblyDescriptionAttribute), false)[0] as AssemblyDescriptionAttribute;
            SoftwareVersion = Assembly.GetEntryAssembly().GetName().Version;
        }

        #endregion
        
        /// <summary>
        /// Create a message based on the parameters
        /// </summary>
        internal Everest.Interfaces.IInteraction CreateMessage(Core.ComponentModel.RegistrationEvent registrationEvent, Configuration.ActionType actionType, TargetConfiguration configuration)
        {
            // Determine the action that was taken
            switch (actionType)
            {
                case Configuration.ActionType.Create:
                    return CreatePatientRegistryRecordAddedMessage(registrationEvent, configuration);
                case Configuration.ActionType.Update:
                    return CreatePatientRegistryRecordRevisedMessage(registrationEvent, configuration);
                case Configuration.ActionType.DuplicatesResolved:
                    return CreatePatientRegistryDuplicatesResolvedMessage(registrationEvent, configuration);
                default:
                    return null;
            }
        }

        /// <summary>
        /// Create a patient registry record added message
        /// </summary>
        private Everest.Interfaces.IInteraction CreatePatientRegistryRecordAddedMessage(Core.ComponentModel.RegistrationEvent registrationEvent, TargetConfiguration configuration)
        {
            // Construct the return value
            PRPA_IN201301UV02 retVal = new PRPA_IN201301UV02(
                Guid.NewGuid(),
                DateTime.Now,
                PRPA_IN201301UV02.GetInteractionId(),
                ProcessingID.Production,
                "T",
                AcknowledgementCondition.Always);

            // Construct the sending node
            retVal.VersionCode = HL7StandardVersionCode.Version3_Prerelease1;
            retVal.Sender = CreateSenderNode();
            retVal.Receiver.Add(CreateReceiverNode(configuration));

            // Construct the control act process
            retVal.controlActProcess = new Everest.RMIM.UV.NE2008.MFMI_MT700701UV01.ControlActProcess<Everest.RMIM.UV.NE2008.PRPA_MT201301UV02.Patient, object>("EVN");

            var subject = new Everest.RMIM.UV.NE2008.MFMI_MT700701UV01.Subject1<Everest.RMIM.UV.NE2008.PRPA_MT201301UV02.Patient, object>(false,
                new Everest.RMIM.UV.NE2008.MFMI_MT700701UV01.RegistrationEvent<Everest.RMIM.UV.NE2008.PRPA_MT201301UV02.Patient, object>(
                    ConvertActStatusCode(registrationEvent.Status),
                    CreatePatient(registrationEvent, configuration)
                )
            );
            retVal.controlActProcess.Subject.Add(subject);

            // Custodian?
            subject.RegistrationEvent.Custodian = CreateCustodian(registrationEvent, configuration);

            return retVal;
        }

        /// <summary>
        /// Create a patient registry record revised message
        /// </summary>
        private Everest.Interfaces.IInteraction CreatePatientRegistryRecordRevisedMessage(Core.ComponentModel.RegistrationEvent registrationEvent, TargetConfiguration configuration)
        {
            // Construct the return value
            PRPA_IN201302UV02 retVal = new PRPA_IN201302UV02(
                Guid.NewGuid(),
                DateTime.Now,
                PRPA_IN201302UV02.GetInteractionId(),
                ProcessingID.Production,
                "T",
                AcknowledgementCondition.Always);

            // Construct the sending node
            retVal.VersionCode = HL7StandardVersionCode.Version3_Prerelease1;
            retVal.Sender = CreateSenderNode();
            retVal.Receiver.Add(CreateReceiverNode(configuration));

            // Construct the control act process
            retVal.controlActProcess = new Everest.RMIM.UV.NE2008.MFMI_MT700701UV01.ControlActProcess<Everest.RMIM.UV.NE2008.PRPA_MT201302UV02.Patient, object>("EVN");

            var subject = new Everest.RMIM.UV.NE2008.MFMI_MT700701UV01.Subject1<Everest.RMIM.UV.NE2008.PRPA_MT201302UV02.Patient, object>(false,
                new Everest.RMIM.UV.NE2008.MFMI_MT700701UV01.RegistrationEvent<Everest.RMIM.UV.NE2008.PRPA_MT201302UV02.Patient, object>(
                    ConvertActStatusCode(registrationEvent.Status),
                    CreatePatientUpdate(registrationEvent, configuration)
                )
            );

            retVal.controlActProcess.Subject.Add(subject);

            // Custodian?
            subject.RegistrationEvent.Custodian = CreateCustodian(registrationEvent, configuration);

            return retVal;
        }

        /// <summary>
        /// Create a patient registry duplicates resolved message
        /// </summary>
        private Everest.Interfaces.IInteraction CreatePatientRegistryDuplicatesResolvedMessage(Core.ComponentModel.RegistrationEvent registrationEvent, TargetConfiguration configuration)
        {
            // Construct the return value
            PRPA_IN201304UV02 retVal = new PRPA_IN201304UV02(
                Guid.NewGuid(),
                DateTime.Now,
                PRPA_IN201304UV02.GetInteractionId(),
                ProcessingID.Production,
                "T",
                AcknowledgementCondition.Always);

            // Construct the sending node
            retVal.VersionCode = HL7StandardVersionCode.Version3_Prerelease1;
            retVal.Sender = CreateSenderNode();
            retVal.Receiver.Add(CreateReceiverNode(configuration));

            // Construct the control act process
            retVal.controlActProcess = new Everest.RMIM.UV.NE2008.MFMI_MT700701UV01.ControlActProcess<Everest.RMIM.UV.NE2008.PRPA_MT201303UV02.Patient, object>("EVN");

            // Get the subjects and components
            var subject = registrationEvent.FindComponent(SVC.Core.ComponentModel.HealthServiceRecordSiteRoleType.SubjectOf) as Person;
            var providerOrg = subject.FindComponent(SVC.Core.ComponentModel.HealthServiceRecordSiteRoleType.PlaceOfEntry | SVC.Core.ComponentModel.HealthServiceRecordSiteRoleType.InformantTo) as HealthcareParticipant;
            var custodian = registrationEvent.FindComponent(SVC.Core.ComponentModel.HealthServiceRecordSiteRoleType.PlaceOfRecord | SVC.Core.ComponentModel.HealthServiceRecordSiteRoleType.ResponsibleFor);
            var replacements = subject.FindAllComponents(SVC.Core.ComponentModel.HealthServiceRecordSiteRoleType.ReplacementOf);

            // Create the person
            List<DomainIdentifier> identifiers = new List<DomainIdentifier>(subject.AlternateIdentifiers.FindAll(o => configuration.NotificationDomain.Exists(d => d.Domain.Equals(o.Domain))));
            var eventRegistration = new Everest.RMIM.UV.NE2008.MFMI_MT700701UV01.Subject1<Everest.RMIM.UV.NE2008.PRPA_MT201303UV02.Patient,object>(false,
                new Everest.RMIM.UV.NE2008.MFMI_MT700701UV01.RegistrationEvent<Everest.RMIM.UV.NE2008.PRPA_MT201303UV02.Patient, object>(
                    ActStatus.Active,
                    new Everest.RMIM.UV.NE2008.MFMI_MT700701UV01.Subject2<Everest.RMIM.UV.NE2008.PRPA_MT201303UV02.Patient>(
                        new Everest.RMIM.UV.NE2008.PRPA_MT201303UV02.Patient(
                            CreateIISet(identifiers),
                            RoleStatus.Active,
                            CreatePerson(subject, new TargetConfiguration(String.Empty, null, configuration.Notifier.GetType().Name, null)),
                            providerOrg == null ? null : CreateProviderOrganization(providerOrg)
                        )
                    )
                )
            );

            // Get person data
            var personData = eventRegistration.RegistrationEvent.Subject1.registeredRole.PatientEntityChoiceSubject as Everest.RMIM.UV.NE2008.PRPA_MT201303UV02.Person;
            personData.AsOtherIDs.Clear();
            retVal.controlActProcess.Subject.Add(eventRegistration);

            // TODO: Replacement of compatibility mode for other XDS registries
            // Replacement 
            var registration = this.Context.GetService(typeof(IDataRegistrationService)) as IDataRegistrationService;
            var persistence = this.Context.GetService(typeof(IDataPersistenceService)) as IDataPersistenceService;

            foreach (PersonRegistrationRef rplc in replacements)
            {
                // First, need to de-persist the identifiers
                QueryParameters qp = new QueryParameters()
                {
                    Confidence = 1.0f,
                    MatchingAlgorithm = MatchAlgorithm.Exact,
                    MatchStrength = MatchStrength.Exact
                };
                var patientQuery = new RegistrationEvent();
                patientQuery.Add(qp, "FLT", SVC.Core.ComponentModel.HealthServiceRecordSiteRoleType.FilterOf, null);
                patientQuery.Add(new Person() { AlternateIdentifiers = rplc.AlternateIdentifiers }, "SUBJ", SVC.Core.ComponentModel.HealthServiceRecordSiteRoleType.SubjectOf, null);
                // Perform the query
                var pid = registration.QueryRecord(patientQuery);
                if (pid.Length == 0)
                    throw new InvalidOperationException();
                var replacedPerson = (persistence.GetContainer(pid[0], true) as RegistrationEvent).FindComponent(SVC.Core.ComponentModel.HealthServiceRecordSiteRoleType.SubjectOf) as Person;

                var ids = CreateIISet(replacedPerson.AlternateIdentifiers.FindAll(o => configuration.NotificationDomain.Exists(d => d.Domain == o.Domain)));
                if (ids.Count == 0)
                    ; // TODO: Trace log
                else
                {
                    eventRegistration.RegistrationEvent.ReplacementOf.Add(new Everest.RMIM.UV.NE2008.MFMI_MT700701UV01.ReplacementOf(
                        new Everest.RMIM.UV.NE2008.MFMI_MT700701UV01.PriorRegistration(
                            null,
                            ActStatus.Obsolete,
                            new Everest.RMIM.UV.NE2008.MFMI_MT700701UV01.Subject3(
                                new Everest.RMIM.UV.NE2008.MFMI_MT700701UV01.PriorRegisteredRole(
                                    ids
                                )
                            ),
                            null
                        )
                        ));
                }
            }

            if (eventRegistration.RegistrationEvent.ReplacementOf.Count == 0)
                throw new InvalidOperationException("Nothing to do");

            // Custodian?
            eventRegistration.RegistrationEvent.Custodian = CreateCustodian(registrationEvent, configuration);

            return retVal;
        }

        #region HL7 Helper Functions

        /// <summary>
        /// Create a receiver node for the HL7 transport wrapper
        /// </summary>
        private Everest.RMIM.UV.NE2008.MCCI_MT100200UV01.Receiver CreateReceiverNode(TargetConfiguration configuration)
        {
            return new Everest.RMIM.UV.NE2008.MCCI_MT100200UV01.Receiver(
                new Everest.RMIM.UV.NE2008.MCCI_MT100200UV01.Device(
                    !String.IsNullOrEmpty(configuration.DeviceIdentifier) ? new SET<II>(new II(configuration.DeviceIdentifier)) : new SET<II>() { NullFlavor = NullFlavor.NoInformation }
                )
            );
        }

        /// <summary>
        /// Create a sender node for the HL7 transport wrapper
        /// </summary>
        /// <returns></returns>
        private Everest.RMIM.UV.NE2008.MCCI_MT100200UV01.Sender CreateSenderNode()
        {
            // Config service
            ISystemConfigurationService configService = this.Context.GetService(typeof(ISystemConfigurationService)) as ISystemConfigurationService;

            var retVal = new Everest.RMIM.UV.NE2008.MCCI_MT100200UV01.Sender(
                new Everest.RMIM.UV.NE2008.MCCI_MT100200UV01.Device(
                    new SET<II>(new II(configService.DeviceIdentifier))
                )
                {
                    Name = BAG<EN>.CreateBAG(
                        new EN(EntityNameUse.Assigned, new ENXP[] { new ENXP(configService.DeviceName) }),
                        new EN(EntityNameUse.Legal, new ENXP[] { new ENXP(Environment.MachineName) })
                    ),
                    SoftwareName = SoftwareName.Product,
                    Desc = SoftwareDescription.Description,
                    ManufacturerModelName = SoftwareVersion.ToString()
                }
            );

            return retVal;
        }

        /// <summary>
        /// Convert status code
        /// </summary>
        private ActStatus ConvertActStatusCode(SVC.Core.ComponentModel.Components.StatusType statusType)
        {
            return (ActStatus)Enum.Parse(typeof(ActStatus), statusType.ToString());
        }

        /// <summary>
        /// Create patient structure for the person suitable for a create
        /// </summary>
        private Everest.RMIM.UV.NE2008.MFMI_MT700701UV01.Subject2<Everest.RMIM.UV.NE2008.PRPA_MT201301UV02.Patient> CreatePatient(RegistrationEvent registrationEvent, TargetConfiguration configuration)
        {
            // Get the subject from the list of components
            var subject = registrationEvent.FindComponent(SVC.Core.ComponentModel.HealthServiceRecordSiteRoleType.SubjectOf) as Person;
            var masking = subject.FindComponent(SVC.Core.ComponentModel.HealthServiceRecordSiteRoleType.FilterOf) as MaskingIndicator;
            var providerOrg = subject.FindComponent(SVC.Core.ComponentModel.HealthServiceRecordSiteRoleType.PlaceOfEntry | SVC.Core.ComponentModel.HealthServiceRecordSiteRoleType.InformantTo) as HealthcareParticipant;

            if (subject == null) // validate
                return null;

            var iiSet = new List<II>(CreateIISet(subject.AlternateIdentifiers));
            iiSet.RemoveAll(ii => !configuration.NotificationDomain.Exists(o => o.Domain.Equals(ii.Root)));

            // Construct the return value
            var retVal = new Everest.RMIM.UV.NE2008.MFMI_MT700701UV01.Subject2<Everest.RMIM.UV.NE2008.PRPA_MT201301UV02.Patient>(
                new Everest.RMIM.UV.NE2008.PRPA_MT201301UV02.Patient(
                    new SET<II>(iiSet),
                    CreatePerson(subject, configuration),
                    null
                ));
            
            // Act as a source?
            // Masking indicator
            if (masking != null)
                retVal.registeredRole.ConfidentialityCode = new SET<CE<string>>(CreateCD<String>(masking.MaskingCode));

            // Provider org
            var oidData = m_configService.OidRegistrar.FindData(iiSet[0].Root);
            if (oidData != null)
            {
                retVal.registeredRole.ProviderOrganization = new Everest.RMIM.UV.NE2008.COCT_MT150003UV03.Organization()
                {
                    Id = SET<II>.CreateSET(new II(oidData.Oid)),
                    Name = BAG<ON>.CreateBAG(ON.CreateON(null, new ENXP(oidData.Attributes.Find(o=>o.Key == "CustodialOrgName").Value ?? oidData.Description))),
                    ContactParty = new List<Everest.RMIM.UV.NE2008.COCT_MT150003UV03.ContactParty>()
                    {
                        new Everest.RMIM.UV.NE2008.COCT_MT150003UV03.ContactParty() { NullFlavor = NullFlavor.NoInformation }
                    }
                };
                    
            }
            return retVal;
        }

        /// <summary>
        /// Create a patient structure for the person suitable for update
        /// </summary>
        private Everest.RMIM.UV.NE2008.MFMI_MT700701UV01.Subject2<Everest.RMIM.UV.NE2008.PRPA_MT201302UV02.Patient> CreatePatientUpdate(RegistrationEvent registrationEvent, TargetConfiguration configuration)
        {
            // Get the subject from the list of components
            var subject = registrationEvent.FindComponent(SVC.Core.ComponentModel.HealthServiceRecordSiteRoleType.SubjectOf) as Person;
            var masking = subject.FindComponent(SVC.Core.ComponentModel.HealthServiceRecordSiteRoleType.FilterOf) as MaskingIndicator;
            var providerOrg = subject.FindComponent(SVC.Core.ComponentModel.HealthServiceRecordSiteRoleType.PlaceOfEntry | SVC.Core.ComponentModel.HealthServiceRecordSiteRoleType.InformantTo) as HealthcareParticipant;

            if(subject == null) // validate
                return null;

            var iiSet = new List<II>(CreateIISet(subject.AlternateIdentifiers));
            iiSet.RemoveAll(ii=>!configuration.NotificationDomain.Exists(o=>o.Domain.Equals(ii.Root)));

            // Construct the return value
            var retVal = new Everest.RMIM.UV.NE2008.MFMI_MT700701UV01.Subject2<Everest.RMIM.UV.NE2008.PRPA_MT201302UV02.Patient>(
                new Everest.RMIM.UV.NE2008.PRPA_MT201302UV02.Patient(
                    new SET<II>(iiSet),
                    MARC.Everest.Connectors.Util.ToWireFormat(ConvertRoleStatusCode(subject.Status)),
                    null,
                    null
                ));

            
            var person = CreatePerson(subject, configuration);

            // Act as a source?
            if (configuration.Notifier.GetType().Name == "PAT_IDENTITY_SRC_HL7v3")
            {
                // Masking indicator
                if (masking != null)
                    retVal.registeredRole.ConfidentialityCode = new SET<CE<string>>(CreateCD<String>(masking.MaskingCode));

                // Provider org
                if (providerOrg != null)
                {
                    // Provider org
                    var oidData = m_configService.OidRegistrar.FindData(iiSet[0].Root);
                    if (oidData != null)
                    {
                        retVal.registeredRole.ProviderOrganization = new Everest.RMIM.UV.NE2008.COCT_MT150003UV03.Organization()
                        {
                            Id = SET<II>.CreateSET(new II(oidData.Oid)),

                            Name = BAG<ON>.CreateBAG(ON.CreateON(null, new ENXP(oidData.Attributes.Find(o => o.Key == "CustodialOrgName").Value ?? oidData.Description))),
                            ContactParty = new List<Everest.RMIM.UV.NE2008.COCT_MT150003UV03.ContactParty>()
                            {
                                new Everest.RMIM.UV.NE2008.COCT_MT150003UV03.ContactParty() { NullFlavor = NullFlavor.NoInformation }
                            }
                        };

                    }
                }
            }
            retVal.registeredRole.SetPatientEntityChoiceSubject(person);
            return retVal;
        }

        /// <summary>
        /// Create provider organization
        /// </summary>
        private Everest.RMIM.UV.NE2008.COCT_MT150003UV03.Organization CreateProviderOrganization(HealthcareParticipant providerOrg)
        {
            var contactParties = providerOrg.FindAllComponents(SVC.Core.ComponentModel.HealthServiceRecordSiteRoleType.RepresentitiveOf);

            // Construct
            var retVal = new Everest.RMIM.UV.NE2008.COCT_MT150003UV03.Organization(
                CreateIISet(providerOrg.AlternateIdentifiers),
                CreateCD<String>(providerOrg.Type),
                BAG<ON>.CreateBAG(CreateON(providerOrg.LegalName)),
                null
            );

            // Converts the contact party(ies)
            foreach (HealthcareParticipant cp in contactParties)
            {
                var contact = new Everest.RMIM.UV.NE2008.COCT_MT150003UV03.ContactParty(
                    CreateIISet(cp.AlternateIdentifiers),
                    CreateCD<String>(cp.Type),
                    BAG<AD>.CreateBAG(CreateAD(cp.PrimaryAddress)),
                    null,
                    null);

                // Add tel addresses to the contact parties
                if (cp.TelecomAddresses != null)
                {
                    contact.Telecom = new BAG<TEL>();
                    foreach (var tel in cp.TelecomAddresses)
                        contact.Telecom.Add(CreateTEL(tel));
                }

                // Person?
                if (cp.Classifier == HealthcareParticipant.HealthcareParticipantType.Person)
                    contact.ContactPerson = new Everest.RMIM.UV.NE2008.COCT_MT150003UV03.Person(
                        BAG<EN>.CreateBAG(CreatePN(cp.LegalName))
                    );

                retVal.ContactParty.Add(contact);
            }

            return retVal;
        }

        /// <summary>
        /// Create a person
        /// </summary>
        private Everest.RMIM.UV.NE2008.PRPA_MT201303UV02.Person CreatePerson(Person subject, TargetConfiguration configuration)
        {
            var retVal = new Everest.RMIM.UV.NE2008.PRPA_MT201303UV02.Person();

            // Personal relationships
            var personalRelationships = subject.FindAllComponents(SVC.Core.ComponentModel.HealthServiceRecordSiteRoleType.RepresentitiveOf);

            // Names
            if (subject.Names != null && subject.Names.Count > 0)
            {
                retVal.Name = new BAG<PN>();
                foreach (var name in subject.Names)
                    retVal.Name.Add(CreatePN(name));
            }

            // Other identifiers
            retVal.AsOtherIDs = new List<Everest.RMIM.UV.NE2008.PRPA_MT201303UV02.OtherIDs>();
            if (subject.OtherIdentifiers != null)
                foreach (var othId in subject.OtherIdentifiers)
                {
                    var otherIdentifier = new Everest.RMIM.UV.NE2008.PRPA_MT201303UV02.OtherIDs()
                    {
                        Id = new SET<II>(CreateII(othId.Value))
                    };

                    // Any extensions that apply to this?
                    var extId = subject.FindAllExtensions(o => o.Name == "AssigningIdOrganizationId" && o.PropertyPath == String.Format("OtherIdentifiers[{0}{1}]", othId.Value.Domain, othId.Value.Identifier));
                    var extName = subject.FindExtension(o => o.Name == "AssigningIdOrganizationName" && o.PropertyPath == String.Format("OtherIdentifiers[{0}{1}]", othId.Value.Domain, othId.Value.Identifier));
                    var extCode = subject.FindExtension(o => o.Name == "AssigningIdOrganizationCode" && o.PropertyPath == String.Format("OtherIdentifiers[{0}{1}]", othId.Value.Domain, othId.Value.Identifier));

                    // Additioanl identitifiers
                    foreach (var addlId in subject.FindAllExtensions(o => o.Name == "AssigningIdOrganizationExtraId" && o.PropertyPath == String.Format("OtherIdentifiers[{0}{1}]", othId.Value.Domain, othId.Value.Identifier)))
                        otherIdentifier.Id.Add(CreateII(addlId.Value as DomainIdentifier));

                    // Scoping org
                    if (extId.Count() > 0 || extName != null || extCode != null)
                    {
                        otherIdentifier.ScopingOrganization = new Everest.RMIM.UV.NE2008.COCT_MT150002UV01.Organization(
                            new SET<II>(),
                            extCode != null ? CreateCD<String>(extCode.Value as CodeValue) : null,
                            null,
                            null
                        );

                        // Identifiers
                        foreach (var id in extId)
                            otherIdentifier.ScopingOrganization.Id.Add(CreateII(id.Value as DomainIdentifier));

                        // Name
                        if (extName != null)
                        {
                            otherIdentifier.ScopingOrganization.Name = BAG<ON>.CreateBAG(new ON());
                            otherIdentifier.ScopingOrganization.Name[0].Part.Add(new ENXP(extName.Value as string));
                        }

                    }

                    retVal.AsOtherIDs.Add(otherIdentifier);
                }

            // Acting as a source?
            if (configuration.Notifier.GetType().Name == "PAT_IDENTITY_X_REF_MGR_HL7v3")
                return retVal;

            if (subject.Addresses != null) // addresses
            {
                retVal.Addr = new BAG<AD>();
                foreach (var ad in subject.Addresses)
                    retVal.Addr.Add(CreateAD(ad));
            }

            // Telecoms
            if (subject.TelecomAddresses != null)
            {
                retVal.Telecom = new BAG<TEL>();
                foreach (var tel in subject.TelecomAddresses)
                    retVal.Telecom.Add(CreateTEL(tel));
            }

            // Gender and birth
            if (subject.BirthTime != null)
                retVal.BirthTime = CreateTS(subject.BirthTime);
            if (subject.GenderCode != null)
                retVal.AdministrativeGenderCode = subject.GenderCode == "M" ? AdministrativeGender.Male : subject.GenderCode == "F" ? AdministrativeGender.Female : AdministrativeGender.Undifferentiated;
            if (subject.BirthOrder.HasValue)
            {
                retVal.MultipleBirthInd = true;
                retVal.MultipleBirthOrderNumber = subject.BirthOrder;
            }

            // Deceased
            if (subject.DeceasedTime != null)
            {
                retVal.DeceasedInd = true;
                retVal.DeceasedTime = CreateTS(subject.DeceasedTime);
            }

            // citizenship
            if (subject.Citizenship != null)
                foreach (var cit in subject.Citizenship)
                    retVal.AsCitizen.Add(CreateCitizenship(cit));
            // Employment
            if (subject.Employment != null)
                foreach (var emp in subject.Employment)
                    retVal.AsEmployee.Add(CreateEmployment(emp));
            // Language
            if (subject.Language != null)
                foreach (var lang in subject.Language)
                    retVal.LanguageCommunication.Add(CreateLanguage(lang));
            // Marital status
            if (subject.MaritalStatus != null)
                retVal.MaritalStatusCode = CreateCD<String>(subject.MaritalStatus);
            // Race
            if (subject.Race != null)
                foreach (var rce in subject.Race)
                    retVal.RaceCode.Add(CreateCD<String>(rce));
            // Religion
            if (subject.ReligionCode != null)
                retVal.ReligiousAffiliationCode = CreateCD<String>(subject.ReligionCode);
            if (subject.BirthPlace != null)
                retVal.BirthPlace = CreateLocation(subject.BirthPlace);

            // relationships
            foreach (PersonalRelationship psn in personalRelationships)
                retVal.PersonalRelationship.Add(CreatePersonalRelationship(psn));
            
            return retVal;
        }

        /// <summary>
        /// Create a personal relationship
        /// </summary>
        /// <param name="psn"></param>
        /// <returns></returns>
        private Everest.RMIM.UV.NE2008.PRPA_MT201303UV02.PersonalRelationship CreatePersonalRelationship(PersonalRelationship psn)
        {
            var retVal = new Everest.RMIM.UV.NE2008.PRPA_MT201303UV02.PersonalRelationship(
                CreateIISet(psn.AlternateIdentifiers),
                false,
                new CE<string>(psn.RelationshipKind, "2.16.840.1.113883.5.111"),
                psn.PerminantAddress != null ? BAG<AD>.CreateBAG(CreateAD(psn.PerminantAddress)) : null,
                null,
                ConvertRoleStatusCode(psn.Status),
                null,
                null);

            // Now to determine the additional parameters
            if (psn.TelecomAddresses != null)
                foreach (var tel in psn.TelecomAddresses)
                    retVal.Telecom.Add(CreateTEL(tel));

            // efft time stored as an extension
            var efftTs = psn.FindExtension(o => o.Name == "EffectiveTime");
            if (efftTs != null)
                retVal.EffectiveTime = CreateIVL(efftTs.Value as TimestampSet);

            // Relationship holder
            if (psn.GenderCode != null || psn.BirthTime != null || psn.LegalName != null)
            {
                var rh = new Everest.RMIM.UV.NE2008.COCT_MT030007UV.Person();
                if(psn.GenderCode != null)
                    rh.AdministrativeGenderCode = psn.GenderCode == "M" ? AdministrativeGender.Male : psn.GenderCode == "F" ? AdministrativeGender.Female : AdministrativeGender.Undifferentiated;
                if (psn.BirthTime != null)
                    rh.BirthTime = CreateTS(psn.BirthTime);
                if (psn.LegalName != null)
                    rh.Name = BAG<EN>.CreateBAG(CreatePN(psn.LegalName));
            }

            return retVal;
                       
        }

        /// <summary>
        /// Create a location
        /// </summary>
        /// <param name="serviceDeliveryLocation"></param>
        /// <returns></returns>
        private Everest.RMIM.UV.NE2008.PRPA_MT201303UV02.BirthPlace CreateLocation(ServiceDeliveryLocation serviceDeliveryLocation)
        {
            return new Everest.RMIM.UV.NE2008.PRPA_MT201303UV02.BirthPlace(
                null,
                new Everest.RMIM.UV.NE2008.COCT_MT710007UV.Place(
                    serviceDeliveryLocation.AlternateIdentifiers != null ? CreateIISet(serviceDeliveryLocation.AlternateIdentifiers) : null,
                    serviceDeliveryLocation.LocationType != null ? CreateCD<string>(serviceDeliveryLocation.LocationType) : null,
                    serviceDeliveryLocation.Name != null ? BAG<EN>.CreateBAG(EN.CreateEN(EntityNameUse.Legal, new ENXP(serviceDeliveryLocation.Name))) : null,
                    null,
                    serviceDeliveryLocation.Address != null ? CreateAD(serviceDeliveryLocation.Address) : null,
                    null, 
                    null, 
                    null
                ),
                null
            );

        }

        /// <summary>
        /// Create a language of communication
        /// </summary>
        private Everest.RMIM.UV.NE2008.COCT_MT030000UV04.LanguageCommunication CreateLanguage(PersonLanguage lang)
        {

            var retVal =new Everest.RMIM.UV.NE2008.COCT_MT030000UV04.LanguageCommunication(
                new CE<string>(lang.Language, m_configService.OidRegistrar.GetOid("ISO639-1").Oid)
            );
            
            // Abilities
            if(lang.Type.HasFlag(LanguageType.Spoken))
                retVal.ModeCode = lang.Type.HasFlag(LanguageType.Fluency) ? LanguageAbilityMode.ExpressedSpoken : LanguageAbilityMode.ReceivedSpoken;
            else if(lang.Type.HasFlag(LanguageType.Written))
                retVal.ModeCode = lang.Type.HasFlag(LanguageType.Fluency) ? LanguageAbilityMode.ExpressedWritten : LanguageAbilityMode.ReceivedWritten;

            // Fluency?
            if(lang.Type.HasFlag(LanguageType.Fluency))
            {
                retVal.ProficiencyLevelCode = new CE<LanguageAbilityProficiency>(LanguageAbilityProficiency.Excellent);
                retVal.PreferenceInd = true;
            }

            return retVal;
        }

        /// <summary>
        /// Employment
        /// </summary>
        private Everest.RMIM.UV.NE2008.PRPA_MT201303UV02.Employee CreateEmployment(Employment emp)
        {
            var retVal = new Everest.RMIM.UV.NE2008.PRPA_MT201303UV02.Employee();
            if(emp.Occupation != null)
                retVal.OccupationCode = CreateCD<String>(emp.Occupation);
            if(emp.EffectiveTime != null)
                retVal.EffectiveTime = CreateIVL(emp.EffectiveTime);

            retVal.StatusCode = ConvertRoleStatusCode(emp.Status);
            return retVal;
        }

        /// <summary>
        /// Convert role status
        /// </summary>
        private CS<RoleStatus> ConvertRoleStatusCode(StatusType statusType)
        {
            // Status
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
                    return new CS<RoleStatus>() { NullFlavor = NullFlavor.Unknown };
                default:
                    return new CS<RoleStatus>() { NullFlavor = NullFlavor.Other };
            }
        }

        /// <summary>
        /// Create citizenship
        /// </summary>
        private Everest.RMIM.UV.NE2008.PRPA_MT201303UV02.Citizen CreateCitizenship(Citizenship cit)
        {
            return new Everest.RMIM.UV.NE2008.PRPA_MT201303UV02.Citizen(
                null,
                cit.EffectiveTime != null ? CreateIVL(cit.EffectiveTime) : null,
                new Everest.RMIM.UV.NE2008.PRPA_MT201303UV02.Nation(
                    new CD<string>(cit.CountryCode, m_configService.OidRegistrar.GetOid("ISO3166-1").Oid),
                    new ON(EntityNameUse.Assigned, new ENXP[] { new ENXP(cit.CountryName) })
                )
            );
        }


        /// <summary>
        /// Create custodial data from either a RepositoryDevice or a HealthcareParticipant
        /// </summary>
        private Everest.RMIM.UV.NE2008.MFMI_MT700701UV01.Custodian CreateCustodian(RegistrationEvent registrationEvent, TargetConfiguration configuration)
        {


            ISystemConfigurationService sysConfig = this.Context.GetService(typeof(ISystemConfigurationService)) as ISystemConfigurationService;

            var subject = registrationEvent.FindComponent(SVC.Core.ComponentModel.HealthServiceRecordSiteRoleType.SubjectOf) as Person;
            var iiSet = new List<II>(CreateIISet(subject.AlternateIdentifiers));
            iiSet.RemoveAll(ii => !configuration.NotificationDomain.Exists(o => o.Domain.Equals(ii.Root)));

            var oidData = sysConfig.OidRegistrar.FindData(iiSet[0].Root);
            if(oidData == null)
                throw new InvalidOperationException("Cannot find notification settings for " + oidData);

            var retVal = new Everest.RMIM.UV.NE2008.MFMI_MT700701UV01.Custodian(
                new Everest.RMIM.UV.NE2008.COCT_MT090003UV01.AssignedEntity()
            );

            // Device
            retVal.AssignedEntity.SetAssignedPrincipalChoiceList(
                new Everest.RMIM.UV.NE2008.COCT_MT090303UV01.Device(
                    SET<II>.CreateSET(new II(oidData.Attributes.Find(o => o.Key == "CustodialDeviceId").Value ?? oidData.Oid)),
                    null,
                    oidData.Attributes.Find(o=>o.Key == "CustodialDeviceName").Value ?? oidData.Description
                )
            );
            retVal.AssignedEntity.Id = SET<II>.CreateSET(new II(oidData.Oid));

            return retVal;

        }

        #endregion
    }
}
