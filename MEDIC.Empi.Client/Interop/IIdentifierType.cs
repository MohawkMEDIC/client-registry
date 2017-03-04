using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace MEDIC.Empi.Client.Interop
{
    /// <summary>
    /// Interface
    /// </summary>
    [ComVisible(true)]
    [Guid("8ED7D31F-5323-4E9A-910B-4EFC682AF570")]
    [InterfaceType(ComInterfaceType.InterfaceIsDual)]
    public interface IPatientIdentifier
    {

        /// <summary>
        /// Gets or sets the domain
        /// </summary>
        string Domain { get; set; }

        /// <summary>
        /// Gets or sets the value
        /// </summary>
        string Value { get; set; }
    }


    /// <summary>
    /// Identifier collection
    /// </summary>
    [ComVisible(true)]
    [Guid("AE3E4DB4-5323-4E9A-910B-4EFC682AF570")]
    [InterfaceType(ComInterfaceType.InterfaceIsDual)]
    public interface IIdentifierCollection
    {

        /// <summary>
        /// Add identifier
        /// </summary>
        void Add(PatientIdentifier identifier);

        /// <summary>
        /// Domain value
        /// </summary>
        void Add(String domain, String value);

        /// <summary>
        /// Remove at
        /// </summary>
        /// <param name="i"></param>
        void RemoveAt(int i);

        /// <summary>
        /// Gets the index
        /// </summary>
        PatientIdentifier this[int index] { get; }

        /// <summary>
        /// Get the count of items in the collection
        /// </summary>
        int Count { get; }

        /// <summary>
        /// Find the specified domain name
        /// </summary>
        PatientIdentifier Find(String domainName);

    }
}
