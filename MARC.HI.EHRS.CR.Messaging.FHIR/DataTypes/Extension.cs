using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using MARC.HI.EHRS.CR.Messaging.FHIR.Resources;

namespace MARC.HI.EHRS.CR.Messaging.FHIR.DataTypes
{
    /// <summary>
    /// Represents an extension
    /// </summary>
    [XmlType("Extension", Namespace = "http://hl7.org/fhir")]
    public class Extension : Shareable
    {
        /// <summary>
        /// URL of the extension definition
        /// </summary>
        [XmlElement("url")]
        public FhirUri Url { get; set; }

        /// <summary>
        /// True if is modifier
        /// </summary>
        [XmlElement("isModifier")]
        public FhirBoolean IsModifier { get; set; }

        /// <summary>
        /// Value choice
        /// </summary>
        [XmlElement("valueInteger", typeof(FhirInt))]
        [XmlElement("valueDecimal", typeof(Primitive<decimal>))]
        [XmlElement("valueDateTime", typeof(Date))]
        [XmlElement("valueDate", typeof(DateOnly))]
        [XmlElement("valueInstant", typeof(Primitive<DateTime>))]
        [XmlElement("valueString", typeof(FhirString))]
        [XmlElement("valueUri", typeof(FhirUri))]
        [XmlElement("valueBoolean", typeof(FhirBoolean))]
        [XmlElement("valueCode", typeof(PrimitiveCode<String>))]
        [XmlElement("valueBase64Binary", typeof(Primitive<byte[]>))]
        [XmlElement("valueCoding", typeof(Coding))]
        [XmlElement("valueCodeableConcept", typeof(CodeableConcept))]
        [XmlElement("valueAttachment", typeof(Attachment))]
        [XmlElement("valueIdentifier", typeof(Identifier))]
        [XmlElement("valueQuantity", typeof(Quantity))]
        [XmlElement("valueChoice", typeof(Choice))]
        [XmlElement("valueRange", typeof(Range))]
        [XmlElement("valuePeriod", typeof(Period))]
        [XmlElement("valueRatio", typeof(Ratio))]
        [XmlElement("valueHumanName", typeof(HumanName))]
        [XmlElement("valueAddress", typeof(Address))]
        [XmlElement("valueContact" ,typeof(Telecom))]
        [XmlElement("valueSchedule", typeof(Schedule))]
        [XmlElement("valueResource", typeof(Resource<Shareable>))]
        public Shareable Value { get; set; }

        /// <summary>
        /// Extensions
        /// </summary>
        [XmlElement("extension")]
        public List<Extension> Extensions { get; set; }
    }
}
