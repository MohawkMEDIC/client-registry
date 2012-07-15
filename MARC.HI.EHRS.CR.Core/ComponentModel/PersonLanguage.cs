using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace MARC.HI.EHRS.CR.Core.ComponentModel
{
    /// <summary>
    /// Language type
    /// </summary>
    public enum LanguageType
    {
        Written = 1,
        Spoken = 2,
        Fluency = 4,
        WrittenAndSpoken = Written | Spoken
    }

    /// <summary>
    /// Identifies a language component
    /// </summary>
    [XmlType("PersonLanguage", Namespace = "urn:marc-hi:ca/cr")]
    public class PersonLanguage
    {

        /// <summary>
        /// The type of language
        /// </summary>
        [XmlAttribute("type")]
        public LanguageType Type { get; set; }

        /// <summary>
        /// Identifies ISO639-1 code 
        /// </summary>
        public String Language { get; set; }
    }
}
