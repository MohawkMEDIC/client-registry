using MEDIC.Empi.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace MEDIC.Empi.Client.Interop
{
    [Guid("1CC45636-12B4-4F07-A51E-2C1D32A5F3D2")]
    [ComVisible(true)]
    [InterfaceType(ComInterfaceType.InterfaceIsDual)]
    public interface IEmpiClient
    {

        /// <summary>
        /// Gets or sets the sending facility
        /// </summary>
        string SendingFacility { get; set; }

        /// <summary>
        /// Gets or sets the sending devvice
        /// </summary>
        string SendingApplication { get; set; }

        /// <summary>
        /// Get or sets the receiving application
        /// </summary>
        string ReceivingApplication { get; set; }

        /// <summary>
        /// Get or set the receiving facility
        /// </summary>
        string ReceivingFacility { get; set; }

        /// <summary>
        /// Client certificate name
        /// </summary>
        void SetClientCertificate(String subject, String store, String location);

        /// <summary>
        /// Shows a certificate picker
        /// </summary>
        void PickClientCertificate();

        /// <summary>
        /// Gets or sets the endpoint
        /// </summary>
        string Endpoint { get; set; }

        /// <summary>
        /// Open the connection
        /// </summary>
        void Open();

        /// <summary>
        /// Close the connection
        /// </summary>
        void Close();

        /// <summary>
        /// Query for patient
        /// </summary>
        PatientSearchResult DemographicsQuery(Patient match);

        /// <summary>
        /// Query for patient
        /// </summary>
        PatientSearchResult DemographicsQuery(Patient match, int offset, int count, object continuationPointer);

        /// <summary>
        /// Query for patient
        /// </summary>
        String CrossReferenceQuery(PatientIdentifier localId, String remoteDomain);

        /// <summary>
        /// Registers a patient
        /// </summary>
        void RegisterPatient(Patient patient);

        /// <summary>
        /// Update patient
        /// </summary>
        void UpdatePatient(Patient patient);

    }
}
