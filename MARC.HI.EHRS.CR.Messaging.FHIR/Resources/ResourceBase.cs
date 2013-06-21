using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MARC.HI.EHRS.CR.Messaging.FHIR.DataTypes;
using System.Xml.Serialization;
using System.Xml;
using MARC.HI.EHRS.CR.Messaging.FHIR.Processors;
using System.IO;

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
        protected Narrative GenerateNarrative()
        {
            // Create a new narrative
            Narrative retVal = new Narrative();

            XmlDocument narrativeContext = new XmlDocument();
            retVal.Status = new PrimitiveCode<string>("generated");
            StringWriter writer = new StringWriter();

            using (XmlWriter xw = XmlWriter.Create(writer, new XmlWriterSettings() { ConformanceLevel = ConformanceLevel.Fragment }))
            {
                xw.WriteStartElement("body", NS_XHTML);
                this.WriteText(xw);
                xw.WriteEndElement();
            }

            narrativeContext.LoadXml(writer.ToString());

            retVal.Div = new XmlElement[narrativeContext.DocumentElement.ChildNodes.Count];
            for (int i = 0; i < retVal.Div.Elements.Length; i++)
                retVal.Div.Elements[i] = narrativeContext.DocumentElement.ChildNodes[i] as XmlElement;
            return retVal;
        }

        /// <summary>
        /// Write text fragement
        /// </summary>
        internal override void WriteText(XmlWriter w)
        {
            w.WriteStartElement("p", NS_XHTML);
            w.WriteString(this.GetType().Name + " - No text defined for resource");
            w.WriteEndElement();
        }

       
    }
}
