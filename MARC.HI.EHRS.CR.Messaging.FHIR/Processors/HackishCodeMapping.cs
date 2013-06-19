using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
        public static readonly Dictionary<String, MARC.HI.EHRS.SVC.Core.DataTypes.AddressSet.AddressSetUse> ADDRESS_USE = new Dictionary<string, SVC.Core.DataTypes.AddressSet.AddressSetUse>() {
            { "home", MARC.HI.EHRS.SVC.Core.DataTypes.AddressSet.AddressSetUse.HomeAddress },
            { "work", MARC.HI.EHRS.SVC.Core.DataTypes.AddressSet.AddressSetUse.WorkPlace },
            { "temp", MARC.HI.EHRS.SVC.Core.DataTypes.AddressSet.AddressSetUse.TemporaryAddress },
            { "old", MARC.HI.EHRS.SVC.Core.DataTypes.AddressSet.AddressSetUse.BadAddress }
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
            { "d", MARC.HI.EHRS.CR.Messaging.FHIR.DataTypes.DatePrecision.Day },
            { "m", MARC.HI.EHRS.CR.Messaging.FHIR.DataTypes.DatePrecision.Month },
            { "y", MARC.HI.EHRS.CR.Messaging.FHIR.DataTypes.DatePrecision.Year }
        };

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
