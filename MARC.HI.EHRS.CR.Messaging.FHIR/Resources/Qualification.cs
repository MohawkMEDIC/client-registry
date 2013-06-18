using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MARC.HI.EHRS.CR.Messaging.FHIR.DataTypes;
using System.Xml.Serialization;

namespace MARC.HI.EHRS.CR.Messaging.FHIR.Resources
{
    /// <summary>
    /// Qualification
    /// </summary>
    [XmlType("Qualification", Namespace = "http://hl7.org/fhir")]
    public class Qualification : Shareable
    {
        /// <summary>
        /// Gets or sets the code
        /// </summary>
        [XmlElement("code")]
        public CodeableConcept Code { get; set; }
        /// <summary>
        /// Gets or sets the period of time
        /// </summary>
        [XmlElement("period")]
        public Period Period { get; set; }
        /// <summary>
        /// Gets or sets the issuer organization
        /// </summary>
        [XmlElement("issuer")]
        public Resource<Organization> Issuer { get; set; }

    }
}
