using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ClientRegistryAdmin.Models
{
    /// <summary>
    /// Patient record match
    /// </summary>
    public class PatientMatch
    {
        /// <summary>
        /// Name
        /// </summary>
        public String GivenName { get; set; }

        /// <summary>
        /// Name
        /// </summary>
        public String FamilyName { get; set; }

        /// <summary>
        /// Gets or sets the dob
        /// </summary>
        public DateTime DateOfBirth { get; set; }

        /// <summary>
        /// Gets or sets the addr
        /// </summary>
        public String Address { get; set; }

        /// <summary>
        /// Gets or sets the gender
        /// </summary>
        public String Gender { get; set; }

        /// <summary>
        /// Gets the ECID
        /// </summary>
        public String Id { get; set; }

        /// <summary>
        /// Get the confidence
        /// </summary>
        public int Confidence { get; set; }

        /// <summary>
        /// Gets or sets the mother's identifier
        /// </summary>
        public String MothersId { get; set; }

        /// <summary>
        /// Gets or sets the mother's name
        /// </summary>
        public String MothersName { get; set; }

        /// <summary>
        /// City
        /// </summary>
        public string City { get; set; }

        /// <summary>
        /// State
        /// </summary>
        public string State { get; set; }

        /// <summary>
        /// Other identifiers
        /// </summary>
        public List<KeyValuePair<String, String>> OtherIds { get; set; }
    }
}
