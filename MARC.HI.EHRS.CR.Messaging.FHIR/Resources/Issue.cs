using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MARC.HI.EHRS.CR.Messaging.FHIR.DataTypes;
using System.Xml.Serialization;

namespace MARC.HI.EHRS.CR.Messaging.FHIR.Resources
{
    /// <summary>
    /// Represents an issue detail
    /// </summary>
    [XmlType("Issue", Namespace = "http://hl7.org/fhir")]
    public class Issue
    {
        /// <summary>
        /// Default constructor
        /// </summary>
        public Issue()
        {
            this.Location = new List<FhirString>();
        }

        /// <summary>
        /// Gets or sets the severity
        /// </summary>
        [XmlElement("severity")]
        public PrimitiveCode<String> Severity { get; set; }
        /// <summary>
        /// Gets or sets the type of error
        /// </summary>
        [XmlElement("type")]
        public Coding Type { get; set; }
        /// <summary>
        /// Gets or sets the details of the issue
        /// </summary>
        [XmlElement("details")]
        public FhirString Details { get; set; }
        /// <summary>
        /// Gets or sets the location
        /// </summary>
        [XmlElement("location")]
        public List<FhirString> Location { get; set; }
    }
}
