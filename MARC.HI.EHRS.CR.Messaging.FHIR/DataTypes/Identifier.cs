using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using MARC.HI.EHRS.CR.Messaging.FHIR.Resources;

namespace MARC.HI.EHRS.CR.Messaging.FHIR.DataTypes
{
    /// <summary>
    /// Represents an identifier
    /// </summary>
    [XmlType("Identifier", Namespace = "http://hl7.org/fhir")]
    public class Identifier : Shareable
    {

        /// <summary>
        /// Identifies the intended use of the item
        /// </summary>
        [XmlElement("use")]
        public FhirString Use { get; set; }

        /// <summary>
        /// Represents a label for the identifier
        /// </summary>
        [XmlElement("label")]
        public FhirString Label { get; set; }

        /// <summary>
        /// Identifies the system which assigned the ID
        /// </summary>
        [XmlElement("system")]
        public FhirUri System { get; set; }

        /// <summary>
        /// Identifies the key (unique value) of the primitive
        /// </summary>
        [XmlElement("key")]
        public FhirString Key { get; set; }

        /// <summary>
        /// Identifies the period the identifier is valid
        /// </summary>
        [XmlElement("period")]
        public Period Period { get; set; }

        /// <summary>
        /// Identifies the assigning organization of the identifier
        /// </summary>
        [XmlElement("assigner")]
        public Resource<Organization> Assigner { get; set; }

        /// <summary>
        /// Identifier
        /// </summary>
        internal override void WriteText(System.Xml.XmlWriter w)
        {

                w.WriteStartElement("strong", NS_XHTML);
                if (this.Label == null)
                    w.WriteString("UNKNOWN");
                else
                    this.Label.WriteText(w);
                w.WriteString(":");
                w.WriteEndElement();//strong



            this.Key.WriteText(w);

            // System in brackets
            if (this.System != null)
            {
                w.WriteString("(");
                this.System.WriteText(w);
                w.WriteString(")");
            }

            // Italic (the name of the maintainer
            if (this.Assigner != null)
            {
                w.WriteStartElement("br", NS_XHTML);
                w.WriteEndElement();
                w.WriteStartElement("em", NS_XHTML);
                this.Assigner.Display.WriteText(w);
                w.WriteEndElement();
            }

            
            
        }

    }
}
