/* 
 * Copyright 2008-2011 Mohawk College of Applied Arts and Technology
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
 * User: Justin Fyfe
 * Date: 08-24-2011
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MARC.Everest.Connectors;
using System.ComponentModel;
using MARC.HI.EHRS.SVC.Core.Services;
using MARC.HI.EHRS.SVC.Core.DataTypes;
using MARC.Everest.RMIM.CA.R020402.Vocabulary;
using MARC.Everest.RMIM.CA.R020402.Interactions;
using MARC.HI.EHRS.SVC.Core.ComponentModel.Components;
using MARC.HI.EHRS.SVC.Core.ComponentModel;
using MARC.HI.EHRS.CR.Core.ComponentModel;

namespace MARC.HI.EHRS.CR.Messaging.Everest
{
    /// <summary>
    /// Message types
    /// </summary>
    public partial class ComponentUtil
    {
        /// <summary>
        /// Create components generic function to be used on the ControlActEvent of a message
        /// </summary>
        private RegistrationEvent CreateComponents<T>(MARC.Everest.RMIM.CA.R020402.MFMI_MT700711CA.ControlActEvent<T> controlActEvent, List<IResultDetail> dtls)
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
                if (iso6393code.CodeSystem != "2.16.840.1.113883.6.121" &&
                    iso6393code.CodeSystem != "2.16.840.1.113883.6.99")
                    dtls.Add(new VocabularyIssueResultDetail(ResultDetailType.Error, this.m_localeService.GetString("MSGE04B"), null));

                // Translate the language code
                if (iso6393code.CodeSystem == "2.16.840.1.113883.6.121") // we need to translate
                    iso6393code = term.Translate(iso6393code, "2.16.840.1.113883.6.99");

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

                if (controlActEvent.Author.AuthorPerson is MARC.Everest.RMIM.CA.R020402.COCT_MT090102CA.AssignedEntity)
                    aut = CreateParticipantComponent(controlActEvent.Author.AuthorPerson as MARC.Everest.RMIM.CA.R020402.COCT_MT090102CA.AssignedEntity, dtls);
                else if (controlActEvent.Author.AuthorPerson is MARC.Everest.RMIM.CA.R020402.COCT_MT090502CA.AssignedEntity)
                    aut = CreateParticipantComponent(controlActEvent.Author.AuthorPerson as MARC.Everest.RMIM.CA.R020402.COCT_MT090502CA.AssignedEntity, dtls);
                else
                    dtls.Add(new ResultDetail(ResultDetailType.Error, this.m_localeService.GetString("MSGE006"), null, null));

                if (aut != null)
                {
                    changeSummary.Add(aut, "AUT", HealthServiceRecordSiteRoleType.AuthorOf, aut.AlternateIdentifiers);
                    if((bool)controlActEvent.Subject.ContextConductionInd)
                        retVal.Add(aut.Clone() as IComponent, "AUT", HealthServiceRecordSiteRoleType.AuthorOf, aut.AlternateIdentifiers);

                    // Assign as RSP?
                    if (controlActEvent.ResponsibleParty == null || controlActEvent.ResponsibleParty.NullFlavor != null)
                    {
                        changeSummary.Add(aut.Clone() as IComponent, "RSP", HealthServiceRecordSiteRoleType.ResponsibleFor, aut.AlternateIdentifiers);
                        if ((bool)controlActEvent.Subject.ContextConductionInd)
                            retVal.Add(aut.Clone() as IComponent, "RSP", HealthServiceRecordSiteRoleType.ResponsibleFor, aut.AlternateIdentifiers);
                    }

                    // Assign as DE?
                    if(controlActEvent.DataEnterer == null || controlActEvent.DataEnterer.NullFlavor != null)
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
                if (controlActEvent.DataEnterer.EntererChoice is MARC.Everest.RMIM.CA.R020402.COCT_MT090502CA.AssignedEntity)
                    ptcpt = CreateParticipantComponent(controlActEvent.DataEnterer.EntererChoice as MARC.Everest.RMIM.CA.R020402.COCT_MT090502CA.AssignedEntity, dtls);
                else if (controlActEvent.DataEnterer.EntererChoice is MARC.Everest.RMIM.CA.R020402.COCT_MT090102CA.AssignedEntity)
                    ptcpt = CreateParticipantComponent(controlActEvent.DataEnterer.EntererChoice as MARC.Everest.RMIM.CA.R020402.COCT_MT090102CA.AssignedEntity, dtls);
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
            if(controlActEvent.ResponsibleParty != null && controlActEvent.ResponsibleParty.NullFlavor == null)
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
                ServiceDeliveryLocation loc = CreateLocationComponent(controlActEvent.Location.ServiceDeliveryLocation, dtls);
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
                ServiceDeliveryLocation loc = CreateLocationComponent(controlActEvent.DataEntryLocation.ServiceDeliveryLocation, dtls);
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
        private RegistrationEvent CreateComponents<T>(MARC.Everest.RMIM.CA.R020402.MFMI_MT700751CA.ControlActEvent<T> controlActEvent, List<IResultDetail> dtls)
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
                if (iso6393code.CodeSystem != "2.16.840.1.113883.6.121" &&
                    iso6393code.CodeSystem != "2.16.840.1.113883.6.99")
                    dtls.Add(new VocabularyIssueResultDetail(ResultDetailType.Error, this.m_localeService.GetString("MSGE04B"), null));

                // Translate the language code
                if (iso6393code.CodeSystem == "2.16.840.1.113883.6.121") // we need to translate
                    iso6393code = term.Translate(iso6393code, "2.16.840.1.113883.6.99");

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

                if (controlActEvent.Author.AuthorPerson is MARC.Everest.RMIM.CA.R020402.COCT_MT090102CA.AssignedEntity)
                    ptcpt = CreateParticipantComponent(controlActEvent.Author.AuthorPerson as MARC.Everest.RMIM.CA.R020402.COCT_MT090102CA.AssignedEntity, dtls);
                else if (controlActEvent.Author.AuthorPerson is MARC.Everest.RMIM.CA.R020402.COCT_MT090502CA.AssignedEntity)
                    ptcpt = CreateParticipantComponent(controlActEvent.Author.AuthorPerson as MARC.Everest.RMIM.CA.R020402.COCT_MT090502CA.AssignedEntity, dtls);
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
                if (controlActEvent.DataEnterer.EntererChoice is MARC.Everest.RMIM.CA.R020402.COCT_MT090502CA.AssignedEntity)
                    ptcpt = CreateParticipantComponent(controlActEvent.DataEnterer.EntererChoice as MARC.Everest.RMIM.CA.R020402.COCT_MT090502CA.AssignedEntity, dtls);
                else if (controlActEvent.DataEnterer.EntererChoice is MARC.Everest.RMIM.CA.R020402.COCT_MT090102CA.AssignedEntity)
                    ptcpt = CreateParticipantComponent(controlActEvent.DataEnterer.EntererChoice as MARC.Everest.RMIM.CA.R020402.COCT_MT090102CA.AssignedEntity, dtls);
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
                ServiceDeliveryLocation loc = CreateLocationComponent(controlActEvent.Location.ServiceDeliveryLocation, dtls);
                if (loc != null)
                    retVal.Add(loc, "LOC", HealthServiceRecordSiteRoleType.PlaceOfOccurence, loc.AlternateIdentifiers);
                else
                    dtls.Add(new ResultDetail(ResultDetailType.Warning, this.m_localeService.GetString("MSGW003"), null, null));
            }

            // data entry location
            if (controlActEvent.DataEntryLocation != null && controlActEvent.DataEntryLocation.NullFlavor == null)
            {
                ServiceDeliveryLocation loc = CreateLocationComponent(controlActEvent.DataEntryLocation.ServiceDeliveryLocation, dtls);
                if (loc != null)
                    retVal.Add(loc, "DTL", HealthServiceRecordSiteRoleType.PlaceOfEntry, loc.AlternateIdentifiers);
                else
                    dtls.Add(new ResultDetail(ResultDetailType.Warning, this.m_localeService.GetString("MSGW004"), null, null));
            }

            return retVal;
        }


        
    }
}
