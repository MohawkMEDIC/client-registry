using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ClientRegistryAdmin.Models
{
    /// <summary>
    /// Conflict patient match
    /// </summary>
    public class ConflictPatientMatch
    {
        /// <summary>
        /// The patient which would survive
        /// </summary>
        public PatientMatch Patient { get; set; }

        /// <summary>
        /// The Matching patients
        /// </summary>
        public List<PatientMatch> Matching { get; set; }
    }
}
