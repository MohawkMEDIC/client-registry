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

namespace MARC.HI.EHRS.CR.Messaging.FHIR.Util
{
    /// <summary>
    /// FHIR Extensions utility
    /// </summary>
    [Profile(ProfileId = "pix-fhir", Name = "PIX Manager FHIR Profile", Import = "svccore")]    
    public static class ExtensionUtil
    {

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
                Url = new Uri("http://cr.marc-hi.ca:8080/fhir/0.10/profile/@pix-fhir#addressPart"),
                Value = new FhirString(part.AddressValue),
                Extension = new List<Extension>() {
                                        new Extension() {
                                            Url =  new Uri("http://cr.marc-hi.ca:8080/fhir/0.10/profile/@pix-fhir#v3-addressPartTypes"),
                                            Value = new Coding(typeof(AddressPartType).GetValueSetDefinition(), wireCode)
                                        }
                                    }
            };
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
                Url = new Uri("http://cr.marc-hi.ca:8080/fhir/0.10/profile/@pix-fhir#addressUse"),
                Value = new Coding(typeof(PostalAddressUse).GetValueSetDefinition(), wireCode)
            };
        }

        /// <summary>
        /// Telecommunications use extension
        /// </summary>
        [ExtensionDefinition(Name = "telecommunicationAddressUse", HostType = typeof(Patient), Property = "Telecom.Use", ValueType = typeof(Coding), Binding = typeof(MARC.Everest.DataTypes.Interfaces.TelecommunicationAddressUse), MustSupport = false, MustUnderstand = false, ShortDescription = "Used when the resource's telecommunications use cannot be mapped to FHIR vocabulary")]
        public static Extension CreateTELUseExtension(Everest.DataTypes.Interfaces.TelecommunicationAddressUse telecommunicationAddressUse)
        {
            return new Extension()
            {
                Url = new Uri("http://cr.marc-hi.ca:8080/fhir/0.10/profile/@pix-fhir#telecommunicationAddressUse"),
                Value = new Coding(typeof(TelecommunicationAddressUse).GetValueSetDefinition(), MARC.Everest.Connectors.Util.ToWireFormat(telecommunicationAddressUse))
            };
        }

        /// <summary>
        /// Confidence of the match
        /// </summary>
        [ExtensionDefinition(Name = "subjectObservation", HostType = typeof(Patient), ValueType = typeof(FhirInt), MustUnderstand = false, MustSupport = false, ShortDescription = "In a query: Identifies the confidence of the returned match")]
        public static Extension CreateConfidenceExtension(Core.ComponentModel.QueryParameters confidence)
        {

            return new Extension()
            {
                Url = new Uri("http://cr.marc-hi.ca:8080/fhir/0.10/profile/@pix-fhir#subjectObservation"),
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
                        Url = new Uri("http://cr.marc-hi.ca:8080/fhir/0.10/profile/@pix-fhir#subjectObservationMatchAlgorithm"),
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
                Url = new Uri("http://cr.marc-hi.ca:8080/fhir/0.10/profile/@pix-fhir#personalRelationshipRoleType"),
                Value = new Coding(
                    typeof(PersonalRelationshipRoleType).GetValueSetDefinition(),
                    relationshipKind
                )
            };
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
                    Url = new Uri("http://cr.marc-hi.ca:8080/fhir/0.10/profile/@pix-fhir#nameUse"),
                    Value = new Coding(
                        typeof(EntityNameUse).GetValueSetDefinition(),
                        MARC.Everest.Connectors.Util.ToWireFormat(entityNameUse)
                    )
                };
            else
                return CreateNullElementExtension(NullFlavor.Other);
                    
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
                Url = new Uri("http://cr.marc-hi.ca:8080/fhir/0.10/profile/@pix-fhir#nullElementReason"),
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
        public static Extension CreateOriginalTextExtension(FhirString value)
        {
            return new Extension()
            {
                Url = new Uri("http://cr.marc-hi.ca:8080/fhir/0.10/profile/@pix-fhir#originalText"),
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
                Url = new Uri("http://cr.marc-hi.ca:8080/fhir/0.10/profile/@pix-fhir#otherId"),
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
                Url = new Uri("http://cr.marc-hi.ca:8080/fhir/0.10/profile/@pix-fhir#otherId-scopingOrganizationId"),
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
                Url = new Uri("http://cr.marc-hi.ca:8080/fhir/0.10/profile/@pix-fhir#otherId-scopingOrganizationName"),
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
                Url = new Uri("http://cr.marc-hi.ca:8080/fhir/0.10/profile/@pix-fhir#otherId-scopingOrganizationType"),
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
                Url = new Uri("http://cr.marc-hi.ca:8080/fhir/0.10/profile/@pix-fhir#identification"),
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
                Url = new Uri(String.Format("http://cr.marc-hi.ca:8080/fhir/0.10/profile/@pix-fhir#related{0}", relatedTarget.GetType().Name)),
                Value = Resource.CreateResourceReference(relatedTarget, WebOperationContext.Current.IncomingRequest.UriTemplateMatch.BaseUri)
            };
        }


    }
}
