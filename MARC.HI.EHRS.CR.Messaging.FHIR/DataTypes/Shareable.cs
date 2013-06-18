using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using System.Reflection;
using System.Collections;

namespace MARC.HI.EHRS.CR.Messaging.FHIR.DataTypes
{
    /// <summary>
    /// Represents a value that can be referenced using IDREF
    /// </summary>
    [XmlType("Element", Namespace = "http://hl7.org/fhir")]
    public class Shareable 
    {

        /// <summary>
        /// Represents a referencable class
        /// </summary>
        public Shareable()
        {
        }

        /// <summary>
        /// Represents the ID of the object
        /// </summary>
        [XmlAttribute("id")]
        public string Id { get; set; }

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
            this.Id = this.GetHashCode().ToString();
            return new Shareable()
            {
                IdRef = this.Id
            };
        }

        /// <summary>
        /// Resolve the IDRef in this object to an identified object
        /// </summary>
        public Shareable ResolveReference(Shareable context)
        {
            
            // Check "this"
            if(context == null)
                return null;
            else if(context.Id == this.IdRef)
                return context;

            // Check each property
            foreach (var pi in context.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                object value = pi.GetValue(context, null);
                if (value is Shareable) // Referencable
                {
                    var refValue = value as Shareable;
                    if (refValue.Id == this.IdRef)
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

        public System.Reflection.BindingFlags BindingFlag { get; set; }
    }
}
