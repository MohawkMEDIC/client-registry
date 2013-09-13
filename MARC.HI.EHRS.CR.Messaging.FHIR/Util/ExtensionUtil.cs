using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MARC.HI.EHRS.SVC.Core.DataTypes;
using MARC.Everest.DataTypes;
using MARC.Everest.Attributes;
using MARC.HI.EHRS.SVC.Messaging.FHIR.DataTypes;
using MARC.HI.EHRS.SVC.Messaging.FHIR.Attributes;
using MARC.HI.EHRS.SVC.Messaging.FHIR.Resources;
using MARC.Everest.RMIM.CA.R020402.Vocabulary;
using MARC.HI.EHRS.CR.Messaging.FHIR.Processors;
using MARC.HI.EHRS.SVC.Messaging.FHIR;
using MARC.Everest.DataTypes.Interfaces;
using System.ServiceModel.Web;
using MARC.Everest.Connectors;
using System.Xml.Serialization;
using System.Collections;
using MARC.Everest.Exceptions;

namespace MARC.HI.EHRS.CR.Messaging.FHIR.Util
{

    /// <summary>
    /// FHIR Extensions utility
    /// </summary>
    [Profile(ProfileId = "pix-fhir", Name = "PIX Manager FHIR Profile", Import = "svccore")]    
    public static class ExtensionUtil
    {

        public static Uri GetExtensionNameUrl(String extension)
        {
            return new Uri(String.Format("{0}/Profile/@pix-fhir#{1}", WebOperationContext.Current.IncomingRequest.UriTemplateMatch.BaseUri, extension));
        }

        public static Uri GetValueSetUrl(String valueSet)
        {
            return new Uri(String.Format("{0}/ValueSet/@{1}", WebOperationContext.Current.IncomingRequest.UriTemplateMatch.BaseUri, valueSet));
        }

        /// <summary>
        /// Create an AD extension
        /// </summary>
        [ExtensionDefinition(Name = "addressPart", HostType = typeof(Patient), ValueType = typeof(FhirString), Property = "Address", MustSupport = false, MustUnderstand = false, ShortDescription = "Additional address information not classified by FHIR parts")]
        [ExtensionDefinition(Name = "v3-addressPartTypes", HostType = typeof(Patient), ValueType = typeof(Coding), Property = "Address.Extension", Binding = typeof(AddressPartType), MustSupport = false, MustUnderstand = false, ShortDescription = "Qualifies the unclassified address parts")]
        public static Extension CreateADExtension(AddressPart part)
        {
            AddressPartType v3PartType = (AddressPartType)Enum.Parse(typeof(AddressPartType), part.PartType.ToString());
            string wireCode = MARC.Everest.Connectors.Util.ToWireFormat(v3PartType);
            
            return new Extension()
            {
                Url = GetExtensionNameUrl("addressPart"),
                Value = new FhirString(part.AddressValue),
                Extension = new List<Extension>() {
                                        new Extension() {
                                            Url =  GetExtensionNameUrl("v3-addressPartTypes"),
                                            Value = new Coding(typeof(AddressPartType).GetValueSetDefinition(), wireCode)
                                        }
                                    }
            };
        }

        /// <summary>
        /// Parse an AD extension
        /// </summary>
        public static List<AddressPart> ParseADExtension(List<Extension> extension, List<IResultDetail> dtls)
        {
            try
            {
                List<AddressPart> retVal = new List<AddressPart>();
                foreach (var adext in extension.FindAll(o => o.Url.Value == GetExtensionNameUrl("addressPart")))
                {
                    AddressPart ap = new AddressPart();
                    ap.AddressValue = (adext.Value as FhirString);

                    // Find the extension identifying the type
                    var typeExt = adext.Extension.Find(o => o.Url == GetExtensionNameUrl("v3-addressPartTypes"));
                    var typeCode = typeExt != null ? typeExt.Value as Coding : null as Coding;
                    if (typeCode != null && typeCode.System == typeof(AddressPartType).GetValueSetDefinition())
                        ap.PartType = (AddressPart.AddressPartType)MARC.Everest.Connectors.Util.Convert<AddressPartType>(typeCode.Code);
                    else
                        dtls.Add(new VocabularyIssueResultDetail(ResultDetailType.Error, String.Format("Extended address parts must carry a classification from code system {0}", typeof(AddressPartType).GetValueSetDefinition()), null, null));
                }
                return retVal;
            }
            catch (VocabularyException e)
            {
                dtls.Add(new VocabularyIssueResultDetail(ResultDetailType.Error, String.Format(ApplicationContext.LocalizationService.GetString("FHIR007"), GetExtensionNameUrl("v3-addressPartTypes"), e.Message), e));
                return new List<AddressPart>();
            }
            
        }

        /// <summary>
        /// Create an AD extension
        /// </summary>
        [ExtensionDefinition(Name = "addressUse", HostType = typeof(Patient), Property = "Address.Use", ValueType = typeof(Coding), Binding = typeof(PostalAddressUse), MustUnderstand = false, MustSupport = false, ShortDescription = "Used when the address use is not defined in FHIR vocabulary")]
        public static Extension CreateADUseExtension(AddressSet.AddressSetUse use)
        {
            if (use == AddressSet.AddressSetUse.Search)
                return CreateNullElementExtension(NullFlavor.Other);

            PostalAddressUse v3Use = (PostalAddressUse)Enum.Parse(typeof(PostalAddressUse), use.ToString());
            string wireCode = MARC.Everest.Connectors.Util.ToWireFormat(v3Use);

            return new Extension()
            {
                Url = GetExtensionNameUrl("addressUse"),
                Value = new Coding(typeof(PostalAddressUse).GetValueSetDefinition(), wireCode)
            };
        }

        /// <summary>
        /// Parse an AD Use extension
        /// </summary>
        public static AddressSet.AddressSetUse ParseADUseExtension(List<Extension> extension, List<IResultDetail> dtls)
        {
            try
            {
                // Now fun part parse
                AddressSet.AddressSetUse value = 0;
                foreach (var ext in extension.FindAll(o => o.Url == GetExtensionNameUrl("addressUse")))
                {
                    var coding = ext.Value as Coding;
                    if (coding == null)
                        dtls.Add(new VocabularyIssueResultDetail(ResultDetailType.Error, String.Format("Address use extension must carry a coding from system {0}", typeof(PostalAddressUse).GetValueSetDefinition()), null, null));
                    else
                        value |= (AddressSet.AddressSetUse)Enum.Parse(typeof(AddressSet.AddressSetUse), MARC.Everest.Connectors.Util.Convert<PostalAddressUse>(coding.Code).ToString());
                }
                return value;
            }
            catch (VocabularyException e)
            {
                dtls.Add(new VocabularyIssueResultDetail(ResultDetailType.Error, String.Format(ApplicationContext.LocalizationService.GetString("FHIR007"), GetExtensionNameUrl("addressUse"), e.Message), e));
                return 0;
            }

        }

        /// <summary>
        /// Telecommunications use extension
        /// </summary>
        [ExtensionDefinition(Name = "telecommunicationAddressUse", HostType = typeof(Patient), Property = "Telecom.Use", ValueType = typeof(Coding), Binding = typeof(MARC.Everest.DataTypes.Interfaces.TelecommunicationAddressUse), MustSupport = false, MustUnderstand = false, ShortDescription = "Used when the resource's telecommunications use cannot be mapped to FHIR vocabulary")]
        public static Extension CreateTELUseExtension(Everest.DataTypes.Interfaces.TelecommunicationAddressUse telecommunicationAddressUse)
        {
            return new Extension()
            {
                Url = GetExtensionNameUrl("telecommunicationAddressUse"),
                Value = new Coding(typeof(TelecommunicationAddressUse).GetValueSetDefinition(), MARC.Everest.Connectors.Util.ToWireFormat(telecommunicationAddressUse))
            };
        }

        /// <summary>
        /// Parse a TEL use extension
        /// </summary>
        /// <param name="list"></param>
        /// <param name="dtls"></param>
        /// <returns></returns>
        internal static string ParseTELUseExtension(List<Extension> extensions, List<IResultDetail> dtls)
        {
            try
            {
                StringBuilder retVal = new StringBuilder();
                foreach (var ext in extensions.FindAll(o => o.Url == GetExtensionNameUrl("telecommunicationAddressUse")))
                {
                    var codeValue = ext.Value as Coding;
                    if (codeValue != null || codeValue.System != typeof(TelecommunicationAddressUse).GetValueSetDefinition())
                        dtls.Add(new VocabularyIssueResultDetail(ResultDetailType.Error, String.Format("Telecom use extension must carry a coding from system {0}", typeof(TelecommunicationAddressUse).GetValueSetDefinition()), null, null));
                    else
                        retVal.AppendFormat(" {0}", codeValue.Code);
                }
                return retVal.ToString();
            }
            catch (VocabularyException e)
            {
                dtls.Add(new VocabularyIssueResultDetail(ResultDetailType.Error, String.Format(ApplicationContext.LocalizationService.GetString("FHIR007"),  GetExtensionNameUrl("telecommunicationAddressUse"), e.Message), e));
                return String.Empty;
            }

        }


        /// <summary>
        /// Confidence of the match
        /// </summary>
        [ExtensionDefinition(Name = "subjectObservation", HostType = typeof(Patient), ValueType = typeof(FhirInt), MustUnderstand = false, MustSupport = false, ShortDescription = "In a query: Identifies the confidence of the returned match")]
        public static Extension CreateConfidenceExtension(Core.ComponentModel.QueryParameters confidence)
        {

            return new Extension()
            {
                Url = GetExtensionNameUrl("subjectObservation"),
                Value = new FhirInt((int)(confidence.Confidence * 100))
            };
        }

        /// <summary>
        /// Create a match algorithm extension
        /// </summary>
        [ExtensionDefinition(Name = "subjectObservationMatchAlgorithm", HostType = typeof(Patient), ValueType = typeof(Coding), MustUnderstand = false, MustSupport = false, Binding = typeof(ObservationQueryMatchType), ShortDescription = "In a query: Identifies the algorithm used to perform the match")]
        public static Extension CreateMatchAlgorithmExtension(Core.ComponentModel.QueryParameters confidence)
        {
            return new Extension() {
                Url = GetExtensionNameUrl("subjectObservationMatchAlgorithm"),
                        Value = 
                            (confidence.MatchingAlgorithm & Core.ComponentModel.MatchAlgorithm.Soundex) != 0 ?
                                new Coding(typeof(ObservationQueryMatchType).GetValueSetDefinition(), "PHCM") { Display = "phonetic match" } :
                                new Coding(typeof(ObservationQueryMatchType).GetValueSetDefinition(), "PTNM") { Display = "pattern match" }
                    };
        }


        /// <summary>
        /// Create a personal relationship code extension
        /// </summary>
        [ExtensionDefinition(Name = "personalRelationshipRoleType", Binding = typeof(PersonalRelationshipRoleType), Property = "Contact.Relationship", HostType = typeof(Patient), ValueType = typeof(Coding), MustUnderstand = false, MustSupport = false, ShortDescription = "Identifies a granular level of relationship role")]
        public static Extension CreateRelationshipExtension(string relationshipKind)
        {
            return new Extension()
            {
                Url = GetExtensionNameUrl("personalRelationshipRoleType"),
                Value = new Coding(
                    typeof(PersonalRelationshipRoleType).GetValueSetDefinition(),
                    relationshipKind
                )
            };
        }

        /// <summary>
        /// Parse a relationship extension
        /// </summary>
        internal static string ParseRelationshipExtension(List<Extension> extensions, List<IResultDetail> dtls)
        {
            try
            {
                var rolExt = extensions.Find(o => o.Url.Value == GetExtensionNameUrl("personalRelationshipRoleType"));
                if (rolExt != null)
                {
                    var codeValue = rolExt.Value as Coding;
                    if (codeValue == null || codeValue.System != typeof(PersonalRelationshipRoleType).GetValueSetDefinition())
                        dtls.Add(new VocabularyIssueResultDetail(ResultDetailType.Error, String.Format("Personal relationship role type extension must carry a coding from system {0}", typeof(PersonalRelationshipRoleType).GetValueSetDefinition()), null, null));
                    else
                    {
                        return MARC.Everest.Connectors.Util.ToWireFormat(MARC.Everest.Connectors.Util.Convert<PersonalRelationshipRoleType>(codeValue.Code));
                    }
                }
                return string.Empty;
            }
            catch (VocabularyException e)
            {
                dtls.Add(new VocabularyIssueResultDetail(ResultDetailType.Error, String.Format(ApplicationContext.LocalizationService.GetString("FHIR007"), GetExtensionNameUrl("personalRelationshipRoleType"), e.Message), e));
                return String.Empty;
            }
        }

        /// <summary>
        /// Create an entity name use extension
        /// </summary>
        [ExtensionDefinition(Name = "nameUse", HostType = typeof(Patient), ValueType = typeof(Coding), Binding=typeof(EntityNameUse), Property = "Name", MustSupport = false, MustUnderstand = false, ShortDescription = "The original entityNameUse of the name when no FHIR code mapping is available")]
        public static Extension CreatePNUseExtension(NameSet.NameSetUse nameSetUse)
        {
            EntityNameUse entityNameUse = EntityNameUse.Alphabetic;
            if (Enum.TryParse<EntityNameUse>(nameSetUse.ToString(), out entityNameUse))
                return new Extension()
                {
                    Url = GetExtensionNameUrl("nameUse"),
                    Value = new Coding(
                        typeof(EntityNameUse).GetValueSetDefinition(),
                        MARC.Everest.Connectors.Util.ToWireFormat(entityNameUse)
                    )
                };
            return null;
                    
        }


        /// <summary>
        /// Parse an PN Use extension
        /// </summary>
        public static NameSet.NameSetUse ParsePNUseExtension(List<Extension> extension, List<IResultDetail> dtls)
        {
            // Now fun part parse
            try
            {
                NameSet.NameSetUse value = 0;
                foreach (var ext in extension.FindAll(o => o.Url == GetExtensionNameUrl("nameUse")))
                {
                    var coding = ext.Value as Coding;
                    if (coding == null)
                        dtls.Add(new NotSupportedChoiceResultDetail(ResultDetailType.Error, "Name use extension must carry a value of type coding", null));
                    else if (coding.System != typeof(EntityNameUse).GetValueSetDefinition())
                        dtls.Add(new VocabularyIssueResultDetail(ResultDetailType.Error, String.Format("Name use extension must carry a value drawn from system {0}", typeof(EntityNameUse).GetValueSetDefinition()), null, null));
                    else
                        value |= (NameSet.NameSetUse)Enum.Parse(typeof(NameSet.NameSetUse), MARC.Everest.Connectors.Util.Convert<EntityNameUse>(coding.Code).ToString());
                }
                return value;
            }
            catch (VocabularyException e)
            {
                dtls.Add(new VocabularyIssueResultDetail(ResultDetailType.Error, String.Format(ApplicationContext.LocalizationService.GetString("FHIR007"), GetExtensionNameUrl("personalRelationshipRoleType"), e.Message), e));
                return 0;
            }
        }

        /// <summary>
        /// Represents a null element 
        /// </summary>
        /// <param name="nullFlavor"></param>
        /// <returns></returns>
        [ExtensionDefinition(Name = "nullElementReason", HostType = typeof(Patient), ValueType = typeof(Coding), Binding = typeof(NullFlavor), Property = "Name", MustSupport = false, MustUnderstand = false, ShortDescription = "Used when an element value is not mappable to FHIR")]
        [ExtensionDefinition(Name = "nullElementReason", HostType = typeof(Patient), ValueType = typeof(Coding), Binding = typeof(NullFlavor), Property = "Address", MustSupport = false, MustUnderstand = false, ShortDescription = "Used when an element value is not mappable to FHIR")]
        public static Extension CreateNullElementExtension(NullFlavor nullFlavor)
        {
            return new Extension()
            {
                Url = GetExtensionNameUrl("nullElementReason"),
                Value = new Coding(
                    typeof(NullFlavor).GetValueSetDefinition(),
                    MARC.Everest.Connectors.Util.ToWireFormat(nullFlavor)
                )
            };
        }

        /// <summary>
        /// Create an original text extension
        /// </summary>
        [ExtensionDefinition(Name = "originalText", HostType = typeof(Patient), ValueType = typeof(FhirString), Property = "Text", MustSupport = false, MustUnderstand = false, ShortDescription = "Stores the original text entry for a resource")]
        [ExtensionDefinition(Name = "originalText", HostType = typeof(Organization), ValueType = typeof(FhirString), Property = "Text", MustSupport = false, MustUnderstand = false, ShortDescription = "Stores the original text entry for a resource")] 
        public static Extension CreateOriginalTextExtension(Attachment value)
        {
            return new Extension()
            {
                Url = GetExtensionNameUrl("originalText"),
                Value = value
            };
        }

        /// <summary>
        /// Create an "otherId" extension
        /// </summary>
        [ExtensionDefinition(Name = "otherId", HostType = typeof(Patient), ValueType = typeof(Identifier), MustSupport = false, MustUnderstand = false, ShortDescription = "Used to convey other, non-medical identifiers related to the patient")]
        public static Extension CreateOtherIdExtension(KeyValuePair<CodeValue, DomainIdentifier> id)
        {
            return new Extension()
            {
                Url = GetExtensionNameUrl("otherId"),
                Value = new PatientMessageProcessor().ConvertDomainIdentifier(id.Value)
            };
        }

        /// <summary>
        /// OtherId Scoping org id
        /// </summary>
        [ExtensionDefinition(Name = "otherId-scopingOrganizationId", HostType = typeof(Patient), ValueType = typeof(Identifier), Property = "Extension", MustSupport = false, MustUnderstand = false, ShortDescription = "Used to convey the scoping organization's identifier for the non-medical identifier")]
        public static Extension CreateOtherIdScopingOrganizationIdExtension(Core.ComponentModel.ExtendedAttribute extId)
        {
            return new Extension()
            {
                Url = GetExtensionNameUrl("otherId-scopingOrganizationId"),
                Value = new PatientMessageProcessor().ConvertDomainIdentifier(extId.Value as DomainIdentifier)
            };
        }

        /// <summary>
        /// OtherId Scoping organization name
        /// </summary>
        [ExtensionDefinition(Name = "otherId-scopingOrganizationName", HostType = typeof(Patient), ValueType = typeof(FhirString), Property = "Extension", MustSupport = false, MustUnderstand = false, ShortDescription = "Used to convey the scoping organization's name for the non-medical identifier")]
        public static Extension CreateOtherIdScopingOrganizationNameExtension(Core.ComponentModel.ExtendedAttribute extName)
        {
            return new Extension()
            {
                Url = GetExtensionNameUrl("otherId-scopingOrganizationName"),
                Value = (FhirString)(extName.Value as String)
            };
        }

        /// <summary>
        /// OtherId Scoping organization type
        /// </summary>
        [ExtensionDefinition(Name = "otherId-scopingOrganizationType", HostType = typeof(Patient), ValueType = typeof(CodeableConcept), Property = "Extension", MustSupport = false, MustUnderstand = false, ShortDescription = "Used to convey the scoping organization type for the non-medical identifier")]
        public static Extension CreateOtherIdScopingOrganizationCodeExtension(Core.ComponentModel.ExtendedAttribute extCode)
        {
            return new Extension()
            {
                Url = GetExtensionNameUrl("otherId-scopingOrganizationType"),
                Value = new PatientMessageProcessor().ConvertCode(extCode.Value as CodeValue)
            };
        }

        /// <summary>
        /// Contact identifier
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [ExtensionDefinition(Name = "identification", HostType = typeof(Organization), ValueType = typeof(Identifier), MustSupport = false, MustUnderstand = false, ShortDescription = "Stores the identifiers for the contact person")]
        public static Extension CreateIdentificationExtension(DomainIdentifier id)
        {
            return new Extension()
            {
                Url = GetExtensionNameUrl("identification"),
                Value = new PatientMessageProcessor().ConvertDomainIdentifier(id)
            };
        }

        /// <summary>
        /// Create patient resource link extension
        /// </summary>
        [ExtensionDefinition(Name = "relatedPatient", HostType = typeof(Patient), ValueType = typeof(Resource<Patient>), Property = "Contact", MustSupport = false, MustUnderstand = false, ShortDescription = "A link to the full demographic of the patient if the relationship holder is another patient")]
        [ExtensionDefinition(Name = "relatedPractitioner", HostType = typeof(Patient), ValueType = typeof(Resource<Practictioner>), Property = "Contact", MustSupport = false, MustUnderstand = false, ShortDescription = "A link to the full demographic of the patient if the relationship holder is a practitioner")]
        [ExtensionDefinition(Name = "relatedPractitioner", HostType = typeof(Organization), ValueType = typeof(Resource<Practictioner>), Property = "ContactEntity", MustSupport = false, MustUnderstand = false, ShortDescription = "A link to the full demographic of the patient if the relationship holder is a practitioner")]
        public static Extension CreateResourceLinkExtension(ResourceBase relatedTarget)
        {
            return new Extension()
            {
                Url = GetExtensionNameUrl(String.Format("related{0}", relatedTarget.GetType().Name)),
                Value = Resource.CreateResourceReference(relatedTarget, WebOperationContext.Current.IncomingRequest.UriTemplateMatch.BaseUri)
            };
        }

        /// <summary>
        /// Verify extensions can be processed
        /// </summary>
        internal static void VerifyExtensions(Shareable resource, List<IResultDetail> dtls, String path = "")
        {
            if (resource == null)
                return;

            path = path == String.Empty ? resource.GetType().Name : path;
            foreach (var ext in resource.Extension)
            {
                bool supported = MARC.HI.EHRS.SVC.Messaging.FHIR.Util.ProfileUtil.GetProfiles().Exists(p => p.ExtensionDefinition.Exists(e => e.Code.Value == ext.Url.Value.Fragment.Replace("#","") && ext.Url.ToString().StartsWith(GetExtensionNameUrl(String.Empty).ToString())));
                if (!supported)
                    dtls.Add(new NotImplementedResultDetail(ResultDetailType.Error, String.Format(ApplicationContext.LocalizationService.GetString("FHIR006"), ext.Url, path), path));
            }
            foreach (var prop in Array.FindAll(resource.GetType().GetProperties(), p => p.GetCustomAttribute<XmlElementAttribute>() != null))
            {
                var value = prop.GetValue(resource, null);
                string fPath = String.Format("{0}.{1}", path, prop.GetCustomAttribute<XmlElementAttribute>().ElementName);
                if (value is IList)
                    for (int i = 0; i < (value as IList).Count; i++)
                        VerifyExtensions((value as IList)[i] as Shareable, dtls, String.Format("{0}[{1}]", fPath, i));
                else
                    VerifyExtensions(value as Shareable, dtls, fPath);
            }
        }
    }
}
