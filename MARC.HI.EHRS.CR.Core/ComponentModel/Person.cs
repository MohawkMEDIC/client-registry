using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MARC.HI.EHRS.SVC.Core.ComponentModel.Components;
using MARC.HI.EHRS.SVC.Core.DataTypes;
using System.Xml.Serialization;

namespace MARC.HI.EHRS.CR.Core.ComponentModel
{
    /// <summary>
    /// Represents a person in the client registry
    /// </summary>
    [Serializable]
    [XmlType("Person", Namespace = "urn:marc-hi:ca/cr")]
    public class Person : CrHealthServiceRecordContainer
    {
        /// <summary>
        /// Represents the alternate identifier that this record is known as
        /// </summary>
        [XmlElement("altId")]
        public List<DomainIdentifier> AlternateIdentifiers { get; set; }

        /// <summary>
        /// Other, non health care domain identifiers
        /// </summary>
        [XmlElement("otherId")]
        public List<KeyValuePair<CodeValue, DomainIdentifier>> OtherIdentifiers { get; set; }

        /// <summary>
        /// Identifies the birth time of the person
        /// </summary>
        [XmlElement("birthTime")]
        public TimestampPart BirthTime { get; set; }

        /// <summary>
        /// IDentifies the gender code
        /// </summary>
        [XmlAttribute("genderCode")]
        public string GenderCode { get; set; }

        /// <summary>
        /// Identifies the telecommunications addresses
        /// </summary>
        [XmlElement("telecom")]
        public List<TelecommunicationsAddress> TelecomAddresses { get; set; }

        /// <summary>
        /// Identifies the version of the person object
        /// </summary>
        [XmlAttribute("verId")]
        public decimal VersionId { get; set; }

        /// <summary>
        /// Identifies the status of the person object
        /// </summary>
        [XmlAttribute("status")]
        public StatusType Status { get; set; }

        /// <summary>
        /// Identifies the deceased time of the person
        /// </summary>
        [XmlElement("deceased")]
        public TimestampPart DeceasedTime { get; set; }

        /// <summary>
        /// Identifies the birth order of the person 
        /// </summary>
        [XmlElement("birth")]
        public int? BirthOrder { get; set; }

        /// <summary>
        /// Identifies the religion code of the person
        /// </summary>
        [XmlElement("religionCode")]
        public CodeValue ReligionCode { get; set; }

        /// <summary>
        /// Identifies the lanugage(s) spoken or understood by the person
        /// </summary>
        [XmlElement("language")]
        public List<PersonLanguage> Language { get; set; }

        /// <summary>
        /// Identifies the addresses for the person
        /// </summary>
        [XmlElement("addr")]
        public List<AddressSet> Addresses { get; set; }

        /// <summary>
        /// Identifies the known names for the person
        /// </summary>
        [XmlElement("name")]
        public List<NameSet> Names { get; set; }

        /// <summary>
        /// Identifies the race of the person
        /// </summary>
        [XmlElement("race")]
        public List<CodeValue> Race { get; set; }


    }
}
