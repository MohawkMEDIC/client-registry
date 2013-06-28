using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using MARC.Everest.Connectors;
using System.Xml;

namespace MARC.HI.EHRS.CR.Messaging.FHIR.DataTypes
{
    /// <summary>
    /// Primitive values
    /// </summary>
    public abstract class Primitive<T> : Shareable
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
        public virtual String XmlValue
        {
            get {

                if (this.Value != null)
                    return this.Value.ToString();
                else
                    return null;
            
            }
            set {
                this.Value = MARC.Everest.Connectors.Util.Convert<T>(value);
            }
        }

        /// <summary>
        /// Convert Primitive to wrapped
        /// </summary>
        public static implicit operator T(Primitive<T> v)
        {
            if (v == null)
                return default(T);
            return v.Value;
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
    /// Represents a Uri
    /// </summary>
    [XmlType("decimal", Namespace = "http://hl7.org/fhir")]
    public class FhirDecimal : Primitive<Decimal?>
    {
        public FhirDecimal() : base() { }
        public FhirDecimal(Decimal value) : base(value) { }
        public static implicit operator FhirDecimal(Decimal v) { return new FhirDecimal(v); }
        [XmlAttribute("value")]
        public override string XmlValue
        {
            get
            {
                if (this.Value != null)
                    return XmlConvert.ToString(this.Value.Value);
                return null;
            }
            set
            {
                if (value != null)
                    base.Value = XmlConvert.ToDecimal(value);
                else
                    this.Value = default(decimal);
            }
        }
    }

    /// <summary>
    /// Represents a boolean
    /// </summary>
    [XmlType("boolean", Namespace = "http://hl7.org/fhir")]
    public class FhirBoolean : Primitive<Boolean?> {
        public FhirBoolean() : base() { }
        public FhirBoolean(Boolean value) : base(value) { }
        public static implicit operator FhirBoolean(bool v) { return new FhirBoolean(v); }
        [XmlAttribute("value")]
        public override string XmlValue
        {
            get
            {
                if(this.Value != null)
                    return XmlConvert.ToString(this.Value.Value);
                return null;
            }
            set
            {
                if(value != null)
                    base.Value = XmlConvert.ToBoolean(value);
                else
                    base.Value = null;
            }
        }
    }
    /// <summary>
    /// Represents a Uri
    /// </summary>
    [XmlType("uri", Namespace = "http://hl7.org/fhir")]
    public class FhirUri : Primitive<Uri> { 
        public FhirUri() : base() { }
        public FhirUri(Uri value) : base(value) { }
        public static implicit operator FhirUri(Uri v) { return new FhirUri(v); }
        /// <summary>
        /// Write as text
        /// </summary>
        internal override void WriteText(System.Xml.XmlWriter w)
        {
            w.WriteStartElement("a", NS_XHTML);
            w.WriteAttributeString("href", this.Value.ToString());
            w.WriteString(this.Value.ToString());
            w.WriteEndElement(); // a
        }

    }
    /// <summary>
    /// Represents an int
    /// </summary>
    [XmlType("integer")]
    public class FhirInt : Primitive<Int32?> {
        public FhirInt() : base() { }
        public FhirInt(Int32 value) : base(value) { }
        public static implicit operator FhirInt(int v) { return new FhirInt(v); }

    }
    /// <summary>
    /// Represents a string
    /// </summary>
    [XmlType("string", Namespace = "http://hl7.org/fhir")]
    public class FhirString : Primitive<String> {
        public FhirString() : base() { }
        public FhirString(String value) : base(value) { }
        public static implicit operator FhirString(string v) { return new FhirString(v); }

    }
    /// <summary>
    /// Represents a string
    /// </summary>
    [XmlType("base64Binary", Namespace = "http://hl7.org/fhir")]
    public class FhirBinary : Primitive<byte[]>
    {
        public FhirBinary() : base() { }
        public FhirBinary(byte[] value) : base(value) { }
        public static implicit operator FhirBinary(byte[] v) { return new FhirBinary(v); }
        [XmlAttribute("value")]
        public override string XmlValue
        {
            get
            {
                if (this.Value != null)
                    return Convert.ToBase64String(this.Value);
                return null;
            }
            set
            {
                if (value != null)
                    this.Value = Convert.FromBase64String(value);
                else
                    this.Value = null;
            }
        }
    }
}
