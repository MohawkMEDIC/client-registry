using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace MARC.HI.EHRS.CR.Messaging.FHIR.DataTypes
{
    /// <summary>
    /// Identifies a postal address
    /// </summary>
    [XmlType("Address", Namespace = "http://hl7.org/fhir")]
    public class Address : Shareable
    {

        /// <summary>
        /// Create new address
        /// </summary>
        public Address()
        {
            this.Line = new List<FhirString>();
        }

        /// <summary>
        /// The use of the value
        /// </summary>
        [XmlElement("use")]
        public PrimitiveCode<String> Use { get; set; }

        /// <summary>
        /// Gets or sets the text
        /// </summary>
        [XmlElement("text")]
        public FhirString Text { get; set; }

        /// <summary>
        /// Gets or sets the line items of the address
        /// </summary>
        [XmlElement("line")]
        public List<FhirString> Line { get; set; }

        /// <summary>
        /// Gets or sets the city 
        /// </summary>
        [XmlElement("city")]
        public FhirString City { get; set; }

        /// <summary>
        /// Gets or sets the state
        /// </summary>
        [XmlElement("state")]
        public FhirString State { get; set; }

        /// <summary>
        /// Gets or sets the zip
        /// </summary>
        [XmlElement("zip")]
        public FhirString Zip { get; set; }

        /// <summary>
        /// Gets or sets the country
        /// </summary>
        [XmlElement("country")]
        public FhirString Country { get; set; }

        /// <summary>
        /// Gets or sets the period
        /// </summary>
        [XmlElement("period")]
        public Period Period { get; set; }

        /// <summary>
        /// Represent as text
        /// </summary>
        internal override void WriteText(System.Xml.XmlWriter w)
        {

            List<String> output = new List<string>();
            foreach (var l in this.Line)
                output.Add(l);
            output.Add(String.Format("{0}, {1}", this.City, this.State));
            output.Add(this.Country);
            output.Add(this.Zip);

            w.WriteStartElement("table", NS_XHTML);
            w.WriteStartElement("tbody", NS_XHTML);
            w.WriteStartElement("tr", NS_XHTML);
            base.WriteTableCell(w, this.Use, 0, 0);
            base.WriteTableCell(w, output.First(), 0, 0);
            w.WriteEndElement();// tr

            for(int i = 1; i < output.Count; i++)
            {
                if(output[i] == null)
                    continue;

                w.WriteStartElement("tr", NS_XHTML);
                base.WriteTableCell(w, String.Empty, 0, 0);
                base.WriteTableCell(w, output[i], 0, 0);
                w.WriteEndElement();
            }
            w.WriteEndElement();//tbody
            w.WriteEndElement();//table

        }

    }
}
