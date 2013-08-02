using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MARC.Everest.DataTypes.Interfaces;
using MARC.HI.EHRS.SVC.Messaging.FHIR.DataTypes;

namespace MARC.HI.EHRS.CR.Messaging.FHIR.Processors
{
    /// <summary>
    /// A hackish code mapping
    /// </summary>
    /// TODO: Move this to the ITerminologyService
    public static class HackishCodeMapping
    {

        /// <summary>
        /// Address use mapping
        /// </summary>
        public static readonly List<KeyValuePair<String, MARC.HI.EHRS.SVC.Core.DataTypes.AddressSet.AddressSetUse>> ADDRESS_USE = new List<KeyValuePair<string, SVC.Core.DataTypes.AddressSet.AddressSetUse>>() {
            new KeyValuePair<String, MARC.HI.EHRS.SVC.Core.DataTypes.AddressSet.AddressSetUse>("home", MARC.HI.EHRS.SVC.Core.DataTypes.AddressSet.AddressSetUse.HomeAddress),
            new KeyValuePair<String, MARC.HI.EHRS.SVC.Core.DataTypes.AddressSet.AddressSetUse>("home", MARC.HI.EHRS.SVC.Core.DataTypes.AddressSet.AddressSetUse.PrimaryHome),
            new KeyValuePair<String, MARC.HI.EHRS.SVC.Core.DataTypes.AddressSet.AddressSetUse>("work", MARC.HI.EHRS.SVC.Core.DataTypes.AddressSet.AddressSetUse.WorkPlace),
            new KeyValuePair<String, MARC.HI.EHRS.SVC.Core.DataTypes.AddressSet.AddressSetUse>("temp", MARC.HI.EHRS.SVC.Core.DataTypes.AddressSet.AddressSetUse.TemporaryAddress),
            new KeyValuePair<String, MARC.HI.EHRS.SVC.Core.DataTypes.AddressSet.AddressSetUse>("old", MARC.HI.EHRS.SVC.Core.DataTypes.AddressSet.AddressSetUse.BadAddress),
        };

        /// <summary>
        /// Address use mapping
        /// </summary>
        public static readonly List<KeyValuePair<String, TelecommunicationAddressUse>> TELECOM_USE = new List<KeyValuePair<string, TelecommunicationAddressUse>>() {
            new KeyValuePair<String, TelecommunicationAddressUse>("home", TelecommunicationAddressUse.Home),
            new KeyValuePair<String, TelecommunicationAddressUse>("home", TelecommunicationAddressUse.PrimaryHome),
            new KeyValuePair<String, TelecommunicationAddressUse>("work", TelecommunicationAddressUse.WorkPlace),
            new KeyValuePair<String, TelecommunicationAddressUse>("temp", TelecommunicationAddressUse.TemporaryAddress),
            new KeyValuePair<String, TelecommunicationAddressUse>("old", TelecommunicationAddressUse.BadAddress),
            new KeyValuePair<String, TelecommunicationAddressUse>("mobile", TelecommunicationAddressUse.MobileContact)
        };

        /// <summary>
        /// Address part type
        /// </summary>
        public static readonly Dictionary<String, MARC.HI.EHRS.SVC.Core.DataTypes.AddressPart.AddressPartType> ADDRESS_PART = new Dictionary<string, SVC.Core.DataTypes.AddressPart.AddressPartType>() {
            { "line", MARC.HI.EHRS.SVC.Core.DataTypes.AddressPart.AddressPartType.AddressLine },
            { "city", MARC.HI.EHRS.SVC.Core.DataTypes.AddressPart.AddressPartType.City },
            { "state", MARC.HI.EHRS.SVC.Core.DataTypes.AddressPart.AddressPartType.State },
            { "zip", MARC.HI.EHRS.SVC.Core.DataTypes.AddressPart.AddressPartType.PostalCode },
            { "country", MARC.HI.EHRS.SVC.Core.DataTypes.AddressPart.AddressPartType.Country }
        };

        /// <summary>
        /// Date precisions
        /// </summary>
        public static readonly Dictionary<String, DatePrecision> DATE_PRECISION = new Dictionary<string, DatePrecision>()
        {
            { "D", DatePrecision.Day },
            { "M", DatePrecision.Month },
            { "Y", DatePrecision.Year },
            { "F", DatePrecision.Full }
        };

        
        /// <summary>
        /// Name use
        /// </summary>
        public static readonly Dictionary<String, MARC.HI.EHRS.SVC.Core.DataTypes.NameSet.NameSetUse> NAME_USE = new Dictionary<string, SVC.Core.DataTypes.NameSet.NameSetUse>()
        {
            { "usual", MARC.HI.EHRS.SVC.Core.DataTypes.NameSet.NameSetUse.Search },
            { "official", MARC.HI.EHRS.SVC.Core.DataTypes.NameSet.NameSetUse.Legal },
            { "temp", MARC.HI.EHRS.SVC.Core.DataTypes.NameSet.NameSetUse.Assigned },
            { "anonymous", MARC.HI.EHRS.SVC.Core.DataTypes.NameSet.NameSetUse.Pseudonym },
            { "nickname", MARC.HI.EHRS.SVC.Core.DataTypes.NameSet.NameSetUse.Artist },        
            { "maiden", MARC.HI.EHRS.SVC.Core.DataTypes.NameSet.NameSetUse.MaidenName }
        };

        /// <summary>
        /// Relationship kinds
        /// </summary>
        public static readonly List<KeyValuePair<String, String>> RELATIONSHIP_KIND = new List<KeyValuePair<string, string>>()
        { 
            new KeyValuePair<String, String>("emergency", "ECON"),
            new KeyValuePair<String, String>("family", "FAMMEMB"),
            new KeyValuePair<String, String>("family", "AUNT"),
            new KeyValuePair<String, String>("family", "BRO"),
            new KeyValuePair<String, String>("family", "BORINLAW"),
            new KeyValuePair<String, String>("family", "CHILD"),
            new KeyValuePair<String, String>("family", "CHLDADOPT"),
            new KeyValuePair<String, String>("guardian", "GUARD"),
            new KeyValuePair<String, String>("family", "CHLDFOST"),
            new KeyValuePair<String, String>("family", "DAUFOST"),
            new KeyValuePair<String, String>("family", "SONFOST"),
            new KeyValuePair<String, String>("family", "CHLDINLAW"),
            new KeyValuePair<String, String>("family", "COUSN"),
            new KeyValuePair<String, String>("family", "DAU"),
            new KeyValuePair<String, String>("family", "DAUADOPT"),
            new KeyValuePair<String, String>("family", "DUAC"),
            new KeyValuePair<String, String>("family", "DAUADOPT"),
            new KeyValuePair<String, String>("family", "DAUINLAW"),
            new KeyValuePair<String, String>("family", "GGRFTH"),
            new KeyValuePair<String, String>("family", "GGRMTH"),
            new KeyValuePair<String, String>("family", "GGRPRN"),
            new KeyValuePair<String, String>("family", "GRFTH"),
            new KeyValuePair<String, String>("family", "GRMTH"),
            new KeyValuePair<String, String>("family", "GRNDCHILD"),
            new KeyValuePair<String, String>("family", "GRNDDAU"),
            new KeyValuePair<String, String>("family", "GRNDSON"),
            new KeyValuePair<String, String>("family", "GRPRN"),
            new KeyValuePair<String, String>("family", "HBRO"),
            new KeyValuePair<String, String>("family", "HSIB"),
            new KeyValuePair<String, String>("family", "HSIS"),
            new KeyValuePair<String, String>("family", "MAUNT"),
            new KeyValuePair<String, String>("family", "MCOUSN"),
            new KeyValuePair<String, String>("family", "MGGRFTH"),
            new KeyValuePair<String, String>("family", "MGGRMTH"),
            new KeyValuePair<String, String>("family", "MGGRPRN"),
            new KeyValuePair<String, String>("family", "MGRFTH"),
            new KeyValuePair<String, String>("family", "MGRMTH"),
            new KeyValuePair<String, String>("family", "MGRPRN"),
            new KeyValuePair<String, String>("family", "MTHINLAW"),
            new KeyValuePair<String, String>("family", "MUNCLE"),
            new KeyValuePair<String, String>("family", "NBRO"),
            new KeyValuePair<String, String>("family", "NCHILD"),
            new KeyValuePair<String, String>("family", "NEPHEW"),
            new KeyValuePair<String, String>("family", "NFTH"),
            new KeyValuePair<String, String>("family", "NFTHF"),
            new KeyValuePair<String, String>("family", "NIECE"),
            new KeyValuePair<String, String>("family", "NIENEPH"),
            new KeyValuePair<String, String>("family", "NSIB"),
            new KeyValuePair<String, String>("family", "NSIS"),
            new KeyValuePair<String, String>("family", "PAUNT"),
            new KeyValuePair<String, String>("family", "PCOUSN"),
            new KeyValuePair<String, String>("family", "PGGRFTH"),
            new KeyValuePair<String, String>("family", "PGGRMTH"),
            new KeyValuePair<String, String>("family", "PGGRPRN"),
            new KeyValuePair<String, String>("family", "PGRFTH"),
            new KeyValuePair<String, String>("family", "PGRMTH"),
            new KeyValuePair<String, String>("family", "PGRPRN"),
            new KeyValuePair<String, String>("family", "PUNCLE"),
            new KeyValuePair<String, String>("family", "SIB"),
            new KeyValuePair<String, String>("family", "SIBINLAW"),
            new KeyValuePair<String, String>("family", "SIS"),
            new KeyValuePair<String, String>("family", "SISINLAW"),
            new KeyValuePair<String, String>("family", "SON"),
            new KeyValuePair<String, String>("family", "SONADOPT"),
            new KeyValuePair<String, String>("family", "SONC"),
            new KeyValuePair<String, String>("family", "SONINLAW"),
            new KeyValuePair<String, String>("family", "STPBRO"),
            new KeyValuePair<String, String>("family", "STPCHLD"),
            new KeyValuePair<String, String>("family", "STPDAU"),
            new KeyValuePair<String, String>("family", "STPSIB"),
            new KeyValuePair<String, String>("family", "STPSIS"),
            new KeyValuePair<String, String>("family", "STPSON"),
            new KeyValuePair<String, String>("family", "UNCLE"),
            new KeyValuePair<String, String>("friend", "FRND"),
            new KeyValuePair<String, String>("family", "NOK"),
            new KeyValuePair<String, String>("friend", "NBOR"),
            new KeyValuePair<String, String>("friend", "ROOM"),
            new KeyValuePair<String, String>("partner", "SIGOTHR"),
            new KeyValuePair<String, String>("partner", "SPS"),
            new KeyValuePair<String, String>("partner", "DOMPART"),
            new KeyValuePair<String, String>("partner", "HUSB"),
            new KeyValuePair<String, String>("partner", "WIFE"),
            new KeyValuePair<String, String>("agent", "SUBDM"),
            new KeyValuePair<String, String>("agent", "POWATT"),
            new KeyValuePair<String, String>("agent", "POWATY"),
            new KeyValuePair<String, String>("agent", "POWATYPR"),
            new KeyValuePair<String, String>("agent", "POWATYPT"),
            new KeyValuePair<String, String>("parent", "PRN"),
            new KeyValuePair<String, String>("parent", "PRNINLAW"),
            new KeyValuePair<String, String>("parent", "NMTH"),
            new KeyValuePair<String, String>("parent", "MTH"),
            new KeyValuePair<String, String>("parent", "FTH"),
            new KeyValuePair<String, String>("parent", "NPRN"),
            new KeyValuePair<String, String>("parent", "STPFTH"),
            new KeyValuePair<String, String>("parent", "STPMTH"),
            new KeyValuePair<String, String>("parent", "STPPRN"),
        };

        /// <summary>
        /// Reverse lookup
        /// </summary>
        public static String ReverseLookup<T>(List<KeyValuePair<String, T>> codeset, T canonicalCode)
        {
            foreach (var kv in codeset)
                if (kv.Value.Equals(canonicalCode))
                    return kv.Key;
            return null;
        }


        /// <summary>
        /// Lookup a code
        /// </summary>
        /// <returns></returns>
        public static T Lookup<T>(List<KeyValuePair<String, T>> codeset, string fhirCode)
        {
            T value = codeset.Find(o => o.Key == fhirCode).Value;
            
            return value;
        }

        /// <summary>
        /// Reverse lookup
        /// </summary>
        public static String ReverseLookup<T>(Dictionary<String, T> codeset, T canonicalCode)
        {
            foreach (var kv in codeset)
                if (kv.Value.Equals(canonicalCode))
                    return kv.Key;
            return null;
        }


        /// <summary>
        /// Lookup a code
        /// </summary>
        /// <returns></returns>
        public static T Lookup<T>(Dictionary<String, T> codeset, string fhirCode)
        {
            T value = default(T);
            codeset.TryGetValue(fhirCode, out value);
            return value;
        }
    }
}
