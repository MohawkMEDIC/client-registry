﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MARC.HI.EHRS.SVC.Core.ComponentModel.Components;
using MARC.HI.EHRS.SVC.Core.DataTypes;
using System.Xml.Serialization;
using MARC.HI.EHRS.CR.Core.Util;
using System.ComponentModel;

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
        /// Calculates the similarity to another person
        /// </summary>
        public QueryParameters Confidence(Person other)
        {

            if (other == null)
                return new QueryParameters() { MatchingAlgorithm = MatchAlgorithm.Unspecified, Confidence = 0.0f, MatchStrength = ComponentModel.MatchStrength.Weak };
            
            // Exact match
            var exactMatch = new QueryParameters()
                {
                    Confidence = 1.0f,
                    MatchStrength = MatchStrength.Exact,
                    MatchingAlgorithm = MatchAlgorithm.Unspecified
                };
            
            // First, how many of the alternate identifiers in this patient match the other
            if (other.AlternateIdentifiers != null && other.AlternateIdentifiers.Exists(o => this.AlternateIdentifiers.Exists(p => p.Domain == o.Domain && p.Identifier == o.Identifier)))
                return exactMatch; // 100% confidence in match because the identifiers match
                
            // Second, how many names match
            if (other.Names != null)
            {
                exactMatch.Confidence = GetMaxNameConfidence(other.Names);
                if (exactMatch.Confidence == 1.0f)
                    exactMatch.MatchingAlgorithm = MatchAlgorithm.Exact;
                else
                    exactMatch.MatchingAlgorithm = GetMatchAlgorithmUsed(other.Names);
                exactMatch.MatchStrength = exactMatch.Confidence < 0.50 ? MatchStrength.Weak : exactMatch.Confidence < 0.75 ? MatchStrength.Moderate : MatchStrength.Strong;
            }

            return exactMatch;

        }

        /// <summary>
        /// Get the match algorithm used
        /// </summary>
        private MatchAlgorithm GetMatchAlgorithmUsed(List<NameSet> otherNames)
        {
            foreach (var name in otherNames)
                if (this.Names.Exists(o => o.IsSoundexMatch(name)))
                    return MatchAlgorithm.Soundex;
            return MatchAlgorithm.Variant;
        }

        /// <summary>
        /// Get the maximum name confidence
        /// </summary>
        private float GetMaxNameConfidence(List<NameSet> otherNames)
        {
            float maxConfidence = 0.0f;

            foreach (var name in otherNames)
            {
                float maxNameMatch = this.Names.Max(p => p.ConfidenceEquals(name));
                if (maxNameMatch > maxConfidence)
                    maxConfidence = maxNameMatch;
            }

            return maxConfidence;
        }


        /// <summary>
        /// Represents the alternate identifier that this record is known as
        /// </summary>
        [XmlElement("altId")]
        [Description("Patient identifiers")]
        public List<DomainIdentifier> AlternateIdentifiers { get; set; }

        /// <summary>
        /// Other, non health care domain identifiers
        /// </summary>
        [XmlElement("otherId")]
        [Description("Alternate (non medical) identifiers")]
        public List<KeyValuePair<CodeValue, DomainIdentifier>> OtherIdentifiers { get; set; }

        /// <summary>
        /// Identifies the birth time of the person
        /// </summary>
        [XmlElement("birthTime")]
        [Description("Date of birth")]
        public TimestampPart BirthTime { get; set; }

        /// <summary>
        /// IDentifies the gender code
        /// </summary>
        [XmlAttribute("genderCode")]
        [Description("Gender code")]
        public string GenderCode { get; set; }

        /// <summary>
        /// Identifies the telecommunications addresses
        /// </summary>
        [XmlElement("telecom")]
        [Description("Telecommunications addresses")]
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
        [Description("Deceased time")]
        public TimestampPart DeceasedTime { get; set; }

        /// <summary>
        /// Identifies the birth order of the person 
        /// </summary>
        [XmlElement("birth")]
        [Description("Birth order")]
        public int? BirthOrder { get; set; }

        /// <summary>
        /// Identifies the religion code of the person
        /// </summary>
        [XmlElement("religionCode")]
        [Description("Religion code")]
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
        [Description("Addresses")]
        public List<AddressSet> Addresses { get; set; }

        /// <summary>
        /// Identifies the known names for the person
        /// </summary>
        [XmlElement("name")]
        [Description("Names")]
        public List<NameSet> Names { get; set; }

        /// <summary>
        /// Identifies the race of the person
        /// </summary>
        [XmlElement("race")]
        [Description("Race code")]
        public List<CodeValue> Race { get; set; }

        /// <summary>
        /// Gets or sets the VIP code
        /// </summary>
        [XmlElement("vip")]
        [Description("VIP Code")]
        public CodeValue VipCode { get; set; }

        /// <summary>
        /// Gets or sets the marital status
        /// </summary>
        [XmlElement("marital")]
        [Description("Marital status")]
        public CodeValue MaritalStatus { get; set; }

        /// <summary>
        /// Gets the birthplace of the person (shortcut to searching for a SDL with role BRTH)
        /// </summary>
        [XmlIgnore]
        public ServiceDeliveryLocation BirthPlace
        {
            get
            {
                return this.XmlComponents.Find(o => o.Site.Name == "BRTH") as ServiceDeliveryLocation;
            }
        }

        /// <summary>
        /// Gets or sets the citizenships of the person
        /// </summary>
        [XmlElement("citizenship")]
        [Description("Citizenship")]
        public List<Citizenship> Citizenship { get; set; }

        /// <summary>
        /// Gets a list of employment
        /// </summary>
        [XmlElement("employment")]
        [Description("Employment codes")]
        public List<Employment> Employment { get; set; }
    }
}
