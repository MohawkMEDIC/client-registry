using System;
using System.Collections.Generic;
using System.Linq;
using MARC.Everest.Connectors;
using MARC.HI.EHRS.CR.Core.ComponentModel;
using MARC.HI.EHRS.SVC.Core.DataTypes;
using MARC.Everest.DataTypes;
using MARC.Everest.Attributes;
using MARC.HI.EHRS.SVC.Core.Services;
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
        protected ILocalizationService m_localeService = null;

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
        public virtual List<DomainIdentifier> CreateDomainIdentifierList(IEnumerable<II> iiList, List<IResultDetail> dtls)
        {
            List<DomainIdentifier> retVal = new List<DomainIdentifier>(10);
            foreach (var ii in iiList)
                retVal.Add(CreateDomainIdentifier(ii, dtls));
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
        protected TimestampPart CreateTimestamp(TS timestamp, List<IResultDetail> dtls)
        {
            return new TimestampPart(TimestampPart.TimestampPartType.Standlone, timestamp.DateValue, m_precisionMap[timestamp.DateValuePrecision.Value]);
        }

        /// <summary>
        /// Create an MDM ivl_ts type
        /// </summary>
        protected TimestampSet CreateTimestamp(IVL<TS> ivl_ts, List<IResultDetail> dtls)
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
        protected CodeValue CreateCodeValue<T>(CV<T> cv, List<IResultDetail> dtls)
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
        protected CodeValue CreateCodeValue<T>(CD<T> cd, List<IResultDetail> dtls)
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
        /// Create an address set
        /// </summary>
        public AddressSet CreateAddressSet(AD address, List<IResultDetail> dtls)
        {
            AddressSet retVal = new AddressSet();

            AddressSet.AddressSetUse internalNameUse = ConvertAddressUse(address.Use, dtls);
            if (address == null || address.IsNull)
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
            if (uses == null) return 0;

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
        public NameSet CreateNameSet(EN legalName, List<IResultDetail> dtls)
        {
            NameSet retVal = new NameSet();
            NameSet.NameSetUse internalNameUse = 0;
            var lnu = legalName.Use == null || legalName.Use.IsNull || legalName.Use.IsEmpty ? EntityNameUse.Search : (EntityNameUse)legalName.Use[0];
            if(!m_nameUseMap.TryGetValue(lnu, out internalNameUse))
                return null;

            retVal.Use = internalNameUse;
            // Create the parts
            foreach(ENXP namePart in legalName.Part)
                retVal.Parts.Add(new NamePart() {
                    Value = namePart.Value, 
                    Type = namePart.Type.HasValue ? m_namePartTypeMap[namePart.Type] : NamePart.NamePartType.None
                });

            return retVal;
        }

        

        /// <summary>
        /// Create domain identifier
        /// </summary>
        /// <param name="iI"></param>
        /// <returns></returns>
        protected virtual DomainIdentifier CreateDomainIdentifier(MARC.Everest.DataTypes.II iI, List<IResultDetail> dtls)
        {
            var retVal = new DomainIdentifier()
            {
                Domain = iI.Root,
                Identifier = iI.Extension,
                AssigningAuthority = iI.AssigningAuthorityName 
            };

            // Assign the assigning authority identifier from configuration
            var config = this.m_context.GetService(typeof(ISystemConfigurationService)) as ISystemConfigurationService;
            var oidData = config.OidRegistrar.FindData(iI.Root);
            if (oidData != null)
            {
                var assigningAut = oidData.Attributes.Find(o => o.Key.Equals("AssigningAuthorityName"));
                if (!assigningAut.Equals(default(KeyValuePair<String, String>)))
                {
                    if (!String.IsNullOrEmpty(retVal.AssigningAuthority) &&
                        !retVal.AssigningAuthority.Equals(assigningAut.Value))
                        dtls.Add(new FixedValueMisMatchedResultDetail(retVal.AssigningAuthority, assigningAut.Value, true, null));

                    retVal.AssigningAuthority = assigningAut.Value;
                }
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
