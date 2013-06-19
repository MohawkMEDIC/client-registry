using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace MARC.HI.EHRS.CR.Messaging.FHIR.DataTypes
{
    /// <summary>
    /// Identity reference type
    /// </summary>
    [XmlType("IdRef", Namespace = "http://hl7.org/fhir")]
    public class IdRef 
    {

        /// <summary>
        /// The value of the IDRef
        /// </summary>
        [XmlAttribute("value")]
        public String Value { get; set; }

        /// <summary>
        /// Resolve reference
        /// </summary>
        public Shareable ResolveReference(Shareable context)
        {
            return new Shareable() { IdRef = this.Value }.ResolveReference(context);
        }
    }
}
