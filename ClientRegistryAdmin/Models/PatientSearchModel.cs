using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;

namespace ClientRegistryAdmin.Models
{
    /// <summary>
    /// Patient search model
    /// </summary>
    public class PatientSearchModel
    {

        /// <summary>
        /// Gets or sets the family name
        /// </summary>
        [Display(Name = "Family Name")]
        public String FamilyName { get; set; }
        /// <summary>
        /// Gets or sets the given name
        /// </summary>
        [Display(Name = "Given Name")]
        public String GivenName { get; set; }
        /// <summary>
        /// Gender
        /// </summary>
        [Display(Name = "Gender")]
        public String Gender { get; set; }
        /// <summary>
        /// Date of birth
        /// </summary>
        [Display(Name = "Date of Birth")]
        public String DateOfBirth { get; set; }
        /// <summary>
        /// Result list
        /// </summary>
        public List<PatientMatch> Outcome { get; set; }
        /// <summary>
        /// Gets or sets the error code
        /// </summary>
        public Boolean IsError { get; set; }
    }
}