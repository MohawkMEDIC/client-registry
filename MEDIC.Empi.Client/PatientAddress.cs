using MEDIC.Empi.Client.Interop;
using System.Runtime.InteropServices;
using System;
using System.Collections.Generic;

namespace MEDIC.Empi.Client
{
    /// <summary>
    /// Patient address classifiers
    /// </summary>
    public enum PatientAddressClassifier
    {
        None = 0,
        Bad = 1,
        BirthLocation = 2,
        Billing = 3,
        CurrentOrTemporary = 4,
        Home = 5,
        CountryOfOrigin = 6,
        Legal = 7,
        Mailing = 8,
        Permanent = 9,
        Vacation = 10
    }

    /// <summary>
    /// Patient address type
    /// </summary>
    [Guid("BD3454EE-3922-4E83-A573-4810C7CA989A")]
    [ComVisible(true)]
    [ClassInterface(ClassInterfaceType.None)]
    public class PatientAddress : IPatientAddress
    {

        /// <summary>
        /// Identifies the use of the address
        /// </summary>
        public PatientAddressClassifier Classifier { get; set; }

        /// <summary>
        /// Gets or sets the street address line
        /// </summary>
        public string StreetAddressLine { get; set; }

        /// <summary>
        /// Gets or sets the city
        /// </summary>
        public string City { get; set; }

        /// <summary>
        /// Gets or sets the state or province
        /// </summary>
        public string StateOrProvince { get; set; }

        /// <summary>
        /// Gets or sets the country
        /// </summary>
        public string Country { get; set; }

        /// <summary>
        /// Gets r sets the zip or postal code
        /// </summary>
        public string ZipOrPostalCode { get; set; }

        /// <summary>
        /// Gets or sets the country
        /// </summary>
        public string County { get; set; }

        /// <summary>
        /// Gets or sets additional locator
        /// </summary>
        public string CensusTract { get; set; }

        /// <summary>
        /// Other locator
        /// </summary>
        public string OtherLocator { get; set; }
    }

    /// <summary>
    /// Patient address type
    /// </summary>
    [Guid("BD345FDE-3922-4E83-A523-4810C7CA989A")]
    [ComVisible(true)]
    [ClassInterface(ClassInterfaceType.None)]
    public class PatientAddressCollection : List<PatientAddress>, IPatientAddressCollection
    {
        /// <summary>
        /// Add a specified address
        /// </summary>
        public void AddFull(PatientAddressClassifier addressType, string country, string stateProvince, string countyParish, string cityVillage, string streetAddress, string additionalLocator, string zipPostCode)
        {
            this.Add(new PatientAddress()
            {
                Classifier = addressType,
                Country = country,
                StateOrProvince = stateProvince,
                County = countyParish,
                City = cityVillage,
                StreetAddressLine = streetAddress,
                ZipOrPostalCode = zipPostCode
            });
        }

        /// <summary>
        /// Add a specified address
        /// </summary>
        public void AddBasic(string country, string stateProvince, string countyParish, string cityVillage, string streetAddress, string additionalLocator, string zipPostCode)
        {
            this.Add(new PatientAddress()
            {
                Classifier = PatientAddressClassifier.None,
                Country = country,
                StateOrProvince = stateProvince,
                County = countyParish,
                City = cityVillage,
                StreetAddressLine = streetAddress,
                ZipOrPostalCode = zipPostCode
            });
        }

        /// <summary>
        /// Find an address
        /// </summary>
        public PatientAddress Find(PatientAddressClassifier classifier)
        {
            return this.Find(o => o.Classifier == classifier);
        }
    }
}