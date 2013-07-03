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
        [ExtensionDefinition(Name = "addressPart", HostType = typeof(Address), ValueType = typeof(FhirString), MustSupport = false, MustUnderstand = false, ShortDescription = "Additional address information not classified by FHIR parts")]
        [ExtensionDefinition(Name = "addressPartType", HostType = typeof(Extension), ValueType = typeof(PrimitiveCode<String>), Binding = typeof(AddressPartType), MustSupport = false, MustUnderstand = false, ShortDescription = "Qualifies the unclassified address parts")]
        public static Extension CreateADExtension(AddressPart part)
        {
            AddressPartType v3PartType = (AddressPartType)Enum.Parse(typeof(AddressPartType), part.PartType.ToString());
            string wireCode = MARC.Everest.Connectors.Util.ToWireFormat(v3PartType);
            
            return new Extension()
            {
                Url = new Uri("http://cr.marc-hi.ca:8080/fhir/profile/addressPart"),
                Value = new FhirString(part.AddressValue),
                Extension = new List<Extension>() {
                                        new Extension() {
                                            Url = new Uri("http://cr.marc-hi.ca:8080/fhir/0.9/profile/@pix-fhir#v3-addressPartTypes"),
                                            Value = new Coding(new Uri("http://cr.marc-hi.ca:8080/fhir/0.9/ValueSet/@v3-AddressPartType"), wireCode)
                                        }
                                    }
            };
        }

        /// <summary>
        /// Create an AD extension
        /// </summary>
        [ExtensionDefinition(Name = "addressUse", HostType = typeof(Address), ValueType = typeof(Coding), Binding = typeof(PostalAddressUse), Property = "Use", MustUnderstand = false, MustSupport = false, ShortDescription = "Used when the address use is not defined in FHIR vocabulary")]
        public static Extension CreateADUseExtension(AddressSet.AddressSetUse use)
        {
            if(use == AddressSet.AddressSetUse.Search)
                return new Extension()
                {
                    Url = new Uri("http://cr.marc-hi.ca:8080/fhir/profile/@iso-21090#addressUse"),
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
        [ExtensionDefinition(Name = "telecomUse", HostType = typeof(Telecom), Property = "Use", ValueType = typeof(Coding), Binding = typeof(MARC.Everest.DataTypes.Interfaces.TelecommunicationAddressUse), MustSupport = false, MustUnderstand = false, ShortDescription = "Used when telecommunications is not defied in FHIR vocabulary")]
        public static Extension CreateTELUseExtension(Everest.DataTypes.Interfaces.TelecommunicationAddressUse telecommunicationAddressUse)
        {
            return new Extension()
            {
                Url = new Uri("http://cr.marc-hi.ca:8080/fhir/profile/@iso-21090#telecommunicationAddressUse"),
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
                Url = new Uri("http://cr.marc-hi.ca:8080/fhir/profile/@pix-fhir#subjectObservation"),
                Value = new FhirDecimal((decimal)confidence.Confidence)
                {
                    Extension = new List<Extension>() {
                        new Extension() { 
                            Url = new Uri("http://cr.marc-hi.ca:8080/fhir/profile/@pix-fhir#subjectObservationMatchAlgorithm"),
                            Value = 
                                (confidence.MatchingAlgorithm & Core.ComponentModel.MatchAlgorithm.Soundex) != 0 ?
                                    new Coding(new Uri("http://cr.marc-hi.ca:8080/fhir/0.9/ValueSet/@v3-ObservationQueryMatchType"), "PHCM") { Display = "phonetic match" } :
                                    new Coding(new Uri("http://cr.marc-hi.ca:8080/fhir/0.9/ValueSet/@v3-ObservationQueryMatchType"), "PTNM") { Display = "pattern match" }
                        }
                    }
                }
            };
        }
    }
}
