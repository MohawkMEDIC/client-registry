using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using MARC.Everest.Connectors;

namespace MARC.HI.EHRS.CR.Messaging.FHIR.DataTypes
{
    /// <summary>
    /// Primitive values
    /// </summary>
    public abstract class Primitive<T>
    {

        /// <summary>
        /// Default ctor
        /// </summary>
        public Primitive()
        {
        }

        /// <summary>
        /// Primitive value
        /// </summary>
        public Primitive(T value)
        {
            this.Value = value;
        }

        /// <summary>
        /// Gets or sets the value of the primitive
        /// </summary>
        [XmlIgnore]
        public virtual T Value { get; set; }

        /// <summary>
        /// Gets or sets the XML Value of the data
        /// </summary>
        [XmlAttribute("value")]
        public String XmlValue
        {
            get {

                if (this.Value is byte[])
                    return Convert.ToBase64String((byte[])(object)this.Value);
                else
                    return this.Value.ToString(); 
            
            }
            set {
                if (typeof(T) == typeof(byte[]))
                    this.Value = (T)(object)Convert.FromBase64String(value);
                else
                    this.Value = Util.Convert<T>(value);
            }
        }


        /// <summary>
        /// Cast as string
        /// </summary>
        public override string ToString()
        {
            return this.XmlValue;
        }
    }

    /// <summary>
    /// Represents a boolean
    /// </summary>
    [XmlType("boolean", Namespace = "http://hl7.org/fhir")]
    public class FhirBoolean : Primitive<Boolean> {
        public FhirBoolean() : base() { }
        public FhirBoolean(Boolean value) : base(value) { }
    }
    /// <summary>
    /// Represents a Uri
    /// </summary>
    [XmlType("uri", Namespace = "http://hl7.org/fhir")]
    public class FhirUri : Primitive<Uri> { 
        public FhirUri() : base() { }
        public FhirUri(Uri value) : base(value) { }
    }
    /// <summary>
    /// Represents an int
    /// </summary>
    [XmlType("integer")]
    public class FhirInt : Primitive<Int32> {
        public FhirInt() : base() { }
        public FhirInt(Int32 value) : base(value) { }
    }
    /// <summary>
    /// Represents a string
    /// </summary>
    [XmlType("string", Namespace = "http://hl7.org/fhir")]
    public class FhirString : Primitive<String> {
        public FhirString() : base() { }
        public FhirString(String value) : base(value) { }
    }
}
