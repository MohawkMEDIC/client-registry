using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MARC.HI.EHRS.CR.Messaging.FHIR.DataTypes;
using MARC.HI.EHRS.SVC.Core.DataTypes;
using MARC.Everest.DataTypes;
using MARC.Everest.Attributes;

namespace MARC.HI.EHRS.CR.Messaging.FHIR.Util
{
    /// <summary>
    /// FHIR Extensions utility
    /// </summary>
    public static class ExtensionUtil
    {

        /// <summary>
        /// Create an AD extension
        /// </summary>
        public static Extension CreateADExtension(AddressPart part)
        {
            AddressPartType v3PartType = (AddressPartType)Enum.Parse(typeof(AddressPartType), part.PartType.ToString());
            string wireCode = MARC.Everest.Connectors.Util.ToWireFormat(v3PartType);
            
            return new Extension()
            {
                Url = new Uri("http://cr.marc-hi.ca:8080/fhir/profile/@iso-21090#addressPart"),
                Value = new FhirString(part.AddressValue),
                Extension = new List<Extension>() {
                                        new Extension() {
                                            Url = new Uri("http://cr.marc-hi.ca:8080/fhir/profile/@iso-21090#v3-addressPartTypes"),
                                            Value = new Coding(new Uri("http://hl7.org/fhir/v3/AddressPartType"), wireCode)
                                        }
                                    }
            };
        }

        /// <summary>
        /// Create an AD extension
        /// </summary>
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
                                    new Coding(new Uri("urn:oid:2.16.840.1.113883.2.20.5.2"), "PHCM") { Display = "phonetic match" } :
                                    new Coding(new Uri("urn:oid:2.16.840.1.113883.2.20.5.2"), "PTNM") { Display = "pattern match" }
                        }
                    }
                }
            };
        }
    }
}
