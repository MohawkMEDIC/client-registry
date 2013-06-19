using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using MARC.HI.EHRS.CR.Messaging.FHIR.DataTypes;
using System.Web.UI;

namespace MARC.HI.EHRS.CR.Messaging.FHIR.Resources
{
    /// <summary>
    /// Operation outcome
    /// </summary>
    [XmlType("OperationOutcome", Namespace="http://hl7.org/fhir")]
    [XmlRoot("OperationOutcome", Namespace = "http://hl7.org/fhir")]
    public class OperationOutcome : ResourceBase
    {

        /// <summary>
        /// Constructor
        /// </summary>
        public OperationOutcome()
        {
            this.Issue = new List<Issue>();
        }

        /// <summary>
        /// Gets or sets a list of issues 
        /// </summary>
        [XmlElement("issue")]
        public List<Issue> Issue { get; set; }

    }
}
