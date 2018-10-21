/**
 * Copyright 2012-2015 Mohawk College of Applied Arts and Technology
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
using MARC.HI.EHRS.SVC.Messaging.FHIR.Resources;
using MARC.HI.EHRS.CR.Core.ComponentModel;
using MARC.HI.EHRS.SVC.Core.ComponentModel.Components;
using MARC.HI.EHRS.SVC.Core.Services;
using MARC.HI.EHRS.SVC.Messaging.FHIR.DataTypes;
using MARC.Everest.Connectors;
using MARC.HI.EHRS.SVC.Core.DataTypes;
using MARC.HI.EHRS.SVC.Core.ComponentModel;
using MARC.HI.EHRS.CR.Messaging.FHIR.Util;
using MARC.HI.EHRS.SVC.Messaging.FHIR;
using MARC.HI.EHRS.SVC.Messaging.FHIR.Util;
using MARC.HI.EHRS.SVC.Messaging.FHIR.Attributes;
using MARC.Everest.RMIM.CA.R020402.Vocabulary;
using System.Collections.Specialized;
using System.ServiceModel.Web;
using System.ComponentModel;
using System.Diagnostics;
using System.Data;
using System.IO;
using System.Xml.Serialization;
using MARC.HI.EHRS.SVC.Messaging.FHIR.Resources.Attributes;
using System.Text.RegularExpressions;

namespace MARC.HI.EHRS.CR.Messaging.FHIR.Processors
{
    /// <summary>
    /// Message processor for patients
    /// </summary>
    [Profile(ProfileId = "pdqm", Name = "Patient Demographics Query Mobile")]
    [ResourceProfile(Resource = typeof(Patient), Name = "Patient Demographics Query for Mobile patient profile")]
    [ExtensionProfile(ExtensionClass = typeof(ExtensionUtil))]
    public class PatientMessageProcessor : MessageProcessorBase, IFhirMessageProcessor
    {
        #region IFhirMessageProcessor Members

        /// <summary>
        /// The name of the resource
        /// </summary>
        public override string ResourceName
        {
            get { return "Patient"; }
        }

        /// <summary>
        /// Gets the type of resource
        /// </summary>
        public override Type ResourceType
        {
            get { return typeof(Patient); }
        }

        /// <summary>
        /// Component type that matches this
        /// </summary>
        public override Type ComponentType
        {
            get { return typeof(Person); }
        }

        /// <summary>
        /// The domain in which data belongs
        /// </summary>
        public override string DataDomain
        {
            get { return ApplicationContext.ConfigurationService.OidRegistrar.GetOid("CR_CID").Oid; }
        }

        /// <summary>
        /// Parse parameters
        /// </summary>
        [SearchParameterProfile(Name = "_id", Type = "token", Description = "Client registry assigned id for the patient (one repetition only)")]
        [SearchParameterProfile(Name = "active", Type = "token", Description = "Whether the patient record is active (one repetition only)")]
        [SearchParameterProfile(Name = "address", Type = "string", Description = "An address in any kind of address part (only supports OR see documentation)")]
        [SearchParameterProfile(Name = "birthdate", Type = "date", Description = "The patient's date of birth (only supports OR)")]
        [SearchParameterProfile(Name = "family", Type = "string", Description = "One of the patient's family names (only supports AND)")]
        [SearchParameterProfile(Name = "given", Type = "string", Description = "One of the patient's given names (only supports AND)")]
        [SearchParameterProfile(Name = "gender", Type = "token", Description = "Gender of the patient (one repetition only)")]
        [SearchParameterProfile(Name = "identifier", Type = "token", Description = "A patient identifier (only supports OR)")]
        [SearchParameterProfile(Name = "mothersMaidenName.given", Type = "string", Description = "Filter on the patient's mother's maiden name (given)")]
        [SearchParameterProfile(Name = "mothersMaidenName.family", Type = "string", Description = "Filter on the patient's mother's maiden name (family)")]
        [SearchParameterProfile(Name = "relatedPerson.id", Type = "string", Description = "Filter on the patient's family member's identifier")]
        [SearchParameterProfile(Name = "telecom", Type = "string", Description = "Filter based on patient's telecommunications address")]
        [SearchParameterProfile(Name= "multipleBirthInteger", Type = "string", Description = "Filter on patient's birth order")]
        [SearchParameterProfile(Name = "provider.identifier", Type = "token", Description = "One of the organizations to which this person is a patient (only supports OR)")]
        [SearchParameterProfile(Name = "variant", Type = "token", Description="When true indicates variant matching")]
        [SearchParameterProfile(Name = "soundex", Type="token", Description = "When true, indicates soundex should be used")]
        public override Util.DataUtil.ClientRegistryFhirQuery ParseQuery(System.Collections.Specialized.NameValueCollection parameters, List<IResultDetail> dtls)
        {
            ITerminologyService termSvc = ApplicationContext.CurrentContext.GetService(typeof(ITerminologyService)) as ITerminologyService;

            Util.DataUtil.ClientRegistryFhirQuery retVal = base.ParseQuery(parameters, dtls);

            var subjectFilter = new Person();
            QueryEvent queryFilter = new QueryEvent();
            QueryParameters queryControl = new QueryParameters()
            {
                MatchingAlgorithm = MatchAlgorithm.Exact
            };
            //MARC.HI.EHRS.SVC.Core.DataTypes.AddressSet addressFilter = null;
            MARC.HI.EHRS.SVC.Core.DataTypes.NameSet nameFilter = null;

            for(int i = 0; i < parameters.Count; i++)
                try
                {
                    

                        switch (parameters.GetKey(i))
                        {
                            case "variant":
                                {
                                    bool variantParam = false;
                                    if (!Boolean.TryParse(parameters.GetValues(i)[0], out variantParam))
                                        dtls.Add(new ValidationResultDetail(ResultDetailType.Error, "Variant parameter must convey a boolean value", null, null));
                                    queryControl.MatchingAlgorithm |= MatchAlgorithm.Variant;
                                    retVal.ActualParameters.Add("variant", variantParam.ToString());
                                    break;
                                }
                            case "soundex":
                                {
                                    bool soundexParam = false;
                                    if (!Boolean.TryParse(parameters.GetValues(i)[0], out soundexParam))
                                        dtls.Add(new ValidationResultDetail(ResultDetailType.Error, "Soundex parameter must convey a boolean value", null, null));
                                    queryControl.MatchingAlgorithm |= MatchAlgorithm.Soundex;
                                    retVal.ActualParameters.Add("soundex", soundexParam.ToString());
                                    break;
                                }
                            case "_id":
                                {
                                    if (subjectFilter.AlternateIdentifiers == null)
                                        subjectFilter.AlternateIdentifiers = new List<DomainIdentifier>();

                                    if (parameters.GetValues(i).Length > 1)
                                        dtls.Add(new InsufficientRepetitionsResultDetail(ResultDetailType.Warning, "Cannot perform AND on identifier", null));

                                    var appDomain = ApplicationContext.ConfigurationService.OidRegistrar.GetOid("CR_CID").Oid;
                                    StringBuilder actualIdParm = new StringBuilder();
                                    foreach (var itm in parameters.GetValues(i)[0].Split(','))
                                    {
                                        var domainId = MessageUtil.IdentifierFromToken(itm);
                                        if (domainId.Domain == null)
                                            domainId.Domain = appDomain;
                                        else if (!domainId.Domain.Equals(appDomain))
                                        {
                                            var dtl = new UnrecognizedPatientDomainResultDetail(ApplicationContext.LocalizationService, domainId.Domain);
                                            dtl.Location = "_id";
                                            dtls.Add(dtl);
                                            continue;
                                        }
                                        subjectFilter.AlternateIdentifiers.Add(domainId);
                                        actualIdParm.AppendFormat("{0},", MessageUtil.UnEscape(itm));
                                    }

                                    retVal.ActualParameters.Add("_id", actualIdParm.ToString());
                                    break;
                                }
                            case "active":

                                bool activeParm = false;
                                if (!Boolean.TryParse(parameters.GetValues(i)[0], out activeParm))
                                    dtls.Add(new ValidationResultDetail(ResultDetailType.Error, "Active parameter must convey a boolean value", null, null));
                                subjectFilter.Status = activeParm ? StatusType.Active | StatusType.Completed : StatusType.Obsolete | StatusType.Nullified | StatusType.Cancelled | StatusType.Aborted;
                                retVal.ActualParameters.Add("active", (subjectFilter.Status == (StatusType.Active | StatusType.Completed)).ToString());
                                break;
                            //case "address.use":
                            //case "address.line":
                            //case "address.city":
                            //case "address.state":
                            //case "address.zip":
                            //case "address.country":
                            //    {
                            //        if (addressFilter == null)
                            //            addressFilter = new SVC.Core.DataTypes.AddressSet();
                            //        string property = parameters.GetKey(i).Substring(parameters.GetKey(i).IndexOf(".") + 1);
                            //        string value = parameters.GetValues(i)[0];

                            //        if(value.Contains(",") || parameters.GetValues(i).Length > 1)
                            //            dtls.Add(new InsufficientRepetitionsResultDetail(ResultDetailType.Warning, "Cannot perform AND or OR on address fields", "address." + property));

                            //        if (property == "use")
                            //        {
                            //            addressFilter.Use = HackishCodeMapping.Lookup(HackishCodeMapping.ADDRESS_USE, value);
                            //            retVal.ActualParameters.Add("address.use", HackishCodeMapping.ReverseLookup(HackishCodeMapping.ADDRESS_USE, addressFilter.Use));
                            //        }
                            //        else
                            //            addressFilter.Parts.Add(new SVC.Core.DataTypes.AddressPart()
                            //            {
                            //                AddressValue = value,
                            //                PartType = HackishCodeMapping.Lookup(HackishCodeMapping.ADDRESS_PART, value)
                            //            });
                            //            retVal.ActualParameters.Add(String.Format("address.{0}", HackishCodeMapping.ReverseLookup(HackishCodeMapping.ADDRESS_PART, addressFilter.Parts.Last().PartType)), value);

                            //        break;
                            //    }
                            case "address": // Address is really messy ... 
                                {
                                    subjectFilter.Addresses = new List<SVC.Core.DataTypes.AddressSet>();
                                    // OR is only supported for this
                                    if (parameters.GetValues(i).Length > 1)
                                        dtls.Add(new InsufficientRepetitionsResultDetail(ResultDetailType.Warning, "Cannot perform AND on address", null));


                                    var actualAdParm = new StringBuilder();
                                    foreach (var ad in parameters.GetValues(i))
                                    {
                                        foreach (var adpn in parameters.GetValues(i)[0].Split(','))
                                        {
                                            foreach (var kv in HackishCodeMapping.ADDRESS_PART)
                                                subjectFilter.Addresses.Add(new SVC.Core.DataTypes.AddressSet()
                                                {
                                                    Use = SVC.Core.DataTypes.AddressSet.AddressSetUse.Search,
                                                    Parts = new List<SVC.Core.DataTypes.AddressPart>() {
                                                    new MARC.HI.EHRS.SVC.Core.DataTypes.AddressPart() {
                                                        PartType = kv.Value,
                                                        AddressValue = adpn
                                                    }
                                                }
                                                });
                                            actualAdParm.AppendFormat("{0},", adpn);
                                        }
                                    }

                                    actualAdParm = actualAdParm.Remove(actualAdParm.Length - 1, 1);
                                    retVal.ActualParameters.Add("address", actualAdParm.ToString());
                                    break;
                                }
                            case "birthdate":
                                {
                                    string value = parameters.GetValues(i)[0];
                                    if (value.Contains(","))
                                        value = value.Substring(0, value.IndexOf(","));
                                    else if (parameters.GetValues(i).Length > 1)
                                        dtls.Add(new InsufficientRepetitionsResultDetail(ResultDetailType.Warning, "Cannot perform AND on birthdate", null));

                                    var dValue = new DateOnly() { Value = MessageUtil.UnEscape(value) };
                                    subjectFilter.BirthTime = new SVC.Core.DataTypes.TimestampPart()
                                    {
                                        Value = dValue.DateValue.Value,
                                        Precision = HackishCodeMapping.ReverseLookup(HackishCodeMapping.DATE_PRECISION, dValue.Precision)
                                    };
                                    retVal.ActualParameters.Add("birthdate", dValue.XmlValue);
                                    break;
                                }
                            case "family":
                                {
                                    if (nameFilter == null)
                                        nameFilter = new SVC.Core.DataTypes.NameSet() { Use = SVC.Core.DataTypes.NameSet.NameSetUse.Search };

                                    foreach (var nm in parameters.GetValues(i))
                                    {
                                        if (nm.Contains(",")) // Cannot do an OR on Name
                                            dtls.Add(new InsufficientRepetitionsResultDetail(ResultDetailType.Warning, "Cannot perform OR on family", null));

                                        nameFilter.Parts.Add(new SVC.Core.DataTypes.NamePart()
                                        {
                                            Type = SVC.Core.DataTypes.NamePart.NamePartType.Family,
                                            Value = parameters.GetValues(i)[0]
                                        });
                                        retVal.ActualParameters.Add("family", nm);
                                    }
                                    break;
                                }
                            case "given":
                                {

                                    if (nameFilter == null)
                                        nameFilter = new SVC.Core.DataTypes.NameSet() { Use = SVC.Core.DataTypes.NameSet.NameSetUse.Search };
                                    foreach (var nm in parameters.GetValues(i))
                                    {
                                        if (nm.Contains(",")) // Cannot do an OR on Name
                                            dtls.Add(new InsufficientRepetitionsResultDetail(ResultDetailType.Warning, "Cannot perform OR on given", null));
                                        nameFilter.Parts.Add(new SVC.Core.DataTypes.NamePart()
                                        {
                                            Type = SVC.Core.DataTypes.NamePart.NamePartType.Given,
                                            Value = nm
                                        });
                                        retVal.ActualParameters.Add("given", nm);
                                    }
                                    break;
                                }
                            case "name":
                                {
                                    if (subjectFilter.Names == null)
                                        subjectFilter.Names = new List<NameSet>();

                                    foreach (var nm in parameters.GetValues(i))
                                    {
                                        if (nm.Contains(",")) // Cannot do an OR on Name
                                            dtls.Add(new InsufficientRepetitionsResultDetail(ResultDetailType.Warning, "Cannot perform OR on name", null));

                                        NameSet fn = new NameSet(), gn = new NameSet();

                                        fn.Parts.Add(new SVC.Core.DataTypes.NamePart()
                                        {
                                            Type = SVC.Core.DataTypes.NamePart.NamePartType.Family,
                                            Value = parameters.GetValues(i)[0]
                                        });
                                        gn.Parts.Add(new SVC.Core.DataTypes.NamePart()
                                        {
                                            Type = SVC.Core.DataTypes.NamePart.NamePartType.Given,
                                            Value = parameters.GetValues(i)[0]
                                        });
                                        subjectFilter.Names.Add(fn);
                                        subjectFilter.Names.Add(gn);
                                        retVal.ActualParameters.Add("name", nm);
                                    }
                                    break;
                                }
                            case "multipleBirthInteger":
                                {
                                    // ultiple Birth Integer
                                    string value = parameters.GetValues(i)[0].ToUpper();
                                    if (value.Contains(",") || parameters.GetValues(i).Length > 1)
                                        dtls.Add(new InsufficientRepetitionsResultDetail(ResultDetailType.Warning, "Cannot perform OR or AND on gender", null));

                                    // Multiple birth integer
                                    int multipleBirthInteger = 0;
                                    if (!Int32.TryParse(value, out multipleBirthInteger))
                                        dtls.Add(new ValidationResultDetail(ResultDetailType.Error, "Parameter must be an integer", "multipleBirthInteger", null));
                                    subjectFilter.BirthOrder = multipleBirthInteger;

                                    break;
                                }
                            case "mothersIdentifier":
                                {

                                    // Relationship for mother
                                    PersonalRelationship prs = subjectFilter.FindComponent(HealthServiceRecordSiteRoleType.RepresentitiveOf) as PersonalRelationship;
                                    if (prs == null)
                                    {
                                        prs = new PersonalRelationship();
                                        prs.RelationshipKind = "MTH";
                                        prs.LegalName = new NameSet();
                                        subjectFilter.Add(prs, "PRS", HealthServiceRecordSiteRoleType.RepresentitiveOf, null);
                                    }

                                    if (prs.AlternateIdentifiers == null)
                                        prs.AlternateIdentifiers = new List<SVC.Core.DataTypes.DomainIdentifier>();
                                    foreach (var cparm in parameters.GetValues(i))
                                    {
                                        StringBuilder actualIdParm = new StringBuilder();
                                        foreach (var val in cparm.Split(','))
                                        {
                                            var domainId = MessageUtil.IdentifierFromToken(val);
                                            prs.AlternateIdentifiers.Add(domainId);
                                            actualIdParm.AppendFormat("{0},", val);
                                        }

                                        if (actualIdParm.Length > 0)
                                        {
                                            actualIdParm.Remove(actualIdParm.Length - 1, 1);
                                            retVal.ActualParameters.Add("mothersIdentifier", actualIdParm.ToString());
                                        }
                                    }
                                    break;
                                }

                            case "mothersMaidenName.given":
                            case "mothersMaidenName.family":
                                {
                                    // Relationship for mother
                                    PersonalRelationship prs = subjectFilter.FindComponent(HealthServiceRecordSiteRoleType.RepresentitiveOf) as PersonalRelationship;
                                    if (prs == null)
                                    {
                                        prs = new PersonalRelationship();
                                        prs.RelationshipKind = "MTH";
                                        prs.LegalName = new NameSet();
                                        subjectFilter.Add(prs, "PRS", HealthServiceRecordSiteRoleType.RepresentitiveOf, null);
                                    }

                                    // Parameters
                                    foreach (var nm in parameters.GetValues(i))
                                    {
                                        if (nm.Contains(",")) // Cannot do an OR on Name
                                            dtls.Add(new InsufficientRepetitionsResultDetail(ResultDetailType.Warning, String.Format("Cannot perform OR on {0}", parameters.GetKey(i)), null));
                                        
                                        prs.LegalName.Parts.Add(new SVC.Core.DataTypes.NamePart()
                                        {
                                            Type = parameters.GetKey(i) == "mothersMaidenName.given" ? NamePart.NamePartType.Given : NamePart.NamePartType.Family,
                                            Value = nm
                                        });
                                        retVal.ActualParameters.Add(parameters.GetKey(i), nm);
                                    }

                                    break;
                                }
                            case "gender":
                                {
                                    string value = parameters.GetValues(i)[0].ToUpper();
                                    if (value.Contains(",") || parameters.GetValues(i).Length > 1)
                                        dtls.Add(new InsufficientRepetitionsResultDetail(ResultDetailType.Warning, "Cannot perform OR or AND on gender", null));

                                    var gCode = MessageUtil.CodeFromToken(value);
                                    if (gCode.Code == "UNK") // Null Flavor
                                        retVal.ActualParameters.Add("gender", String.Format("http://hl7.org/fhir/v3/NullFlavor|UNK"));
                                    else if (!new List<String>() { "M", "F", "UN" }.Contains(gCode.Code))
                                        dtls.Add(new VocabularyIssueResultDetail(ResultDetailType.Error, String.Format("Cannot find code {0} in administrative gender", gCode.Code), null));
                                    else if(gCode.CodeSystem != "http://hl7.org/fhir/v3/AdministrativeGender" || gCode.CodeSystem.EndsWith("v3-AdministrativeGender"))
                                    {
                                        subjectFilter.GenderCode = gCode.Code;
                                        retVal.ActualParameters.Add("gender", String.Format("http://hl7.org/fhir/v3/AdministrativeGender|{0}", gCode.Code));
                                    }
                                    else
                                        dtls.Add(new VocabularyIssueResultDetail(ResultDetailType.Error, String.Format("Cannot use code system {0} for administrative gender", gCode.CodeSystem), null));

                                    break;
                                }
                            case "identifier":
                                {

                                    if (subjectFilter.AlternateIdentifiers == null)
                                        subjectFilter.AlternateIdentifiers = new List<SVC.Core.DataTypes.DomainIdentifier>();
                                    foreach (var cparm in parameters.GetValues(i))
                                    {
                                        StringBuilder actualIdParm = new StringBuilder();
                                        foreach (var val in cparm.Split(','))
                                        {
                                            var domainId = MessageUtil.IdentifierFromToken(val);
                                            subjectFilter.AlternateIdentifiers.Add(domainId);
                                            actualIdParm.AppendFormat("{0},", val);
                                        }

                                        if (actualIdParm.Length > 0)
                                        {
                                            actualIdParm.Remove(actualIdParm.Length - 1, 1);
                                            retVal.ActualParameters.Add("identifier", actualIdParm.ToString());
                                        }
                                    }
                                    break;
                                }
                            case "provider.identifier": // maps to the target domains ? 
                                {

                                    foreach (var val in parameters.GetValues(i)[0].Split(','))
                                    {
                                        var did = new DomainIdentifier() { Domain = MessageUtil.IdentifierFromToken(val).Domain };
                                        if (String.IsNullOrEmpty(did.Domain))
                                        {
                                            dtls.Add(new NotSupportedChoiceResultDetail(ResultDetailType.Warning, "Provider organization identifier unknown", null));
                                            continue;
                                        }

                                        subjectFilter.AlternateIdentifiers.Add(did);
                                        retVal.ActualParameters.Add("provider.identifier", String.Format("{0}|", MessageUtil.TranslateDomain(did.Domain)));
                                    }
                                    break;

                                }
                            case "telecom":
                                {
                                    if (subjectFilter.TelecomAddresses == null)
                                        subjectFilter.TelecomAddresses = new List<TelecommunicationsAddress>();

                                    StringBuilder actualTelParm = new StringBuilder();
                                    foreach (var val in parameters.GetValues(i)[0].Split(','))
                                    {

                                        subjectFilter.TelecomAddresses.Add(new TelecommunicationsAddress() { Value = val });
                                        actualTelParm.AppendFormat("{0},", val);
                                    }

                                    if (actualTelParm.Length > 0)
                                    {
                                        actualTelParm.Remove(actualTelParm.Length - 1, 1);
                                        retVal.ActualParameters.Add("telecom", actualTelParm.ToString());
                                    }
                                    break;

                                }
                            case "_format":
                                break;
                            default:
                                if (retVal.ActualParameters.Get(parameters.GetKey(i)) == null)
                                    dtls.Add(new NotSupportedChoiceResultDetail(ResultDetailType.Warning, String.Format("{0} is not a supported query parameter", parameters.GetKey(i)), null));
                                break;
                        }
                }
                catch (Exception e)
                {
                    dtls.Add(new ResultDetail(ResultDetailType.Error, string.Format("Unable to process parameter {0} due to error {1}", parameters.Get(i), e.Message), e));
                }

            // Add a name filter?
            if(nameFilter != null)
                subjectFilter.Names = new List<SVC.Core.DataTypes.NameSet>() { nameFilter };

            RegistrationEvent regFilter = new RegistrationEvent();
            regFilter.Add(subjectFilter, "SUBJ", HealthServiceRecordSiteRoleType.SubjectOf, null);
            queryFilter.Add(queryControl, "FLT", HealthServiceRecordSiteRoleType.FilterOf, null);
            queryFilter.Add(regFilter, "SUBJ", HealthServiceRecordSiteRoleType.SubjectOf, null);
            retVal.Filter = queryFilter;
    

            return retVal;
        }

        /// <summary>
        /// Process resource
        /// </summary>
        public override System.ComponentModel.IComponent ProcessResource(ResourceBase resource, List<IResultDetail> dtls)
        {

            // Verify extensions
            ExtensionUtil.VerifyExtensions(resource, dtls);

            var resPatient = resource as Patient;

            if (resPatient == null )
                throw new ArgumentNullException("resource", "Resource invalid");


            // Process a resource
            RegistrationEvent regEvent = new RegistrationEvent()
            {
                EventClassifier = RegistrationEventType.Register,
                EventType = new CodeValue("GET"),
                Mode = RegistrationEventType.Register,
                Status = StatusType.Completed,
                Timestamp = DateTime.Now,
                EffectiveTime = new TimestampSet()
                {
                    Parts = new List<TimestampPart>() {
                        new TimestampPart(TimestampPart.TimestampPartType.Standlone, DateTime.Now, "F")
                    }
                },
                LanguageCode = ApplicationContext.ConfigurationService.JurisdictionData.DefaultLanguageCode
            };

            // Person component
            Person psn = new Person();
            psn.Status = resPatient.Active == true ? StatusType.Active : StatusType.Suspended;
            psn.RoleCode = PersonRole.PAT;

            // Person identifier
            psn.AlternateIdentifiers = new List<DomainIdentifier>();
            foreach (var id in resPatient.Identifier)
                psn.AlternateIdentifiers.Add(base.ConvertIdentifier(id, dtls));

            if(!String.IsNullOrEmpty(resource.Id))
                psn.AlternateIdentifiers.Add(new DomainIdentifier()
                {
                    Domain = ApplicationContext.ConfigurationService.OidRegistrar.GetOid("CR_CID").Oid,
                    Identifier = resource.Id
                });

            // HACK:
            // TODO: Make this a configuration option
#if !DEBUG
            if (psn.AlternateIdentifiers.Count == 0)
                dtls.Add(new MandatoryElementMissingResultDetail(ResultDetailType.Error, ApplicationContext.LocalizationService.GetString("MSGE078"), "Patient"));
#endif 

            // Birth date
            if(resPatient.BirthDate != null)
                psn.BirthTime = new TimestampPart(TimestampPart.TimestampPartType.Standlone, resPatient.BirthDate, "D");

            // Deceased time
            if (resPatient.Deceased is FhirBoolean)
            {
                dtls.Add(new NotSupportedChoiceResultDetail(ResultDetailType.Warning, "This registry only supports dates for deceased range. Value was converted to current date and no precision", null, null));
                psn.DeceasedTime = new TimestampPart(TimestampPart.TimestampPartType.Standlone, DateTime.Now, "N");
            }
            else if(resPatient.Deceased is Date)
            {
                var dtPrec = HackishCodeMapping.ReverseLookup(HackishCodeMapping.DATE_PRECISION, (resPatient.Deceased as Date).Precision);
                psn.DeceasedTime = new TimestampPart(TimestampPart.TimestampPartType.Standlone, (resPatient.Deceased as Date).DateValue.Value, dtPrec);
            }

            // Gender code
            if (resPatient.Gender != null)
                psn.GenderCode = HackishCodeMapping.GetGenderCode(resPatient.Gender, dtls);

            // Multiple birth
            if (resPatient.MultipleBirth is FhirInt)
                psn.BirthOrder = (resPatient.MultipleBirth as FhirInt);
            else if(resPatient.MultipleBirth is FhirBoolean)
                psn.BirthOrder= 1;
            
            // Address 
            psn.Addresses = new List<AddressSet>();
            foreach (var fhirAd in resPatient.Address)
                psn.Addresses.Add(base.ConvertAddress(fhirAd, dtls));

            // Marital status\
            if(resPatient.MaritalStatus != null)
                psn.MaritalStatus = base.ConvertCode(resPatient.MaritalStatus, dtls);

            // Photograph?
            if (resPatient.Photo != null)
                foreach(var photo in resPatient.Photo)
                    psn.Add(new ExtendedAttribute()
                    {
                        Name = "FhirPhotographResourceAttachment",
                        PropertyPath = "Patient.Photo",
                        Value = photo
                    });

            // Contact persons
            foreach (var containedResource in resPatient.Contained.Where(p=>p.Item is RelatedPerson))
            {
                var relativeResource = containedResource.Item as RelatedPerson;
                // Now we need to create a personal relationship out of this mess.
                PersonalRelationship relationship = new PersonalRelationship();
                // Name
                if (relativeResource.Name != null)
                    relationship.LegalName = base.ConvertName(relativeResource.Name, dtls);

                // Telecom
                if (relativeResource.Telecom.Count > 0)
                {
                    relationship.TelecomAddresses = new List<TelecommunicationsAddress>();
                    foreach (var tel in relativeResource.Telecom)
                        relationship.TelecomAddresses.Add(base.ConvertTelecom(tel, dtls));
                }

                // Address
                if (relativeResource.Address != null)
                    relationship.PerminantAddress = base.ConvertAddress(relativeResource.Address, dtls);

                // Gender
                if (relativeResource.Gender != null)
                {
                    relationship.GenderCode = HackishCodeMapping.GetGenderCode(relativeResource.Gender, dtls);
                }

                // Now add as a personal relationship
                if(relativeResource.Relationship != null)
                {
                    Coding relationshipKind = relativeResource.Relationship.GetPrimaryCode();
                    if (relationshipKind != null)
                        relationship.RelationshipKind = relationshipKind.Code;
                    else
                    {
                        relationshipKind = relativeResource.Relationship.GetCoding(new Uri("http://hl7.org/fhir/patient-contact-relationship"));
                        if (relationshipKind != null)
                            relationship.RelationshipKind = HackishCodeMapping.Lookup(HackishCodeMapping.RELATIONSHIP_KIND, relationshipKind.Code);
                        else
                            dtls.Add(new VocabularyIssueResultDetail(ResultDetailType.Warning, "Could not process RelationshipKind", null));
                    }

                }

                relationship.Status = StatusType.Active;

                // Add the relationship
                psn.Add(relationship, Guid.NewGuid().ToString(), HealthServiceRecordSiteRoleType.RepresentitiveOf, null);
            }

            // TODO: Communication lanugage
            psn.Language = new List<PersonLanguage>();
            foreach (var lang in resPatient.Language)
            {
                var termService = ApplicationContext.CurrentContext.GetService(typeof(ITerminologyService)) as ITerminologyService;

                PersonLanguage pl = new PersonLanguage();

                CodeValue languageCode = this.ConvertCode(lang, dtls);
                // Default ISO 639-3
                languageCode.CodeSystem = languageCode.CodeSystem ?? ApplicationContext.ConfigurationService.OidRegistrar.GetOid("ISO639-3").Oid;

                // Validate the language code
                if (languageCode.CodeSystem != ApplicationContext.ConfigurationService.OidRegistrar.GetOid("ISO639-3").Oid &&
                    languageCode.CodeSystem != ApplicationContext.ConfigurationService.OidRegistrar.GetOid("ISO639-1").Oid)
                    dtls.Add(new VocabularyIssueResultDetail(ResultDetailType.Error, ApplicationContext.LocalizationService.GetString("MSGE04B"), null));

                // Translate the language code
                if (languageCode.CodeSystem == ApplicationContext.ConfigurationService.OidRegistrar.GetOid("ISO639-3").Oid) // we need to translate
                    languageCode = termService.Translate(languageCode, ApplicationContext.ConfigurationService.OidRegistrar.GetOid("ISO639-1").Oid);

                if (languageCode == null)
                    dtls.Add(new VocabularyIssueResultDetail(ResultDetailType.Error, ApplicationContext.LocalizationService.GetString("MSGE04C"), null, null));
                else
                    pl.Language = languageCode.Code;

                pl.Type = LanguageType.Preferred;

                // Add
                psn.Language.Add(pl);

            }

            // Extensions
            foreach (var extension in resource.Extension)
            {
                if (extension.Url == ExtensionUtil.GetExtensionNameUrl("ethnicGroup"))
                {
                    var ethnicGroup = extension.Value as CodeableConcept;
                    psn.EthnicGroup.Add(this.ConvertCode(ethnicGroup, dtls));
                }
                else if (extension.Url == ExtensionUtil.GetExtensionNameUrl("mothersMaidenName"))
                {
                    // Extension, see if the person has a mother that we're processing?
                    var representatives = psn.FindAllComponents(HealthServiceRecordSiteRoleType.RepresentitiveOf);
                    PersonalRelationship mother = representatives.OfType<PersonalRelationship>().FirstOrDefault(p => p.Status == StatusType.Active && p.RelationshipKind == "MTH");
                    NameSet extensionNameValue = base.ConvertName(extension.Value as HumanName, dtls);

                    if (mother == null)
                    {
                        mother = new PersonalRelationship();
                        mother.LegalName = extensionNameValue;
                        mother.LegalName.Use = NameSet.NameSetUse.MaidenName;
                    }
                    else if (mother.LegalName.SimilarityTo(extensionNameValue) != 1)// Already have a mother, the name match?
                        dtls.Add(new ValidationResultDetail(ResultDetailType.Error, "When RelatedPerson of type 'mother' is contained in patient resource, the 'mothersMaidenName' value must match the name of the relatedPerson.", null, null));
                }
            }

            // Telecoms
            if (resPatient.Telecom.Count > 0)
            {
                psn.TelecomAddresses = new List<TelecommunicationsAddress>();
                foreach (var tel in resPatient.Telecom)
                    psn.TelecomAddresses.Add(base.ConvertTelecom(tel, dtls));
            }
            // Names
            psn.Names = new List<NameSet>();
            foreach (var name in resPatient.Name)
                psn.Names.Add(base.ConvertName(name, dtls));

            // Original Text
            if (resource.Text != null)
            {
                XmlSerializer xsz = new XmlSerializer(typeof(RawXmlWrapper));
                using (MemoryStream ms = new MemoryStream())
                {
                    xsz.Serialize(ms, resource.Text.Div);
                    ms.Seek(0, SeekOrigin.Begin);
                    byte[] buffer = new byte[ms.Length];
                    ms.Read(buffer, 0, (int)ms.Length);
                    psn.Add(new ExtendedAttribute()
                    {
                        Name = "OriginalText",
                        PropertyPath = "Patient.Text",
                        Value = new Attachment()
                        {
                            ContentType = new PrimitiveCode<string>("text/xhtml"),
                            Data = new FhirBinary(buffer)
                        }
                    });
                }
            }
            // Add subject
            regEvent.Add(psn, "SUBJ", HealthServiceRecordSiteRoleType.SubjectOf, null);
            if(dtls.Exists(o=>o.Type == ResultDetailType.Error))
                return null;
            return regEvent;

        }

        /// <summary>
        /// Process components
        /// </summary>
        /// TODO: make this more robust
        [ElementProfile(Property = "Animal", MaxOccurs = 0, Comment = "This registry only supports human patients")]
        [ElementProfile(Property = "Contact.Organization", MaxOccurs = 0, Comment = "This registry only supports relationships with 'Person' objects")]
        //[ElementProfile(Property = "Photo", MaxOccurs = 0, Comment = "This registry does not support the storage of photographs directly")]
        [ElementProfile(Property = "Name", MinOccurs = 1, MaxOccurs = -1, Comment = "PDQm Requirement that patients must have a name")]
        [ElementProfile(Property = "Identifier", MinOccurs = 1, MaxOccurs = -1, Comment = "PIX Manager and PDQm profile requires at least one identifier. When submitting a resource the @system attribute must match the referenced provider identifier's @system attribute (i.e. you may only register new identifiers for systems which you are the registered creator)")]
        [ElementProfile(Property = "Deceased", ValueType = typeof(Date), Comment = "PDQm specifies only date values are supported for deceased indication. Boolean will be translated to a non-zero date")]
        [ElementProfile(Property = "MultipleBirth", ValueType = typeof(FhirInt), Comment = "PDQm specifies that only multiple birth number is supported. Boolean will be translated to a non-zero value")]
        [ElementProfile(Property = "MaritalStatus", RemoteBinding = "http://hl7.org/fhir/vs/marital-status", Comment = "Marital status can be drawn from this marital-status value set")]
        [ElementProfile(Property = "Extension", Comment = "Additional attributes which could not be mapped to FHIR will be placed in the \"Extension\" element")]
        [ElementProfile(Property = "Communication", RemoteBinding = "http://hl7.org/fhir/sid/iso-639-1", Comment = "Language codes should be drawn from ISO-639-1 , ISO-639-3 is acceptable but will be translated")]
        //[ElementProfile(Property = "Language.Mode", Binding = typeof(LanguageAbilityMode), Comment = "Language mode is restricted to ESP and EWR")]
        //[ElementProfile(Property = "Language.ProficiencyLevel", Binding = typeof(LanguageAbilityProficiency), Comment = "Language proficiency will be either E or not provided")]
        //[ElementProfile(Property = "Language.Preference", Comment = "Treated as indicator. Will only appear if set to 'true'")]
        [ElementProfile(Property = "Text", Comment = "Will be auto-generated, any provided will be stored and represented as an extension \"originalText\"")]
        public override ResourceBase ProcessComponent(System.ComponentModel.IComponent component, List<IResultDetail> dtls)
        {
            // Setup references
            if (component is RegistrationEvent)
                component = (component as RegistrationEvent).FindComponent(HealthServiceRecordSiteRoleType.SubjectOf);
            
            Patient retVal = new Patient();
            
            Person person = component as Person;
            RegistrationEvent regEvt = component.Site != null ? component.Site.Container as RegistrationEvent: null;

            if (person.RoleCode != PersonRole.PAT)
                return null;

            if ((person.Status == StatusType.Terminated || person.Status == StatusType.Nullified) && (person.Site == null || person.Site.Container is RegistrationEvent))
                throw new FileLoadException("Resource is no longer available");

            retVal.Id = person.Id.ToString();

            // Load registration event
            if (regEvt == null && component.Site == null)
            {
                IDataPersistenceService idp = ApplicationContext.CurrentContext.GetService(typeof(IDataPersistenceService)) as IDataPersistenceService;
                IDataRegistrationService idq = ApplicationContext.CurrentContext.GetService(typeof(IDataRegistrationService))as IDataRegistrationService;

                // Fetch by the primary id
                var id = person.Id;
                
                var queryFilter = new QueryEvent();
                var registrationFilter = new RegistrationEvent();
                registrationFilter.Add(new Person() { Id = person.Id }, "SUBJ", HealthServiceRecordSiteRoleType.SubjectOf, null);
                queryFilter.Add(registrationFilter, "SUBJ", HealthServiceRecordSiteRoleType.SubjectOf, null);

                var regEvts = idq.QueryRecord(queryFilter);
                if (regEvts.Length == 1)
                {
                    regEvt = idp.GetContainer(regEvts[0], true) as RegistrationEvent;
                    person = regEvt.FindComponent(HealthServiceRecordSiteRoleType.SubjectOf) as Person;
                }
                else
                    dtls.Add(new ResultDetail(ResultDetailType.Warning, "Could not load related registration event. Data may be incomplete", null, null));
            }

            //retVal.Id = person.Id.ToString();
            retVal.VersionId = person.VersionId.ToString();
            retVal.Active = (person.Status & (StatusType.Active | StatusType.Completed)) != 0;
            retVal.Timestamp = person.Timestamp;

           
            // Identifiers
            foreach(var itm in person.AlternateIdentifiers)
                retVal.Identifier.Add(base.ConvertDomainIdentifier(itm));

            // Birth order
            if (person.BirthOrder.HasValue)
                retVal.MultipleBirth = (FhirInt)person.BirthOrder.Value;

            // Names
            if(person.Names != null)
                foreach (var name in person.Names)
                    retVal.Name.Add(base.ConvertNameSet(name));
            // Addresses
            if (person.Addresses != null)
                foreach (var addr in person.Addresses)
                    retVal.Address.AddRange(base.ConvertAddressSet(addr));
            // Telecom
            if (person.TelecomAddresses != null)
                foreach (var tel in person.TelecomAddresses)
                    retVal.Telecom.AddRange(base.ConvertTelecom(tel));
            // Gender
            if (person.GenderCode != null)
            {
                retVal.Gender = base.ConvertPrimitiveCode<AdministrativeGender>(person.GenderCode);
            }
            else
                retVal.Gender = base.ConvertPrimitiveCode<MARC.Everest.DataTypes.NullFlavor>("UNK");

            // DOB
            if(person.BirthTime != null)
                retVal.BirthDate = new Date(person.BirthTime.Value) { Precision = HackishCodeMapping.Lookup(HackishCodeMapping.DATE_PRECISION, person.BirthTime.Precision) };

            // Deceased time
            if (person.DeceasedTime != null)
                retVal.Deceased = new Date(person.DeceasedTime.Value) { Precision = HackishCodeMapping.Lookup(HackishCodeMapping.DATE_PRECISION, person.DeceasedTime.Precision) };
            
            // Marital status
            if (person.MaritalStatus != null)
                retVal.MaritalStatus = base.ConvertCode(person.MaritalStatus);

            // Photograph?
            var photoExtensions = person.FindAllExtensions(o=>o.Name == "FhirPhotographResourceAttachment" && o.PropertyPath == "Patient.Photo");
            if(photoExtensions != null && photoExtensions.Count() != 0)
                foreach(var pext in photoExtensions)
                    retVal.Photo.Add(pext.Value as Attachment);
             
            // Contacts
            var relationships = person.FindAllComponents(HealthServiceRecordSiteRoleType.RepresentitiveOf);
            if(relationships != null && relationships.Count > 0)
                foreach (PersonalRelationship rel in relationships)
                {
                    // Is this related person a mother? 
                    if (rel.RelationshipKind == "MTH")
                        retVal.Extension.Add(ExtensionUtil.CreateMothersMaidenNameExtension(base.ConvertNameSet(rel.LegalName)));

                    // Construct resource
                    RelatedPerson relatedPerson = new RelatedPerson()
                    {
                        Name = rel.LegalName != null ? base.ConvertNameSet(rel.LegalName) : null, 
                        Address = rel.PerminantAddress != null ? base.ConvertAddressSet(rel.PerminantAddress).FirstOrDefault() : null,
                        Patient = Resource<Patient>.CreateLocalResourceReference(retVal),
                        Id = rel.Id.ToString(),
                        SuppressText = true
                    };
                    //relatedPerson.MakeIdRef();

                    foreach (var relIdentifier in rel.AlternateIdentifiers)
                        relatedPerson.Identifier.Add(base.ConvertDomainIdentifier(relIdentifier));

                    if (rel.RelationshipKind != null)
                    {
                        relatedPerson.Relationship = base.ConvertPrimitiveCode<PersonalRelationshipRoleType>(rel.RelationshipKind);
                        relatedPerson.Relationship.GetPrimaryCode().System = new Uri("http://hl7.org/fhir/v3/vs/RoleCode");
                    }
                    if (rel.GenderCode != null)
                        relatedPerson.Gender = base.ConvertPrimitiveCode<AdministrativeGender>(person.GenderCode);
                    else
                        relatedPerson.Gender = base.ConvertPrimitiveCode<MARC.Everest.DataTypes.NullFlavor>("UNK");

                    foreach (TelecommunicationsAddress tel in rel.TelecomAddresses)
                        relatedPerson.Telecom.AddRange(base.ConvertTelecom(tel));

                    // Add the contained resource
                    retVal.AddContainedResource(relatedPerson);
                    //contactInfo.Extension.Add(ExtensionUtil.CreateResourceLinkExtension(processResult));
                    
                }

          
            // Original text?
            var originalTextExtension = person.FindAllExtensions(o => o.Name == "OriginalText" && o.PropertyPath == "Patient.Text");
            if (originalTextExtension != null && originalTextExtension.Count() > 0)
            {
                foreach (var oext in originalTextExtension)
                    retVal.Text.Extension.Add(ExtensionUtil.CreateOriginalTextExtension(oext.Value as Attachment));
            }

            // Organizations that have registered this user
            var handler = MARC.HI.EHRS.SVC.Messaging.FHIR.Handlers.FhirResourceHandlerUtil.GetResourceHandler("Organization");
            if (handler != null && regEvt != null)
            {
                var org = regEvt.FindComponent(HealthServiceRecordSiteRoleType.ResponsibleFor | HealthServiceRecordSiteRoleType.PlaceOfRecord);
                
                if(org is RepositoryDevice)
                {
                    var repDevice = org as RepositoryDevice;
                    // todo:
                }
                else if (org is HealthcareParticipant)
                {
                    var repHealthcare = org as HealthcareParticipant;
                    var result = handler.Read(repHealthcare.Id.ToString(), null);
                    if (result == null || result.Results.Count == 0)
                        ;
                    else
                        retVal.ManagingOrganization = Resource<Organization>.CreateResourceReference(result.Results[0] as Organization, WebOperationContext.Current.IncomingRequest.UriTemplateMatch.BaseUri);
                }
            }

            // replacements are links
            //var rplc = person.FindAllComponents(HealthServiceRecordSiteRoleType.ReplacementOf);
            //if(rplc != null)
            //    foreach (var rpl in rplc)
            //        retVal.Link.Add(Resource.CreateResourceReference(this.ProcessComponent(rpl as Person, dtls) as Patient, WebOperationContext.Current.IncomingRequest.UriTemplateMatch.BaseUri));
            // other ids?
            if (person.OtherIdentifiers != null)
            {
                foreach (var id in person.OtherIdentifiers)
                {
                    // Create the "otherId" extension
                    var ext = ExtensionUtil.CreateOtherIdExtension(id);
                    var propertyPath = String.Format("OtherIdentifiers[{0}{1}]", id.Value.Domain, id.Value.Identifier);
                    var extId = person.FindExtension(o => o.Name == "AssigningIdOrganizationId" && o.PropertyPath == propertyPath);
                    var extName = person.FindExtension(o => o.Name == "AssigningIdOrganizationName" && o.PropertyPath == propertyPath);
                    var extCode = person.FindExtension(o => o.Name == "AssigningIdOrganizationCode" && o.PropertyPath == propertyPath);

                    if (extId != null)
                        ext.Extension.Add(ExtensionUtil.CreateOtherIdScopingOrganizationIdExtension(extId));
                    if (extName != null)
                        ext.Extension.Add(ExtensionUtil.CreateOtherIdScopingOrganizationNameExtension(extName));
                    if (extCode != null)
                        ext.Extension.Add(ExtensionUtil.CreateOtherIdScopingOrganizationCodeExtension(extCode));

                    retVal.Extension.Add(ext);
                }
            }

            if (person.Language != null)
                foreach (var p in person.Language)
                    retVal.Language.Add(new CodeableConcept(new Uri("http://hl7.org/fhir/sid/iso-639-1"), p.Language));
                        

            // Confidence has moved 
            var confidence = person.FindComponent(HealthServiceRecordSiteRoleType.ComponentOf | HealthServiceRecordSiteRoleType.CommentOn) as QueryParameters;
            if (confidence != null)
                retVal.Attributes.Add(new ConfidenceAttribute() { Confidence = (decimal)confidence.Confidence });

            return retVal;
        }

        #endregion

        /// <summary>
        /// Delete a patient resource simply nullifies it
        /// </summary>
        public override FhirOperationResult Delete(string id, DataPersistenceMode mode)
        {
            FhirOperationResult result = new FhirOperationResult();
                
            // Registration event for the delete
            RegistrationEvent regEvt = new RegistrationEvent()
            {
                EffectiveTime = new TimestampSet() { Parts = new List<TimestampPart>() { new TimestampPart(TimestampPart.TimestampPartType.Standlone, DateTime.Now, "F") } },
                EventClassifier = RegistrationEventType.Register,
                EventType = new CodeValue("DELETE"),
                LanguageCode = ApplicationContext.ConfigurationService.JurisdictionData.DefaultLanguageCode,
                Mode = RegistrationEventType.Nullify,
                Status = StatusType.Completed,
                Timestamp = DateTime.Now
            };

            // Target
            var psn = new Person() { Status = StatusType.Terminated };
            psn.AlternateIdentifiers = new List<DomainIdentifier>();
            psn.AlternateIdentifiers.Add(new DomainIdentifier()
            {
                Domain = ApplicationContext.ConfigurationService.OidRegistrar.GetOid("CR_CID").Oid,
                Identifier = id
            });
            regEvt.Add(psn, "SUBJ", HealthServiceRecordSiteRoleType.SubjectOf, null);

            // Execute
            try
            {
                DataUtil.Update(regEvt, id, DataPersistenceMode.Production, result.Details);
                result.Outcome = ResultCode.Accepted;
            }
            catch (MissingPrimaryKeyException e)
            {
                result.Details.Add(new ResultDetail(ResultDetailType.Error, e.Message, e));
                result.Outcome = ResultCode.TypeNotAvailable;
            }
            catch (Exception e)
            {
                result.Details.Add(new ResultDetail(ResultDetailType.Error, e.Message, e));
                result.Outcome = ResultCode.Error;
            }

            return result;
        }
    }
}
