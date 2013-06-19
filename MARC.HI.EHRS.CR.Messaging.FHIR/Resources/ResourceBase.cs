using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MARC.HI.EHRS.CR.Messaging.FHIR.DataTypes;
using System.Xml.Serialization;
using System.Xml;
using MARC.HI.EHRS.CR.Messaging.FHIR.Processors;

namespace MARC.HI.EHRS.CR.Messaging.FHIR.Resources
{
    /// <summary>
    /// Base for all resources
    /// </summary>
    [XmlType("ResourceBase", Namespace = "http://hl7.org/fhir")]
    public abstract class ResourceBase : Shareable
    {
        // The narrative
        private Narrative m_narrative;

        /// <summary>
        /// Gets or sets the internal identifier for the resource
        /// </summary>
        [XmlIgnore]
        public decimal Id { get; set; }

        /// <summary>
        /// Version identifier
        /// </summary>
        [XmlIgnore]
        public decimal VersionId { get; set; }

        /// <summary>
        /// Gets or sets the narrative text
        /// </summary>
        [XmlElement("text")]
        public Narrative Text
        {
            get
            {
                if (this.m_narrative == null)
                    this.m_narrative = this.GenerateNarrative();
                return this.m_narrative;
            }
            set
            {
                this.m_narrative = value;
            }
        }

        /// <summary>
        /// Generate a narrative
        /// </summary>
        protected virtual Narrative GenerateNarrative()
        {
            // Create a new narrative
            Narrative retVal = new Narrative();

            XmlDocument narrativeContext = new XmlDocument();
            retVal.Status = new PrimitiveCode<string>("generated");
            retVal.Div = narrativeContext.CreateElement("p", FhirMessageProcessorUtil.NS_XHTML);
            retVal.Div.InnerText = String.Format("{0} - - No human readable text provided for this resource", this.GetType().Name);
            return retVal;
        }



    }
}
