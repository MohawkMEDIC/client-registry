using System;
using System.Xml.Serialization;
using MARC.HI.EHRS.SVC.Core.ComponentModel;
using MARC.HI.EHRS.SVC.Core.ComponentModel.Components;
using MARC.HI.EHRS.SVC.Core.DataTypes;

namespace MARC.HI.EHRS.CR.Core.ComponentModel
{
    /// <summary>
    /// Represents a specific version of a health services record event
    /// </summary>
    [Serializable][XmlType("HealthServiceRecord")]
    [XmlRoot("HealthServiceRecord")]
    public class RegistrationEvent : CrHealthServiceRecordContainer, IIdentifiable
    {

        /// <summary>
        /// Classifies the health service record
        /// </summary>
        [XmlAttribute("classifier")]
        public RegistrationEventType EventClassifier { get; set; }

        /// <summary>
        /// Identifies the status of the object
        /// </summary>
        private StatusType m_status = StatusType.Unknown;

        /// <summary>
        /// Represents alternate identifiers for the event
        /// </summary>
        [XmlElement("altId")]
        public VersionedDomainIdentifier AlternateIdentifier { get; set; }

        /// <summary>
        /// Identifies the version of the HSR that this data comes from
        /// </summary>
        [XmlAttribute("version")]
        public Decimal VersionIdentifier { get; set; }

        /// <summary>
        /// Identifies the type of event being represented
        /// </summary>
        [XmlElement("type")]
        public CodeValue EventType { get; set; }

        /// <summary>
        /// Identifies the document as "refuted"
        /// </summary>
        [XmlAttribute("refuted")]
        public bool Refuted { get; set; }

        /// <summary>
        /// Identifies the time(s) that the item is effective
        /// </summary>
        [XmlElement("efft")]
        public TimestampSet EffectiveTime { get; set; }

        /// <summary>
        /// Identifies the status of the object
        /// </summary>
        [XmlAttribute("status")]
        public StatusType Status
        {
            get
            {
                return m_status;
            }
            set
            {
                m_status = value;
            }
        }

        /// <summary>
        /// Identifies the language
        /// </summary>
        [XmlAttribute("lang")]
        public string LanguageCode { get; set; }

        #region IIdentifiable Members

        /// <summary>
        /// Identifier of the IIdentifiable object
        /// </summary>
        [XmlIgnore]
        decimal IIdentifiable.Identifier
        {
            get
            {
                return this.Id;
            }
            set
            {
                this.Id = value;
            }
        }

        /// <summary>
        /// Version identifier of the IIdentifiable object
        /// </summary>
        [XmlIgnore]
        decimal IIdentifiable.VersionIdentifier
        {
            get
            {
                return this.VersionIdentifier;
            }
            set
            {
                this.VersionIdentifier = value;
            }
        }

        #endregion

    }
}
