/**
 * Copyright 2012-2017 Mohawk College of Applied Arts and Technology
 * 
 * Licensed under the Apache License, Version 2.0 (the "License"); you 
 * may not use this file except in compliance with the License. You may 
 * obtain a copy of the License at 
 * 
 * http://www.apache.org/licenses/LICENSE-2.0 
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS, WITHOUT
 * WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the 
 * License for the specific language governing permissions and limitations under 
 * the License.
 * 
 * User: justi
 * Date: 3-3-2017
 */
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
