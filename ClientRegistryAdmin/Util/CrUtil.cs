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
            pm.OriginalData = reg;
            pm.OtherIds = new List<KeyValuePair<string, string>>();
            foreach (var altid in psn.altId)
                pm.OtherIds.Add(new KeyValuePair<string, string>(altid.domain, altid.uid));
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