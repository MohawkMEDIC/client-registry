using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using MARC.HI.EHRS.CR.Messaging.FHIR.DataTypes;

namespace MARC.HI.EHRS.CR.Messaging.FHIR.Resources
{

    [XmlRoot("Picture", Namespace = "http://hl7.org/fhir")]
    [XmlType("Picture", Namespace = "http://hl7.org/fhir")]
    public class Picture : ResourceBase
    {
        
        /// <summary>
        /// The subject of picture
        /// </summary>
        [XmlElement("subject")]
        public Resource<Patient> Subject { get; set; }

        /// <summary>
        /// The date/time the picture was taken
        /// </summary>
        [XmlElement("dateTime")]
        public Date DateTime { get; set; }

        /// <summary>
        /// Gets or sets the operator of the image
        /// </summary>
        [XmlElement("operator")]
        public Resource<Practictioner> Operator { get; set; }

        /// <summary>
        /// Identifies the image
        /// </summary>
        [XmlElement("identifier")]
        public Identifier Identifier { get; set; }

        /// <summary>
        /// Used by the accessor to link back to image
        /// </summary>
        [XmlElement("accessionNo")]
        public Identifier AccessionNo { get; set; }

        /// <summary>
        /// Identifies the study of which the image is a part
        /// </summary>
        [XmlElement("studyId")]
        public Identifier StudyId { get; set; }

        /// <summary>
        /// Identiifes the series of which the image is a part
        /// </summary>
        [XmlElement("seriesId")]
        public Identifier SeriesId { get; set; }

        /// <summary>
        /// Identiifs the method how the image was taken
        /// </summary>
        [XmlElement("method")]
        public CodeableConcept Method { get; set; }

        /// <summary>
        /// identifies the person that requested the image
        /// </summary>
        [XmlElement("requester")]
        public Resource<Practictioner> Requester { get; set; }

        /// <summary>
        /// Identifies the modality of the image
        /// </summary>
        [XmlElement("modality")]
        public PrimitiveCode<String> Modality { get; set; }

        /// <summary>
        /// Identifies the name of the manufacturer
        /// </summary>
        [XmlElement("deviceName")]
        public FhirString DeviceName { get; set; }

        /// <summary>
        /// Identifies the height
        /// </summary>
        [XmlElement("height")]
        public FhirInt Height { get; set; }

        /// <summary>
        /// Identifies the width
        /// </summary>
        [XmlElement("width")]
        public FhirInt Width { get; set; }

        /// <summary>
        /// Identifies the BPP of the image
        /// </summary>
        [XmlElement("bits")]
        public FhirInt Bits { get; set; }

        /// <summary>
        /// Identifies the number of frames
        /// </summary>
        [XmlElement("frames")]
        public FhirInt Frames { get; set; }

        /// <summary>
        /// Identifies the delay between frames
        /// </summary>
        [XmlElement("frameDelay")]
        public Quantity FrameDelay { get; set; }

        /// <summary>
        /// Identifies the view (lateral, AP, etc)
        /// </summary>
        [XmlElement("view")]
        public CodeableConcept View { get; set; }

        /// <summary>
        /// Identifies the content - ref or data
        /// </summary>
        [XmlElement("content")]
        public Attachment Content { get; set; }
    }
}
