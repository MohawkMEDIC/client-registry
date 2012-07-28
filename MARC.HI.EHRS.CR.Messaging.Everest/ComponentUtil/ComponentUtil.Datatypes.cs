using System;
using System.Collections.Generic;
using System.Linq;
using MARC.Everest.Connectors;
using MARC.HI.EHRS.CR.Core.ComponentModel;
using MARC.HI.EHRS.SVC.Core.DataTypes;
using MARC.Everest.DataTypes;
using MARC.Everest.Attributes;
using MARC.HI.EHRS.SVC.Core.Services;
using MARC.Everest.RMIM.CA.R020402.Vocabulary;
using System.ComponentModel;
using MARC.HI.EHRS.SVC.Core;
using MARC.HI.EHRS.SVC.Core.ComponentModel.Components;
using MARC.HI.EHRS.SVC.Core.ComponentModel;

namespace MARC.HI.EHRS.CR.Messaging.Everest
{
    /// <summary>
    /// Component util aides the message functions in componentizing the messages
    /// into the data model
    /// </summary>
    public partial class ComponentUtil : IUsesHostContext
    {

        // Localization service
        private ILocalizationService m_localeService = null;

        /// <summary>
        /// No timezone specified
        /// </summary>
        private const string ERR_NOTZ = "Timestamp value is missing a timezone '{0}'";

        /// <summary>
        /// Timestamp set contains no parts
        /// </summary>
        private const string ERR_NOTS_PARTS = "Timestamp value is empty";

        /// <summary>
        /// No code has been specified
        /// </summary>
        private const string ERR_NO_CODE = "No Code specified";

        /// <summary>
        /// Precision map
        /// </summary>
        internal static Dictionary<DatePrecision, String> m_precisionMap;

        /// <summary>
        /// Map between HL7v3 name use and name set use
        /// </summary>
        internal static Dictionary<EntityNameUse, NameSet.NameSetUse> m_nameUseMap;

        /// <summary>
        /// Map between HL7v3 name part type and MDM part type
        /// </summary>
        internal static Dictionary<EntityNamePartType?, NamePart.NamePartType> m_namePartTypeMap;

        /// <summary>
        /// Map between address part type
        /// </summary>
        internal static Dictionary<PostalAddressUse, AddressSet.AddressSetUse> m_addressUseMap;

        /// <summary>
        /// Static CTOR
        /// </summary>
        static ComponentUtil()
        {
            m_addressUseMap = new Dictionary<PostalAddressUse, AddressSet.AddressSetUse>() 
            {
                { PostalAddressUse.BadAddress, AddressSet.AddressSetUse.BadAddress },
                { PostalAddressUse.Direct, AddressSet.AddressSetUse.Direct },
                { PostalAddressUse.HomeAddress, AddressSet.AddressSetUse.HomeAddress },
                { PostalAddressUse.PhysicalVisit, AddressSet.AddressSetUse.PhysicalVisit },
                { PostalAddressUse.PostalAddress, AddressSet.AddressSetUse.PostalAddress },
                { PostalAddressUse.PrimaryHome, AddressSet.AddressSetUse.PrimaryHome },
                { PostalAddressUse.Public, AddressSet.AddressSetUse.Public },
                { PostalAddressUse.TemporaryAddress, AddressSet.AddressSetUse.TemporaryAddress },
                { PostalAddressUse.VacationHome, AddressSet.AddressSetUse.VacationHome },
                { PostalAddressUse.WorkPlace, AddressSet.AddressSetUse.WorkPlace }
            };

            // Create the date precision maps
            m_precisionMap = new Dictionary<DatePrecision, string>()
                { 
                       { DatePrecision.Day, "D" },
                       { DatePrecision.Full, "F" },
                       { DatePrecision.Hour, "H" },
                       { DatePrecision.Minute, "m" },
                       { DatePrecision.Month, "M" }, 
                       { DatePrecision.Second, "S" },
                       { DatePrecision.Year, "Y" }
                };
                 
            // Create the name use maps
            m_nameUseMap = new Dictionary<EntityNameUse, NameSet.NameSetUse>()
            {
                { EntityNameUse.Artist, NameSet.NameSetUse.Artist },
                { EntityNameUse.Assigned, NameSet.NameSetUse.Assigned },
                { EntityNameUse.Indigenous, NameSet.NameSetUse.Indigenous },
                { EntityNameUse.Legal, NameSet.NameSetUse.Legal },
                { EntityNameUse.License, NameSet.NameSetUse.License },
                { EntityNameUse.OfficialRecord, NameSet.NameSetUse.OfficialRecord },
                { EntityNameUse.Phonetic, NameSet.NameSetUse.Phonetic },
                { EntityNameUse.Pseudonym, NameSet.NameSetUse.Pseudonym },
                { EntityNameUse.Religious, NameSet.NameSetUse.Religious },
                { EntityNameUse.MaidenName, NameSet.NameSetUse.MaidenName },
                { EntityNameUse.Search, NameSet.NameSetUse.Search }
            };

            // Create name part type map
            m_namePartTypeMap = new Dictionary<EntityNamePartType?, NamePart.NamePartType>()
            {
                { EntityNamePartType.Delimiter, NamePart.NamePartType.Delimeter },
                { EntityNamePartType.Family, NamePart.NamePartType.Family },
                { EntityNamePartType.Given, NamePart.NamePartType.Given } ,
                { EntityNamePartType.Prefix, NamePart.NamePartType.Prefix },
                { EntityNamePartType.Suffix, NamePart.NamePartType.Suffix }
            };
        }

        /// <summary>
        /// Create a domain identifier list from the list 
        /// </summary>
        public List<DomainIdentifier> CreateDomainIdentifierList(IEnumerable<II> iiList)
        {
            List<DomainIdentifier> retVal = new List<DomainIdentifier>(10);
            foreach (var ii in iiList)
                retVal.Add(new DomainIdentifier()
                {
                    Domain = ii.Root,
                    Identifier = ii.Extension
                });
            return retVal;
        }

        /// <summary>
        /// Determine if the specified TS has a timezone
        /// </summary>
        private bool HasTimezone(TS value)
        {
            return value.DateValuePrecision >= DatePrecision.Hour &&
                value.DateValuePrecision != DatePrecision.FullNoTimezone ||
                value.DateValuePrecision <= DatePrecision.Day;
        }

        /// <summary>
        /// Create a TS
        /// </summary>
        private TimestampPart CreateTimestamp(TS timestamp, List<IResultDetail> dtls)
        {
            return new TimestampPart(TimestampPart.TimestampPartType.Standlone, timestamp.DateValue, m_precisionMap[timestamp.DateValuePrecision.Value]);
        }

        /// <summary>
        /// Create an MDM ivl_ts type
        /// </summary>
        public TimestampSet CreateTimestamp(IVL<TS> ivl_ts, List<IResultDetail> dtls)
        {
            TimestampSet tss = new TimestampSet();

            // value
            if (ivl_ts.Value != null && !ivl_ts.Value.IsNull)
            {
                if (!HasTimezone(ivl_ts.Value))
                    dtls.Add(new MandatoryElementMissingResultDetail(ResultDetailType.Error, String.Format(ERR_NOTZ, ivl_ts.Value), null));
                else if ((ivl_ts.Low == null || ivl_ts.Low.IsNull) && (ivl_ts.High == null || ivl_ts.High.IsNull))
                {
                    //dtls.Add(new ResultDetail(ResultDetailType.Warning, this.m_localeService.GetString("MSGW00D"), null, null));
                    //ivl_ts = ivl_ts.Value.ToIVL();
                    tss.Parts.Add(new TimestampPart(TimestampPart.TimestampPartType.Standlone, ivl_ts.Value.DateValue, m_precisionMap[ivl_ts.Value.DateValuePrecision.Value]));
                }
                else
                    dtls.Add(new ResultDetail(ResultDetailType.Error, this.m_localeService.GetString("MSGE027"), null, null));
            }
            else
            {
                // low
                if (ivl_ts.Low != null && !ivl_ts.Low.IsNull)
                {
                    if (!HasTimezone(ivl_ts.Low))
                        dtls.Add(new MandatoryElementMissingResultDetail(ResultDetailType.Error, String.Format(ERR_NOTZ, ivl_ts.Low), null));
                    else
                        tss.Parts.Add(new TimestampPart(TimestampPart.TimestampPartType.LowBound, ivl_ts.Low.DateValue, m_precisionMap[ivl_ts.Low.DateValuePrecision.Value]));
                }
                // high
                if (ivl_ts.High != null && !ivl_ts.High.IsNull)
                {
                    if (!HasTimezone(ivl_ts.High))
                        dtls.Add(new MandatoryElementMissingResultDetail(ResultDetailType.Error, String.Format(ERR_NOTZ, ivl_ts.High), null));
                    else
                        tss.Parts.Add(new TimestampPart(TimestampPart.TimestampPartType.HighBound, ivl_ts.High.DateValue, m_precisionMap[ivl_ts.High.DateValuePrecision.Value]));
                }
            }

            // check that some data exists
            if (tss.Parts.Count == 0)
            {
                dtls.Add(new ResultDetail(ResultDetailType.Error, ERR_NOTS_PARTS, (string)null));
                return null;
            }
            return tss;
        }

        /// <summary>
        /// Create a codified value
        /// </summary>
        /// <param name="cV"></param>
        /// <returns></returns>
        public CodeValue CreateCodeValue<T>(CV<T> cv, List<IResultDetail> dtls)
        {
            // Get terminology service from the host context
            ITerminologyService termSvc = Context.GetService(typeof(ITerminologyService)) as ITerminologyService;

            // Code is null then return
            if(cv == null || cv.Code == null || cv.IsNull)
            {
                dtls.Add(new ResultDetail(ResultDetailType.Error, ERR_NO_CODE, (string)null));
                return null;
            }

            // Return value
            CodeValue retVal = new CodeValue(Util.ToWireFormat(cv.Code));
            if (cv.Code.IsAlternateCodeSpecified || !String.IsNullOrEmpty(cv.CodeSystem))
            {
                retVal.CodeSystem = cv.CodeSystem;
                if (retVal.CodeSystem == null)
                    dtls.Add(new MandatoryElementMissingResultDetail(ResultDetailType.Error,
                        String.Format(this.m_localeService.GetString("MSGE04A"),
                        cv.Code, typeof(T).Name), null));

            }
            else if (String.IsNullOrEmpty(cv.CodeSystem))
            {
                object[] attList = typeof(T).GetCustomAttributes(typeof(StructureAttribute), false);
                if (attList.Length > 0)
                    retVal.CodeSystem = (attList[0] as StructureAttribute).CodeSystem;
            }
            else
                retVal.CodeSystem = cv.CodeSystem;

            // Code system data
            retVal.CodeSystemVersion = cv.CodeSystemVersion;
            retVal.DisplayName = cv.DisplayName;

            // Validate with termservice
            if (termSvc != null && cv.Code.IsAlternateCodeSpecified)
            {
                var tval = termSvc.Validate(retVal);
                foreach (var dtl in tval.Details)
                    dtls.Add(new VocabularyIssueResultDetail(dtl.IsError ? ResultDetailType.Error : ResultDetailType.Warning, dtl.Message, null));
            }

            if(cv.OriginalText != null && !cv.IsNull)
                retVal.OriginalText = cv.OriginalText.ToString();

            return retVal;
        }

        /// <summary>
        /// Create a codified value
        /// </summary>
        /// <param name="cd"></param>
        /// <returns></returns>
        public CodeValue CreateCodeValue<T>(CD<T> cd, List<IResultDetail> dtls)
        {
            CodeValue retVal = CreateCodeValue<T>((CV<T>)cd, dtls);
            if (retVal == null) return null;
            else if (cd.Qualifier != null)
            {
                retVal.Qualifies = new Dictionary<CodeValue, CodeValue>();
                foreach (var qualifier in cd.Qualifier)
                    retVal.Qualifies.Add(CreateCodeValue<T>(qualifier.Name, dtls), CreateCodeValue<T>(qualifier.Value, dtls));
            }

            return retVal;
        }


        /// <summary>
        /// Create client component
        /// </summary>
        public Client CreateClientComponent(MARC.Everest.RMIM.CA.R020402.COCT_MT050207CA.Patient patient, List<IResultDetail> dtls)
        {
            // Patient is null
            if (patient == null || patient.NullFlavor != null)
            {
                dtls.Add(new MandatoryElementMissingResultDetail(ResultDetailType.Error,
                    this.m_localeService.GetString("MSGE028"), null));
                return null;
            }

            Client retVal = new Client();

            // Resolve id
            if (patient.Id == null || patient.Id.NullFlavor != null ||
                patient.Id.IsEmpty || patient.Id.Count(o => o.Use == IdentifierUse.Business) == 0)
            {
                dtls.Add(new ResultDetail(ResultDetailType.Error,
                    this.m_localeService.GetString("MSGE029"),
                    null, null));
                return null;
            }

            // Alternative identifiers
            retVal.AlternateIdentifiers.AddRange(CreateDomainIdentifierList(patient.Id));

            // Client Legal Name
            PN legalName = patient.PatientPerson.Name;
            if (legalName == null || legalName.IsNull)
                dtls.Add(new ResultDetail(ResultDetailType.Warning, this.m_localeService.GetString("MSGE02A"), null, null));
            else
                retVal.LegalName = CreateNameSet(legalName, dtls);

            // Client telecom
            if (patient.Telecom != null &&
                !patient.Telecom.IsNull)
                foreach (TEL tel in patient.Telecom)
                    retVal.TelecomAddresses.Add(new TelecommunicationsAddress()
                    {
                        Value = tel.Value,
                        Use = tel.Use == null ? null : Util.ToWireFormat(tel.Use)
                    });

            AD addr = patient.Addr;
            if (addr != null && !addr.IsNull)
                retVal.PerminantAddress = CreateAddressSet(addr, dtls);

            return retVal;
        }

        /// <summary>
        /// Create client component
        /// </summary>
        public Client CreateClientComponent(MARC.Everest.RMIM.CA.R020402.COCT_MT050202CA.Patient patient, List<IResultDetail> dtls)
        {
            // Patient is null
            if (patient == null || patient.NullFlavor != null ||
                patient.PatientPerson == null || patient.PatientPerson.NullFlavor != null ||
                patient.PatientPerson.AdministrativeGenderCode == null || patient.PatientPerson.AdministrativeGenderCode.IsNull ||
                patient.PatientPerson.BirthTime == null || patient.PatientPerson.BirthTime.IsNull)
            {
                dtls.Add(new MandatoryElementMissingResultDetail(ResultDetailType.Error,
                    this.m_localeService.GetString("MSGE028"), null));
                return null;
            }

            Client retVal = new Client();
            // Resolve id
            if(patient.Id == null || patient.Id.NullFlavor != null ||
                patient.Id.IsEmpty)
            {
                dtls.Add(new ResultDetail(ResultDetailType.Error,
                    this.m_localeService.GetString("MSGE029"), 
                    null, null));
                return null;
            }

            // Alternative identifiers
            retVal.AlternateIdentifiers.AddRange(CreateDomainIdentifierList(patient.Id));

            // Client Legal Name
            PN legalName = patient.PatientPerson.Name;
            if (legalName == null || legalName.IsNull)
                dtls.Add(new ResultDetail(ResultDetailType.Warning, this.m_localeService.GetString("MSGE02A"), null, null));
            else
                retVal.LegalName = CreateNameSet(legalName, dtls);

            // Client birth time
            retVal.BirthTime = new TimestampPart()
            {
                PartType = TimestampPart.TimestampPartType.Standlone,
                Precision = m_precisionMap[patient.PatientPerson.BirthTime.DateValuePrecision.Value],
                Value = patient.PatientPerson.BirthTime.DateValue
            };

            // Client gender
            retVal.GenderCode = Util.ToWireFormat(patient.PatientPerson.AdministrativeGenderCode);

            return retVal;
        }

        /// <summary>
        /// Create an address set
        /// </summary>
        public AddressSet CreateAddressSet(AD address, List<IResultDetail> dtls)
        {
            AddressSet retVal = new AddressSet();

            AddressSet.AddressSetUse internalNameUse = ConvertAddressUse(address.Use, dtls);
            if (address == null || address.IsNull || internalNameUse == 0)
                return null;

            retVal.Use = internalNameUse;
            // Create the parts
            foreach (ADXP namePart in address.Part)
                retVal.Parts.Add(new AddressPart()
                { 
                    AddressValue = namePart.Value,
                    PartType = (AddressPart.AddressPartType)Enum.Parse(typeof(AddressPart.AddressPartType), namePart.Type.ToString())
                });

            return retVal;
        }

        /// <summary>
        /// Convert address uses
        /// </summary>
        private AddressSet.AddressSetUse ConvertAddressUse(SET<CS<PostalAddressUse>> uses, List<IResultDetail> dtls)
        {
            AddressSet.AddressSetUse retVal = 0;
            foreach(var use in uses)
                switch ((PostalAddressUse)use)
                {
                    case PostalAddressUse.Direct:
                        retVal |= AddressSet.AddressSetUse.Direct;
                        break;
                    case PostalAddressUse.BadAddress:
                        retVal |= AddressSet.AddressSetUse.BadAddress;
                        break;
                    case PostalAddressUse.HomeAddress:
                        retVal |= AddressSet.AddressSetUse.HomeAddress;
                        break;
                    case PostalAddressUse.PhysicalVisit:
                        retVal |= AddressSet.AddressSetUse.PhysicalVisit;
                        break;
                    case PostalAddressUse.PostalAddress:
                        retVal |= AddressSet.AddressSetUse.PostalAddress;
                        break;
                    case PostalAddressUse.PrimaryHome:
                        retVal |= AddressSet.AddressSetUse.PrimaryHome;
                        break;
                    case PostalAddressUse.Public:
                        retVal |= AddressSet.AddressSetUse.Public;
                        break;
                    case PostalAddressUse.TemporaryAddress:
                        retVal |= AddressSet.AddressSetUse.TemporaryAddress;
                            break;
                    case PostalAddressUse.VacationHome:
                        retVal |= AddressSet.AddressSetUse.VacationHome;
                        break;
                    case PostalAddressUse.WorkPlace:
                        retVal |= AddressSet.AddressSetUse.WorkPlace;
                        break;
                    default:
                        dtls.Add(new VocabularyIssueResultDetail(ResultDetailType.Error, String.Format(m_localeService.GetString("MSGE04D"), use), null, null));
                        break;
                }
            return retVal;
        }

        /// <summary>
        /// Create a name set
        /// </summary>
        public NameSet CreateNameSet(PN legalName, List<IResultDetail> dtls)
        {
            NameSet retVal = new NameSet();
            NameSet.NameSetUse internalNameUse = NameSet.NameSetUse.Legal;
            var lnu = legalName.Use.IsNull || legalName.Use.IsEmpty ? EntityNameUse.Legal : (EntityNameUse)legalName.Use[0];
            if(!m_nameUseMap.TryGetValue(lnu, out internalNameUse))
                return null;

            retVal.Use = internalNameUse;
            // Create the parts
            foreach(ENXP namePart in legalName.Part)
                retVal.Parts.Add(new NamePart() {
                    Value = namePart.Value, 
                    Type = m_namePartTypeMap[namePart.Type]
                });

            return retVal;
        }

        /// <summary>
        /// Create a healthcare participant component
        /// </summary>
        private HealthcareParticipant CreateParticipantComponent(MARC.Everest.RMIM.CA.R020402.COCT_MT090508CA.AssignedEntity assignedEntity, List<IResultDetail> dtls)
        {
            HealthcareParticipant retVal = new HealthcareParticipant();
            retVal.Classifier = HealthcareParticipant.HealthcareParticipantType.Organization;

            // Check for id
            if (assignedEntity.RepresentedOrganization == null || assignedEntity.RepresentedOrganization.NullFlavor != null
             || assignedEntity.RepresentedOrganization.Id == null || assignedEntity.RepresentedOrganization.Id.IsNull)
            {
                dtls.Add(new ResultDetail(ResultDetailType.Error, this.m_localeService.GetString("MSGE02B"), null, null));
                return null;
            }

            // Add identifier
            retVal.AlternateIdentifiers.Add(new DomainIdentifier()
            {
                Domain = assignedEntity.RepresentedOrganization.Id.Root,
                Identifier = assignedEntity.RepresentedOrganization.Id.Extension
            });

            // Legal name
            if (assignedEntity.RepresentedOrganization.Name != null &&
                !assignedEntity.RepresentedOrganization.Name.IsNull)
            {
                retVal.LegalName = new NameSet();
                retVal.LegalName.Use = NameSet.NameSetUse.Legal;
                retVal.LegalName.Parts.Add(new NamePart()
                {
                    Value = assignedEntity.RepresentedOrganization.Name,
                    Type = NamePart.NamePartType.Given
                });
            }

            // Organization type
            if (assignedEntity.RepresentedOrganization.AssignedOrganization != null && 
                assignedEntity.RepresentedOrganization.AssignedOrganization.NullFlavor == null)
            {
                if (assignedEntity.RepresentedOrganization.AssignedOrganization.Code == null || assignedEntity.RepresentedOrganization.AssignedOrganization.Code.IsNull)
                    dtls.Add(new ResultDetail(ResultDetailType.Warning, this.m_localeService.GetString("MSGW00F"), null, null));
                else
                    retVal.Type = CreateCodeValue<String>(assignedEntity.RepresentedOrganization.AssignedOrganization.Code, dtls);

                // Telecommunications
                if (assignedEntity.RepresentedOrganization.AssignedOrganization.Telecom != null && 
                    !assignedEntity.RepresentedOrganization.AssignedOrganization.Telecom.IsEmpty)
                    foreach (var tel in assignedEntity.RepresentedOrganization.AssignedOrganization.Telecom)
                        retVal.TelecomAddresses.Add(new TelecommunicationsAddress()
                        {
                            Use = tel.Use == null ? null : Util.ToWireFormat(tel.Use),
                            Value = tel.Value
                        });
            }
            
            return retVal;
        }

        /// <summary>
        /// Create a personal relationship component
        /// </summary>
        private PersonalRelationship CreatePersonalRelationshipComponent(MARC.Everest.RMIM.CA.R020402.COCT_MT910108CA.PersonalRelationship personalRelationship, List<IResultDetail> dtls)
        {
            PersonalRelationship retVal = new PersonalRelationship();

            // Identifier
            if (personalRelationship.Id == null || personalRelationship.Id.IsNull)
            {
                dtls.Add(new MandatoryElementMissingResultDetail(ResultDetailType.Error, this.m_localeService.GetString("MSGE02C"), null));
                return null;
            }
            retVal.AlternateIdentifiers.Add(new DomainIdentifier()
                {
                    Domain = personalRelationship.Id.Root,
                    Identifier = personalRelationship.Id.Extension
                });

            // Name
            if(personalRelationship.RelationshipHolder == null || personalRelationship.RelationshipHolder.NullFlavor != null)
            {
                dtls.Add(new MandatoryElementMissingResultDetail(ResultDetailType.Error, this.m_localeService.GetString("MSGE02D"), null));
                return null;
            }
            retVal.LegalName = CreateNameSet(personalRelationship.RelationshipHolder.Name, dtls);

            // Type
            if(personalRelationship.Code == null || personalRelationship.Code.IsNull ||
                personalRelationship.Code.Code.IsAlternateCodeSpecified)
            {
                dtls.Add(new MandatoryElementMissingResultDetail(ResultDetailType.Error, this.m_localeService.GetString("MSGE02E"), null));
                return null;
            }
            retVal.RelationshipKind = Util.ToWireFormat(personalRelationship.Code);

            // Telecom addresses
            if (personalRelationship.RelationshipHolder.Telecom != null && !personalRelationship.RelationshipHolder.Telecom.IsEmpty)
            {
                foreach (var telecom in personalRelationship.RelationshipHolder.Telecom)
                    retVal.TelecomAddresses.Add(new TelecommunicationsAddress()
                    {
                        Use = telecom.Use == null ? null : Util.ToWireFormat(telecom.Use),
                        Value = telecom.Value
                    });
            }

            return retVal;

        }


        /// <summary>
        /// Create a healthcare participant component
        /// </summary>
        private HealthcareParticipant CreateParticipantComponent(MARC.Everest.RMIM.CA.R020402.COCT_MT090108CA.AssignedEntity assignedEntity, List<IResultDetail> dtls)
        {
            HealthcareParticipant retVal = new HealthcareParticipant();
            retVal.Classifier = HealthcareParticipant.HealthcareParticipantType.Person;
            
            // Check for an id
            if (assignedEntity.Id == null || assignedEntity.Id.IsNull)
            {
                dtls.Add(new ResultDetail(ResultDetailType.Error, this.m_localeService.GetString("MSGE02F"), null, null));
                return null;
            }
            
            // Identifiers
            retVal.AlternateIdentifiers.AddRange(CreateDomainIdentifierList(assignedEntity.Id));
            
            // Name
            if (assignedEntity.AssignedPerson == null || assignedEntity.AssignedPerson.NullFlavor != null)
            {
                dtls.Add(new MandatoryElementMissingResultDetail(ResultDetailType.Error, this.m_localeService.GetString("MSGE030"), null));
                return null;
            }

            if(assignedEntity.AssignedPerson.Name != null && !assignedEntity.AssignedPerson.Name.IsNull)
                retVal.LegalName = CreateNameSet(assignedEntity.AssignedPerson.Name, dtls);

            // Type
            if (assignedEntity.Code == null || assignedEntity.Code.IsNull)
            {
                dtls.Add(new MandatoryElementMissingResultDetail(ResultDetailType.Error, this.m_localeService.GetString("MSGE031"), null));
                return null;
            }
            retVal.Type = CreateCodeValue<HealthCareProviderRoleType>(assignedEntity.Code, dtls);

            // Telecom addresses
            if (assignedEntity.Telecom != null && !assignedEntity.Telecom.IsEmpty)
            {
                foreach (var telecom in assignedEntity.Telecom)
                    retVal.TelecomAddresses.Add(new TelecommunicationsAddress()
                    {
                        Use = telecom.Use == null ? null : Util.ToWireFormat(telecom.Use),
                        Value = telecom.Value
                    });
            }

            // License number
            if (assignedEntity.AssignedPerson.AsHealthCareProvider != null &&
                assignedEntity.AssignedPerson.AsHealthCareProvider.Id != null &&
                !assignedEntity.AssignedPerson.AsHealthCareProvider.Id.IsNull)
                retVal.AlternateIdentifiers.Add(new DomainIdentifier()
                {
                    Domain = assignedEntity.AssignedPerson.AsHealthCareProvider.Id.Root,
                    Identifier = assignedEntity.AssignedPerson.AsHealthCareProvider.Id.Extension,
                    IsLicenseAuthority = true
                });

            // Represented organization
            if (assignedEntity.RepresentedOrganization != null && assignedEntity.RepresentedOrganization.NullFlavor == null)
            {
                HealthcareParticipant ptcpt = CreateParticipantComponent(assignedEntity.RepresentedOrganization, dtls);
                if (ptcpt != null)
                    retVal.Add(ptcpt, "RPO", HealthServiceRecordSiteRoleType.RepresentitiveOf, ptcpt.AlternateIdentifiers);
                else
                    dtls.Add(new ResultDetail(ResultDetailType.Warning, this.m_localeService.GetString("MSGW010"), null, null));
            }

            return retVal;
        }

        private HealthcareParticipant CreateParticipantComponent(MARC.Everest.RMIM.CA.R020402.COCT_MT090108CA.Organization organization, List<IResultDetail> dtls)
        {
            HealthcareParticipant retVal = new HealthcareParticipant();
            retVal.Classifier = HealthcareParticipant.HealthcareParticipantType.Organization;

            // Organization identifier
            if (organization.Id == null || organization.Id.IsNull)
            {
                dtls.Add(new MandatoryElementMissingResultDetail(ResultDetailType.Error, this.m_localeService.GetString("MSGE032"), null));
                return null;
            }
            retVal.AlternateIdentifiers.Add(new DomainIdentifier()
            {
                Domain = organization.Id.Root,
                Identifier = organization.Id.Extension
            });

            // Organization name
            if (organization.Name != null)
            {
                retVal.LegalName = new NameSet()
                {
                    Use = NameSet.NameSetUse.Legal
                };
                retVal.LegalName.Parts.Add(new NamePart()
                {
                    Type = NamePart.NamePartType.Given,
                    Value = organization.Name
                });
            };

            // Organization type
            if (organization.AssignedOrganization != null && organization.AssignedOrganization.NullFlavor == null)
            {
                if (organization.AssignedOrganization.Code == null || organization.AssignedOrganization.Code.IsNull)
                    dtls.Add(new ResultDetail(ResultDetailType.Warning, this.m_localeService.GetString("MSGW011"), null, null));
                else
                    retVal.Type = CreateCodeValue<String>(organization.AssignedOrganization.Code, dtls);

                // Telecommunications
                if(organization.AssignedOrganization.Telecom != null && !organization.AssignedOrganization.Telecom.IsEmpty)
                    foreach(var tel in organization.AssignedOrganization.Telecom)
                        retVal.TelecomAddresses.Add(new TelecommunicationsAddress()
                         { 
                             Use = tel.Use == null ? null : Util.ToWireFormat(tel.Use), 
                             Value = tel.Value
                         });
            }

            return retVal;
        }

        /// <summary>
        /// Create a healthcare participant component
        /// </summary>
        private HealthcareParticipant CreateParticipantComponent(MARC.Everest.RMIM.CA.R020402.COCT_MT090102CA.AssignedEntity assignedEntity, List<IResultDetail> dtls)
        {
            // Create healthcare participant
            HealthcareParticipant retVal = new HealthcareParticipant();
            retVal.Classifier = HealthcareParticipant.HealthcareParticipantType.Person;
            
            // Check for an id
            if (assignedEntity.Id == null || assignedEntity.Id.IsNull)
            {
                dtls.Add(new ResultDetail(ResultDetailType.Error, this.m_localeService.GetString("MSGE02F"), null, null));
                return null;
            }

            // Identifiers
            retVal.AlternateIdentifiers.AddRange(CreateDomainIdentifierList(assignedEntity.Id));

            // Name
            if (assignedEntity.AssignedPerson != null && assignedEntity.AssignedPerson.NullFlavor == null
                && assignedEntity.AssignedPerson.Name != null && !assignedEntity.AssignedPerson.Name.IsNull)
                retVal.LegalName = CreateNameSet(assignedEntity.AssignedPerson.Name, dtls);

            // License number
            if(assignedEntity.AssignedPerson.AsHealthCareProvider != null &&
                assignedEntity.AssignedPerson.AsHealthCareProvider.Id != null &&
                !assignedEntity.AssignedPerson.AsHealthCareProvider.Id.IsNull)
                retVal.AlternateIdentifiers.Add(new DomainIdentifier() { 
                    Domain = assignedEntity.AssignedPerson.AsHealthCareProvider.Id.Root, 
                    Identifier = assignedEntity.AssignedPerson.AsHealthCareProvider.Id.Extension,
                    IsLicenseAuthority = true
                });

            // Represented organization
            if(assignedEntity.RepresentedOrganization != null && assignedEntity.RepresentedOrganization.NullFlavor == null)
            {
                HealthcareParticipant ptcpt = CreateParticipantComponent(assignedEntity.RepresentedOrganization, dtls);
                if (ptcpt != null)
                    retVal.Add(ptcpt, "RPO", HealthServiceRecordSiteRoleType.RepresentitiveOf, ptcpt.AlternateIdentifiers);
                else
                    dtls.Add(new ResultDetail(ResultDetailType.Warning, this.m_localeService.GetString("MSGW010"), null, null));
            }

            return retVal;
        }

        /// <summary>
        /// Create location component
        /// </summary>
        private ServiceDeliveryLocation CreateLocationComponent(MARC.Everest.RMIM.CA.R020402.COCT_MT240003CA.ServiceDeliveryLocation serviceDeliveryLocation, List<IResultDetail> dtls)
        {
            ServiceDeliveryLocation retVal = new ServiceDeliveryLocation();

            // Check for identifier
            if (serviceDeliveryLocation.Id == null || serviceDeliveryLocation.Id.IsNull)
            {
                dtls.Add(new ResultDetail(ResultDetailType.Error, this.m_localeService.GetString("MSGE033"), null, null));
                return null;
            }
            retVal.AlternateIdentifiers.Add(
                new DomainIdentifier()
                {
                    Domain = serviceDeliveryLocation.Id.Root,
                    Identifier = serviceDeliveryLocation.Id.Extension
                });

            // Check for name
            if (serviceDeliveryLocation.Location != null && serviceDeliveryLocation.Location.NullFlavor == null)
                retVal.Name = serviceDeliveryLocation.Location.Name;

            // Telecom
            if (serviceDeliveryLocation.Telecom != null)
                foreach (var tel in serviceDeliveryLocation.Telecom)
                    retVal.TelecomAddresses.Add(new TelecommunicationsAddress()
                    {
                        Value = tel.Value,
                        Use = tel.Use != null ? Util.ToWireFormat(tel.Use) : null
                    });

            // Address
            if (serviceDeliveryLocation.Addr != null)
                retVal.Address = CreateAddressSet(serviceDeliveryLocation.Addr, dtls);
            
            // Location type
            if (serviceDeliveryLocation.Code == null || serviceDeliveryLocation.Code.IsNull)
            {
                dtls.Add(new MandatoryElementMissingResultDetail(ResultDetailType.Error, this.m_localeService.GetString("MSGE034"), null));
                return null;
            }
            retVal.LocationType = CreateCodeValue<ServiceDeliveryLocationRoleType>(serviceDeliveryLocation.Code, dtls);
                        
            return retVal;
        }

        /// <summary>
        /// Create a participant component from an organization
        /// </summary>
        private HealthcareParticipant CreateParticipantComponent(MARC.Everest.RMIM.CA.R020402.COCT_MT090102CA.Organization organization, List<IResultDetail> dtls)
        {
            HealthcareParticipant retVal = new HealthcareParticipant();
            retVal.Classifier = HealthcareParticipant.HealthcareParticipantType.Organization;
            
            // Organization identifier
            if (organization.Id == null || organization.Id.IsNull)
            {
                dtls.Add(new MandatoryElementMissingResultDetail(ResultDetailType.Error, this.m_localeService.GetString("MSGE032"), null));
                return null;
            }
            retVal.AlternateIdentifiers.Add(new DomainIdentifier()
                {
                    Domain = organization.Id.Root,
                    Identifier = organization.Id.Extension
                });

            // Organization name
            if(organization.Name != null)
            {
                retVal.LegalName = new NameSet() {
                    Use = NameSet.NameSetUse.Legal
                };
                retVal.LegalName.Parts.Add(new NamePart()
                    {
                        Type = NamePart.NamePartType.Given,
                        Value = organization.Name
                    });
            };

            return retVal;
        }

        /// <summary>
        /// Create a healthcare participant
        /// </summary>
        /// <param name="p"></param>
        /// <param name="dtls"></param>
        /// <returns></returns>
        private HealthcareParticipant CreateParticipantComponent(MARC.Everest.RMIM.CA.R020402.COCT_MT090502CA.AssignedEntity assignedEntity, List<IResultDetail> dtls)
        {
            // Create the healthcare participant
            HealthcareParticipant retVal = new HealthcareParticipant();
            retVal.Classifier = HealthcareParticipant.HealthcareParticipantType.Organization;

            // Check for id
            if (assignedEntity.RepresentedOrganization == null || assignedEntity.RepresentedOrganization.NullFlavor != null
             || assignedEntity.RepresentedOrganization.Id == null || assignedEntity.RepresentedOrganization.Id.IsNull)
            {
                dtls.Add(new ResultDetail(ResultDetailType.Error, this.m_localeService.GetString("MSGE02B"), null, null));
                return null;
            }

            // Add identifier
            retVal.AlternateIdentifiers.Add(new DomainIdentifier()
            {
                Domain = assignedEntity.RepresentedOrganization.Id.Root,
                Identifier = assignedEntity.RepresentedOrganization.Id.Extension
            });

            // Legal name
            if (assignedEntity.RepresentedOrganization.Name != null &&
                !assignedEntity.RepresentedOrganization.Name.IsNull)
            {
                retVal.LegalName = new NameSet();
                retVal.LegalName.Use = NameSet.NameSetUse.Legal;
                retVal.LegalName.Parts.Add(new NamePart()
                {
                    Value = assignedEntity.RepresentedOrganization.Name,
                    Type = NamePart.NamePartType.Given
                });
            }

            return retVal;
        }

        /// <summary>
        /// Create location component
        /// </summary>
        private ServiceDeliveryLocation CreateLocationComponent(MARC.Everest.RMIM.CA.R020402.COCT_MT240012CA.ServiceDeliveryLocation serviceDeliveryLocation, List<IResultDetail> dtls)
        {
            ServiceDeliveryLocation retVal = new ServiceDeliveryLocation();

            // Check for identifier
            if(serviceDeliveryLocation.Id == null || serviceDeliveryLocation.Id.IsNull)
            {
                dtls.Add(new ResultDetail(ResultDetailType.Error, this.m_localeService.GetString("MSGE033"), null, null));
                return null;
            }
            retVal.AlternateIdentifiers.Add(
                new DomainIdentifier() 
                {
                    Domain = serviceDeliveryLocation.Id.Root, 
                    Identifier = serviceDeliveryLocation.Id.Extension
                });

            // Check for name
            if(serviceDeliveryLocation.Location != null && serviceDeliveryLocation.Location.NullFlavor == null)
                retVal.Name = serviceDeliveryLocation.Location.Name;

            return retVal;
        }

        /// <summary>
        /// Create a location component
        /// </summary>
        private ServiceDeliveryLocation CreateLocationComponent(MARC.Everest.RMIM.CA.R020402.COCT_MT240007CA.ServiceDeliveryLocation serviceDeliveryLocation, List<IResultDetail> dtls)
        {
            ServiceDeliveryLocation retVal = new ServiceDeliveryLocation();

            // Check for identifier
            if (serviceDeliveryLocation.Id == null || serviceDeliveryLocation.Id.IsNull)
                dtls.Add(new ResultDetail(ResultDetailType.Error, this.m_localeService.GetString("MSGE033"), null, null));
            else
                retVal.AlternateIdentifiers.Add(
                    new DomainIdentifier()
                    {
                        Domain = serviceDeliveryLocation.Id.Root,
                        Identifier = serviceDeliveryLocation.Id.Extension
                    });

            // SDL type
            if (serviceDeliveryLocation.Code == null || serviceDeliveryLocation.Code.IsNull)
                dtls.Add(new ResultDetail(ResultDetailType.Error, this.m_localeService.GetString("MSGE034"), null, null));
            else
                retVal.LocationType = CreateCodeValue<ServiceDeliveryLocationRoleType>(serviceDeliveryLocation.Code, dtls);

            // Telecom
            if (serviceDeliveryLocation.Telecom != null)
                foreach (var tel in serviceDeliveryLocation.Telecom)
                    retVal.TelecomAddresses.Add(new TelecommunicationsAddress()
                    {
                        Value = tel.Value,
                        Use = tel.Use != null ? Util.ToWireFormat(tel.Use) : null
                    });

            // Address
            if (serviceDeliveryLocation.Addr != null)
                retVal.Address = CreateAddressSet(serviceDeliveryLocation.Addr, dtls);

            // Check for name
            if (serviceDeliveryLocation.Location != null && serviceDeliveryLocation.Location.NullFlavor == null)
                retVal.Name = serviceDeliveryLocation.Location.Name;
            
            return retVal;
        }

        /// <summary>
        /// Create domain identifier
        /// </summary>
        /// <param name="iI"></param>
        /// <returns></returns>
        private DomainIdentifier CreateDomainIdentifier(MARC.Everest.DataTypes.II iI)
        {
            return new DomainIdentifier()
            {
                Domain = iI.Root,
                Identifier = iI.Extension
            };
        }

        /// <summary>
        /// Creates the component of events
        /// </summary>
        /// <param name="components"></param>
        /// <param name="dtls"></param>
        /// <returns></returns>
        private List<IComponent> CreateComponentOfRefs(List<MARC.Everest.RMIM.CA.R020402.REPC_MT410001CA.Component3> components, List<IResultDetail> dtls)
        {
            List<IComponent> retVal = new List<IComponent>(components.Count);
            foreach (var cmpOf in components)
            {
                if (cmpOf == null ||
                    cmpOf.NullFlavor != null ||
                    cmpOf.PatientCareProvisionEvent == null ||
                    cmpOf.PatientCareProvisionEvent.NullFlavor != null)
                    continue;

                if (cmpOf.PatientCareProvisionEvent.Id == null ||
                    cmpOf.PatientCareProvisionEvent.Id.IsNull)
                    dtls.Add(new RequiredElementMissingResultDetail(ResultDetailType.Error, this.m_localeService.GetString("MSGE036"), null));
                else
                    retVal.Add(new HealthServiceRecordComponentRef()
                    {
                        AlternateIdentifier = CreateDomainIdentifier(cmpOf.PatientCareProvisionEvent.Id)
                    });
            }
            return retVal;
        }


        /// <summary>
        /// Create the component of references for document based transactions
        /// </summary>
        private IEnumerable<IComponent> CreateComponentOfRefs(List<MARC.Everest.RMIM.CA.R020402.REPC_MT230003CA.Component6> components, List<IResultDetail> dtls)
        {
            
            List<IComponent> retVal = new List<IComponent>(components.Count);
            foreach (var cmpOf in components)
            {
                if (cmpOf == null ||
                    cmpOf.NullFlavor != null ||
                    cmpOf.PatientCareProvisionEvent == null ||
                    cmpOf.PatientCareProvisionEvent.NullFlavor != null)
                    continue;

                if (cmpOf.PatientCareProvisionEvent.Id == null ||
                    cmpOf.PatientCareProvisionEvent.Id.IsNull)
                    dtls.Add(new RequiredElementMissingResultDetail(ResultDetailType.Error, this.m_localeService.GetString("MSGE036"), null));
                else
                    retVal.Add(new HealthServiceRecordComponentRef()
                    {
                        AlternateIdentifier = CreateDomainIdentifier(cmpOf.PatientCareProvisionEvent.Id)
                    });
            }
            return retVal;
        }

        #region IUsesHostContext Members

        // Host context
        private HostContext m_context;

        /// <summary>
        /// Gets or sets the host context
        /// </summary>
        public MARC.HI.EHRS.SVC.Core.HostContext Context
        {
            get { return this.m_context; }
            set
            {
                this.m_context = value;
                if (this.m_context != null)
                    this.m_localeService = this.m_context.GetService(typeof(ILocalizationService)) as ILocalizationService;

            }
        }

        #endregion


    }
}
