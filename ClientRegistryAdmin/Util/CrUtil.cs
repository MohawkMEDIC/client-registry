using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using ClientRegistryAdmin.ClientRegistryAdminService;
using MARC.Everest.DataTypes;

namespace ClientRegistryAdmin.Util
{
    /// <summary>
    /// CR Utility
    /// </summary>
    public static class CrUtil
    {

        /// <summary>
        /// Get recent changes
        /// </summary>
        public static List<Models.PatientMatch> GetRecentActivity(TimeSpan since)
        {
            try
            {
                ClientRegistryAdminInterfaceClient client = new ClientRegistryAdminInterfaceClient();

                DateTime high = DateTime.Now,
                    low = DateTime.Now.Subtract(since);

                TimestampSet sinceRange = new TimestampSet()
                {
                    part = new TimestampPart[] {
                        new TimestampPart() { value = low, type = TimestampPartType.LowBound },
                        new TimestampPart() { value = high, type = TimestampPartType.HighBound }
                    }
                };

                var registrations = client.GetRecentActivity(sinceRange);
                List<ClientRegistryAdmin.Models.PatientMatch> retVal = new List<Models.PatientMatch>();
                foreach (var reg in registrations)
                {
                    ClientRegistryAdmin.Models.PatientMatch pm = ConvertRegistrationEvent(reg);
                    // Address?
                    retVal.Add(pm);
                }
                return retVal;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Search 
        /// </summary>
        public static List<Models.PatientMatch> Search(string familyName, string givenName, string dob)
        {
            ClientRegistryAdminInterfaceClient client = new ClientRegistryAdminInterfaceClient();

            // Query prototype
            Person queryPrototype = new Person();
            NameSet name = new NameSet();
            NamePart familyNamePart = null,
                givenNamePart = null;
            if (familyName != null)
                familyNamePart = new NamePart()
                {
                    type = NamePartType.Family,
                    value = familyName
                };
            if (givenName != null)
                givenNamePart = new NamePart()
                {
                    type = NamePartType.Given,
                    value = givenName
                };
            List<NamePart> parts = new List<NamePart>();
            if (givenNamePart != null)
                parts.Add(givenNamePart);
            if (familyNamePart != null)
                parts.Add(familyNamePart);
            name.part = parts.ToArray();
            if (name.part.Length > 0)
                queryPrototype.name = new NameSet[] { name };

            // dob
            if (dob != null)
            {
                TS tsDob = (TS)dob;
                queryPrototype.birthTime = new TimestampPart()
                {
                    type = TimestampPartType.Standlone,
                    value = tsDob.DateValue,
                    precision = tsDob.DateValuePrecision == DatePrecision.Day ? "D" :
                            tsDob.DateValuePrecision == DatePrecision.Month ? "M" :
                            tsDob.DateValuePrecision == DatePrecision.Year ? "Y" :
                            "F"
                };
            }

            try
            {
                var registrations = client.GetRegistrations(queryPrototype);
                List<ClientRegistryAdmin.Models.PatientMatch> retVal = new List<Models.PatientMatch>();
                foreach (var reg in registrations)
                {
                    ClientRegistryAdmin.Models.PatientMatch pm = ConvertRegistrationEvent(reg);
                    // Address?
                    retVal.Add(pm);
                }
                return retVal;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Convert registration event
        /// </summary>
        private static Models.PatientMatch ConvertRegistrationEvent(HealthServiceRecord reg)
        {
            var psn = reg.Items1.Where(o => o.hsrSite.roleType == HealthServiceRecordSiteRoleType.SubjectOf).First() as Person;
            ClientRegistryAdmin.Models.PatientMatch pm = new Models.PatientMatch();
            NamePart familyNamePart = null,
                givenNamePart = null;

            // Name
            if (psn.name != null)
            {
                familyNamePart = psn.name[0].part.FirstOrDefault(o => o.type == NamePartType.Family);
                givenNamePart = psn.name[0].part.FirstOrDefault(o => o.type == NamePartType.Given);
                if (familyNamePart != null)
                    pm.FamilyName = familyNamePart.value;
                if (givenNamePart != null)
                    pm.GivenName = givenNamePart.value;
            }

            pm.DateOfBirth = psn.birthTime.value;
            pm.Gender = psn.genderCode;

            pm.Id = psn.id.ToString();
            pm.RegistrationId = reg.id;
            pm.HealthServiceEvent = reg;
            pm.OriginalData = psn;
            pm.OtherIds = new List<KeyValuePair<string, string>>();
            foreach (var altid in psn.altId)
                pm.OtherIds.Add(new KeyValuePair<string, string>(altid.domain, altid.uid));

            // Address
            if (psn.addr != null)
            {
                AddressPart cityPart = psn.addr[0].part.FirstOrDefault(o => o.type == ClientRegistryAdminService.AddressPartType.City),
                    countyPart = psn.addr[0].part.FirstOrDefault(o => o.type == ClientRegistryAdminService.AddressPartType.County),
                    countryPart = psn.addr[0].part.FirstOrDefault(o => o.type == ClientRegistryAdminService.AddressPartType.Country),
                    statePart = psn.addr[0].part.FirstOrDefault(o => o.type == ClientRegistryAdminService.AddressPartType.State),
                    postalPart = psn.addr[0].part.FirstOrDefault(o => o.type == ClientRegistryAdminService.AddressPartType.PostalCode),
                    censusPart = psn.addr[0].part.FirstOrDefault(o => o.type == ClientRegistryAdminService.AddressPartType.CensusTract),
                    streetPart = psn.addr[0].part.FirstOrDefault(o => o.type == ClientRegistryAdminService.AddressPartType.AddressLine ||
                        o.type == ClientRegistryAdminService.AddressPartType.StreetAddressLine);
                if (cityPart != null)
                    pm.City = cityPart.value;
                if (streetPart != null)
                    pm.Address = streetPart.value;
                if (countyPart != null)
                    pm.County = countyPart.value;
                if (countryPart != null)
                    pm.Country = countryPart.value;
                if (statePart != null)
                    pm.State = statePart.value;
                if (postalPart != null)
                    pm.PostCode = postalPart.value;
                if (censusPart != null)
                    pm.CensusTract = censusPart.value;

            }

            // Relationships
            var mother = psn.Items.Where(o => o.hsrSite.roleType == HealthServiceRecordSiteRoleType.RepresentitiveOf)
                .Select(o => o as PersonalRelationship).FirstOrDefault(o => o.kind == "MTH");
            if (mother != null)
            {
                pm.MothersId = mother.id.ToString();
                familyNamePart = mother.legalName.part.FirstOrDefault(o => o.type == NamePartType.Family);
                givenNamePart = mother.legalName.part.FirstOrDefault(o => o.type == NamePartType.Given);
                if (familyNamePart != null)
                    pm.MothersName = familyNamePart.value  + ", ";
                if (givenNamePart != null)
                    pm.MothersName += givenNamePart.value;

                if (pm.MothersName.EndsWith(", "))
                    pm.MothersName = pm.MothersName.Substring(0, pm.MothersName.Length - 2);
            }
            return pm;
        }

        /// <summary>
        /// Get the specified patient identifier
        /// </summary>
        internal static Models.PatientMatch Get(decimal id)
        {
            try
            {
                ClientRegistryAdminInterfaceClient client = new ClientRegistryAdminInterfaceClient();
                var regEvent = client.GetRegistrationEvent(id);
                if (regEvent == null)
                    return null;

                return ConvertRegistrationEvent(regEvent);

            }
            catch (Exception e)
            {
                return null;
            }
        }
    }
}