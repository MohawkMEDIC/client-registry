using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using System.Globalization;

namespace MARC.HI.EHRS.CR.Messaging.FHIR.DataTypes
{
    /// <summary>
    /// Date precisions
    /// </summary>
    public enum DatePrecision
    {
        Unspecified = 0,
        Year = 4,
        Month = 7, 
        Day = 10,
        Full = 28
    }

    /// <summary>
    /// Date
    /// </summary>
    [XmlType("dateTime", Namespace = "http://hl7.org/fhir")]
    public class Date : FhirString
    {

        /// <summary>
        /// Default ctor
        /// </summary>
        public Date() { }

        /// <summary>
        /// Create a date 
        /// </summary>
        public Date(DateTime value)
            : base()
        {
            this.DateValue = value;
        }

        /// <summary>
        /// Gets the precision of the date
        /// </summary>
        public DatePrecision Precision { get; set; }

        /// <summary>
        /// Start time
        /// </summary>
        [XmlIgnore()]
        public DateTime? DateValue
        {
            get;
            set;
        }

        /// <summary>
        /// Convert this date to a date
        /// </summary>
        public static implicit operator DateTime(Date v)
        {
            return v.DateValue.Value;
        }

        /// <summary>
        /// Convert this date to a date
        /// </summary>
        public static implicit operator Date(DateTime v)
        {
            return new Date(v) ;
        }

        /// <summary>
        /// Gets or sets the value
        /// </summary>
        public override string Value
        {
            get
            {

                string dateFormat = @"yyyy-MM-dd\THH:mm:ss.ffffzzz";
                if (this.Precision == DatePrecision.Unspecified)
                    this.Precision = DatePrecision.Full;
                dateFormat = dateFormat.Substring(0, (int)this.Precision);
                return this.DateValue.Value.ToString(dateFormat);
            }
            set
            {
                if (value != null)
                {
                    string dateFormat = @"yyyy-MM-dd\THH:mm:ss.ffffzzz";
                    this.Precision = (DatePrecision)value.Length;
                    if (this.Precision > DatePrecision.Full)
                        this.Precision = DatePrecision.Full;

                    // Correct parse
                    if (this.Precision == DatePrecision.Year)
                        this.DateValue = DateTime.ParseExact(value, "yyyy", CultureInfo.InvariantCulture);
                    else
                        this.DateValue = DateTime.Parse(value);

                }
                else
                    this.DateValue = DateTime.Parse(value);
            }
        }

        /// <summary>
        /// Date string
        /// </summary>
        public override string ToString()
        {
            return this.XmlValue;
        }
    }

    /// <summary>
    /// Date only
    /// </summary>
    [XmlType("date", Namespace = "http://hl7.org/fhir")]
    public class DateOnly : Date
    {
        /// <summary>
        /// Only date is permitted
        /// </summary>
        public override string Value
        {
            get
            {
                if (base.Precision > DatePrecision.Day)
                    this.Precision = DatePrecision.Day;
                return base.Value;
            }
            set
            {
                base.Value = value;
                if (base.Precision > DatePrecision.Day)
                    this.Precision = DatePrecision.Day;
            }
        }
    }
}
