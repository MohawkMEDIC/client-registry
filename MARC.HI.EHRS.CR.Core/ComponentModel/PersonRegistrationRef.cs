using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using MARC.HI.EHRS.SVC.Core.DataTypes;

namespace MARC.HI.EHRS.CR.Core.ComponentModel
{
    /// <summary>
    /// Person registration record reference
    /// </summary>
    [XmlType("PersonLanguage", Namespace = "urn:marc-hi:ca/cr")]
    [Serializable]
    public class PersonRegistrationRef : CrHealthServiceRecordContainer
    {

        /// <summary>
        /// Alternate identifiers
        /// </summary>
        [XmlElement("altId")]
        public List<DomainIdentifier> AlternateIdentifiers { get; set; }

    }
}
