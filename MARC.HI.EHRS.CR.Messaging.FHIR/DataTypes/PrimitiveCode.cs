using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace MARC.HI.EHRS.CR.Messaging.FHIR.DataTypes
{
    /// <summary>
    /// Primitive code value
    /// </summary>
    [XmlType("PrimitiveCode", Namespace = "http://hl7.org/fhir")]
    public class PrimitiveCode<T> : Primitive<T>
    {

        /// <summary>
        /// Constructs a new instance of code
        /// </summary>
        public PrimitiveCode()
        {

        }

        /// <summary>
        /// Creates a new instance of the code
        /// </summary>
        public PrimitiveCode(T code) : base(code)
        {
        }

    }
}
