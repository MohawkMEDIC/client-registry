using MEDIC.Empi.Client.Exceptions;
using MEDIC.Empi.Client.Interop;
using MEDIC.Empi.Client.Transport;
using NHapi.Base.Model;
using NHapi.Base.Util;
using NHapi.Model.V25.Message;
using NHapi.Model.V25.Segment;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace MEDIC.Empi.Client
{

    [Guid("455EE614-FB4D-4127-8886-1A5343A932BE")]
    [ComVisible(true)]
    [ClassInterface(ClassInterfaceType.None)]
    [ProgId("MEDICEmpiClient.EmpiClient")]
    public class EmpiClient : IEmpiClient
    {

        // Address codes
        private readonly Dictionary<PatientAddressClassifier, String> m_addressCodes = new Dictionary<PatientAddressClassifier, string>()
        {
            { PatientAddressClassifier.Bad, "BA" },
            { PatientAddressClassifier.Billing, "BI" },
            { PatientAddressClassifier.BirthLocation, "BDL" },
            { PatientAddressClassifier.CountryOfOrigin, "F" },
            { PatientAddressClassifier.CurrentOrTemporary, "C" },
            { PatientAddressClassifier.Home, "H" },
            { PatientAddressClassifier.Legal, "L" },
            { PatientAddressClassifier.Mailing, "M" },
            { PatientAddressClassifier.None, "" },
            { PatientAddressClassifier.Permanent, "P" },
            { PatientAddressClassifier.Vacation, "V" }
        };

        // Name representations
        private readonly Dictionary<PatientNameRepresentation, String> m_nameRepresentationCodes = new Dictionary<PatientNameRepresentation, string>()
        {
            { PatientNameRepresentation.Alphabetic, "A" },
            { PatientNameRepresentation.Ideographic, "I" },
            { PatientNameRepresentation.Phonetic, "P" }
        };

        // Name use codes
        private readonly Dictionary<PatientNameUse, String> m_nameUseCodes = new Dictionary<PatientNameUse, string>()
        {
            { PatientNameUse.Alias, "A" },
            { PatientNameUse.Birth, "B" },
            { PatientNameUse.Display, "D" },
            { PatientNameUse.Legal, "L" },
            { PatientNameUse.Maiden, "M" },
            { PatientNameUse.Nickname, "N" },
            { PatientNameUse.None, "U" },
            { PatientNameUse.Pseudonym, "S" }
        };

        // The sender
        private MllpMessageSender m_sender;

        private string m_sendingFacility;
        private string m_sendingDevice;
        private string m_endpoint;
        private X509Certificate2 m_certificate;
        private PatientNameUse classifier;

        /// <summary>
        /// Facility
        /// </summary>
        public String SendingFacility
        {
            get { return this.m_sendingFacility; }
            set
            {
                if (this.m_sender == null)
                    this.m_sendingFacility = value;
                else throw new InvalidOperationException("Cannot set facility after connection opened");
            }
        }

        /// <summary>
        /// Device
        /// </summary>
        public String SendingApplication
        {
            get { return this.m_sendingDevice; }
            set
            {
                if (this.m_sender == null)
                    this.m_sendingDevice = value;
                else throw new InvalidOperationException("Cannot set device after connection opened");
            }

        }

        /// <summary>
        /// Endpoint
        /// </summary>
        public String Endpoint
        {
            get { return this.m_endpoint; }
            set
            {
                if (this.m_endpoint == null)
                    this.m_endpoint = value;
                else throw new InvalidOperationException("Cannot set endpoint after connection opened");
            }
        }

        /// <summary>
        /// Receiving application
        /// </summary>
        public string ReceivingApplication { get; set; }

        /// <summary>
        /// Receiving facility
        /// </summary>
        public string ReceivingFacility { get; set; }

        /// <summary>
        /// Open
        /// </summary>
        public void Open()
        {
            if (string.IsNullOrEmpty(this.SendingApplication) || string.IsNullOrEmpty(this.SendingFacility))
                throw new InvalidOperationException("Missing SendingDevice or SendingFacility");
            else if (string.IsNullOrEmpty(this.Endpoint))
                throw new InvalidOperationException("Missing Endpoint");
            this.m_sender = new MllpMessageSender(new Uri(this.Endpoint), null, null);
        }

        /// <summary>
        /// Close
        /// </summary>
        public void Close()
        {
            this.m_sender = null;
        }

        public EmpiClient()
        {
        }

        /// <summary>
        /// Register patient
        /// </summary>
        public void RegisterPatient(Patient p)
        {
            if (this.m_sender == null)
                throw new InvalidOperationException("Connection not open"); ;
            try
            {
                ADT_A01 request = this.CreateADT(p);
                this.UpdateMSH(request.MSH, "ADT_A01", "ADT", "A04");
                var retVal = this.m_sender.SendAndReceive<NHapi.Model.V231.Message.ACK>(request);

                // Is the response success?
                if (retVal == null || !retVal.MSA.AcknowledgementCode.Value.EndsWith("A"))
                {
                    foreach (var err in retVal.ERR.GetErrorCodeAndLocation())
                        Trace.TraceError("CR ERR: {0} ({1})", err.CodeIdentifyingError.Text, err.CodeIdentifyingError.AlternateText);
                    throw new IntegrationException(
                        $"REMOTE: {retVal.ERR.GetErrorCodeAndLocation(0).CodeIdentifyingError.Text.Value} - {retVal.ERR.GetErrorCodeAndLocation(0).CodeIdentifyingError.AlternateText}"
                        );

                }

            }
            catch (Exception e)
            {
                Trace.TraceError("Error registering patient: {0}", e);
                throw;
            }
        }

        /// <summary>
        /// Update patient
        /// </summary>
        public void UpdatePatient(Patient p)
        {
            if (this.m_sender == null)
                throw new InvalidOperationException("Connection not open"); ;
            try
            {
                ADT_A01 request = this.CreateADT(p);
                this.UpdateMSH(request.MSH, "ADT_A01", "ADT", "A08");
                var retVal = this.m_sender.SendAndReceive<NHapi.Model.V231.Message.ACK>(request);

                // Is the response success?
                if (retVal == null || !retVal.MSA.AcknowledgementCode.Value.EndsWith("A"))
                {
                    foreach (var err in retVal.ERR.GetErrorCodeAndLocation())
                        Trace.TraceError("CR ERR: {0} ({1})", err.CodeIdentifyingError.Text, err.CodeIdentifyingError.AlternateText);
                    throw new IntegrationException(retVal.ERR.GetErrorCodeAndLocation(0).CodeIdentifyingError.Text.Value);
                }

            }
            catch (Exception e)
            {
                Trace.TraceError("Error registering patient: {0}", e);
                throw;
            }

        }

        /// <summary>
        /// Cross reference an identifier only
        /// </summary>
        public String CrossReferenceQuery(PatientIdentifier localId, String targetDomain)
        {
            if (this.m_sender == null)
                throw new InvalidOperationException("Connection not open"); ;
            try
            {
                var request = this.CreatePIXSearch(localId, targetDomain);
                var retVal = this.m_sender.SendAndReceive<RSP_K23>(request);

                // Is the response success?
                if (retVal == null || !retVal.MSA.AcknowledgmentCode.Value.EndsWith("A"))
                {
                    foreach (var err in retVal.ERR.GetErrorCodeAndLocation())
                        Trace.TraceError("CR ERR: {0} ({1})", err.CodeIdentifyingError.Text, err.CodeIdentifyingError.AlternateText);
                    throw new IntegrationException(retVal.ERR.GetErrorCodeAndLocation(0).CodeIdentifyingError.Text.Value);
                }

                return retVal.QUERY_RESPONSE.PID.GetPatientIdentifierList().FirstOrDefault(o => o.AssigningAuthority.NamespaceID.Value == targetDomain)?.IDNumber.Value;
            }
            catch (Exception e)
            {
                Trace.TraceError("Error registering patient: {0}", e);
                throw;
            }

        }

        /// <summary>
        /// Query match
        /// </summary>
        public PatientSearchResult DemographicsQuery(Patient queryMatch)
        {
            if (this.m_sender == null)
                throw new InvalidOperationException("Connection not open");
            try
            {
                var searchParms = this.CreateSearchParms(queryMatch);
                var request = this.CreatePDQSearch(0, 100, null, searchParms);
                var result = this.m_sender.SendAndReceive<RSP_K21>(request);

                // Is the response success?
                if (result == null || !result.MSA.AcknowledgmentCode.Value.EndsWith("A"))
                {
                    foreach (var err in result.ERR.GetErrorCodeAndLocation())
                        Trace.TraceError("CR ERR: {0} ({1})", err.CodeIdentifyingError.Text, err.CodeIdentifyingError.AlternateText);
                    throw new IntegrationException(result.ERR.GetErrorCodeAndLocation(0).CodeIdentifyingError.Text.Value);
                }

                // Create result
                var retVal = new PatientSearchResult()
                {
                    Pointer = result.DSC.ContinuationPointer.Value,
                    Count = result.QUERY_RESPONSERepetitionsUsed
                };
                if (!String.IsNullOrEmpty(result.QAK.HitCount.Value))
                    retVal.TotalResults = Int32.Parse(result.QAK.HitCount.Value);

                for (var i = 0; i < result.QUERY_RESPONSERepetitionsUsed; i++)
                {
                    var p = new Patient();
                    this.CopyChildRecordFields(p, result.GetQUERY_RESPONSE(i).PID);
                    retVal.Results.Add(p);
                }
                return retVal;
            }
            catch (Exception e)
            {
                Trace.TraceError("Error registering patient: {0}", e);
                throw;
            }

        }

        /// <summary>
        /// Create search parameters
        /// </summary>
        private KeyValuePair<String, String>[] CreateSearchParms(Patient queryMatch)
        {
            var retVal = new List<KeyValuePair<String, String>>();
            if (!String.IsNullOrEmpty(queryMatch.Names.FirstOrDefault()?.GivenName))
                retVal.Add(new KeyValuePair<string, string>("@PID.5.2", queryMatch.Names.FirstOrDefault().GivenName));
            if (!String.IsNullOrEmpty(queryMatch.Names.FirstOrDefault()?.Surname))
                retVal.Add(new KeyValuePair<string, string>("@PID.5.1", queryMatch.Names.FirstOrDefault().Surname));
            switch (queryMatch.Gender)
            {
                case GenderType.Male:
                    retVal.Add(new KeyValuePair<string, string>("@PID.8", "M"));
                    break;
                case GenderType.Female:
                    retVal.Add(new KeyValuePair<string, string>("@PID.8", "F"));
                    break;
                case GenderType.Undifferentiated:
                    retVal.Add(new KeyValuePair<string, string>("@PID.8", "UN"));
                    break;
            }

            if (queryMatch.DateOfBirth != default(DateTime))
                retVal.Add(new KeyValuePair<string, string>("@PID.7", queryMatch.DateOfBirth.ToString("yyyyMMdd")));

            // Identifiers
            if (queryMatch.Identifiers?.Count > 0)
            {
                retVal.Add(new KeyValuePair<string, string>("@PID.3.1", queryMatch.Identifiers.OfType<PatientIdentifier>().First().Value));
                retVal.Add(new KeyValuePair<string, string>("@PID.3.4.1", queryMatch.Identifiers.OfType<PatientIdentifier>().First().Domain));
            }

            return retVal.ToArray();
        }

        /// <summary>
        /// Query match
        /// </summary>
        public PatientSearchResult DemographicsQuery(Patient queryMatch, int offset, int count, object pointer)
        {
            if (this.m_sender == null)
                throw new InvalidOperationException("Connection not open"); ;
            try
            {
                var searchParms = this.CreateSearchParms(queryMatch);
                var request = this.CreatePDQSearch(offset, count, pointer, searchParms);
                var result = this.m_sender.SendAndReceive<RSP_K21>(request);

                // Is the response success?
                if (result == null || !result.MSA.AcknowledgmentCode.Value.EndsWith("A"))
                {
                    foreach (var err in result.ERR.GetErrorCodeAndLocation())
                        Trace.TraceError("CR ERR: {0} ({1})", err.CodeIdentifyingError.Text, err.CodeIdentifyingError.AlternateText);
                    throw new IntegrationException(result.ERR.GetErrorCodeAndLocation(0).CodeIdentifyingError.Text.Value);
                }

                // Create result
                var retVal = new PatientSearchResult()
                {
                    Pointer = result.DSC.ContinuationPointer.Value,
                    Count = result.QUERY_RESPONSERepetitionsUsed,
                    TotalResults = Int32.Parse(result.QAK.HitsRemaining.Value)
                };
                for (var i = 0; i < result.QUERY_RESPONSERepetitionsUsed; i++)
                {
                    var p = new Patient();
                    this.UpdatePatient(p);
                    retVal.Results.Add(p);
                }
                return retVal;
            }
            catch (Exception e)
            {
                Trace.TraceError("Error registering patient: {0}", e);
                throw;
            }

        }


        /// <summary>
        /// Create a PIX search message
        /// </summary>
        private QBP_Q21 CreatePIXSearch(PatientIdentifier localId, string targetDomain)
        {
            QBP_Q21 retVal = new QBP_Q21();
            this.UpdateMSH(retVal.MSH, "QBP_Q21", "QBP", "Q23");
            Terser terser = new Terser(retVal);
            terser.Set("/QPD-1", "IHE PIX Query");
            terser.Set("/QPD-2", Guid.NewGuid().ToString().Substring(0, 8));
            terser.Set("/QPD-3-1", localId.Value);
            terser.Set("/QPD-3-4-1", localId.Domain);
            terser.Set("/QPD-4-4-1", targetDomain);
            return retVal;
        }

        /// <summary>
        /// Update a child record to match the PID segment
        /// </summary>
        private void CopyChildRecordFields(Patient patient, PID pid)
        {

            Trace.TraceInformation("Parsing patient structure");
            // Gender
            switch (pid.AdministrativeSex.Value)
            {
                case "M":
                    patient.Gender = GenderType.Male;
                    break;
                case "F":
                    patient.Gender = GenderType.Female;
                    break;
                case "UN":
                    patient.Gender = GenderType.Undifferentiated;
                    break;
            }

            // Date of birth
            if (!String.IsNullOrEmpty(pid.DateTimeOfBirth.Time.Value))
                patient.DateOfBirth = pid.DateTimeOfBirth.Time.GetAsDate();

            // Is part of a multiple birth?
            if (!String.IsNullOrEmpty(pid.MultipleBirthIndicator.Value))
                patient.MultipleBirthIndicator = pid.MultipleBirthIndicator.Value == "Y";

            // Phone
            if (!String.IsNullOrEmpty(pid.GetPhoneNumberHome(0).TelephoneNumber.Value))
                patient.Telephone = pid.GetPhoneNumberHome(0).TelephoneNumber.Value;

            // Mother's name?
            if (pid.MotherSMaidenNameRepetitionsUsed > 0)
            {
                patient.MotherName = new PatientName()
                {
                    Surname = pid.GetMotherSMaidenName()[0].FamilyName.Surname.Value,
                    GivenName = pid.GetMotherSMaidenName()[0].GivenName.Value
                };
            }

            // Name
            if (pid.PatientNameRepetitionsUsed > 0)
                for (int i = 0; i < pid.PatientNameRepetitionsUsed; i++)
                {
                    PatientNameUse use = PatientNameUse.None;
                    if (!String.IsNullOrEmpty(pid.GetPatientName(i).NameTypeCode.Value))
                        use = this.m_nameUseCodes.FirstOrDefault(o => o.Value == pid.GetPatientName(i).NameTypeCode.Value).Key;
                    PatientNameRepresentation representation = PatientNameRepresentation.Alphabetic;
                    if (!String.IsNullOrEmpty(pid.GetPatientName(i).NameRepresentationCode.Value))
                        representation = this.m_nameRepresentationCodes.FirstOrDefault(o => o.Value == pid.GetPatientName(i).NameRepresentationCode.Value).Key;

                    patient.Names.Add(new PatientName()
                    {
                        Surname = pid.GetPatientName(i).FamilyName.Surname.Value,
                        MaidenSurname = pid.GetPatientName(i).FamilyName.OwnSurname.Value,
                        GivenName = pid.GetPatientName(i).GivenName.Value,
                        SecondNamesOrInitials = pid.GetPatientName(i).SecondAndFurtherGivenNamesOrInitialsThereof.Value,
                        Use = use,
                        Representation = representation
                    });
                }

            // Identification numbers
            foreach (var id in pid.GetPatientIdentifierList())
                patient.Identifiers.Add(new PatientIdentifier()
                {
                    Domain = id.AssigningAuthority.NamespaceID.Value,
                    Value = id.IDNumber.Value
                });

            // Address information
            // TODO: How does this map to GIIS?
            if (pid.PatientAddressRepetitionsUsed > 0)
            {
                for (int i = 0; i < pid.PatientAddressRepetitionsUsed; i++)
                {
                    PatientAddressClassifier classifier = PatientAddressClassifier.None;
                    if (!String.IsNullOrEmpty(pid.GetPatientAddress(i).AddressType.Value))
                        classifier = this.m_addressCodes.FirstOrDefault(o => o.Value == pid.GetPatientAddress(i).AddressType.Value).Key;

                    patient.Addresses.Add(new PatientAddress()
                    {
                        CensusTract = pid.GetPatientAddress(i).CensusTract.Value,
                        City = pid.GetPatientAddress(i).City.Value,
                        Country = pid.GetPatientAddress(i).Country.Value,
                        County = pid.GetPatientAddress(i).CountyParishCode.Value,
                        OtherLocator = pid.GetPatientAddress(i).OtherDesignation.Value,
                        StateOrProvince = pid.GetPatientAddress(i).StateOrProvince.Value,
                        StreetAddressLine = pid.GetPatientAddress(i).StreetAddress.StreetOrMailingAddress.Value,
                        ZipOrPostalCode = pid.GetPatientAddress(i).ZipOrPostalCode.Value,
                        Classifier = classifier
                    });
                }
            }

            // Death?
            if (!String.IsNullOrEmpty(pid.PatientDeathDateAndTime.Time.Value))
            {
                patient.DeceasedDate = pid.PatientDeathDateAndTime.Time.GetAsDate();
            }

            Trace.TraceInformation("Successfully parsed patient structure");
        }

        /// <summary>
        /// Create an ADT message for the child
        /// </summary>
        private ADT_A01 CreateADT(Patient patient)
        {
            ADT_A01 retVal = new ADT_A01();
            retVal.MSH.VersionID.VersionID.Value = "2.3.1";

            this.UpdateMSH(retVal.MSH, "ADT_A01", "ADT", "A01");
            this.UpdatePID(retVal.PID, patient);

            return retVal;
        }

        /// <summary>
        /// Update PID segment from a child
        /// </summary>
        private void UpdatePID(PID pid, Patient patient)
        {
            switch (patient.Gender)
            {
                case GenderType.Male:
                    pid.AdministrativeSex.Value = "M";
                    break;
                case GenderType.Female:
                    pid.AdministrativeSex.Value = "F";
                    break;
                case GenderType.Undifferentiated:
                    pid.AdministrativeSex.Value = "UN";
                    break;
            }

            if (patient.DateOfBirth != default(DateTime))
                pid.DateTimeOfBirth.Time.Value = patient.DateOfBirth.ToString("yyyyMMdd");

            if (!String.IsNullOrEmpty(patient.Telephone))
                pid.GetPhoneNumberHome(0).AnyText.Value = patient.Telephone;

            if (!String.IsNullOrEmpty(patient.MotherName?.GivenName))
                pid.GetMotherSMaidenName(0).GivenName.Value = patient.MotherName?.GivenName;
            if (!String.IsNullOrEmpty(patient.MotherName?.Surname))
                pid.GetMotherSMaidenName(0).FamilyName.Surname.Value = patient.MotherName?.Surname;

            if (patient.Names != null)
                foreach (var xpn in patient.Names)
                {
                    int i = pid.PatientNameRepetitionsUsed;

                    pid.GetPatientName(i).GivenName.Value = xpn?.GivenName;
                    pid.GetPatientName(i).SecondAndFurtherGivenNamesOrInitialsThereof.Value = xpn?.SecondNamesOrInitials;
                    pid.GetPatientName(i).FamilyName.Surname.Value = xpn?.Surname;
                    pid.GetPatientName(i).FamilyName.OwnSurname.Value = xpn?.MaidenSurname;
                    pid.GetPatientName(i).NameTypeCode.Value = this.m_nameUseCodes[xpn.Use];
                    pid.GetPatientName(i).NameRepresentationCode.Value = this.m_nameRepresentationCodes[xpn.Representation];

                }

            // Domicile
            if (patient.Addresses != null)
            {
                foreach (var xad in patient.Addresses)
                {
                    var adi = pid.PatientAddressRepetitionsUsed;
                    List<AbstractPrimitive> addressParts = new List<AbstractPrimitive>() {
                        pid.GetPatientAddress(adi).OtherDesignation, // Kitongoji
                        pid.GetPatientAddress(adi).StreetAddress.StreetOrMailingAddress, // Street or Village
                        pid.GetPatientAddress(adi).City, // Ward
                        pid.GetPatientAddress(adi).CountyParishCode, // district
                        pid.GetPatientAddress(adi).StateOrProvince, // Region
                        pid.GetPatientAddress(adi).Country, // National
                        pid.GetPatientAddress(adi).CensusTract, //
                        pid.GetPatientAddress(adi).AddressType
                    };

                    String addressType = String.Empty;
                    this.m_addressCodes.TryGetValue(xad.Classifier, out addressType);

                    // Queue places 
                    List<String> addressValues = new List<string>()
                    {
                        xad.OtherLocator,
                        xad.StreetAddressLine,
                        xad.City,
                        xad.County,
                        xad.StateOrProvince,
                        xad.Country,
                        xad.CensusTract,
                        addressType
                    };

                    for (int i = 0; i < addressParts.Count; i++)
                        addressParts[i].Value = addressValues[i];
                }
            }

            if (patient.DeceasedDate != default(DateTime))
            {
                pid.PatientDeathIndicator.Value = "Y";
                pid.PatientDeathDateAndTime.Time.Value = patient.DeceasedDate.ToString("yyyyMMdd");
            }

            // Identifiers
            var idList = patient.Identifiers.OfType<PatientIdentifier>().ToArray();
            for (int i = 0; i < patient.Identifiers.Count; i++)
            {
                pid.GetPatientIdentifierList(i).IDNumber.Value = idList[i].Value;
                pid.GetPatientIdentifierList(i).AssigningAuthority.NamespaceID.Value = idList[i].Domain;
            }

        }

        /// <summary>
        /// Update the MSH header
        /// </summary>
        private void UpdateMSH(MSH header, String message, String structure, String trigger)
        {
            // Message header
            header.AcceptAcknowledgmentType.Value = "AL"; // Always send response
            header.DateTimeOfMessage.Time.Value = DateTime.Now.ToString("yyyyMMddHHmmss"); // Date/time of creation of message
            header.MessageControlID.Value = Guid.NewGuid().ToString(); // Unique id for message
            header.MessageType.MessageStructure.Value = message; // Message structure type (Query By Parameter Type 21)
            header.MessageType.MessageCode.Value = structure; // Message Structure Code (Query By Parameter)
            header.MessageType.TriggerEvent.Value = trigger; // Trigger event (Event Query 22)
            header.ProcessingID.ProcessingID.Value = "P"; // Production
            header.ReceivingApplication.NamespaceID.Value = this.ReceivingApplication; // Client Registry
            header.ReceivingFacility.NamespaceID.Value = this.ReceivingFacility; // SAMPLE
            header.SendingApplication.NamespaceID.Value = this.SendingApplication; // What goes here?
            header.SendingFacility.NamespaceID.Value = this.SendingFacility; // You're at the college ... right?
        }

        /// <summary>
        /// Create a PDQ search message
        /// </summary>
        /// <param name="filters">The parameters for query</param>
        private QBP_Q21 CreatePDQSearch(int offset, int count, object state, params KeyValuePair<String, String>[] filters)
        {
            // Search - Construct a v2 message this is found in IHE ITI TF-2:3.21
            QBP_Q21 message = new QBP_Q21();

            this.UpdateMSH(message.MSH, "QBP_Q21", "QBP", "Q22");
            //message.MSH.VersionID.VersionID.Value = "2.3.1";

            // Message query
            message.QPD.MessageQueryName.Identifier.Value = "Patient Demographics Query";
            message.DSC.ContinuationPointer.Value = state?.ToString();
            message.RCP.QuantityLimitedRequest.Quantity.Value = count.ToString();
            message.RCP.QuantityLimitedRequest.Units.Identifier.Value = "RD";

            // Sometimes it is easier to use a terser
            Terser terser = new Terser(message);
            terser.Set("/QPD-2", Guid.NewGuid().ToString()); // Tag of the query
            terser.Set("/QPD-1", "Patient Demographics Query"); // Name of the query
            for (int i = 0; i < filters.Length; i++)
            {
                terser.Set(String.Format("/QPD-3({0})-1", i), filters[i].Key);
                terser.Set(String.Format("/QPD-3({0})-2", i), filters[i].Value);
            }

            return message;
        }

        /// <summary>
        /// Select a client certificate
        /// </summary>
        public void SetClientCertificate(string subject, string store, string location)
        {
            if (this.m_sender != null)
                throw new InvalidOperationException("Cannot set the certificate after connection open");
            StoreName storeName = (StoreName)Enum.Parse(typeof(StoreName), store);
            StoreLocation storeLocation = (StoreLocation)Enum.Parse(typeof(StoreLocation), store);
            this.m_certificate = MllpMessageSender.FindCertificate(storeName, storeLocation, X509FindType.FindBySubjectName, subject);
        }

        /// <summary>
        /// Pick client certificate
        /// </summary>
        public void PickClientCertificate()
        {
            throw new NotImplementedException();
        }
    }
}
