/**
 * Copyright 2012-2012 Mohawk College of Applied Arts and Technology
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
 * Date: 17-9-2012
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MARC.Everest.Connectors;
using MARC.HI.EHRS.CR.Core.ComponentModel;
using MARC.HI.EHRS.SVC.Core.Services;
using MARC.HI.EHRS.SVC.Core.ComponentModel.Components;
using MARC.HI.EHRS.SVC.Core.ComponentModel;
using MARC.Everest.DataTypes;
using MARC.Everest.RMIM.UV.NE2008.Vocabulary;
using System.ComponentModel;
using MARC.HI.EHRS.SVC.Core.DataTypes;
using MARC.Everest.RMIM.UV.NE2008.Interactions;

namespace MARC.HI.EHRS.CR.Messaging.Everest.MessageReceiver.UV
{
    /// <summary>
    /// Universal component utility
    /// </summary>
    public class UvComponentUtil : ComponentUtil
    {

        /// <summary>
        /// Create components for the registration event based on the control act process
        /// </summary>
        private RegistrationEvent CreateComponents<T, U>(MARC.Everest.RMIM.UV.NE2008.MFMI_MT700701UV01.ControlActProcess<T, U> controlActEvent, List<IResultDetail> dtls)
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

            if (controlActEvent.EffectiveTime != null && !controlActEvent.EffectiveTime.IsNull)
                changeSummary.EffectiveTime = CreateTimestamp(controlActEvent.EffectiveTime, dtls);

            if (controlActEvent.ReasonCode != null)
                foreach(var ce in controlActEvent.ReasonCode)
                    changeSummary.Add(new Reason()
                    {
                        ReasonType = CreateCodeValue<String>(ce, dtls)
                    }, Guid.NewGuid().ToString(), HealthServiceRecordSiteRoleType.ReasonFor, null);
            retVal.Add(changeSummary, "CHANGE", HealthServiceRecordSiteRoleType.ReasonFor | HealthServiceRecordSiteRoleType.OlderVersionOf, null);
            (changeSummary.Site as HealthServiceRecordSite).IsSymbolic = true; // this link adds no real value to the parent's data
            

            // author ( this is optional in IHE)
            if(controlActEvent.Subject == null || controlActEvent.Subject.Count != 1 || controlActEvent.Subject[0].RegistrationEvent == null || controlActEvent.Subject[0].RegistrationEvent.NullFlavor != null)
                ;
            else if (controlActEvent.Subject[0].RegistrationEvent.Author == null || controlActEvent.Subject[0].RegistrationEvent.Author.NullFlavor != null)
                dtls.Add(new ResultDetail(ResultDetailType.Warning, this.m_localeService.GetString("MSGE004"), null, null));
            else
            {
                var autOrPerf  = controlActEvent.Subject[0].RegistrationEvent.Author;
                HealthcareParticipant aut = null;

                if (autOrPerf.Time != null && !autOrPerf.Time.IsNull)
                {
                    var time = autOrPerf.Time.ToBoundIVL();
                    changeSummary.Timestamp  = retVal.Timestamp = (DateTime)(time.Value ?? time.Low);
                    if(controlActEvent.Subject[0].RegistrationEvent.EffectiveTime == null || controlActEvent.Subject[0].RegistrationEvent.EffectiveTime.IsNull || time.SemanticEquals(controlActEvent.Subject[0].RegistrationEvent.EffectiveTime.ToBoundIVL()) == false)
                        dtls.Add(new ValidationResultDetail(ResultDetailType.Error, m_localeService.GetString("MSGE051"), null, null));
                }
                
                // Assigned entity
                if(autOrPerf.AssignedEntity == null || autOrPerf.AssignedEntity.NullFlavor != null)
                    dtls.Add(new ResultDetail(ResultDetailType.Error, this.m_localeService.GetString("MSGE006"), null, null));
                else
                    aut = CreateParticipantComponent(autOrPerf.AssignedEntity, dtls);

                if (aut != null)
                {
                    changeSummary.Add(aut, "AUT", HealthServiceRecordSiteRoleType.AuthorOf, aut.AlternateIdentifiers);
                    retVal.Add(aut.Clone() as IComponent, "AUT".ToString(), HealthServiceRecordSiteRoleType.AuthorOf, aut.AlternateIdentifiers);

                }
                else
                    dtls.Add(new ResultDetail(ResultDetailType.Error, this.m_localeService.GetString("MSGE004"), null, null));
            }

            return retVal;
        }

        /// <summary>
        /// Create components for the registration event based on the control act process
        /// </summary>
        private RegistrationEvent CreateComponents<T>(MARC.Everest.RMIM.UV.NE2008.QUQI_MT021001UV01.ControlActProcess<T> controlActEvent, List<IResultDetail> dtls)
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

            if (controlActEvent.EffectiveTime != null && !controlActEvent.EffectiveTime.IsNull)
                changeSummary.EffectiveTime = CreateTimestamp(controlActEvent.EffectiveTime, dtls);

            if (controlActEvent.ReasonCode != null)
                foreach (var ce in controlActEvent.ReasonCode)
                    changeSummary.Add(new Reason()
                    {
                        ReasonType = CreateCodeValue<String>(ce, dtls)
                    }, Guid.NewGuid().ToString(), HealthServiceRecordSiteRoleType.ReasonFor, null);
            retVal.Add(changeSummary, "CHANGE", HealthServiceRecordSiteRoleType.ReasonFor | HealthServiceRecordSiteRoleType.OlderVersionOf, null);
            (changeSummary.Site as HealthServiceRecordSite).IsSymbolic = true; // this link adds no real value to the parent's data


            // author ( this is optional in IHE)
            if (controlActEvent.AuthorOrPerformer.Count != 0)
            {
                var autOrPerf = controlActEvent.AuthorOrPerformer[0];
                HealthServiceRecordComponent aut = null;

                if (autOrPerf.Time != null && !autOrPerf.Time.IsNull)
                {
                    var time = autOrPerf.Time.ToBoundIVL();
                    changeSummary.Timestamp = retVal.Timestamp = (DateTime)(time.Value ?? time.Low);
                }
                // Assigned entity
                if (autOrPerf.ParticipationChoice == null || autOrPerf.ParticipationChoice.NullFlavor != null)
                    dtls.Add(new ResultDetail(ResultDetailType.Error, this.m_localeService.GetString("MSGE006"), null, null));
                else
                    aut = CreateParticipantComponent(autOrPerf.ParticipationChoice, dtls);

                if (aut != null)
                {
                    changeSummary.Add(aut, "AUT", HealthServiceRecordSiteRoleType.AuthorOf, null);
                    retVal.Add(aut.Clone() as IComponent, "AUT".ToString(), HealthServiceRecordSiteRoleType.AuthorOf, null);

                }
                else
                    dtls.Add(new ResultDetail(ResultDetailType.Error, this.m_localeService.GetString("MSGE004"), null, null));
            }

            return retVal;
        }

        /// <summary>
        /// Create the participant component
        /// </summary>
        private HealthServiceRecordComponent CreateParticipantComponent(MARC.Everest.RMIM.UV.NE2008.MFMI_MT700701UV01.ParticipationChoice participationChoice, List<IResultDetail> dtls)
        {
            if (participationChoice is MARC.Everest.RMIM.UV.NE2008.COCT_MT090100UV01.AssignedPerson)
            {
                var psn = participationChoice as MARC.Everest.RMIM.UV.NE2008.COCT_MT090100UV01.AssignedPerson;
                HealthcareParticipant retval = new HealthcareParticipant()
                {
                    Classifier = HealthcareParticipant.HealthcareParticipantType.Person
                };

                // Identifiers
                if (psn.Id != null && !psn.Id.IsNull)
                    retval.AlternateIdentifiers = CreateDomainIdentifierList(psn.Id, dtls);
                else
                    dtls.Add(new RequiredElementMissingResultDetail(ResultDetailType.Error, m_localeService.GetString("MSGE02F"), null));
                return retval;
            }
            else if (participationChoice is MARC.Everest.RMIM.UV.NE2008.COCT_MT090300UV01.AssignedDevice)
            {
                var dev = participationChoice as MARC.Everest.RMIM.UV.NE2008.COCT_MT090300UV01.AssignedDevice;
                var retVal = new RepositoryDevice();

                if (dev.Id != null && !dev.Id.IsEmpty)
                    retVal.AlternateIdentifier = CreateDomainIdentifier(dev.Id[0], dtls);
                return retVal;
            }
            else
                dtls.Add(new NotSupportedChoiceResultDetail(ResultDetailType.Error, m_localeService.GetString("MSGE06B"), null, null));
            return null;
        }

        /// <summary>
        /// Create a person component
        /// </summary>
        private HealthcareParticipant CreateParticipantComponent(MARC.Everest.RMIM.UV.NE2008.COCT_MT090003UV01.AssignedEntity assignedPerson, List<IResultDetail> dtls)
        {
            HealthcareParticipant retval = new HealthcareParticipant() { Classifier = HealthcareParticipant.HealthcareParticipantType.Person };

            // Identifiers
            if (assignedPerson.Id == null || assignedPerson.Id.IsNull || assignedPerson.Id.IsEmpty)
                dtls.Add(new MandatoryElementMissingResultDetail(ResultDetailType.Error, this.m_localeService.GetString("MSGE02F"), null));
            else
                retval.AlternateIdentifiers.AddRange(CreateDomainIdentifierList(assignedPerson.Id, dtls));
            
            // Type
            if(assignedPerson.Code != null && !assignedPerson.Code.IsNull)
                retval.Type = CreateCodeValue(assignedPerson.Code, dtls);

            // Address
            if(assignedPerson.Addr != null && !assignedPerson.Addr.IsEmpty)
                retval.PrimaryAddress = CreateAddressSet(assignedPerson.Addr.Find(o=>o.Use.Contains(PostalAddressUse.WorkPlace)) ?? assignedPerson.Addr[0], dtls);

            // Telecom
            if(assignedPerson.Telecom != null && !assignedPerson.Telecom.IsEmpty)
                foreach(var tel in assignedPerson.Telecom)
                    retval.TelecomAddresses.Add(new SVC.Core.DataTypes.TelecommunicationsAddress() {
                        Value = tel.Value, 
                        Use = Util.ToWireFormat(tel.Use)
                    });

            // Assigned person
            if(assignedPerson.AssignedPrincipalChoiceList == null || assignedPerson.AssignedPrincipalChoiceList.NullFlavor != null)
                dtls.Add(new MandatoryElementMissingResultDetail(ResultDetailType.Error, this.m_localeService.GetString("MSGE030"), null));
            else
            {
                if(assignedPerson.AssignedPrincipalChoiceList is MARC.Everest.RMIM.UV.NE2008.COCT_MT090103UV01.Person)
                {
                    var psn = assignedPerson.AssignedPrincipalChoiceList as MARC.Everest.RMIM.UV.NE2008.COCT_MT090103UV01.Person;
                    retval.LegalName = psn.Name != null && !psn.Name.IsNull ? CreateNameSet(psn.Name.Find(o=>o.Use.Contains(EntityNameUse.Legal)) ?? psn.Name[0], dtls) : null;
                }
                else if(assignedPerson.AssignedPrincipalChoiceList is MARC.Everest.RMIM.UV.NE2008.COCT_MT090303UV01.Device)
                {
                    var dev = assignedPerson.AssignedPrincipalChoiceList as MARC.Everest.RMIM.UV.NE2008.COCT_MT090303UV01.Device;
                    retval.LegalName = dev.SoftwareName != null && !dev.SoftwareName.IsNull ? new NameSet() { Parts = new List<NamePart>() { new NamePart() { Value = dev.SoftwareName, Type = NamePart.NamePartType.Given }}} : null;
                    retval.Classifier = HealthcareParticipant.HealthcareParticipantType.Organization | HealthcareParticipant.HealthcareParticipantType.Person;
                }
                else
                    dtls.Add(new NotSupportedChoiceResultDetail(ResultDetailType.Error, m_localeService.GetString("MSGE051"), null));
            }

            return retval;
        }

        /// <summary>
        /// Create components for the message IHE ITI44
        /// </summary>
        internal RegistrationEvent CreateComponents(MARC.Everest.RMIM.UV.NE2008.MFMI_MT700701UV01.ControlActProcess<MARC.Everest.RMIM.UV.NE2008.PRPA_MT201301UV02.Patient, object> controlActProcess, List<IResultDetail> dtls)
        {
            ITerminologyService termSvc = Context.GetService(typeof(ITerminologyService)) as ITerminologyService;
            ISystemConfigurationService config = Context.GetService(typeof(ISystemConfigurationService)) as ISystemConfigurationService;

            // Create return value
            RegistrationEvent retVal = CreateComponents<MARC.Everest.RMIM.UV.NE2008.PRPA_MT201301UV02.Patient, object>(controlActProcess, dtls);

            // Very important, if there is more than one subject then we have a problem
            if (controlActProcess.Subject.Count != 1)
            {
                dtls.Add(new InsufficientRepetitionsResultDetail(ResultDetailType.Error, this.m_localeService.GetString("MSGE04F"), "//urn:hl7-org:v3#controlActProcess/urn:hl7-org:v3#subject"));
                return null;
            }

            var subject = controlActProcess.Subject[0].RegistrationEvent;

            retVal.EventClassifier = RegistrationEventType.Register;
            retVal.EventType = new CodeValue("REG");
            retVal.Status =subject.StatusCode == null || subject.StatusCode.IsNull ? StatusType.Active : ConvertStatusCode(subject.StatusCode, dtls);

            // Control act event code
            if (controlActProcess.Code != null && !controlActProcess.Code.IsNull && !controlActProcess.Code.Code.Equals(PRPA_IN201301UV02.GetTriggerEvent().Code))
            {
                dtls.Add(new ResultDetail(ResultDetailType.Error, this.m_localeService.GetString("MSGE00C"), null, null));
                return null;
            }

            if (retVal == null) return null;

            // Validate
            if (subject.NullFlavor != null)
                dtls.Add(new MandatoryElementMissingResultDetail(ResultDetailType.Error, this.m_localeService.GetString("MSGE003"), null));

            // Subject ID
            if(subject.Id != null && subject.Id.Count > 0 && subject.Id.FindAll(o=>!o.IsNull).Count > 0)
                retVal.Add(new ExtendedAttribute() {
                    PropertyPath = "Id", 
                    Value = CreateDomainIdentifierList(subject.Id, dtls), 
                    Name = "RegistrationEventAltId"
                });

            // Effective time of the registration event = authored time
            if(subject.EffectiveTime != null && !subject.EffectiveTime.IsNull)
            {
                var ivl = subject.EffectiveTime.ToBoundIVL();
                retVal.Timestamp  = (DateTime)(ivl.Value ?? ivl.Low);
                if(subject.Author == null || subject.Author.Time == null || subject.Author.Time.IsNull || subject.Author.Time.ToBoundIVL().SemanticEquals(ivl) == false)
                        dtls.Add(new ValidationResultDetail(ResultDetailType.Error, m_localeService.GetString("MSGE051"), null, null));

            }

            // Custodian of the record
            if (subject.Custodian == null || subject.Custodian.NullFlavor != null ||
                subject.Custodian.AssignedEntity == null || subject.Custodian.AssignedEntity.NullFlavor != null)
                dtls.Add(new MandatoryElementMissingResultDetail(ResultDetailType.Error, this.m_localeService.GetString("MSGE00B"), null));
            else
            {
                var cstdn = CreateRepositoryDevice(subject.Custodian.AssignedEntity, dtls);
                if (cstdn != null)
                    retVal.Add(CreateRepositoryDevice(subject.Custodian.AssignedEntity, dtls), "CST",
                        HealthServiceRecordSiteRoleType.PlaceOfRecord | HealthServiceRecordSiteRoleType.ResponsibleFor,
                            CreateDomainIdentifierList(subject.Custodian.AssignedEntity.Id, dtls)
                        );
            }

            // Create the subject
            Person subjectOf = CreatePersonSubject(subject.Subject1.registeredRole, dtls);

            // Replacement of?
            foreach (var rplc in subject.ReplacementOf)
                if (rplc.NullFlavor == null &&
                    rplc.PriorRegistration != null && rplc.PriorRegistration.NullFlavor == null)
                {

                    if (rplc.PriorRegistration.Subject1 == null || rplc.PriorRegistration.Subject1.NullFlavor != null ||
                        rplc.PriorRegistration.Subject1.PriorRegisteredRole == null || rplc.PriorRegistration.Subject1.PriorRegisteredRole.NullFlavor != null ||
                        rplc.PriorRegistration.Subject1.PriorRegisteredRole.Id == null || rplc.PriorRegistration.Subject1.PriorRegisteredRole.Id.IsEmpty ||
                        rplc.PriorRegistration.Subject1.PriorRegisteredRole.Id.IsNull)
                        dtls.Add(new MandatoryElementMissingResultDetail(ResultDetailType.Error, m_localeService.GetString("MSGE050"), "//urn:hl7-org:v3#priorRegisteredRole"));
                    else
                    {
                        var re = new PersonRegistrationRef()
                        {
                            AlternateIdentifiers = CreateDomainIdentifierList(rplc.PriorRegistration.Subject1.PriorRegisteredRole.Id, dtls)
                        };
                        subjectOf.Add(re, Guid.NewGuid().ToString(), HealthServiceRecordSiteRoleType.ReplacementOf, null);
                    }                            
                }


            // Effective time
            if (subjectOf.Status == StatusType.Active || subject.Subject1.registeredRole.EffectiveTime == null || subject.Subject1.registeredRole.EffectiveTime.IsNull)
            {
                dtls.Add(new RequiredElementMissingResultDetail(ResultDetailType.Warning, this.m_localeService.GetString("MSGW005"), null));
                retVal.EffectiveTime = CreateTimestamp(new IVL<TS>(DateTime.Now, new TS() { NullFlavor = NullFlavor.NotApplicable }), dtls);
            }
            else
                retVal.EffectiveTime = CreateTimestamp(subject.Subject1.registeredRole.EffectiveTime, dtls);

            // Add constructed subject
            retVal.Add(subjectOf, "SUBJ", HealthServiceRecordSiteRoleType.SubjectOf,
                subjectOf.AlternateIdentifiers);

            // Error?
            if (dtls.Exists(o => o.Type == ResultDetailType.Error))
                retVal = null;

            return retVal;
        }

        /// <summary>
        /// Create a person subject
        /// </summary>
        /// <param name="patient"></param>
        /// <param name="dtls"></param>
        /// <returns></returns>
        private Person CreatePersonSubject(MARC.Everest.RMIM.UV.NE2008.PRPA_MT201301UV02.Patient patient, List<IResultDetail> dtls)
        {
            ITerminologyService termSvc = Context.GetService(typeof(ITerminologyService)) as ITerminologyService;
            ISystemConfigurationService config = Context.GetService(typeof(ISystemConfigurationService)) as ISystemConfigurationService;

            var retVal = new Person();

            // Any alternate ids?
            if (patient.Id != null && !patient.Id.IsNull)
            {
                retVal.AlternateIdentifiers = new List<DomainIdentifier>();
                foreach (var ii in patient.Id)
                    if (ii != null && !ii.IsNull)
                        retVal.AlternateIdentifiers.Add(CreateDomainIdentifier(ii, dtls));

            }
             
            
            if(retVal.AlternateIdentifiers == null)
                dtls.Add(new MandatoryElementMissingResultDetail(ResultDetailType.Error, m_localeService.GetString("MSGE063"), null));
            else if(retVal.AlternateIdentifiers.Count == 0)
                dtls.Add(new InsufficientRepetitionsResultDetail(ResultDetailType.Error, m_localeService.GetString("MSGE064"), null));


            // Status code
            if (patient.StatusCode != null && !patient.StatusCode.IsNull)
                retVal.Status = ConvertStatusCode(patient.StatusCode, dtls);
            else
                retVal.Status = StatusType.Active;

            // Masking indicator
            if (patient.ConfidentialityCode != null && !patient.ConfidentialityCode.IsNull)
                foreach (var msk in patient.ConfidentialityCode)
                    retVal.Add(new MaskingIndicator()
                    {
                        MaskingCode = CreateCodeValue(msk, dtls)
                    }, Guid.NewGuid().ToString(), HealthServiceRecordSiteRoleType.FilterOf, null);

            // Identified entity check
            var ident = patient.PatientEntityChoiceSubject as MARC.Everest.RMIM.UV.NE2008.PRPA_MT201310UV02.Person;
            if (ident == null || ident.NullFlavor != null)
            {
                dtls.Add(new MandatoryElementMissingResultDetail(ResultDetailType.Error, this.m_localeService.GetString("MSGE012"), null));
                return null;
            }

            //if (ident.Id != null && !ident.Id.IsNull)
            //{
            //    if (subjectOf.AlternateIdentifiers == null)
            //        subjectOf.AlternateIdentifiers = new List<DomainIdentifier>();
            //    subjectOf.AlternateIdentifiers.AddRange(CreateDomainIdentifierList(ident.Id));
            //}

            // Names
            if (ident.Name != null)
            {
                retVal.Names = new List<SVC.Core.DataTypes.NameSet>(ident.Name.Count);
                foreach (var nam in ident.Name)
                    if (!nam.IsNull)
                        retVal.Names.Add(CreateNameSet(nam, dtls));
            }

            // Telecoms
            if (ident.Telecom != null)
            {
                retVal.TelecomAddresses = new List<SVC.Core.DataTypes.TelecommunicationsAddress>(ident.Telecom.Count);
                foreach (var tel in ident.Telecom)
                {
                    if (tel.IsNull) continue;

                    retVal.TelecomAddresses.Add(new SVC.Core.DataTypes.TelecommunicationsAddress()
                    {
                        Use = Util.ToWireFormat(tel.Use),
                        Value = tel.Value
                    });

                    // Store usable period as an extension as it is not storable here
                    if (tel.UseablePeriod != null && !tel.UseablePeriod.IsNull)
                    {
                        retVal.Add(new ExtendedAttribute()
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
                retVal.GenderCode = Util.ToWireFormat(ident.AdministrativeGenderCode);

            // Birth
            if (ident.BirthTime != null && !ident.BirthTime.IsNull)
                retVal.BirthTime = CreateTimestamp(ident.BirthTime, dtls);

            // Deceased
            if (ident.DeceasedInd != null && !ident.DeceasedInd.IsNull)
                dtls.Add(new NotImplementedElementResultDetail(ResultDetailType.Warning, "DeceasedInd", this.m_localeService.GetString("MSGW006"), null));
            if (ident.DeceasedTime != null && !ident.DeceasedTime.IsNull)
                retVal.DeceasedTime = CreateTimestamp(ident.DeceasedTime, dtls);

            // Multiple Birth
            if (ident.MultipleBirthInd != null && !ident.MultipleBirthInd.IsNull)
            {
                dtls.Add(new NotImplementedElementResultDetail(ResultDetailType.Warning, "MultipleBirthInd", this.m_localeService.GetString("MSGW007"), null));
                retVal.BirthOrder = -1;
            }
            if (ident.MultipleBirthOrderNumber != null && !ident.MultipleBirthOrderNumber.IsNull)
                retVal.BirthOrder = ident.MultipleBirthOrderNumber;

            // Address(es)
            if (ident.Addr != null)
            {
                retVal.Addresses = new List<SVC.Core.DataTypes.AddressSet>(ident.Addr.Count);
                foreach (var addr in ident.Addr)
                    if (!addr.IsNull)
                        retVal.Addresses.Add(CreateAddressSet(addr, dtls));
            }

            // As other identifiers
            if (ident.AsOtherIDs != null)
            {
                retVal.OtherIdentifiers = new List<KeyValuePair<CodeValue, DomainIdentifier>>(ident.AsOtherIDs.Count);
                foreach (var id in ident.AsOtherIDs)
                    if (id.NullFlavor == null)
                    {

                        // Ignore
                        if (id.Id == null || id.Id.IsNull || id.Id.IsEmpty)
                            continue;

                        // Other identifiers 
                        var priId = id.Id[0];
                        var myId = CreateDomainIdentifier(priId, dtls);
                        retVal.OtherIdentifiers.Add(new KeyValuePair<CodeValue, DomainIdentifier>(
                            null,
                            myId
                         ));

                        // Extra "other" identifiers are extensions
                        for (int i = 1; i < id.Id.Count; i++)
                            retVal.Add(new ExtendedAttribute()
                            {
                                PropertyPath = String.Format("OtherIdentifiers[{0}{1}]", priId.Root, priId.Extension),
                                Value = CreateDomainIdentifier(id.Id[i], dtls),
                                Name = "AssigningIdOrganizationExtraId"
                            });

                        // Extra scoping org data
                        if (id.ScopingOrganization != null && id.ScopingOrganization.NullFlavor == null)
                        {

                            // Other identifier assigning organization ext We do this extension like this so the patient record
                            // is compatible with the Canadian handler
                            if (id.ScopingOrganization.Id != null && !id.ScopingOrganization.Id.IsNull)
                                foreach (var othScopeId in id.ScopingOrganization.Id)
                                {
                                    // Validate the identifiers match
                                    if(!II.IsValidOidFlavor(othScopeId))
                                        dtls.Add(new FormalConstraintViolationResultDetail(ResultDetailType.Error, this.m_localeService.GetString("MSGE05A"), null, null));
                                    if (priId.Root != othScopeId.Root)
                                        dtls.Add(new FormalConstraintViolationResultDetail(ResultDetailType.Warning, this.m_localeService.GetString("MSGW015"), null, null));
                                    
                                    retVal.Add(new ExtendedAttribute()
                                    {
                                        PropertyPath = String.Format("OtherIdentifiers[{0}{1}]", priId.Root, priId.Extension),
                                        Value = CreateDomainIdentifier(othScopeId, dtls),
                                        Name = "AssigningIdOrganizationId"
                                    });
                                }
                            else
                                dtls.Add(new FormalConstraintViolationResultDetail(ResultDetailType.Error, this.m_localeService.GetString("MSGE05A"), null, null));

                            // Other identifier assigning organization name
                            if (id.ScopingOrganization.Name != null && !id.ScopingOrganization.Name.IsNull)
                            {
                                foreach (var othScopeName in id.ScopingOrganization.Name)
                                    retVal.Add(new ExtendedAttribute()
                                    {
                                        PropertyPath = String.Format("OtherIdentifiers[{0}{1}]", priId.Root, priId.Extension),
                                        Value = othScopeName.ToString(),
                                        Name = "AssigningIdOrganizationName"
                                    });

                                //if (String.IsNullOrEmpty(myId.AssigningAuthority))
                                //    myId.AssigningAuthority = id.ScopingOrganization.Name[0].ToString();
                                
                            }
                            if (id.ScopingOrganization.Code != null && !id.ScopingOrganization.Code.IsNull)
                                retVal.Add(new ExtendedAttribute()
                                {
                                    PropertyPath = String.Format("OtherIdentifiers[{0}{1}]", priId.Root, priId.Extension),
                                    Value = CreateCodeValue(id.ScopingOrganization.Code, dtls),
                                    Name = "AssigningIdOrganizationCode"
                                });

                        }
                    }
            }

            // Languages
            if (ident.LanguageCommunication != null)
            {
                retVal.Language = new List<PersonLanguage>(ident.LanguageCommunication.Count);
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
                        pl.Type = LanguageType.Fluency;
                    else
                        pl.Type = LanguageType.WrittenAndSpoken;

                    // Add
                    retVal.Language.Add(pl);
                }
            }

            // Personal relationship
            if (ident.PersonalRelationship != null)
                foreach (var psn in ident.PersonalRelationship)
                    if (psn.NullFlavor == null && psn.RelationshipHolder1 != null &&
                        psn.RelationshipHolder1.NullFlavor == null)
                        retVal.Add(CreatePersonalRelationship(psn, dtls), Guid.NewGuid().ToString(), HealthServiceRecordSiteRoleType.RepresentitiveOf, null);

            // VIP Code
            if (patient.VeryImportantPersonCode != null && !patient.VeryImportantPersonCode.IsNull)
                retVal.VipCode = CreateCodeValue(patient.VeryImportantPersonCode, dtls);

            // Birthplace
            if (ident.BirthPlace != null && ident.BirthPlace.NullFlavor == null &&
                ident.BirthPlace.Birthplace != null && ident.BirthPlace.Birthplace.NullFlavor == null)
                retVal.Add(CreateBirthplace(ident.BirthPlace.Birthplace, dtls),
                    "BRTH");

            // Race Codes
            if (ident.RaceCode != null && !ident.RaceCode.IsNull)
            {
                retVal.Race = new List<CodeValue>(ident.RaceCode.Count);
                foreach (var rc in ident.RaceCode)
                    retVal.Race.Add(CreateCodeValue(rc, dtls));
            }

            // Ethnicity Codes
            // Didn't actually have a place for this so this will be an extension
            if (ident.EthnicGroupCode != null && !ident.EthnicGroupCode.IsNull)
                foreach (var eth in ident.EthnicGroupCode)
                    retVal.Add(new ExtendedAttribute()
                    {
                        Name = "EthnicGroupCode",
                        PropertyPath = "",
                        Value = CreateCodeValue(eth, dtls)
                    });


            // Marital Status Code
            if (ident.MaritalStatusCode != null && !ident.MaritalStatusCode.IsNull)
                retVal.MaritalStatus = CreateCodeValue(ident.MaritalStatusCode, dtls);

            // Religion code
            if (ident.ReligiousAffiliationCode != null && !ident.ReligiousAffiliationCode.IsNull)
                retVal.ReligionCode = CreateCodeValue(ident.ReligiousAffiliationCode, dtls);

            // Citizenship Code
            if (ident.AsCitizen.Count > 0)
            {
                retVal.Citizenship = new List<Citizenship>(ident.AsCitizen.Count);
                foreach (var cit in ident.AsCitizen)
                {
                    if (cit.NullFlavor != null) continue;

                    Citizenship citizenship = new Citizenship(); // canonical 
                    if (cit.PoliticalNation != null && cit.PoliticalNation.NullFlavor == null &&
                        cit.PoliticalNation.Code != null && !cit.PoliticalNation.Code.IsNull)
                    {

                        // The internal canonical form specifies ISO3166 codes to be used for storage of nation codes
                        var iso3166code = CreateCodeValue(cit.PoliticalNation.Code, dtls);
                        if (iso3166code.CodeSystem != config.OidRegistrar.GetOid("ISO3166-1").Oid &&
                            iso3166code.CodeSystem != config.OidRegistrar.GetOid("ISO3166-2").Oid)
                            dtls.Add(new VocabularyIssueResultDetail(ResultDetailType.Error, this.m_localeService.GetString("MSGE057"), null));

                        // Translate the language code
                        if (iso3166code.CodeSystem == config.OidRegistrar.GetOid("ISO3166-2").Oid) // we need to translate
                            iso3166code = termSvc.Translate(iso3166code, config.OidRegistrar.GetOid("ISO3166-1").Oid);

                        if (iso3166code == null)
                            dtls.Add(new VocabularyIssueResultDetail(ResultDetailType.Error, this.m_localeService.GetString("MSGE058"), null, null));
                        else
                            citizenship.CountryCode = iso3166code.Code;

                        // Name of the country
                        if (cit.PoliticalNation.Name != null && !cit.PoliticalNation.Name.IsNull && cit.PoliticalNation.Name.Part.Count > 0)
                            citizenship.CountryName = cit.PoliticalNation.Name.Part[0].Value;

                    }
                    else
                        dtls.Add(new MandatoryElementMissingResultDetail(ResultDetailType.Error, m_localeService.GetString("MSGE056"), null));

                    // Get other details
                    // Effective time of the citizenship
                    if (cit.EffectiveTime != null && !cit.EffectiveTime.IsNull)
                        citizenship.EffectiveTime = CreateTimestamp(cit.EffectiveTime, dtls);

                    // Identifiers of the citizen in the role
                    if (cit.Id != null && !cit.Id.IsNull)
                        retVal.Add(new ExtendedAttribute()
                        {
                            Name = "CitizenshipIds",
                            PropertyPath = String.Format("Citizenship[{0}]", citizenship.CountryCode),
                            Value = CreateDomainIdentifierList(cit.Id, dtls)
                        });

                    retVal.Citizenship.Add(citizenship);
                }
            }

            // Employment Code
            if (ident.AsEmployee.Count > 0)
            {
                retVal.Employment = new List<Employment>(ident.AsEmployee.Count);
                foreach (var emp in ident.AsEmployee)
                {
                    if (emp.NullFlavor != null) continue;

                    Employment employment = new Employment();

                    // Occupation code
                    if (emp.OccupationCode != null && !emp.OccupationCode.IsNull)
                        employment.Occupation = CreateCodeValue(emp.OccupationCode, dtls);

                    // efft time
                    if (emp.EffectiveTime != null && !emp.EffectiveTime.IsNull)
                        employment.EffectiveTime = CreateTimestamp(emp.EffectiveTime, dtls);

                    // status
                    if (emp.StatusCode != null && !emp.StatusCode.IsNull)
                        employment.Status = ConvertStatusCode(Util.Convert<RoleStatus1>(Util.ToWireFormat(emp.StatusCode)), dtls);
                    else
                        employment.Status = StatusType.Active;

                    retVal.Employment.Add(employment);
                }
            }

            // Scoping org?
            if (patient.ProviderOrganization != null &&
                patient.ProviderOrganization.NullFlavor == null)
            {
                var scoper = CreateProviderOrganization(patient.ProviderOrganization, dtls);
                retVal.Add(scoper, "SCP", HealthServiceRecordSiteRoleType.PlaceOfEntry | HealthServiceRecordSiteRoleType.InformantTo, null);

            }

            return retVal;
        }

        /// <summary>
        /// Create a provider organization
        /// </summary>
        private IComponent CreateProviderOrganization(MARC.Everest.RMIM.UV.NE2008.COCT_MT150003UV03.Organization organization, List<IResultDetail> dtls)
        {
            HealthcareParticipant retVal = new HealthcareParticipant() { Classifier = HealthcareParticipant.HealthcareParticipantType.Organization };

            // Ensure that the scoping org id is correct
            if (organization.Id == null || organization.Id.IsNull)
                dtls.Add(new MandatoryElementMissingResultDetail(ResultDetailType.Error, this.m_localeService.GetString("MSGE05A"), null));
            else
            {
                retVal.AlternateIdentifiers = new List<DomainIdentifier>();
                foreach (var ii in organization.Id)
                {
                    if (II.IsValidOidFlavor(ii))
                        retVal.AlternateIdentifiers.Add(CreateDomainIdentifier(ii, dtls));
                    else
                        dtls.Add(new FormalConstraintViolationResultDetail(ResultDetailType.Error, this.m_localeService.GetString("MSGE05A"), null, null));
                }
            }

            // Name
            if (organization.Name != null && !organization.Name.IsNull)
                retVal.LegalName = CreateNameSet(organization.Name[0], dtls);

            // Type
            if (organization.Code != null && !organization.Code.IsNull)
                retVal.Type = CreateCodeValue(organization.Code, dtls);

            // Contact person
            if (organization.ContactParty != null)
            {
                // Add contact parties
                foreach (var ctp in organization.ContactParty)
                {
                    if (ctp == null || ctp.NullFlavor == null) continue;
                    HealthcareParticipant contactParty = new HealthcareParticipant()
                    {
                        Classifier = HealthcareParticipant.HealthcareParticipantType.Organization
                    };
    
                    // Identifier
                    if(ctp.Id != null)
                        contactParty.AlternateIdentifiers = CreateDomainIdentifierList(ctp.Id, dtls);

                    // Start by having the the contact party identified telecoms
                    if(ctp.Telecom != null)
                    {
                        contactParty.TelecomAddresses = new List<TelecommunicationsAddress>();
                        foreach(var telecom in ctp.Telecom)
                            contactParty.TelecomAddresses.Add(new TelecommunicationsAddress() {
                                Use = Util.ToWireFormat(telecom.Use),
                                Value = telecom.Value
                            });
                    }

                    // Address?
                    if(ctp.Addr != null)
                        contactParty.PrimaryAddress = CreateAddressSet(ctp.Addr.Find(o=>o.Use.Contains(PostalAddressUse.Direct)) ?? ctp.Addr[0], dtls);
                    
                    // Type
                    if(ctp.Code != null && !ctp.Code.IsNull)
                        contactParty.Type = CreateCodeValue(ctp.Code, dtls);

                    // Contact party?
                    if(ctp.ContactPerson != null && ctp.ContactPerson.NullFlavor == null)
                    {
                        contactParty.Classifier = HealthcareParticipant.HealthcareParticipantType.Person;
                        contactParty.LegalName = CreateNameSet(ctp.ContactPerson.Name[0], dtls);
                    }

                    // Add an contact party
                    retVal.Add(contactParty, Guid.NewGuid().ToString(), HealthServiceRecordSiteRoleType.RepresentitiveOf, null);
                }

            }

            return retVal;
        }
        
        /// <summary>
        /// Create a birthplace
        /// </summary>
        private ServiceDeliveryLocation CreateBirthplace(MARC.Everest.RMIM.UV.NE2008.COCT_MT710007UV.Place place, List<IResultDetail> dtls)
        {
            var retVal = new ServiceDeliveryLocation();

            // Place id
            if (place.Id != null && !place.Id.IsNull)
                retVal.AlternateIdentifiers = CreateDomainIdentifierList(place.Id, dtls);

            // Address of the place
            if (place.Addr != null && !place.Addr.IsNull)
                retVal.Address = CreateAddressSet(place.Addr, dtls);

            // Name of the place
            if (place.Name != null && !place.Name.IsNull && !place.Name.IsNull)
                retVal.Name = place.Name[0].ToString();

            // Place type
            if (place.Code != null && !place.Code.IsNull)
                retVal.LocationType = CreateCodeValue(place.Code, dtls);

            return retVal;
        }


        /// <summary>
        /// Convert an act status code
        /// </summary>
        private StatusType ConvertStatusCode(CS<ActStatus> statusCode,List<IResultDetail> dtls)
        {
            // Determine if a valid code was selected from the ActStatus domain
            if (statusCode.Code.IsAlternateCodeSpecified)
            {
                dtls.Add(new VocabularyIssueResultDetail(ResultDetailType.Error, m_localeService.GetString("MSGE053"), null, null));
                return StatusType.Unknown;
            }

            switch ((ActStatus)statusCode)
            {
                case ActStatus.Aborted:
                    return StatusType.Aborted;
                case ActStatus.Active:
                    return StatusType.Active;
                case ActStatus.Cancelled:
                    return StatusType.Cancelled;
                case ActStatus.Completed:
                    return StatusType.Completed;
                case ActStatus.New:
                    return StatusType.New;
                case ActStatus.Nullified:
                    return StatusType.Nullified;
                case ActStatus.Obsolete:
                    return StatusType.Obsolete;
                case ActStatus.Suspended:
                case ActStatus.Normal:
                case ActStatus.Held:
                    dtls.Add(new VocabularyIssueResultDetail(ResultDetailType.Error, m_localeService.GetString("MSGE054"), null, null));
                    return StatusType.Unknown;
                default:
                    dtls.Add(new VocabularyIssueResultDetail(ResultDetailType.Error, m_localeService.GetString("MSGE053"), null, null));
                    return StatusType.Unknown;
            }
        } 

        /// <summary>
        /// Create a repository device
        /// </summary>
        private IComponent CreateRepositoryDevice(MARC.Everest.RMIM.UV.NE2008.COCT_MT090003UV01.AssignedEntity assignedEntity, List<IResultDetail> dtls)
        {
            if (assignedEntity.AssignedPrincipalChoiceList is MARC.Everest.RMIM.UV.NE2008.COCT_MT090303UV01.Device)
            {
                var dev = assignedEntity.AssignedPrincipalChoiceList as MARC.Everest.RMIM.UV.NE2008.COCT_MT090303UV01.Device;
                // Validate
                if(dev.NullFlavor != null ||
                    dev.Id == null ||
                    dev.Id.IsNull ||
                    dev.Id.IsEmpty)
                    dtls.Add(new MandatoryElementMissingResultDetail(ResultDetailType.Error, m_localeService.GetString("MSGE00D"), null));

                // Create the repo dev
                var retVal = new RepositoryDevice()
                {
                    AlternateIdentifier = CreateDomainIdentifier(dev.Id[0], dtls),
                    Name = dev.SoftwareName
                };

                return retVal;
            }
            else if (assignedEntity.AssignedPrincipalChoiceList is MARC.Everest.RMIM.UV.NE2008.COCT_MT090203UV01.Organization)
            {
                // Create org
                var org = assignedEntity.AssignedPrincipalChoiceList as MARC.Everest.RMIM.UV.NE2008.COCT_MT090203UV01.Organization;

                if(assignedEntity.NullFlavor != null ||
                    assignedEntity.Id == null ||
                    assignedEntity.Id.IsNull ||
                    assignedEntity.Id.IsEmpty)
                    dtls.Add(new MandatoryElementMissingResultDetail(ResultDetailType.Error, m_localeService.GetString("MSGE00D"), null));

                HealthcareParticipant ptcpt = new HealthcareParticipant() { Classifier = HealthcareParticipant.HealthcareParticipantType.Organization };
                ptcpt.AlternateIdentifiers = CreateDomainIdentifierList(assignedEntity.Id, dtls);
                ptcpt.LegalName = org.Name != null && !org.Name.IsNull ? CreateNameSet(org.Name.Find(o => o.Use != null && o.Use.Contains(EntityNameUse.Legal)) ?? org.Name[0], dtls) : null;
                ptcpt.PrimaryAddress = assignedEntity.Addr != null && !assignedEntity.Addr.IsNull ? CreateAddressSet(assignedEntity.Addr.Find(o => o.Use.Contains(PostalAddressUse.Direct)) ?? assignedEntity.Addr[0], dtls) : null;

                // Telecom addresses
                if(assignedEntity.Telecom != null && assignedEntity.Telecom.IsNull)
                {
                    ptcpt.TelecomAddresses = new List<TelecommunicationsAddress>();
                    foreach(var tel in assignedEntity.Telecom)
                        ptcpt.TelecomAddresses.Add(new TelecommunicationsAddress() {
                            Value= tel.Value,
                            Use = Util.ToWireFormat(tel.Use)
                        });
                };

                return ptcpt;
            }
            else if (assignedEntity.AssignedPrincipalChoiceList == null)
            {
                return new RepositoryDevice()
                {
                    AlternateIdentifier = CreateDomainIdentifier(assignedEntity.Id[0], dtls)
                };
            }
            else
            {
                dtls.Add(new NotSupportedChoiceResultDetail(ResultDetailType.Error, m_localeService.GetString("MSGE055"), null, null));
                return null;
            }
        }

        /// <summary>
        /// Create a personal relationship
        /// </summary>
        private PersonalRelationship CreatePersonalRelationship(MARC.Everest.RMIM.UV.NE2008.PRPA_MT201303UV02.PersonalRelationship psn, List<IResultDetail> dtls)
        {
            var retVal = new PersonalRelationship();

            // Person identifier
            if (psn.Id != null && !psn.Id.IsNull)
                retVal.AlternateIdentifiers = CreateDomainIdentifierList(psn.Id, dtls);

            // type of relation
            if (psn.Code == null || psn.Code.IsNull)
                dtls.Add(new MandatoryElementMissingResultDetail(ResultDetailType.Error, this.m_localeService.GetString("MSGE02E"), null));
            else
                retVal.RelationshipKind = Util.ToWireFormat(psn.Code);
            
            // status code
            if (psn.StatusCode != null && !psn.StatusCode.IsNull)
                retVal.Status = ConvertStatusCode(Util.Convert<RoleStatus1>(Util.ToWireFormat(psn.StatusCode)), dtls);
            else
                retVal.Status = StatusType.Active;


            // effective time
            if (psn.EffectiveTime != null && !psn.EffectiveTime.IsNull)
                retVal.Add(new ExtendedAttribute()
                {
                    Value = CreateTimestamp(psn.EffectiveTime, dtls),
                    Name = "EffectiveTime",
                    PropertyPath = ""
                });

            // Relationship holder
            if (psn.RelationshipHolder1 is MARC.Everest.RMIM.UV.NE2008.COCT_MT030007UV.Person)
            {
                var rh = psn.RelationshipHolder1 as MARC.Everest.RMIM.UV.NE2008.COCT_MT030007UV.Person;

                if (rh.AdministrativeGenderCode != null && !rh.AdministrativeGenderCode.IsNull)
                    retVal.GenderCode = Util.ToWireFormat(rh.AdministrativeGenderCode);
                if (rh.BirthTime != null && !rh.BirthTime.IsNull)
                    retVal.BirthTime = CreateTimestamp(rh.BirthTime, dtls);
                if (rh.Id != null && rh.Id.IsNull)
                {
                    if (retVal.AlternateIdentifiers == null)
                        retVal.AlternateIdentifiers = new List<DomainIdentifier>();
                    retVal.AlternateIdentifiers.AddRange(CreateDomainIdentifierList(rh.Id, dtls));
                }
                if (rh.Name != null && !rh.Name.IsNull)
                    retVal.LegalName = CreateNameSet(rh.Name.Find(o => o.Use != null && o.Use.Contains(EntityNameUse.Legal)) ?? rh.Name[0], dtls);
            }
            else
                dtls.Add(new NotSupportedChoiceResultDetail(ResultDetailType.Error, this.m_localeService.GetString("MSGE0059"), null));
            
            // Person address
            if (psn.Addr != null && !psn.Addr.IsNull)
                retVal.PerminantAddress = CreateAddressSet(psn.Addr.Find(o => o.Use.Contains(PostalAddressUse.HomeAddress)) ?? psn.Addr[0], dtls);
            // Telecom
            if (psn.Telecom != null && !psn.Telecom.IsNull)
                foreach (var tel in psn.Telecom)
                    retVal.TelecomAddresses.Add(new TelecommunicationsAddress()
                    {
                        Use = Util.ToWireFormat(tel.Use),
                        Value = tel.Value
                    });

            return retVal;
        }

        /// <summary>
        /// Convert status code
        /// </summary>
        private StatusType ConvertStatusCode(CS<RoleStatus1> status, List<IResultDetail> dtls)
        {
            if (status.Code.IsAlternateCodeSpecified)
            {
                dtls.Add(new VocabularyIssueResultDetail(ResultDetailType.Error, m_localeService.GetString("MSGE010"), null, null));
                return StatusType.Unknown;
            }

            // Status
            switch ((RoleStatus1)status)
            {
                case RoleStatus1.Active:
                    return StatusType.Active;
                case RoleStatus1.Cancelled:
                    return StatusType.Cancelled;
                case RoleStatus1.Nullified:
                    return StatusType.Nullified;
                case RoleStatus1.Pending:
                    return StatusType.New;
                case RoleStatus1.Suspended:
                    return StatusType.Cancelled | StatusType.Active;
                case RoleStatus1.Terminated:
                    return StatusType.Cancelled | StatusType.Obsolete;
                case RoleStatus1.Normal:
                    dtls.Add(new VocabularyIssueResultDetail(ResultDetailType.Error, m_localeService.GetString("MSGE011"), null, null));
                    return StatusType.Unknown;
                default:
                    dtls.Add(new VocabularyIssueResultDetail(ResultDetailType.Error, m_localeService.GetString("MSGE010"), null, null));
                    return StatusType.Unknown;
            }
        }

        /// <summary>
        /// Create components for the update message
        /// </summary>
        internal RegistrationEvent CreateComponents(MARC.Everest.RMIM.UV.NE2008.MFMI_MT700701UV01.ControlActProcess<MARC.Everest.RMIM.UV.NE2008.PRPA_MT201302UV02.Patient, object> controlActProcess, List<IResultDetail> dtls)
        {
            ITerminologyService termSvc = Context.GetService(typeof(ITerminologyService)) as ITerminologyService;
            ISystemConfigurationService config = Context.GetService(typeof(ISystemConfigurationService)) as ISystemConfigurationService;

            // Create return value
            RegistrationEvent retVal = CreateComponents<MARC.Everest.RMIM.UV.NE2008.PRPA_MT201302UV02.Patient, object>(controlActProcess, dtls);

            // Very important, if there is more than one subject then we have a problem
            if (controlActProcess.Subject.Count != 1)
            {
                dtls.Add(new InsufficientRepetitionsResultDetail(ResultDetailType.Error, this.m_localeService.GetString("MSGE04F"), "//urn:hl7-org:v3#controlActProcess/urn:hl7-org:v3#subject"));
                return null;
            }

            var subject = controlActProcess.Subject[0].RegistrationEvent;

            retVal.EventClassifier = RegistrationEventType.Register;
            retVal.EventType = new CodeValue("REG");
            retVal.Status = subject.StatusCode == null || subject.StatusCode.IsNull ? StatusType.Active : ConvertStatusCode(subject.StatusCode, dtls);

            // Control act event code
            if (controlActProcess.Code != null && !controlActProcess.Code.IsNull && !controlActProcess.Code.Code.Equals(PRPA_IN201302UV02.GetTriggerEvent().Code))
            {
                dtls.Add(new ResultDetail(ResultDetailType.Error, this.m_localeService.GetString("MSGE00C"), null, null));
                return null;
            }

            if (retVal == null) return null;

            // Validate
            if (subject.NullFlavor != null)
                dtls.Add(new MandatoryElementMissingResultDetail(ResultDetailType.Error, this.m_localeService.GetString("MSGE003"), null));

            // Subject ID
            if (subject.Id != null && subject.Id.Count > 0 && subject.Id.FindAll(o => !o.IsNull).Count > 0)
                retVal.Add(new ExtendedAttribute()
                {
                    PropertyPath = "Id",
                    Value = CreateDomainIdentifierList(subject.Id, dtls),
                    Name = "RegistrationEventAltId"
                });

            // Effective time of the registration event = authored time
            if (subject.EffectiveTime != null && !subject.EffectiveTime.IsNull)
            {
                var ivl = subject.EffectiveTime.ToBoundIVL();
                retVal.Timestamp = (DateTime)(ivl.Value ?? ivl.Low);
                if (subject.Author == null || subject.Author.Time == null || subject.Author.Time.IsNull || subject.Author.Time.ToBoundIVL().SemanticEquals(ivl) == false)
                    dtls.Add(new ValidationResultDetail(ResultDetailType.Error, m_localeService.GetString("MSGE051"), null, null));

            }

            // Custodian of the record
            if (subject.Custodian == null || subject.Custodian.NullFlavor != null ||
                subject.Custodian.AssignedEntity == null || subject.Custodian.AssignedEntity.NullFlavor != null)
                dtls.Add(new MandatoryElementMissingResultDetail(ResultDetailType.Error, this.m_localeService.GetString("MSGE00B"), null));
            else
            {
                var cstdn = CreateRepositoryDevice(subject.Custodian.AssignedEntity, dtls);
                if (cstdn != null)
                    retVal.Add(CreateRepositoryDevice(subject.Custodian.AssignedEntity, dtls), "CST",
                        HealthServiceRecordSiteRoleType.PlaceOfRecord | HealthServiceRecordSiteRoleType.ResponsibleFor,
                            CreateDomainIdentifierList(subject.Custodian.AssignedEntity.Id, dtls)
                        );
            }

            // Create the subject
            var regRole = subject.Subject1.registeredRole;
            Person subjectOf = CreatePersonSubject(new MARC.Everest.RMIM.UV.NE2008.PRPA_MT201301UV02.Patient(
                    regRole.Id,
                    regRole.Addr,
                    regRole.Telecom,
                    regRole.EffectiveTime,
                    regRole.ConfidentialityCode,
                    regRole.VeryImportantPersonCode,
                    regRole.PatientEntityChoiceSubject,
                    regRole.ProviderOrganization,
                    null,
                    null
                )
                {
                    StatusCode = Util.Convert<RoleStatus1>(Util.ToWireFormat(regRole.StatusCode)),
                    SubjectOf = regRole.SubjectOf,
                    CoveredPartyOf = regRole.CoveredPartyOf
                }, dtls);
            
            // Replacement of?
            foreach (var rplc in subject.ReplacementOf)
                if (rplc.NullFlavor == null &&
                    rplc.PriorRegistration != null && rplc.PriorRegistration.NullFlavor == null)
                {

                    if (rplc.PriorRegistration.Subject1 == null || rplc.PriorRegistration.Subject1.NullFlavor != null ||
                        rplc.PriorRegistration.Subject1.PriorRegisteredRole == null || rplc.PriorRegistration.Subject1.PriorRegisteredRole.NullFlavor != null ||
                        rplc.PriorRegistration.Subject1.PriorRegisteredRole.Id == null || rplc.PriorRegistration.Subject1.PriorRegisteredRole.Id.IsEmpty ||
                        rplc.PriorRegistration.Subject1.PriorRegisteredRole.Id.IsNull)
                        dtls.Add(new MandatoryElementMissingResultDetail(ResultDetailType.Error, m_localeService.GetString("MSGE050"), "//urn:hl7-org:v3#priorRegisteredRole"));
                    else
                    {
                        var re = new PersonRegistrationRef()
                           {
                               AlternateIdentifiers = CreateDomainIdentifierList(rplc.PriorRegistration.Subject1.PriorRegisteredRole.Id, dtls)
                           };
                        subjectOf.Add(re, Guid.NewGuid().ToString(), HealthServiceRecordSiteRoleType.ReplacementOf, null);
                    }

                }

            
            // Add constructed subject
            retVal.Add(subjectOf, "SUBJ", HealthServiceRecordSiteRoleType.SubjectOf,
                subjectOf.AlternateIdentifiers);

            // Error?
            if (dtls.Exists(o => o.Type == ResultDetailType.Error))
                retVal = null;

            return retVal;
        }

        /// <summary>
        /// Handle patient duplicates resolved
        /// </summary>
        internal RegistrationEvent CreateComponents(MARC.Everest.RMIM.UV.NE2008.MFMI_MT700701UV01.ControlActProcess<MARC.Everest.RMIM.UV.NE2008.PRPA_MT201303UV02.Patient, object> controlActProcess, List<IResultDetail> dtls)
        {
            ISystemConfigurationService config = Context.GetService(typeof(ISystemConfigurationService)) as ISystemConfigurationService;

            // Create return value
            RegistrationEvent retVal = CreateComponents<MARC.Everest.RMIM.UV.NE2008.PRPA_MT201303UV02.Patient, object>(controlActProcess, dtls);

            // Very important, if there is more than one subject then we have a problem
            if (controlActProcess.Subject.Count != 1)
            {
                dtls.Add(new InsufficientRepetitionsResultDetail(ResultDetailType.Error, this.m_localeService.GetString("MSGE04F"), "//urn:hl7-org:v3#controlActProcess/urn:hl7-org:v3#subject"));
                return null;
            }

            var subject = controlActProcess.Subject[0].RegistrationEvent;

            retVal.EventClassifier = RegistrationEventType.Register;
            retVal.EventType = new CodeValue("REG");
            retVal.Status = subject.StatusCode == null || subject.StatusCode.IsNull ? StatusType.Active : ConvertStatusCode(subject.StatusCode, dtls);

            if (retVal == null) return null;

            // Validate
            if (subject.NullFlavor != null)
                dtls.Add(new MandatoryElementMissingResultDetail(ResultDetailType.Error, this.m_localeService.GetString("MSGE003"), null));

            // Subject ID
            if (subject.Id != null && subject.Id.Count > 0 && subject.Id.FindAll(o => !o.IsNull).Count > 0)
                retVal.Add(new ExtendedAttribute()
                {
                    PropertyPath = "Id",
                    Value = CreateDomainIdentifierList(subject.Id, dtls),
                    Name = "RegistrationEventAltId"
                });

            // Effective time of the registration event = authored time
            if (subject.EffectiveTime != null && !subject.EffectiveTime.IsNull)
            {
                var ivl = subject.EffectiveTime.ToBoundIVL();
                retVal.Timestamp = (DateTime)(ivl.Value ?? ivl.Low);
                if (subject.Author == null || subject.Author.Time == null || subject.Author.Time.IsNull || subject.Author.Time.ToBoundIVL().SemanticEquals(ivl) == false)
                    dtls.Add(new ValidationResultDetail(ResultDetailType.Error, m_localeService.GetString("MSGE051"), null, null));

            }

            // Custodian of the record
            if (subject.Custodian == null || subject.Custodian.NullFlavor != null ||
                subject.Custodian.AssignedEntity == null || subject.Custodian.AssignedEntity.NullFlavor != null)
                dtls.Add(new MandatoryElementMissingResultDetail(ResultDetailType.Error, this.m_localeService.GetString("MSGE00B"), null));
            else
            {
                var cstdn = CreateRepositoryDevice(subject.Custodian.AssignedEntity, dtls);
                if (cstdn != null)
                    retVal.Add(CreateRepositoryDevice(subject.Custodian.AssignedEntity, dtls), "CST",
                        HealthServiceRecordSiteRoleType.PlaceOfRecord | HealthServiceRecordSiteRoleType.ResponsibleFor,
                            CreateDomainIdentifierList(subject.Custodian.AssignedEntity.Id, dtls)
                        );
            }

            // Create the subject
            var patient = subject.Subject1.registeredRole;
            Person subjectOf = new Person();

            // First, ensure that we have an identifier
            // Any alternate ids?
            if (patient.Id != null && !patient.Id.IsNull)
            {
                subjectOf.AlternateIdentifiers = new List<DomainIdentifier>();
                foreach (var ii in patient.Id)
                    if (ii != null && !ii.IsNull)
                        subjectOf.AlternateIdentifiers.Add(CreateDomainIdentifier(ii, dtls));

            }
            if(subjectOf.AlternateIdentifiers == null)
                dtls.Add(new MandatoryElementMissingResultDetail(ResultDetailType.Error, m_localeService.GetString("MSGE063"), null));
            else if (subjectOf.AlternateIdentifiers.Count == 0)
                dtls.Add(new InsufficientRepetitionsResultDetail(ResultDetailType.Error, m_localeService.GetString("MSGE064"), null));

            // STatus code IHE rule this must be active
            if (patient.StatusCode == null || patient.StatusCode.IsNull)
                subjectOf.Status = StatusType.Active;
            else if ((RoleStatus)patient.StatusCode != RoleStatus.Active)
                dtls.Add(new FixedValueMisMatchedResultDetail(Util.ToWireFormat(patient.StatusCode), "Active", true, null));
            subjectOf.Status = StatusType.Active;

            // VAlidate patient
            var person = patient.PatientEntityChoiceSubject as MARC.Everest.RMIM.UV.NE2008.PRPA_MT201310UV02.Person;
            if (person == null || person.NullFlavor != null )
                dtls.Add(new MandatoryElementMissingResultDetail(ResultDetailType.Error, m_localeService.GetString("MSGE065"), null));

            // Name of the person .. for validation only
            if (person.Name != null && !person.Name.IsNull)
            {
                subjectOf.Names = new List<NameSet>();
                foreach (var nam in person.Name)
                    if (nam != null && !nam.IsNull)
                        subjectOf.Names.Add(CreateNameSet(nam, dtls));
            }
            if (subjectOf.Names == null)
                dtls.Add(new MandatoryElementMissingResultDetail(ResultDetailType.Error, m_localeService.GetString("MSGE02A"), null));
            else if (subjectOf.Names.Count == 0)
                dtls.Add(new InsufficientRepetitionsResultDetail(ResultDetailType.Error, m_localeService.GetString("MSGE02A"), null));

            // Scoping org?
            if (patient.ProviderOrganization != null &&
                patient.ProviderOrganization.NullFlavor == null)
            {
                var scoper = CreateProviderOrganization(patient.ProviderOrganization, dtls);
                subjectOf.Add(scoper, "SCP", HealthServiceRecordSiteRoleType.PlaceOfEntry | HealthServiceRecordSiteRoleType.InformantTo, null);

            }
            
            // Replacement of?
            if (subject.ReplacementOf == null || subject.ReplacementOf.Count == 0)
                dtls.Add(new InsufficientRepetitionsResultDetail(ResultDetailType.Error, m_localeService.GetString("MSGE066"), null));
            else
                foreach (var rplc in subject.ReplacementOf)
                    if (rplc.NullFlavor == null &&
                        rplc.PriorRegistration != null && rplc.PriorRegistration.NullFlavor == null &&
                        rplc.PriorRegistration.StatusCode == null || rplc.PriorRegistration.StatusCode == ActStatus.Obsolete)
                    {

                        if (rplc.PriorRegistration.Subject1 == null || rplc.PriorRegistration.Subject1.NullFlavor != null ||
                            rplc.PriorRegistration.Subject1.PriorRegisteredRole == null || rplc.PriorRegistration.Subject1.PriorRegisteredRole.NullFlavor != null)
                        {
                            // HACK: This should be an error according to the IHE ITI-TF 2b specification
                            dtls.Add(new MandatoryElementMissingResultDetail(ResultDetailType.Warning, m_localeService.GetString("MSGW019"), "//urn:hl7-org:v3#priorRegistration"));
                            if (rplc.PriorRegistration.Id != null && !rplc.PriorRegistration.Id.IsNull)
                            {
                                var re = new PersonRegistrationRef()
                                {
                                    AlternateIdentifiers = CreateDomainIdentifierList(rplc.PriorRegistration.Id, dtls)
                                };
                                subjectOf.Add(re, Guid.NewGuid().ToString(), HealthServiceRecordSiteRoleType.ReplacementOf, null);
                            }
                            else
                                dtls.Add(new MandatoryElementMissingResultDetail(ResultDetailType.Error, m_localeService.GetString("MSGE068"), "//urn:hl7-org:v3#priorRegistration/urn:hl7-org:v3#id"));
                        }
                        else if (rplc.PriorRegistration.Subject1.PriorRegisteredRole.Id == null || rplc.PriorRegistration.Subject1.PriorRegisteredRole.Id.IsEmpty ||
                            rplc.PriorRegistration.Subject1.PriorRegisteredRole.Id.IsNull || rplc.PriorRegistration.Subject1.PriorRegisteredRole.Id.Count > 1)
                            dtls.Add(new InsufficientRepetitionsResultDetail(ResultDetailType.Error, m_localeService.GetString("MSGE069"), "//urn:hl7-org:v3#priorRegisteredRole"));
                        else
                        {
                            var re = new PersonRegistrationRef()
                            {
                                AlternateIdentifiers = CreateDomainIdentifierList(rplc.PriorRegistration.Subject1.PriorRegisteredRole.Id, dtls)
                            };
                            subjectOf.Add(re, Guid.NewGuid().ToString(), HealthServiceRecordSiteRoleType.ReplacementOf, null);
                            //(re.Site as HealthServiceRecordSite).IsSymbolic = true;
                        }

                    }
                    else
                        dtls.Add(new MandatoryElementMissingResultDetail(ResultDetailType.Error, m_localeService.GetString("MSGE067"), null));

                       
            // Add constructed subject
            retVal.Add(subjectOf, "SUBJ", HealthServiceRecordSiteRoleType.SubjectOf,
                subjectOf.AlternateIdentifiers);

            // Error?
            if (dtls.Exists(o => o.Type == ResultDetailType.Error))
                retVal = null;

            return retVal;
        }

        /// <summary>
        /// Create query match paramters for the get identifiers query
        /// </summary>
        internal RegistrationEvent CreateQueryMatch(MARC.Everest.RMIM.UV.NE2008.QUQI_MT021001UV01.ControlActProcess<MARC.Everest.RMIM.UV.NE2008.PRPA_MT201307UV02.QueryByParameter> controlActProcess, List<IResultDetail> dtls, ref List<DomainIdentifier> ids)
        {
            ITerminologyService termSvc = Context.GetService(typeof(ITerminologyService)) as ITerminologyService;

            // Details about the query
            if (!controlActProcess.Code.Code.Equals(PRPA_IN201309UV02.GetTriggerEvent().Code))
            {
                dtls.Add(new ResultDetail(ResultDetailType.Error, this.m_localeService.GetString("MSGE00C"), null, null));
                return null;
            }

            // REturn value
            RegistrationEvent retVal = CreateComponents<MARC.Everest.RMIM.UV.NE2008.PRPA_MT201307UV02.QueryByParameter>(controlActProcess, dtls);

            // Filter
            RegistrationEvent filter = new RegistrationEvent();
            retVal.Add(filter, "QRY", HealthServiceRecordSiteRoleType.FilterOf, null);

            // Parameter list validation
            var parameterList = controlActProcess.queryByParameter.ParameterList;
            if (parameterList == null || parameterList.NullFlavor != null)
            {
                dtls.Add(new MandatoryElementMissingResultDetail(ResultDetailType.Error, this.m_localeService.GetString("MSGE04E"), null));
                parameterList = new MARC.Everest.RMIM.UV.NE2008.PRPA_MT201307UV02.ParameterList();
            }

            // Discrete record identifiers
            ids = new List<DomainIdentifier>(100);

            // Create the actual query
            Person filterPerson = new Person();

            // Alternate identifiers
            filterPerson.AlternateIdentifiers = new List<DomainIdentifier>();
            if (parameterList.PatientIdentifier == null ||
                    parameterList.PatientIdentifier.Count != 1)
                dtls.Add(new FormalConstraintViolationResultDetail(ResultDetailType.Error, m_localeService.GetString("MSGE06C"), null, null));
            else if(parameterList.PatientIdentifier[0].Value == null ||
                    parameterList.PatientIdentifier[0].Value.IsNull ||
                    parameterList.PatientIdentifier[0].Value.Count != 1)
                dtls.Add(new FormalConstraintViolationResultDetail(ResultDetailType.Error, m_localeService.GetString("MSGE06D"), null, null));
            else
                filterPerson.AlternateIdentifiers.Add(CreateDomainIdentifier(parameterList.PatientIdentifier[0].Value[0], dtls));

            // Target domains
            foreach(var id in parameterList.DataSource)
                ids.Add(CreateDomainIdentifier(id.Value[0], dtls));

            // Filter
            filter.Add(filterPerson, "SUBJ", HealthServiceRecordSiteRoleType.SubjectOf, null);
            // Determine if errors exist that prevent the processing of this message
            if (dtls.Count(o => o.Type == ResultDetailType.Error) > 0)
                return null;

            return retVal;
        }

        /// <summary>
        /// Create the query match parameters
        /// </summary>
        internal RegistrationEvent CreateQueryMatch(MARC.Everest.RMIM.UV.NE2008.QUQI_MT021001UV01.ControlActProcess<MARC.Everest.RMIM.UV.NE2008.PRPA_MT201306UV02.QueryByParameter> controlActProcess, List<IResultDetail> dtls, ref List<DomainIdentifier> ids)
        {
            ITerminologyService termSvc = Context.GetService(typeof(ITerminologyService)) as ITerminologyService;

            // Details about the query
            if (!controlActProcess.Code.Code.Equals(PRPA_IN201305UV02.GetTriggerEvent().Code))
            {
                dtls.Add(new ResultDetail(ResultDetailType.Error, this.m_localeService.GetString("MSGE00C"), null, null));
                return null;
            }

            // REturn value
            RegistrationEvent retVal = CreateComponents<MARC.Everest.RMIM.UV.NE2008.PRPA_MT201306UV02.QueryByParameter>(controlActProcess, dtls);

            // Filter
            RegistrationEvent filter = new RegistrationEvent();
            retVal.Add(filter, "QRY", HealthServiceRecordSiteRoleType.FilterOf, null);

            // Parameter list validation
            var qbp = controlActProcess.queryByParameter;
            if (qbp == null || qbp.NullFlavor != null)
            {
                dtls.Add(new MandatoryElementMissingResultDetail(ResultDetailType.Error, this.m_localeService.GetString("MSGE04E"), null));
                qbp = new MARC.Everest.RMIM.UV.NE2008.PRPA_MT201306UV02.QueryByParameter();
            }

            var parameterList = qbp.ParameterList;
            if (parameterList == null || parameterList.NullFlavor != null)
            {
                dtls.Add(new MandatoryElementMissingResultDetail(ResultDetailType.Error, this.m_localeService.GetString("MSGE04E"), null));
                parameterList = new MARC.Everest.RMIM.UV.NE2008.PRPA_MT201306UV02.ParameterList();
            }

            // Discrete record identifiers
            ids = new List<DomainIdentifier>(100);

            // Create the actual query
            Person filterPerson = new Person();

            // Alternate identifiers
            filterPerson.AlternateIdentifiers = new List<DomainIdentifier>();
            
            // Administrative gender
            foreach (var gender in parameterList.LivingSubjectAdministrativeGender)
            {
                if (gender.NullFlavor == null && gender.Value != null && gender.Value.Count == 1)
                    filterPerson.GenderCode = Util.ToWireFormat(gender.Value[0].Code);
                else
                    dtls.Add(new InsufficientRepetitionsResultDetail(ResultDetailType.Error, this.m_localeService.GetString("MSGE073"), "//urn:hl7-org:v3#controlActProcess/urn:hl7-org:v3#queryByParmaeter/urn:hl7-org:v3#parameterList/urn:hl7-org:v3#livingSubjectAdministrativeGender"));
                break;
            }

            // Living Subject Birth Time
            foreach (var birth in parameterList.LivingSubjectBirthTime)
            {
                if (birth.NullFlavor == null && birth.Value != null && birth.Value.Count == 1)
                    filterPerson.BirthTime = CreateTimestamp(birth.Value[0].Value, dtls);
                else
                    dtls.Add(new InsufficientRepetitionsResultDetail(ResultDetailType.Error, this.m_localeService.GetString("MSGE073"), "//urn:hl7-org:v3#controlActProcess/urn:hl7-org:v3#queryByParmaeter/urn:hl7-org:v3#parameterList/urn:hl7-org:v3#livingSubjectBirthTime"));
                break;
            }

            // Living Subject Id
            foreach (var id in parameterList.LivingSubjectId)
            {
                if (filterPerson.AlternateIdentifiers == null) filterPerson.AlternateIdentifiers = new List<DomainIdentifier>();

                if (id.NullFlavor == null && id.Value != null && id.Value.Count == 1)
                    filterPerson.AlternateIdentifiers.Add(CreateDomainIdentifier(id.Value[0], dtls));
                else
                    dtls.Add(new InsufficientRepetitionsResultDetail(ResultDetailType.Error, this.m_localeService.GetString("MSGE073"), "//urn:hl7-org:v3#controlActProcess/urn:hl7-org:v3#queryByParmaeter/urn:hl7-org:v3#parameterList/urn:hl7-org:v3#livingSubjectId"));
            }

            // Living Subject Name
            foreach (var name in parameterList.LivingSubjectName)
            {
                if (filterPerson.Names == null) filterPerson.Names = new List<NameSet>();
                if (name.NullFlavor == null && name.Value != null && name.Value.Count == 1)
                    filterPerson.Names.Add(CreateNameSet(name.Value[0], dtls));
                else
                    dtls.Add(new InsufficientRepetitionsResultDetail(ResultDetailType.Error, this.m_localeService.GetString("MSGE073"), "//urn:hl7-org:v3#controlActProcess/urn:hl7-org:v3#queryByParmaeter/urn:hl7-org:v3#parameterList/urn:hl7-org:v3#livingSubjectName"));
            }

            // Living Subject Address
            foreach (var addr in parameterList.PatientAddress)
            {
                if (addr.NullFlavor == null && addr.Value != null && addr.Value.Count == 1)
                    filterPerson.Addresses = new List<AddressSet>() { CreateAddressSet(addr.Value[0], dtls) };
                else
                    dtls.Add(new InsufficientRepetitionsResultDetail(ResultDetailType.Error, this.m_localeService.GetString("MSGE073"), "//urn:hl7-org:v3#controlActProcess/urn:hl7-org:v3#queryByParmaeter/urn:hl7-org:v3#parameterList/urn:hl7-org:v3#patientAddress"));
                break;
            }            

            // Telecom
            foreach (var tel in parameterList.PatientTelecom)
            {
                if (filterPerson.TelecomAddresses == null) filterPerson.TelecomAddresses = new List<TelecommunicationsAddress>();
                if (tel.NullFlavor == null && tel.Value != null && tel.Value.Count == 1)
                    filterPerson.TelecomAddresses.Add(new TelecommunicationsAddress()
                    {
                        Use = Util.ToWireFormat(tel.Value[0].Use),
                        Value = tel.Value[0].Value
                    });
                else
                    dtls.Add(new InsufficientRepetitionsResultDetail(ResultDetailType.Error, this.m_localeService.GetString("MSGE073"), "//urn:hl7-org:v3#controlActProcess/urn:hl7-org:v3#queryByParmaeter/urn:hl7-org:v3#parameterList/urn:hl7-org:v3#patientTelecom"));
            }

            // Other ids
            foreach (var otherId in parameterList.OtherIDsScopingOrganization)
            {
                if(otherId != null && otherId.NullFlavor == null &&
                    otherId.Value.Count == 1)
                    ids.Add(CreateDomainIdentifier(otherId.Value[0], dtls));
                else
                    dtls.Add(new InsufficientRepetitionsResultDetail(ResultDetailType.Error, this.m_localeService.GetString("MSGE073"), "//urn:hl7-org:v3#controlActProcess/urn:hl7-org:v3#queryByParmaeter/urn:hl7-org:v3#parameterList/urn:hl7-org:v3#otherIDsScopingOrganization"));
            }

            // Filter
            filter.Add(filterPerson, "SUBJ", HealthServiceRecordSiteRoleType.SubjectOf, null);
            // Determine if errors exist that prevent the processing of this message
            if (dtls.Count(o => o.Type == ResultDetailType.Error) > 0)
                return null;

            return retVal;
        }
    }
}
