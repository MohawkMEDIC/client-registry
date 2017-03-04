using MEDIC.Empi.Client.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Collections;

namespace MEDIC.Empi.Client
{
    /// <summary>
    /// Patient search results
    /// </summary>
    [Guid("FD394833-3922-4E83-A573-4810C7CA989A")]
    [ComVisible(true)]
    [ClassInterface(ClassInterfaceType.None)]
    public class PatientSearchResult : IPatientSearchResult, IEnumerable<Patient>
    {

        /// <summary>
        /// Patient search result
        /// </summary>
        public PatientSearchResult()
        {
            this.Results = new List<Patient>();
        }

        /// <summary>
        /// Gets the total results
        /// </summary>
        public int TotalResults { get; internal set; }

        /// <summary>
        /// Gets the count in this result set
        /// </summary>
        public int Count { get; internal set; }

        /// <summary>
        /// Gets the offset of this result set
        /// </summary>
        public int Offset { get; internal set; }

        /// <summary>
        /// Pointer 
        /// </summary>
        public object Pointer { get; internal set; }

        /// <summary>
        /// The results themselves
        /// </summary>
        public List<Patient> Results { get; internal set; }

        /// <summary>
        /// Get the results
        /// </summary>
        public Patient this[int index]
        {
            get
            {
                return this.Results[index];
            }
        }

        /// <summary>
        /// Get enumerator
        /// </summary>
        public IEnumerator<Patient> GetEnumerator()
        {
            return this.Results.GetEnumerator();
        }

        /// <summary>
        /// Get enumerator
        /// </summary>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.Results.GetEnumerator();
        }
    }
}
