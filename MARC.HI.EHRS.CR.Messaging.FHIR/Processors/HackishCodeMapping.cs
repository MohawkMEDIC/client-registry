using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MARC.Everest.DataTypes.Interfaces;

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
        public static readonly Dictionary<String, MARC.HI.EHRS.CR.Messaging.FHIR.DataTypes.DatePrecision> DATE_PRECISION = new Dictionary<string, DataTypes.DatePrecision>()
        {
            { "D", MARC.HI.EHRS.CR.Messaging.FHIR.DataTypes.DatePrecision.Day },
            { "M", MARC.HI.EHRS.CR.Messaging.FHIR.DataTypes.DatePrecision.Month },
            { "Y", MARC.HI.EHRS.CR.Messaging.FHIR.DataTypes.DatePrecision.Year }
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
