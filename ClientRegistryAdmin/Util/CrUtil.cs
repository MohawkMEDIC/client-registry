using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using ClientRegistryAdmin.ClientRegistryAdminService;
using MARC.Everest.DataTypes;
using ClientRegistryAdmin.Models;
using System.Diagnostics;

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
        public static Models.PatientMatch[] GetRecentActivity(TimeSpan since, int offset, int count)
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

                var registrations = client.GetRecentActivity(sinceRange, offset, count, false);
                ClientRegistryAdmin.Models.PatientMatch[] retVal = new PatientMatch[registrations.count];
                for(int i = 0; i < registrations.registration.Length; i++)
                {
                    ClientRegistryAdmin.Models.PatientMatch pm = ConvertRegistrationEvent(registrations.registration[i]);
                    // Address?
                    retVal[offset + i] = pm;
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
        public static Models.PatientMatch[] Search(string familyName, string givenName, string dob, string identifier, int offset, int count)
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

            if (identifier != null)
                queryPrototype.altId = new DomainIdentifier[] { new DomainIdentifier() { uid = identifier } };
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
                var registrations = client.GetRegistrations(queryPrototype, offset, count);
                ClientRegistryAdmin.Models.PatientMatch[] retVal = new PatientMatch[registrations.count];
                for (int i = 0; i < registrations.registration?.Length; i++)
                {
                    ClientRegistryAdmin.Models.PatientMatch pm = ConvertRegistrationEvent(registrations.registration[i]);
                    // Address?
                    retVal[offset + i] = pm;
                }

                return retVal;
            }
            catch(Exception e)
            {
                return null;
            }
        }

        /// <summary>
        /// Convert registration event
        /// </summary>
        private static Models.PatientMatch ConvertRegistrationEvent(RegistrationEvent reg)
        {
            var psn = reg.Items1.Where(o => o.hsrSite.roleType == HealthServiceRecordSiteRoleType.SubjectOf).First() as Person;
            ClientRegistryAdmin.Models.PatientMatch pm = new Models.PatientMatch();
            NamePart familyNamePart = null,
                givenNamePart = null;
            pm.Status = psn.status.ToString();
            pm.VersionId = psn.verId.ToString();
            // Name
            if (psn.name != null && psn.name[0].part != null)
            {
                familyNamePart = psn.name[0].part.FirstOrDefault(o => o.type == NamePartType.Family);
                givenNamePart = psn.name[0].part.FirstOrDefault(o => o.type == NamePartType.Given);
                if (familyNamePart != null)
                    pm.FamilyName = familyNamePart.value;
                if (givenNamePart != null)
                    pm.GivenName = givenNamePart.value;
            }

            if(psn.birthTime != null)
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
                    precinctPart = psn.addr[0].part.FirstOrDefault(o => o.type == ClientRegistryAdminService.AddressPartType.Precinct),
                    locatorPart = psn.addr[0].part.FirstOrDefault(o => o.type == ClientRegistryAdminService.AddressPartType.AdditionalLocator),
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
                if (precinctPart != null)
                    pm.Precinct = precinctPart.value;
                if (locatorPart != null)
                    pm.Locator = locatorPart.value;
            }

            // Relationships
            if (psn.Items != null)
            {
                var mother = psn.Items.Where(o => o.hsrSite.roleType == HealthServiceRecordSiteRoleType.RepresentitiveOf)
                    .Select(o => o as PersonalRelationship).FirstOrDefault(o => o.kind == "MTH");
                if (mother != null)
                {
                    pm.MothersId = mother.id.ToString();
                    if (mother.legalName != null && mother.legalName.part != null)
                    {
                        familyNamePart = mother.legalName.part.FirstOrDefault(o => o.type == NamePartType.Family);
                        givenNamePart = mother.legalName.part.FirstOrDefault(o => o.type == NamePartType.Given);
                    }
                    else
                        familyNamePart = givenNamePart = new NamePart();

                    if (familyNamePart != null)
                        pm.MothersName = familyNamePart.value + ", ";
                    if (givenNamePart != null)
                        pm.MothersName += givenNamePart.value;

                    if (pm.MothersName.EndsWith(", "))
                        pm.MothersName = pm.MothersName.Substring(0, pm.MothersName.Length - 2);
                }
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
                {
                    throw new Exception("Patient not found");
                }

                return ConvertRegistrationEvent(regEvent);

            }
            catch (Exception e)
            {
                Trace.TraceError("Error getting patient: {0}", e);
                throw;
            }
        }

        /// <summary>
        /// Get conflicts
        /// </summary>
        public static Models.ConflictPatientMatch[] GetConflicts(int offset, int count)
        {
            try
            {
                ClientRegistryAdminInterfaceClient client = new ClientRegistryAdminInterfaceClient();
                var conflicts = client.GetConflicts(offset, count, false);
                Models.ConflictPatientMatch[] retVal = new ConflictPatientMatch[conflicts.count];
                if (conflicts == null || conflicts.conflict == null)
                    return retVal;
                for (int i = 0; i < conflicts.conflict.Length; i++)
                {
                    ConflictPatientMatch match = new ConflictPatientMatch();
                    match.Patient = ConvertRegistrationEvent(conflicts.conflict[i].source);
                    match.Matching = new List<PatientMatch>();
                    foreach (var m in conflicts.conflict[i].matches)
                        match.Matching.Add(ConvertRegistrationEvent(m));
                    retVal[offset + i] = match;
                }
                return retVal;
                
            }
            catch (Exception e)
            {
                return null;
            }
        }

        /// <summary>
        /// Get a particular conflict
        /// </summary>
        public static ConflictPatientMatch GetConflict(decimal id)
        {
            try
            {
                ClientRegistryAdminInterfaceClient client = new ClientRegistryAdminInterfaceClient();
                var conflicts = client.GetConflict(id).conflict;
                var conflict = conflicts[0];
                ConflictPatientMatch retVal = new ConflictPatientMatch();
                retVal.Patient = ConvertRegistrationEvent(conflict.source);
                retVal.Matching = new List<PatientMatch>();
                foreach (var m in conflict.matches)
                    retVal.Matching.Add(ConvertRegistrationEvent(m));
                return retVal;
            }
            catch (Exception e)
            {
                return null;
            }
        }

        /// <summary>
        /// Count recent activity
        /// </summary>
        public static int CountRecentActivity(TimeSpan since)
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

                return client.GetRecentActivity(sinceRange, 0, 0, true).count;
                
            }
            catch (Exception e)
            {
                return 0;
            }
        }

        /// <summary>
        /// Count conflicts
        /// </summary>
        /// <returns></returns>
        public static int CountConflicts()
        {
            try
            {
                ClientRegistryAdminInterfaceClient client = new ClientRegistryAdminInterfaceClient();
            
                return client.GetConflicts(0, Int32.MaxValue, true).count;

            }
            catch (Exception e)
            {
                return 0;
            }
        }
    }
}