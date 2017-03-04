using MEDIC.Empi.Client.Interop;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace MEDIC.Empi.Client
{
    /// <summary>
    /// Identifier type
    /// </summary>
    [Guid("78BCE323-3849-2039-A573-4810C7CA989A")]
    [ComVisible(true)]
    [ClassInterface(ClassInterfaceType.None)]
    public class PatientIdentifier : IPatientIdentifier
    {

        /// <summary>
        /// New identifier type
        /// </summary>
        public PatientIdentifier()
        {
            
        }

        /// <summary>
        /// Create new identifier type
        /// </summary>
        public PatientIdentifier(string domain, string value)
        {
            this.Domain = domain;
            this.Value = value;
        }

        /// <summary>
        /// Domain name
        /// </summary>
        public string Domain { get; set; }

        /// <summary>
        /// Value name
        /// </summary>
        public string Value { get; set; }

    }

    /// <summary>
    /// Identifier type collection
    /// </summary>
    [Guid("78BCE323-3922-4E83-A573-4810C7CA989A")]
    [ComVisible(true)]
    [ClassInterface(ClassInterfaceType.None)]
    public class IdentifierTypeCollection : List<PatientIdentifier>, IIdentifierCollection
    {

        /// <summary>
        /// Add new identifier type
        /// </summary>
        public void Add (string domain, string value)
        {
            this.Add(new PatientIdentifier(domain, value));
        }

        /// <summary>
        /// Find the specified value
        /// </summary>
        public PatientIdentifier Find(string domainName)
        {
            return this.Find(o => o.Domain == domainName);
        }
    }
}