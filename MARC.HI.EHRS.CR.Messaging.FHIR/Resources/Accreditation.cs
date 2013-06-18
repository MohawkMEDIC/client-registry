using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using MARC.HI.EHRS.CR.Messaging.FHIR.DataTypes;

namespace MARC.HI.EHRS.CR.Messaging.FHIR.Resources
{
    /// <summary>
    /// Represents an accreditation
    /// </summary>
    [XmlType("Accreditation", Namespace = "http://hl7.org/fhir")]
    public class Accreditation : Shareable
    {
        /// <summary>
        /// Gets or sets the identifier for the accreditation
        /// </summary>
        [XmlElement("identifier")]
        public Identifier Identifier { get; set; }
        /// <summary>
        /// Gets or sets the code (type) of the accreditation
        /// </summary>
        [XmlElement("code")]
        public CodeableConcept Code { get; set; }
        /// <summary>
        /// Gets or sets the issuing organization of the accreditation
        /// </summary>
        [XmlElement("issuer")]
        public Resource<Organization> Issuer { get; set; }
        /// <summary>
        /// Gets or sets the period of the accreditation
        /// </summary>
        [XmlElement("period")]
        public Period Period { get; set; }

    }
}
