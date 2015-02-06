using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ClientRegistryAdmin.Models
{
    /// <summary>
    /// Conflict resolution page
    /// </summary>
    public class ConflictListModel
    {

        /// <summary>
        /// Patients 
        /// </summary>
        public List<ConflictPatientMatch> Patients { get; set; }


        public bool IsError { get; set; }
    }
}