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

namespace MARC.HI.EHRS.CR.Messaging.FHIR.Processors
{
    /// <summary>
    /// Message processor for patients
    /// </summary>
    [Profile(ProfileId = "pix-fhir")]
    [ResourceProfile(Resource = typeof(Patient), Name = "Client registry patient profile")]
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
        [SearchParameterProfile(Name = "provider.identifier", Type = "token", Description = "One of the organizations to which this person is a patient (only supports OR)")]
        public override Util.DataUtil.ClientRegistryFhirQuery ParseQuery(System.Collections.Specialized.NameValueCollection parameters, List<IResultDetail> dtls)
        {
            ITerminologyService termSvc = ApplicationContext.CurrentContext.GetService(typeof(ITerminologyService)) as ITerminologyService;

            Util.DataUtil.ClientRegistryFhirQuery retVal = base.ParseQuery(parameters, dtls);

            var subjectFilter = new Person();
            RegistrationEvent queryFilter = new RegistrationEvent();

            //MARC.HI.EHRS.SVC.Core.DataTypes.AddressSet addressFilter = null;
            MARC.HI.EHRS.SVC.Core.DataTypes.NameSet nameFilter = null;

            for(int i = 0; i < parameters.Count; i++)
                try
                {
                        switch (parameters.GetKey(i))
                        {
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
                                        actualIdParm.AppendFormat("{0},", itm);
                                    }

                                    retVal.ActualParameters.Add("_id", actualIdParm.ToString());
                                    break;
                                }
                            case "active":
                                subjectFilter.Status = Boolean.Parse(parameters.GetValues(i)[0]) ? StatusType.Active | StatusType.Completed : StatusType.Obsolete | StatusType.Nullified | StatusType.Cancelled | StatusType.Aborted;
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

                                    var dValue = new DateOnly() { Value = value };
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
                            case "gender":
                                {
                                    string value = parameters.GetValues(i)[0].ToUpper();
                                    if (value.Contains(",") || parameters.GetValues(i).Length > 1)
                                        dtls.Add(new InsufficientRepetitionsResultDetail(ResultDetailType.Warning, "Cannot perform OR or AND on gender", null));

                                    var gCode = MessageUtil.CodeFromToken(value);
                                    if (gCode.Code == "UNK") // Null Flavor
                                        retVal.ActualParameters.Add("gender", String.Format("http://hl7.org/fhir/v3/NullFlavor!UNK"));
                                    else if (!new List<String>() { "M", "F", "UN" }.Contains(gCode.Code))
                                        dtls.Add(new VocabularyIssueResultDetail(ResultDetailType.Error, String.Format("Cannot find code {0} in administrative gender", gCode.Code), null));

                                    else
                                    {
                                        subjectFilter.GenderCode = gCode.Code;
                                        retVal.ActualParameters.Add("gender", String.Format("http://hl7.org/fhir/v3/AdministrativeGender!{0}", gCode.Code));
                                    }
                                    break;
                                }
                            case "identifier":
                                {
                                    if (parameters.GetValues(i).Length > 1)
                                        dtls.Add(new InsufficientRepetitionsResultDetail(ResultDetailType.Warning, "Cannot perform AND on identifiers", null));

                                    if (subjectFilter.AlternateIdentifiers == null)
                                        subjectFilter.AlternateIdentifiers = new List<SVC.Core.DataTypes.DomainIdentifier>();
                                    StringBuilder actualIdParm = new StringBuilder();
                                    foreach (var val in parameters.GetValues(i)[0].Split(','))
                                    {
                                        var domainId = MessageUtil.IdentifierFromToken(val);
                                        if (String.IsNullOrEmpty(domainId.Domain))
                                        {
                                            dtls.Add(new NotImplementedResultDetail(ResultDetailType.Error, "'identifier' must carry system, cannot perform generic query on identifiers", null, null));
                                            continue;
                                        }
                                        subjectFilter.AlternateIdentifiers.Add(domainId);
                                        actualIdParm.AppendFormat("{0},", val);
                                    }

                                    if (actualIdParm.Length > 0)
                                        retVal.ActualParameters.Add("identifier", actualIdParm.ToString());
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
                                        retVal.ActualParameters.Add("provider.identifier", String.Format("{0}!", MessageUtil.TranslateDomain(did.Domain)));
                                    }
                                    break;

                                }
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

            queryFilter.Add(subjectFilter, "SUBJ", HealthServiceRecordSiteRoleType.SubjectOf, null);
            retVal.Filter = queryFilter;

            return retVal;
        }

        /// <summary>
        /// Process resource
        /// </summary>
        public override System.ComponentModel.IComponent ProcessResource(ResourceBase resource, List<IResultDetail> dtls)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Process components
        /// </summary>
        /// TODO: make this more robust
        [ElementProfile(Property = "Link", MaxOccurs = 0, Comment = "This registry only supports links through 'contact' relationships")]
        [ElementProfile(Property = "Animal", MaxOccurs = 0, Comment = "This registry only supports human patients")]
        [ElementProfile(Property = "Contact.Organization", MaxOccurs = 0, Comment = "This registry only supports relationships with 'Person' objects")]
        [ElementProfile(Property = "Photo", MaxOccurs = 0, Comment = "This registry does not support the storage of photographs directly")]
        [ElementProfile(Property = "Gender", Binding = typeof(AdministrativeGender), Comment = "Since this FHIR registry is also a PIX manager and HL7v3 client registry the v3 AdministrativeGender code set used internally has been referenced")]
        [ElementProfile(Property = "Identifier", MinOccurs = 1, MaxOccurs = -1, Comment = "PIX Manager logic requires at least one identifier. When submitting a resource the @system attribute must match the referenced provider identifier's @system attribute (i.e. you may only register new identifiers for systems which you are the registered creator)")]
        [ElementProfile(Property = "Deceased", ValueType = typeof(Date), Comment = "Only date values are supported for deceased indication. Boolean will be translated to a non-zero date")]
        [ElementProfile(Property = "MultipleBirth", ValueType = typeof(FhirInt), Comment = "Only multiple birth number is supported. Boolean will be translated to a non-zero value")]
        [ElementProfile(Property = "MaritalStatus", RemoteBinding = null, Comment = "Marital status can be drawn from any code system")]
        [ElementProfile(Property = "Extension", Comment = "Additional attributes which could not be mapped to FHIR will be placed in the \"Extension\" element")]
        [ElementProfile(Property = "Language.Value", RemoteBinding = "http://hl7.org/fhir/sid/iso-639-1", Comment = "Language codes should be drawn from ISO-639-1 , ISO-639-3 is acceptable but will be translated")]
        [ElementProfile(Property = "Language.Mode", Binding = typeof(LanguageAbilityMode), Comment = "Language mode is restricted to ESP and EWR")]
        [ElementProfile(Property = "Language.ProficiencyLevel", Binding = typeof(LanguageAbilityProficiency), Comment = "Language proficiency will be either E or not provided")]
        [ElementProfile(Property = "Language.Preference", Comment = "Treated as indicator. Will only appear if set to 'true'")]
        [ElementProfile(Property = "Text", Comment = "Will be auto-generated, any provided will be stored and represented as an extension \"originalText\"")]
        public override ResourceBase ProcessComponent(System.ComponentModel.IComponent component, List<IResultDetail> dtls)
        {
            // Setup references
            Patient retVal = new Patient();
            Person person = component as Person;
            RegistrationEvent regEvt = component.Site != null ? component.Site.Container as RegistrationEvent: null;

            // Load registration event
            if (regEvt == null)
            {
                IDataPersistenceService idp = ApplicationContext.CurrentContext.GetService(typeof(IDataPersistenceService)) as IDataPersistenceService;
                IDataRegistrationService idq = ApplicationContext.CurrentContext.GetService(typeof(IDataRegistrationService))as IDataRegistrationService;

                var queryFilter = new RegistrationEvent();
                queryFilter.Add(component, "SUBJ", HealthServiceRecordSiteRoleType.SubjectOf, null);
                var regEvts = idq.QueryRecord(queryFilter);
                if (regEvts.Length == 1)
                {
                    regEvt = idp.GetContainer(regEvts[0], true) as RegistrationEvent;
                    person = regEvt.FindComponent(HealthServiceRecordSiteRoleType.SubjectOf) as Person;
                }
                else
                    dtls.Add(new ResultDetail(ResultDetailType.Warning, "Could not load related registration event. Data may be incomplete", null, null));
            }

            retVal.Id = person.Id.ToString();
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
                retVal.Gender = base.ConvertPrimitiveCode<AdministrativeGender>(person.GenderCode);
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
                    Contact contactInfo = new Contact();
                    // Is there a person component here
                    IComponent relatedTarget = rel.FindComponent(HealthServiceRecordSiteRoleType.SubjectOf) as IComponent;
                    if (relatedTarget != null)
                    {
                        var processor = FhirMessageProcessorUtil.GetComponentProcessor(relatedTarget.GetType());
                        var processResult = processor.ProcessComponent(relatedTarget, dtls);

                        if (processResult is Patient)
                        {
                            var pat = processResult as Patient;
                            contactInfo.Name = pat.Name;
                            contactInfo.Address = pat.Address;
                            contactInfo.Gender = pat.Gender;
                            contactInfo.Telecom = pat.Telecom;
                        }
                        else if (processResult is Practictioner)
                        {
                            var prac = processResult as Practictioner;
                            contactInfo.Name = prac.Name;
                            contactInfo.Address = prac.Address;
                            contactInfo.Gender = prac.Gender;
                            contactInfo.Telecom = prac.Telecom;
                        }
                        contactInfo.Extension.Add(ExtensionUtil.CreateResourceLinkExtension(processResult));
                    }
                    
                    contactInfo.Relationship = new List<CodeableConcept>() {
                        new CodeableConcept(new Uri("http://hl7.org/fhir/vs/patient-contact-relationship"), HackishCodeMapping.ReverseLookup(HackishCodeMapping.RELATIONSHIP_KIND, rel.RelationshipKind))
                    };
                    // Now add an extension as the relationship kind is more detailed in our expression
                    contactInfo.Relationship[0].Extension.Add(ExtensionUtil.CreateRelationshipExtension(rel.RelationshipKind));
                    retVal.Contact.Add(contactInfo);
                }

          
            // Original text?
            var originalTextExtension = person.FindAllExtensions(o => o.Name == "OriginalText" && o.PropertyPath == "Patient.Text");
            if (originalTextExtension != null && originalTextExtension.Count() > 0)
                foreach (var oext in originalTextExtension)
                    retVal.Text.Extension.Add(ExtensionUtil.CreateOriginalTextExtension(oext.Value as FhirString));
            
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
                        retVal.Provider = Resource<Organization>.CreateResourceReference(result.Results[0] as Organization, WebOperationContext.Current.IncomingRequest.UriTemplateMatch.BaseUri);
                }
            }

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


            // Confidence?
            var confidence = person.FindComponent(HealthServiceRecordSiteRoleType.ComponentOf | HealthServiceRecordSiteRoleType.CommentOn) as QueryParameters;
            if (confidence != null)
            {
                retVal.Extension.Add(ExtensionUtil.CreateConfidenceExtension(confidence));
                retVal.Extension.Add(ExtensionUtil.CreateMatchAlgorithmExtension(confidence));
            }

            return retVal;
        }

        #endregion
    }
}
