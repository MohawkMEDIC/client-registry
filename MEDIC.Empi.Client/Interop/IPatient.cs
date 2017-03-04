using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace MEDIC.Empi.Client.Interop
{
    /// <summary>
    /// Patient interface
    /// </summary>
    [Guid("8AAE7392-B084-4F2B-9C40-F0D23092647F")]
    [ComVisible(true)]
    [InterfaceType(ComInterfaceType.InterfaceIsDual)]
    public interface IPatient
    {
        /// <summary>
        /// Surnames
        /// </summary>
        PatientNameCollection Names { get; set; }

        /// <summary>
        /// Gender code
        /// </summary>
        GenderType Gender { get; set; }

        /// <summary>
        /// Date of birth
        /// </summary>
        DateTime DateOfBirth { get; set; }

        /// <summary>
        /// Mother's given name
        /// </summary>
        PatientName MotherName { get; set; }

        /// <summary>
        /// Identifiers
        /// </summary>
        IdentifierTypeCollection Identifiers { get; set; }

        /// <summary>
        /// Gets or sets the mothers identifiers
        /// </summary>
        IdentifierTypeCollection MotherIdentifiers { get; set; }

        /// <summary>
        /// Gets or sets the telephone number
        /// </summary>
        String Telephone { get; set; }

        /// <summary>
        /// Gets or sets the addresses
        /// </summary>
        PatientAddressCollection Addresses { get; set; }

        /// <summary>
        /// Deceased date
        /// </summary>
        DateTime DeceasedDate { get; set; }

        /// <summary>
        /// Multiple birth indicator
        /// </summary>
        bool MultipleBirthIndicator { get; set; }

    }

    /// <summary>
    /// Patient interface
    /// </summary>
    [Guid("48349283-B084-4F2B-9C40-F0D23092647F")]
    [ComVisible(true)]
    [InterfaceType(ComInterfaceType.InterfaceIsDual)]
    public interface IPatientSearchResult
    {

        /// <summary>
        /// Count of objects
        /// </summary>
        int Count { get; }

        /// <summary>
        /// Total results
        /// </summary>
        int TotalResults { get; }

        /// <summary>
        /// Offset
        /// </summary>
        int Offset { get; }

        /// <summary>
        /// Pointer
        /// </summary>
        object Pointer { get; }

        /// <summary>
        /// Get a specified patient
        /// </summary>
        Patient this[int index] { get; }
    }

    /// <summary>
    /// Patient name type
    /// </summary>
    [Guid("66684938-B084-4F2B-9C40-F0D23092647F")]
    [ComVisible(true)]
    [InterfaceType(ComInterfaceType.InterfaceIsDual)]
    public interface IPatientName {

        /// <summary>
        /// Gets or sets the use of the name
        /// </summary>
        PatientNameUse Use { get; set; }

        /// <summary>
        /// Gets or sets the representation
        /// </summary>
        PatientNameRepresentation Representation { get; set; }

        /// <summary>
        /// Given name
        /// </summary>
        string GivenName { get; set; }

        /// <summary>
        /// Gets or sets the middle names or initials
        /// </summary>
        string SecondNamesOrInitials { get; set; }

        /// <summary>
        /// Gets or sets the surname
        /// </summary>
        string Surname { get; set; }

        /// <summary>
        /// Gets or sets the maiden surnames
        /// </summary>
        string MaidenSurname { get; set; }
    }

    /// <summary>
    /// Patient address type
    /// </summary>
    [Guid("6656FB34-B084-4F2B-9C40-F0D23092647F")]
    [ComVisible(true)]
    [InterfaceType(ComInterfaceType.InterfaceIsDual)]
    public interface IPatientAddress
    {

        /// <summary>
        /// Classifier
        /// </summary>
        PatientAddressClassifier Classifier { get; set; }

        /// <summary>
        /// Gets or sets the street address line
        /// </summary>
        string StreetAddressLine { get; set; }

        /// <summary>
        /// Gets or sets the city
        /// </summary>
        string City { get; set; }

        /// <summary>
        /// Gets or sets the state or province
        /// </summary>
        string StateOrProvince { get; set; }

        /// <summary>
        /// Gets or sets the country
        /// </summary>
        string Country { get; set; }

        /// <summary>
        /// Gets r sets the zip or postal code
        /// </summary>
        string ZipOrPostalCode { get; set; }

        /// <summary>
        /// Gets or sets the country
        /// </summary>
        string County { get; set; }

        /// <summary>
        /// Gets or sets additional locator
        /// </summary>
        string CensusTract { get; set; }

        /// <summary>
        /// Other locator
        /// </summary>
        string OtherLocator { get; set; }
    }

    /// <summary>
    /// Address collection type
    /// </summary>
    [ComVisible(true)]
    [Guid("AE3E4DB4-5359-4E9A-910B-4EFC682AF570")]
    [InterfaceType(ComInterfaceType.InterfaceIsDual)]
    public interface IPatientAddressCollection
    {

        /// <summary>
        /// Gets the count of addresses in the collection
        /// </summary>
        int Count { get; }

        /// <summary>
        /// Adds an address to the collection
        /// </summary>
        void Add(PatientAddress address);

        /// <summary>
        /// Add an address to the collection given its type
        /// </summary>
        void AddFull(PatientAddressClassifier address, String country, String stateProvince, String countyParish, String cityVillage, String streetAddress, String additionalLocator, String zipPostCode);

        /// <summary>
        /// Add an address to the collection given its type
        /// </summary>
        void AddBasic(String country, String stateProvince, String countyParish, String cityVillage, String streetAddress, String additionalLocator, String zipPostCode);

        /// <summary>
        /// Find by address classifier
        /// </summary>
        PatientAddress Find(PatientAddressClassifier classifier);

        /// <summary>
        /// Remove the name at the specified index
        /// </summary>
        void RemoveAt(int index);

        /// <summary>
        /// Retrieve a patient address at the specified name
        /// </summary>
        PatientAddress this[int index] { get; }
    }

    /// <summary>
    /// Address collection type
    /// </summary>
    [ComVisible(true)]
    [Guid("AE3E4DB4-2314-4E9A-910B-4EFC682AF570")]
    [InterfaceType(ComInterfaceType.InterfaceIsDual)]
    public interface IPatientNameCollection
    {
        /// <summary>
        /// Gets the count of addresses in the collection
        /// </summary>
        int Count { get; }

        /// <summary>
        /// Adds an address to the collection
        /// </summary>
        void Add(PatientName address);

        /// <summary>
        /// Add an address to the collection given its type
        /// </summary>
        void AddFull(PatientNameUse use, PatientNameRepresentation representation, String surname, String givenName);

        /// <summary>
        /// Add an address to the collection given its type
        /// </summary>
        void AddBasic(String surname, String givenName);

        /// <summary>
        /// Find patient name by use
        /// </summary>
        PatientName Find(PatientNameUse use);

        /// <summary>
        /// Remove the name at the specified index
        /// </summary>
        void RemoveAt(int index);

        /// <summary>
        /// Retrieve a patient address at the specified name
        /// </summary>
        PatientName this[int index] { get; }
    }
}
