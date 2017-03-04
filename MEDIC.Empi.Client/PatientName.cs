using MEDIC.Empi.Client.Interop;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System;

namespace MEDIC.Empi.Client
{

    /// <summary>
    /// Patient name use
    /// </summary>
    public enum PatientNameUse
    {
        None = 0,
        Alias = 1,
        Birth = 2,
        Display = 3,
        Legal = 4,
        Maiden = 5,
        Nickname = 6,
        Pseudonym = 7
    }

    /// <summary>
    /// Patient name representation
    /// </summary>
    public enum PatientNameRepresentation
    {
        Alphabetic = 0,
        Ideographic = 1,
        Phonetic = 2
    }

    /// <summary>
    /// Patient name type
    /// </summary>
    [Guid("FB45EE43-3922-4E83-A573-4810C7CA989A")]
    [ComVisible(true)]
    [ClassInterface(ClassInterfaceType.None)]
    public class PatientName  : IPatientName
    {

        /// <summary>
        /// Gets or sets the representation
        /// </summary>
        public PatientNameRepresentation Representation { get; set; }

        /// <summary>
        /// Classifies the patient name
        /// </summary>
        public PatientNameUse Use { get; set; }

        /// <summary>
        /// Given name
        /// </summary>
        public string GivenName { get; set; }

        /// <summary>
        /// Gets or sets the middle names or initials
        /// </summary>
        public string SecondNamesOrInitials { get; set; }

        /// <summary>
        /// Gets or sets the surname
        /// </summary>
        public string Surname { get; set; }

        /// <summary>
        /// Gets or sets the maiden surnames
        /// </summary>
        public string MaidenSurname { get; set; }
    }


    /// <summary>
    /// Patient address type
    /// </summary>
    [Guid("BE354234-3922-4E83-A523-4810C7CA989A")]
    [ComVisible(true)]
    [ClassInterface(ClassInterfaceType.None)]
    public class PatientNameCollection : List<PatientName>, IPatientNameCollection
    {

        /// <summary>
        /// Add specified name to the collection
        /// </summary>
        public void AddFull(PatientNameUse use, PatientNameRepresentation representation,  string surname, string givenName)
        {
            this.Add(new PatientName()
            {
                Use = use,
                Representation = representation,
                Surname = surname,
                GivenName = givenName
            });
        }

        /// <summary>
        /// Add specified name to the collection
        /// </summary>
        public void AddBasic(string surname, string givenName)
        {
            this.Add(new PatientName()
            {
                Use = PatientNameUse.Legal,
                Representation = PatientNameRepresentation.Alphabetic,
                Surname = surname,
                GivenName = givenName
            });
        }

        /// <summary>
        /// Find name by use
        /// </summary>
        public PatientName Find(PatientNameUse use)
        {
            return this.Find(o => o.Use == use);
        }

    }
}