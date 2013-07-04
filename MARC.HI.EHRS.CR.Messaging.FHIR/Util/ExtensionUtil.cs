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
        [ExtensionDefinition(Name = "addressPart", HostType = typeof(Patient), ValueType = typeof(FhirString), Property = "Details.Address", MustSupport = false, MustUnderstand = false, ShortDescription = "Additional address information not classified by FHIR parts")]
        [ExtensionDefinition(Name = "v3-addressPartTypes", HostType = typeof(Patient), ValueType = typeof(Coding), Property = "Details.Address.Extension", Binding = typeof(AddressPartType), MustSupport = false, MustUnderstand = false, ShortDescription = "Qualifies the unclassified address parts")]
        public static Extension CreateADExtension(AddressPart part)
        {
            AddressPartType v3PartType = (AddressPartType)Enum.Parse(typeof(AddressPartType), part.PartType.ToString());
            string wireCode = MARC.Everest.Connectors.Util.ToWireFormat(v3PartType);
            
            return new Extension()
            {
                Url = new Uri("http://cr.marc-hi.ca:8080/fhir/0.9/profile/@pix-fhir#addressPart"),
                Value = new FhirString(part.AddressValue),
                Extension = new List<Extension>() {
                                        new Extension() {
                                            Url =  new Uri("http://cr.marc-hi.ca:8080/fhir/0.9/profile/@pix-fhir#v3-addressPartTypes"),
                                            Value = new Coding(typeof(AddressPartType).GetValueSetDefinition(), wireCode)
                                        }
                                    }
            };
        }

        /// <summary>
        /// Create an AD extension
        /// </summary>
        [ExtensionDefinition(Name = "addressUse", HostType = typeof(Patient), Property = "Details.Address.Use", ValueType = typeof(Coding), Binding = typeof(PostalAddressUse), MustUnderstand = false, MustSupport = false, ShortDescription = "Used when the address use is not defined in FHIR vocabulary")]
        public static Extension CreateADUseExtension(AddressSet.AddressSetUse use)
        {
            if(use == AddressSet.AddressSetUse.Search)
                return new Extension()
                {
                    Url = new Uri("http://cr.marc-hi.ca:8080/fhir/0.9/profile/@pix-fhir#addressUse"),
                    Value = new Coding(new Uri("http://hl7.org/fhir/v3/NullFlavor"), "OTH")
                };

            PostalAddressUse v3Use = (PostalAddressUse)Enum.Parse(typeof(PostalAddressUse), use.ToString());
            string wireCode = MARC.Everest.Connectors.Util.ToWireFormat(v3Use);

            return new Extension()
            {
                Url = new Uri("http://cr.marc-hi.ca:8080/fhir/profile/@iso-21090#addressUse"),
                Value = new Coding(new Uri("http://hl7.org/fhir/v3/PostalAddressUse"), wireCode)
            };
        }

        /// <summary>
        /// Telecommunications use extension
        /// </summary>
        [ExtensionDefinition(Name = "telecommunicationAddressUse", HostType = typeof(Patient), Property = "Details.Telecom.Use", ValueType = typeof(Coding), Binding = typeof(MARC.Everest.DataTypes.Interfaces.TelecommunicationAddressUse), MustSupport = false, MustUnderstand = false, ShortDescription = "Used when telecommunications is not defied in FHIR vocabulary")]
        public static Extension CreateTELUseExtension(Everest.DataTypes.Interfaces.TelecommunicationAddressUse telecommunicationAddressUse)
        {
            return new Extension()
            {
                Url = new Uri("http://cr.marc-hi.ca:8080/fhir/0.9/profile/@pix-fhir#telecommunicationAddressUse"),
                Value = new Coding(new Uri("http://hl7.org/fhir/v3/AddressUse"), MARC.Everest.Connectors.Util.ToWireFormat(telecommunicationAddressUse))
            };
        }

        /// <summary>
        /// Confidence of the match
        /// </summary>
        [ExtensionDefinition(Name = "subjectObservation", HostType = typeof(Patient), ValueType = typeof(FhirDecimal), MustUnderstand = false, MustSupport = false, ShortDescription = "Identifies the confidence of the returned match")]
        public static Extension CreateConfidenceExtension(Core.ComponentModel.QueryParameters confidence)
        {

            return new Extension()
            {
                Url = new Uri("http://cr.marc-hi.ca:8080/fhir/0.9/profile/@pix-fhir#subjectObservation"),
                Value = new FhirDecimal((decimal)confidence.Confidence)
            };
        }

        /// <summary>
        /// Create a match algorithm extension
        /// </summary>
        [ExtensionDefinition(Name = "subjectObservationMatchAlgorithm", HostType = typeof(Patient), ValueType = typeof(Coding), MustUnderstand = false, MustSupport = false, Binding = typeof(ObservationQueryMatchType), ShortDescription = "Identifies the confidence of the returned match")]
        public static Extension CreateMatchAlgorithmExtension(Core.ComponentModel.QueryParameters confidence)
        {
            return new Extension() { 
                        Url = new Uri("http://cr.marc-hi.ca:8080/fhir/profile/@pix-fhir#subjectObservationMatchAlgorithm"),
                        Value = 
                            (confidence.MatchingAlgorithm & Core.ComponentModel.MatchAlgorithm.Soundex) != 0 ?
                                new Coding(new Uri("http://cr.marc-hi.ca:8080/fhir/0.9/ValueSet/@v3-ObservationQueryMatchType"), "PHCM") { Display = "phonetic match" } :
                                new Coding(new Uri("http://cr.marc-hi.ca:8080/fhir/0.9/ValueSet/@v3-ObservationQueryMatchType"), "PTNM") { Display = "pattern match" }
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
                Url = new Uri("http://cr.marc-hi.ca:8080/fhir/0.9/profile/@pix-fhir#personalRelationshipRoleType"),
                Value = new Coding(
                    typeof(PersonalRelationshipRoleType).GetValueSetDefinition(),
                    relationshipKind
                )
            };
        }
    }
}
