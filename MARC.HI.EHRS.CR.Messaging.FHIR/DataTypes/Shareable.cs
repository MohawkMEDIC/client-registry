using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using System.Reflection;
using System.Collections;
using System.Xml;

namespace MARC.HI.EHRS.CR.Messaging.FHIR.DataTypes
{
    /// <summary>
    /// Represents a value that can be referenced using IDREF
    /// </summary>
    [XmlType("Element", Namespace = "http://hl7.org/fhir")]
    public class Shareable 
    {

        // XHTML
        public const string NS_XHTML = "http://www.w3.org/1999/xhtml";

        /// <summary>
        /// Represents a referencable class
        /// </summary>
        public Shareable()
        {
            this.Extension = new List<Extension>();
        }

        /// <summary>
        /// Represents the ID of the object
        /// </summary>
        [XmlAttribute("id")]
        public string XmlId { get; set; }

        /// <summary>
        /// Identifier reference
        /// </summary>
        [XmlAttribute("idref")]
        public string IdRef { get; set; }

        /// <summary>
        /// Extension
        /// </summary>
        [XmlElement("extension")]
        public List<Extension> Extension { get; set; }

        /// <summary>
        /// Make this a reference type
        /// </summary>
        public Shareable MakeReference()
        {
            if(String.IsNullOrEmpty(this.XmlId))
                this.XmlId = this.GetHashCode().ToString();
            
            return new Shareable()
            {
                IdRef = this.XmlId
            };
        }

        /// <summary>
        /// Make an IDref
        /// </summary>
        public IdRef MakeIdRef()
        {
            return new IdRef() { Value = this.MakeReference().IdRef };
        }

        /// <summary>
        /// Make an IDref
        /// </summary>
        public static Shareable ResolveReference(IdRef idRef, Shareable context)
        {
            return new Shareable() { IdRef = idRef.Value }.ResolveReference(context);
        }

        /// <summary>
        /// Resolve the IDRef in this object to an identified object
        /// </summary>
        public Shareable ResolveReference(Shareable context)
        {
            
            // Check "this"
            if(context == null)
                return null;
            else if(context.XmlId == this.IdRef)
                return context;

            // Check each property
            foreach (var pi in context.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                object value = pi.GetValue(context, null);
                if (value is Shareable) // Referencable
                {
                    var refValue = value as Shareable;
                    if (refValue.XmlId == this.IdRef)
                        return refValue;
                    else
                    {
                        refValue = this.ResolveReference(refValue);
                        if (refValue != null) return refValue;
                    }
                }
                else if (value is IEnumerable)
                    foreach (var val in value as IEnumerable)
                    {
                        var refValue = this.ResolveReference(val as Shareable);
                        if (refValue != null) return refValue;
                    }
            }
            return null;
        }

        /// <summary>
        /// Write text
        /// </summary>
        internal virtual void WriteText(System.Xml.XmlWriter w)
        {
            w.WriteString(this.ToString());
        }

        /// <summary>
        /// Write a table row
        /// </summary>
        protected void WriteTableRows(XmlWriter w, string key, params Shareable[] values)
        {
            // Span calc
            if (values == null || values.Length == 0) return;
            int tSpan = values.Length;

            w.WriteStartElement("tr", NS_XHTML);
            this.WriteTableCell(w, key, 0, tSpan);
            w.WriteStartElement("td", NS_XHTML);
            if (values[0] == null)
                w.WriteString("N/A");
            else
                values[0].WriteText(w);
            w.WriteEndElement(); // tr
            w.WriteEndElement(); // tr
            for (int i = 1; i < values.Length; i++)
            {
                w.WriteStartElement("tr", NS_XHTML);
                w.WriteStartElement("td", NS_XHTML);
                values[i].WriteText(w);
                w.WriteEndElement();
                w.WriteEndElement();
            }
        }

        /// <summary>
        /// Write a table cell
        /// </summary>
        protected void WriteTableCell(XmlWriter w, string value, int colspan, int rowspan)
        {
            w.WriteStartElement("td", NS_XHTML);
            if (colspan > 1)
                w.WriteAttributeString("colspan", colspan.ToString());
            if (rowspan > 1)
                w.WriteAttributeString("rowspan", rowspan.ToString());

            w.WriteString(value);
            w.WriteEndElement(); // td
        }
    }
}
