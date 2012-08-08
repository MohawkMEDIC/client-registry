/* 
 * Copyright 2008-2011 Mohawk College of Applied Arts and Technology
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
 * User: Justin Fyfe
 * Date: 08-24-2011
 */
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using MARC.HI.EHRS.CR.Core.ComponentModel;
using MARC.HI.EHRS.SVC.Core.ComponentModel;
using MARC.HI.EHRS.SVC.Core.ComponentModel.Components;
using MARC.HI.EHRS.SVC.Core.DataTypes;
using MARC.HI.EHRS.SVC.Core.Services;
using MARC.HI.EHRS.SVC.HealthWorkerIdentity;

namespace MARC.HI.EHRS.CR.Persistence.Data.ComponentPersister
{
    /// <summary>
    /// Healthcare participant registration
    /// </summary>
    public class HealthcareParticipantPersister : IComponentPersister
    {
        #region IComponentPersister Members

        /// <summary>
        /// What component does this persister handle
        /// </summary>
        public Type HandlesComponent { get { return typeof(HealthcareParticipant); } }

        /// <summary>
        /// Persist an object
        /// </summary>
        public VersionedDomainIdentifier Persist(System.Data.IDbConnection conn, System.Data.IDbTransaction tx, System.ComponentModel.IComponent data, bool isUpdate)
        {
            ISystemConfigurationService configServce = ApplicationContext.ConfigurationService; //ApplicationContext.Current.GetService(typeof(ISystemConfigurationService)) as ISystemConfigurationService;

            // First, we must determine if we're going to be performing an update or a put
            HealthcareParticipant ptcpt = data as HealthcareParticipant;

            // Do we have the shrid?
            if (ptcpt.Id == default(decimal) && ptcpt.AlternateIdentifiers.Count > 0) // nope
            {
                // Attempt to get the SHRID
                int iId = 0;
                HealthcareParticipant resPtcpt = null;
                while (resPtcpt == null && iId < ptcpt.AlternateIdentifiers.Count)
                {
                    resPtcpt = GetProvider(conn, tx, ptcpt.AlternateIdentifiers[iId]);
                    iId++;
                }

                if (resPtcpt == null && !DatabasePersistenceService.ValidationSettings.PersonsMustExist) // We need to create a client
                {
                    // Validate Clients
                    if (DatabasePersistenceService.ValidationSettings.ValidateHealthcareParticipants)
                    {
                        IHealthcareWorkerIdentityService ptcptLookup = ApplicationContext.CurrentContext.GetService(typeof(IHealthcareWorkerIdentityService)) as IHealthcareWorkerIdentityService;
                        if (ptcptLookup == null)
                            throw new InvalidOperationException("Unable to validate participant as no participant lookup service exists that can fulfill this request");
                        resPtcpt = ptcptLookup.FindParticipant(ptcpt.AlternateIdentifiers[0]);
                        if (resPtcpt == null || QueryUtil.MatchName(resPtcpt.LegalName, resPtcpt.LegalName) < DatabasePersistenceService.ValidationSettings.PersonNameMatch)
                            throw new DataException(String.Format("Could not validate participant {1}^^^&{0}&ISO against the participant validation service", ptcpt.AlternateIdentifiers[0].Domain, ptcpt.AlternateIdentifiers[0].Identifier));
                    }
                    CreatePtcpt(conn, tx, ptcpt);
                }
                else if (resPtcpt == null)
                    throw new DataException(String.Format("Particiapant {1}^^^&{0}&ISO cannot be found in this system", ptcpt.AlternateIdentifiers[0].Domain, ptcpt.AlternateIdentifiers[0].Identifier));
                else
                {
                    // Validate the name given matches the legal name. Has to be more than
                    // 80% match
                    if (QueryUtil.MatchName(ptcpt.LegalName, resPtcpt.LegalName) < DatabasePersistenceService.ValidationSettings.PersonNameMatch)
                        throw new DataException(String.Format("The provided legal name does not match the legal name of participant {1}^^^&{0}&ISO, please ensure participant name is correct", ptcpt.AlternateIdentifiers[0].Domain, ptcpt.AlternateIdentifiers[0].Identifier));

                    ptcpt.Id = resPtcpt.Id;
                    ptcpt.LegalName = resPtcpt.LegalName ?? ptcpt.LegalName;
                    ptcpt.PrimaryAddress = resPtcpt.PrimaryAddress ?? ptcpt.PrimaryAddress;
                    ptcpt.Type = resPtcpt.Type ?? ptcpt.Type;

                    // TODO: Update the information about the provider 

                    // Register alternative identifiers
                    foreach (var id in ptcpt.AlternateIdentifiers)
                        if (resPtcpt.AlternateIdentifiers.Count(o => o.Domain == id.Domain) == 0) // register
                            CreateAlternateIdentifier(conn, tx, ptcpt.Id, id);
                }
            }

            // Persist the site with the container if known
            if (ptcpt.Site.Container is RegistrationEvent)
                LinkHealthServiceRecord(conn, tx, (ptcpt.Site.Container as RegistrationEvent).Id, ptcpt.Site as HealthServiceRecordSite);

            // Persist any components within the provider record
            DbUtil.PersistComponents(conn, tx, isUpdate, this, ptcpt);

            // Return the versioned identifier
            return new VersionedDomainIdentifier()
            {
                Domain = configServce.OidRegistrar.GetOid(ClientRegistryOids.PROVIDER_CRID).Oid,
                Identifier = ptcpt.Id.ToString()
            };
        }

        /// <summary>
        /// Link participant to a health service record 
        /// </summary>
        private void LinkHealthServiceRecord(IDbConnection conn, IDbTransaction tx, decimal identifier, HealthServiceRecordSite healthServiceRecordSite)
        {
            IDbCommand cmd = DbUtil.CreateCommandStoredProc(conn, tx);
            try
            {
                cmd.CommandText = "link_hc_ptcpt";

                // Represented organization
                HealthcareParticipant paticipantComponent = healthServiceRecordSite.Component as HealthcareParticipant;
                var repOrganizations = from HealthServiceRecordComponent comp in paticipantComponent.Components
                                       where (comp.Site as HealthServiceRecordSite).SiteRoleType == HealthServiceRecordSiteRoleType.RepresentitiveOf
                                       select comp;
                HealthcareParticipant representedOrganization = repOrganizations.Count() > 0 ? repOrganizations.First() as HealthcareParticipant : null;
                if (representedOrganization != null)
                    representedOrganization.Id = Convert.ToDecimal(Persist(conn, tx, representedOrganization, false).Identifier);

                // Parameters
                cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "hsr_id_in", DbType.Decimal, identifier));
                cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "ptcpt_id_in", DbType.Decimal, paticipantComponent.Id));
                cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "ptcpt_cls_in", DbType.Decimal, (decimal)healthServiceRecordSite.SiteRoleType));
                cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "ptcpt_rep_org_id_in", DbType.Decimal, representedOrganization == null ? DBNull.Value : (object)representedOrganization.Id));

                // Insert
                cmd.ExecuteNonQuery();

                // Insert original identifiers
                if(healthServiceRecordSite.OriginalIdentifier != null)
                    foreach (var id in healthServiceRecordSite.OriginalIdentifier)
                        CreateOriginalIdentifier(conn, tx, identifier, healthServiceRecordSite, id);

            }
            finally
            {
                cmd.Dispose();
            }
        }

        /// <summary>
        /// Create original identifier for a link
        /// </summary>
        private void CreateOriginalIdentifier(IDbConnection conn, IDbTransaction tx, decimal identifier, HealthServiceRecordSite healthServiceRecordSite, DomainIdentifier id)
        {
            IDbCommand cmd = DbUtil.CreateCommandStoredProc(conn, tx);
            try
            {
                // Participant
                HealthcareParticipant paticipantComponent = healthServiceRecordSite.Component as HealthcareParticipant;

                cmd.CommandText = "add_link_hc_ptcpt_orig_id";
                cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "hsr_id_in", DbType.Decimal, identifier));
                cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "ptcpt_id_in", DbType.Decimal, paticipantComponent.Id));
                cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "ptcpt_cls_in", DbType.Decimal, (decimal)healthServiceRecordSite.SiteRoleType));
                cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "orig_id_domain_in", DbType.StringFixedLength, id.Domain));
                cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "orig_id_in", DbType.StringFixedLength, (object)id.Identifier ?? DBNull.Value));
                cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "license_ind_in", DbType.Boolean, id.IsLicenseAuthority));

                // Insert
                cmd.ExecuteNonQuery();
            }
            finally
            {
                cmd.Dispose();
            }
        }

        /// <summary>
        /// Create an alternate identifier for the provider
        /// </summary>
        private void CreateAlternateIdentifier(IDbConnection conn, IDbTransaction tx, decimal identifier, DomainIdentifier altId)
        {
            IDbCommand cmd = DbUtil.CreateCommandStoredProc(conn, tx);

            try
            {
                cmd.CommandText = "crt_ptcpt_alt_id";

                // parameters
                cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "ptcpt_id_in", DbType.Decimal, identifier));
                cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "alt_id_domain_in", DbType.StringFixedLength, altId.Domain));
                cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "alt_id_in", DbType.StringFixedLength, (object)altId.Identifier ?? DBNull.Value));

                // Execute
                cmd.ExecuteNonQuery();
            }
            finally
            {
                cmd.Dispose();
            }
        }

        /// <summary>
        /// Create a participant
        /// </summary>
        private void CreatePtcpt(System.Data.IDbConnection conn, System.Data.IDbTransaction tx, HealthcareParticipant ptcpt)
        {
            IDbCommand cmd = DbUtil.CreateCommandStoredProc(conn, tx);

            try
            {
                // Create name set
                decimal? nameSetId = null, addrSetId = null, 
                    typeCodeId = null;

                // Create name
                if (ptcpt.LegalName != null)
                    nameSetId = DbUtil.CreateNameSet(conn, tx, ptcpt.LegalName);
                if (ptcpt.PrimaryAddress != null)
                    addrSetId = DbUtil.CreateAddressSet(conn, tx, ptcpt.PrimaryAddress);
                if(ptcpt.Type != null)
                    typeCodeId = DbUtil.CreateCodedValue(conn, tx, ptcpt.Type);

                // Does
                // Create person
                if(ptcpt.Classifier == HealthcareParticipant.HealthcareParticipantType.Person)
                {
                    cmd.CommandText = "crt_hc_ptcpt_psn";
                    cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "ptcpt_name_set_id_in", DbType.Decimal, (object)nameSetId ?? DBNull.Value));
                    cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "ptcpt_addr_set_id_in", DbType.Decimal, (object)addrSetId ?? DBNull.Value));
                    cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "ptcpt_tel_in", DbType.StringFixedLength, DBNull.Value));
                    cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "ptcpt_typ_cd_id_in", DbType.Decimal, (object)typeCodeId ?? DBNull.Value));
                }
                else
                {
                    cmd.CommandText = "crt_hc_ptcpt_org";
                    cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "ptcpt_name_in", DbType.StringFixedLength, ptcpt.LegalName != null && ptcpt.LegalName.Parts.Count > 0 ? (object)ptcpt.LegalName.Parts[0].Value : DBNull.Value));
                    cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "ptcpt_addr_set_id_in", DbType.Decimal, (object)addrSetId ?? DBNull.Value));
                    cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "ptcpt_work_tel_in", DbType.StringFixedLength, DBNull.Value));
                    cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "ptcpt_typ_cd_id_in", DbType.Decimal, (object)typeCodeId ?? DBNull.Value));
                }
                
                // Execute a scalar 
                ptcpt.Id = Convert.ToDecimal(cmd.ExecuteScalar());

                // Register an alternate identifier if they exist
                
                // normalize alt identifiers, remove duplicates
                for (int i = ptcpt.AlternateIdentifiers.Count - 1; i > 0; i--)
                    if (ptcpt.AlternateIdentifiers.Count(o => o.Domain.Equals(ptcpt.AlternateIdentifiers[i].Domain) ||
                        o.Identifier.Equals(ptcpt.AlternateIdentifiers[i].Identifier)) > 1)
                        ptcpt.AlternateIdentifiers.RemoveAt(i);

                foreach (var id in ptcpt.AlternateIdentifiers)
                    CreateAlternateIdentifier(conn, tx, ptcpt.Id, id);

                // Register all telecom addresses
                if (ptcpt.TelecomAddresses != null)
                    foreach (var tel in ptcpt.TelecomAddresses)
                        CreateTelecommunicationsAddress(conn, tx, tel, ptcpt.Id);
            }
            finally
            {
                cmd.Dispose();
            }
        }

        /// <summary>
        /// Create a telecomm address
        /// </summary>
        private void CreateTelecommunicationsAddress(IDbConnection conn, IDbTransaction tx, TelecommunicationsAddress tel, decimal pId)
        {
            IDbCommand cmd = DbUtil.CreateCommandStoredProc(conn, tx);
            try
            {
                cmd.CommandText = "add_hc_ptcpt_tel";

                // Parameters
                cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "ptcpt_id_in", DbType.Decimal, pId));
                cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "tel_value_in", DbType.String, tel.Value));
                cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "tel_use_in", DbType.String, tel.Use ?? "DIR"));

                // Now insert
                cmd.ExecuteNonQuery();
            }
            catch (DataException) { }
            finally
            {
                cmd.Dispose();
            }
        }

        /// <summary>
        /// Get provider by an identifier
        /// </summary>
        internal HealthcareParticipant GetProvider(System.Data.IDbConnection conn, System.Data.IDbTransaction tx, DomainIdentifier domainIdentifier)
        {
            // Get configuration service
            ISystemConfigurationService config = ApplicationContext.ConfigurationService; //ApplicationContext.Current.GetService(typeof(ISystemConfigurationService)) as ISystemConfigurationService;

            // Create database command
            IDbCommand cmd = DbUtil.CreateCommandStoredProc(conn, tx);

            try
            {
                // Fetch the client data using shrid
                if (String.IsNullOrEmpty(domainIdentifier.Domain) || domainIdentifier.Domain.Equals(config.OidRegistrar.GetOid(ClientRegistryOids.PROVIDER_CRID).Oid))
                {
                    cmd.CommandText = "get_hc_ptcpt";
                    cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "ptcpt_id_in", DbType.Decimal, Convert.ToDecimal(domainIdentifier.Identifier)));
                }
                else // get using alt id
                {
                    cmd.CommandText = "get_hc_ptcpt_extern";
                    cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "ptcpt_id_domain_in", DbType.StringFixedLength, domainIdentifier.Domain));
                    cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "ptcpt_id_in", DbType.StringFixedLength, (object)domainIdentifier.Identifier ?? DBNull.Value));
                }

                // Execute the reader
                IDataReader reader = cmd.ExecuteReader();

                // read data
                if (!reader.Read())
                    return null;

                // Parse data
                HealthcareParticipant retVal = new HealthcareParticipant();
                decimal? nsSetId = null, addrSetId = null, typeCodeId = null;
                try
                {
                    retVal.Id = Convert.ToDecimal(reader["ptcpt_id"]);
                    retVal.Classifier = Convert.ToString(reader["ptcpt_cls_cs"]).Trim().Equals("PSN") ? HealthcareParticipant.HealthcareParticipantType.Person : HealthcareParticipant.HealthcareParticipantType.Organization;
                    // Define set identifiers
                    nsSetId = reader["ptcpt_name_set_id"] == DBNull.Value ? null : (decimal?)Convert.ToDecimal(reader["ptcpt_name_set_id"]);
                    addrSetId = reader["ptcpt_addr_set_id"] == DBNull.Value ? null : (decimal?)Convert.ToDecimal(reader["ptcpt_addr_set_id"]);
                    typeCodeId = reader["ptcpt_typ_cd_id"] == DBNull.Value ? null : (decimal?)Convert.ToDecimal(reader["ptcpt_typ_cd_id"]);

                }
                finally
                {
                    // Close the reader
                    reader.Close();
                }

                // Get name set
                if (nsSetId.HasValue)
                    retVal.LegalName = DbUtil.GetName(conn, tx, nsSetId);
                // Get addr set
                if (addrSetId.HasValue)
                    retVal.PrimaryAddress = DbUtil.GetAddress(conn, tx, addrSetId);
                // Get type code
                if (typeCodeId.HasValue)
                    retVal.Type = DbUtil.GetCodedValue(conn, tx, typeCodeId);

                // Read alternate identifiers
                retVal.AlternateIdentifiers.AddRange(GetAlternateIdentifiers(conn, tx, retVal.Id));
                retVal.AlternateIdentifiers.Add(new DomainIdentifier()
                {
                    Domain = config.OidRegistrar.GetOid(ClientRegistryOids.PROVIDER_CRID).Oid,
                    Identifier = retVal.Id.ToString()
                });

                // Read telecoms
                retVal.TelecomAddresses.AddRange(GetTelecomAddresses(conn, tx, retVal.Id));

                // Return type
                return retVal;
            }
            finally
            {
                cmd.Dispose();
            }
        }

        /// <summary>
        /// Get telecommunications addresses
        /// </summary>
        private IEnumerable<DomainIdentifier> GetAlternateIdentifiers(IDbConnection conn, IDbTransaction tx, decimal identifier)
        {
            IDbCommand cmd = DbUtil.CreateCommandStoredProc(conn, tx);

            try
            {
                List<DomainIdentifier> retVal = new List<DomainIdentifier>();

                cmd.CommandText = "get_ptcpt_alt_id";
                cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "ptcpt_id_in", DbType.Decimal, identifier));
                IDataReader rdr = cmd.ExecuteReader();
                try
                {
                    while (rdr.Read())
                        retVal.Add(new DomainIdentifier()
                        {
                            Domain = Convert.ToString(rdr["alt_id_domain"]),
                            Identifier = Convert.ToString(rdr["alt_id"])
                        });
                    return retVal;
                }
                finally
                {
                    rdr.Close();
                    rdr.Dispose();
                }
            }
            finally
            {
                cmd.Dispose();
            }
        }

        /// <summary>
        /// Get telecommunications addresses
        /// </summary>
        private IEnumerable<TelecommunicationsAddress> GetTelecomAddresses(IDbConnection conn, IDbTransaction tx, decimal identifier)
        {
            IDbCommand cmd = DbUtil.CreateCommandStoredProc(conn, tx);

            try
            {
                List<TelecommunicationsAddress> retVal = new List<TelecommunicationsAddress>();

                cmd.CommandText = "get_hc_ptcpt_tel";
                cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "ptcpt_id_in", DbType.Decimal, identifier));
                IDataReader rdr = cmd.ExecuteReader();
                try
                {
                    while (rdr.Read())
                        retVal.Add(new TelecommunicationsAddress()
                        {
                            Value = Convert.ToString(rdr["tel_value"]),
                            Use = Convert.ToString(rdr["tel_use"])
                        });
                    return retVal;
                }
                finally
                {
                    rdr.Close();
                    rdr.Dispose();
                }
            }
            finally
            {
                cmd.Dispose();
            }
        }


        /// <summary>
        /// De-persist
        /// </summary>
        public System.ComponentModel.IComponent DePersist(System.Data.IDbConnection conn, decimal identifier, IContainer container, HealthServiceRecordSiteRoleType? roleType, bool loadFast)
        {
            HealthcareParticipant retVal = new HealthcareParticipant();

            IDbCommand cmd = DbUtil.CreateCommandStoredProc(conn, null);
            try
            {

                // Determine the container
                RegistrationEvent hsrParent = container as RegistrationEvent;

                retVal = GetProvider(conn, null, new DomainIdentifier() { Identifier = identifier.ToString() });

                // Data reader
                if (hsrParent != null)
                {
                    cmd.CommandText = "get_hsr_ptcpt";
                    cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "hsr_id_in", DbType.Decimal, hsrParent.Id));
                    //cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "vrsn_id_in", DbType.Decimal, hsrParent.VersionIdentifier));
                    cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "ptcpt_id_in", DbType.Decimal, identifier));
                    cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "ptcpt_cls_in", DbType.Decimal, (decimal)roleType.Value));

                    // Role
                    Decimal repOrgId = default(decimal);
                    List<DomainIdentifier> originalIds = new List<DomainIdentifier>();

                    // Execute a reader
                    IDataReader reader = cmd.ExecuteReader();
                    try
                    {
                        // Read a row
                        while (reader.Read())
                        {
                            repOrgId = reader["ptcpt_rep_org_id"] == DBNull.Value ? default(decimal) : Convert.ToDecimal(reader["ptcpt_rep_org_id"]);
                            originalIds.Add(new DomainIdentifier()
                            {
                                Domain = Convert.ToString(reader["orig_id_domain"]),
                                Identifier = Convert.ToString(reader["orig_id"]),
                                IsLicenseAuthority = Convert.ToBoolean(reader["license_ind"])
                            });
                        }
                    }
                    finally
                    {
                        reader.Close();
                    }

                    // Representative of
                    if (repOrgId != default(decimal))
                        retVal.Add(GetProvider(conn, null, new DomainIdentifier() { Identifier = repOrgId.ToString() }), "REPOF", HealthServiceRecordSiteRoleType.RepresentitiveOf, null);

                    // Add to parent
                    hsrParent.Add(retVal, Guid.NewGuid().ToString(), roleType.Value, originalIds);
                }
                else
                    container.Add(retVal);

            }
            finally
            {
                cmd.Dispose();
            }

            // TODO: Get original identifiers for this link

            return retVal;
        }

        #endregion

    }
}
