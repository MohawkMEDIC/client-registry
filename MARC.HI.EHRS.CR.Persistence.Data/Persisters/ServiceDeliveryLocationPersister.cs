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
using System.Linq;
using System.Text;
using MARC.HI.EHRS.SVC.Core.DataTypes;
using MARC.HI.EHRS.SVC.Core.Services;
using System.Data;
using System.ComponentModel;
using MARC.HI.EHRS.SVC.Core;
using MARC.HI.EHRS.SVC.Core.Exceptions;
using MARC.HI.EHRS.SVC.Core.ComponentModel;
using MARC.HI.EHRS.SVC.Core.ComponentModel.Components;
using MARC.HI.EHRS.CR.Core.ComponentModel;

namespace MARC.HI.EHRS.CR.Persistence.Data.ComponentPersister
{
    public class ServiceDeliveryLocationPersister : IComponentPersister
    {
        #region IComponentPersister Members


        /// <summary>
        /// Gets the component type that this persister handles
        /// </summary>
        public Type HandlesComponent { get { return typeof(ServiceDeliveryLocation); } }

        /// <summary>
        /// Persist the specified component to the database
        /// </summary>
        public MARC.HI.EHRS.SVC.Core.DataTypes.VersionedDomainIdentifier Persist(System.Data.IDbConnection conn, System.Data.IDbTransaction tx, System.ComponentModel.IComponent data, bool isUpdate)
        {

            ISystemConfigurationService configServce = ApplicationContext.ConfigurationService; //ApplicationContext.Current.GetService(typeof(ISystemConfigurationService)) as ISystemConfigurationService;

            // First, we must determine if we're going to be performing an update or a put
            ServiceDeliveryLocation loc = data as ServiceDeliveryLocation;

            // Do we have the shrid?
            if (loc.Id == default(decimal) && loc.AlternateIdentifiers.Count > 0) // nope
            {
                // Attempt to get the SHRID
                int iId = 0;
                ServiceDeliveryLocation resLoc = null;
                while (resLoc == null && iId < loc.AlternateIdentifiers.Count)
                {
                    resLoc = GetLocation(conn, tx, loc.AlternateIdentifiers[iId]);
                    iId++;
                }

                if (resLoc == null) // We need to create a client
                    CreateLocation(conn, tx, loc);
                else
                {
                    // Validate the name given matches the legal name. Has to be more than
                    // 80% match
                    if ((loc.Name == null) ^ (resLoc.Name == null) || loc.Name != null && resLoc.Name != null && !loc.Name.ToLower().Equals(resLoc.Name.ToLower()))
                        throw new DataException("The provided name does not match the name of location in data store");

                    loc.Id = resLoc.Id;

                    bool nUpdate = resLoc.LocationType != null && loc.LocationType != null && resLoc.LocationType.Code != loc.LocationType.Code ||
                        QueryUtil.MatchAddress(resLoc.Address, loc.Address) != 1.0f;

                    loc.Name = resLoc.Name ?? loc.Name;
                    loc.Address = resLoc.Address ?? loc.Address;
                    loc.LocationType = resLoc.LocationType ?? loc.LocationType;

                    // Register alternative identifiers
                    foreach (var id in loc.AlternateIdentifiers)
                        if (resLoc.AlternateIdentifiers.Count(o => o.Domain == id.Domain) == 0) // register
                            CreateAlternateIdentifier(conn, tx, loc.Id, id);
                    if(nUpdate) UpdateLocation(conn, tx, loc);
                }
            }

            // Persist the site with the container if known
            if (loc.Site.Container is RegistrationEvent)
                LinkHealthServiceRecord(conn, tx, (loc.Site.Container as RegistrationEvent).Id, loc.Site as HealthServiceRecordSite);

            // Return the versioned identifier
            return new VersionedDomainIdentifier()
            {
                Domain = configServce.OidRegistrar.GetOid(ClientRegistryOids.LOCATION_CRID).Oid,
                Identifier = loc.Id.ToString()
            };
        }

        /// <summary>
        /// Update location
        /// </summary>
        private void UpdateLocation(IDbConnection conn, IDbTransaction tx, ServiceDeliveryLocation loc)
        {
            IDbCommand cmd = DbUtil.CreateCommandStoredProc(conn, tx);
            try
            {
                cmd.CommandText = "upd_sdl";

                // parameters
                cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "sdl_id_in", DbType.Decimal, loc.Id));
                cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "sdl_addr_set_id_in", DbType.Decimal, loc.Address == null ? DBNull.Value : (Object)DbUtil.CreateAddressSet(conn, tx, loc.Address)));
                cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "sdl_typ_cd_id_in", DbType.Decimal, loc.LocationType == null ? DBNull.Value : (Object)DbUtil.CreateCodedValue(conn, tx, loc.LocationType)));

                // Execute
                cmd.ExecuteNonQuery();
            }
            finally
            {
                cmd.Dispose();
            }
        }

        /// <summary>
        /// Link a service delivery location record to an HSR
        /// </summary>
        private void LinkHealthServiceRecord(IDbConnection conn, IDbTransaction tx, decimal hsrId, HealthServiceRecordSite loc)
        {
            IDbCommand cmd = DbUtil.CreateCommandStoredProc(conn, tx);
            try
            {
                cmd.CommandText = "link_sdl";

                // Parameters
                cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "hsr_id_in", DbType.Decimal, hsrId));
                cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "sdl_id_in", DbType.Decimal, (loc.Component as ServiceDeliveryLocation).Id));
                cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "sdl_cls_in", DbType.Decimal, (decimal)loc.SiteRoleType));

                // Insert
                cmd.ExecuteNonQuery();
            }
            finally
            {
                cmd.Dispose();
            }
        }

        /// <summary>
        /// Create an alternate identifier for the SDL
        /// </summary>
        private void CreateAlternateIdentifier(IDbConnection conn, IDbTransaction tx, decimal identifier, DomainIdentifier altId)
        {
            IDbCommand cmd = DbUtil.CreateCommandStoredProc(conn, tx);

            try
            {
                cmd.CommandText = "crt_sdl_alt_id";

                // parameters
                cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "sdl_id_in", DbType.Decimal, identifier));
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
        /// Create a location 
        /// </summary>
        private void CreateLocation(System.Data.IDbConnection conn, System.Data.IDbTransaction tx, ServiceDeliveryLocation loc)
        {
            IDbCommand cmd = DbUtil.CreateCommandStoredProc(conn, tx);
            try
            {
                cmd.CommandText = "crt_sdl";

                // Insert the code
                decimal? codeId = loc.LocationType != null ? (decimal?)DbUtil.CreateCodedValue(conn, tx, loc.LocationType) : null,
                    addrSetId = loc.Address != null ? (decimal?)DbUtil.CreateAddressSet(conn, tx, loc.Address) : null;

                // parameters
                cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "sdl_name_in", DbType.StringFixedLength, (object)loc.Name ?? DBNull.Value));
                cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "sdl_addr_set_id_in", DbType.Decimal, (object)addrSetId ?? DBNull.Value));
                cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "sdl_typ_cd_id_in", DbType.Decimal, (object)codeId ?? DBNull.Value));

                // Execute the parameter
                loc.Id = Convert.ToDecimal(cmd.ExecuteScalar());

                // Register an alternate identifier if they exist
                foreach (var id in loc.AlternateIdentifiers)
                    CreateAlternateIdentifier(conn, tx, loc.Id, id);

            }
            finally
            {
                cmd.Dispose();
            }
        }

        /// <summary>
        /// Get location data from the database
        /// </summary>
        /// <param name="conn"></param>
        /// <param name="tx"></param>
        /// <param name="domainIdentifier"></param>
        /// <returns></returns>
        private ServiceDeliveryLocation GetLocation(System.Data.IDbConnection conn, System.Data.IDbTransaction tx, DomainIdentifier domainIdentifier)
        {
            // Get configuration service
            ISystemConfigurationService config = ApplicationContext.ConfigurationService; //ApplicationContext.Current.GetService(typeof(ISystemConfigurationService)) as ISystemConfigurationService;

            // Create database command
            IDbCommand cmd = DbUtil.CreateCommandStoredProc(conn, tx);

            try
            {
                // Fetch the client data using shrid
                if (String.IsNullOrEmpty(domainIdentifier.Domain) || domainIdentifier.Domain.Equals(config.OidRegistrar.GetOid(ClientRegistryOids.LOCATION_CRID).Oid))
                {
                    cmd.CommandText = "get_sdl";
                    cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "sdl_id_in", DbType.Decimal, Convert.ToDecimal(domainIdentifier.Identifier)));
                }
                else // get using alt id
                {
                    cmd.CommandText = "get_sdl_extern";
                    cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "sdl_id_domain_in", DbType.StringFixedLength, domainIdentifier.Domain));
                    cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "sdl_id_in", DbType.StringFixedLength, (object)domainIdentifier.Identifier ?? DBNull.Value));
                }

                // Execute the reader
                IDataReader reader = cmd.ExecuteReader();

                // read data
                if (!reader.Read())
                    return null;

                // Parse data
                ServiceDeliveryLocation retVal = new ServiceDeliveryLocation();
                decimal? codeTypeId = null, addrSetId = null;
                try
                {
                    retVal.Id = Convert.ToDecimal(reader["sdl_id"]);

                    // Define set identifiers
                    retVal.Name = reader["sdl_name"] == DBNull.Value ? null : Convert.ToString(reader["sdl_name"]);
                    addrSetId = reader["sdl_addr_set_id"] == DBNull.Value ? null : (decimal?)Convert.ToDecimal(reader["sdl_addr_set_id"]);
                    codeTypeId = reader["sdl_typ_cd_id"] == DBNull.Value ? null : (decimal?)Convert.ToDecimal(reader["sdl_typ_cd_id"]);
                }
                finally
                {
                    // Close the reader
                    reader.Close();
                }

                // Get addr set
                if (addrSetId.HasValue)
                    retVal.Address = DbUtil.GetAddress(conn, tx, addrSetId);
                // Get type code
                if (codeTypeId.HasValue)
                    retVal.LocationType = DbUtil.GetCodedValue(conn, tx, codeTypeId);

                // Read alternate identifiers
                retVal.AlternateIdentifiers.AddRange(GetAlternateIdentifiers(conn, tx, retVal.Id));

                // Return type
               
                return retVal;
            }
            finally
            {
                cmd.Dispose();
            }
        }

        /// <summary>
        /// Get alternative identifiers for a location
        /// </summary>
        private IEnumerable<DomainIdentifier> GetAlternateIdentifiers(IDbConnection conn, IDbTransaction tx, decimal sdlId)
        {
            IDbCommand cmd = DbUtil.CreateCommandStoredProc(conn, tx);
            try
            {
                cmd.CommandText = "get_sdl_alt_id";
                cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "sdl_id_in", DbType.Decimal, sdlId));

                List<DomainIdentifier> retVal = new List<DomainIdentifier>();

                // Execute a reader
                IDataReader reader = cmd.ExecuteReader();
                try
                {
                    while (reader.Read()) 
                        retVal.Add(new DomainIdentifier() {
                            Domain = Convert.ToString(reader["alt_id_domain"]),
                            Identifier = reader["alt_id"] == DBNull.Value ? null : Convert.ToString(reader["alt_id"])
                        });
                }
                finally
                {
                    reader.Close();
                    reader.Dispose();
                }

                return retVal;
            }
            finally
            {
                cmd.Dispose();
            }
        }

        /// <summary>
        /// De-persist the component from the database
        /// </summary>
        public System.ComponentModel.IComponent DePersist(System.Data.IDbConnection conn, decimal identifier, IContainer container, HealthServiceRecordSiteRoleType? roleType, bool loadFast)
        {
            ServiceDeliveryLocation retVal = new ServiceDeliveryLocation();

            IDbCommand cmd = DbUtil.CreateCommandStoredProc(conn, null);
            try
            {

                // Determine the container
                RegistrationEvent hsrParent = container as RegistrationEvent;

                retVal = GetLocation(conn, null, new DomainIdentifier() { Identifier = identifier.ToString() });

                // Data reader
                if (hsrParent != null)
                    hsrParent.Add(retVal, Guid.NewGuid().ToString(), roleType.Value, retVal.AlternateIdentifiers);
                else
                    container.Add(retVal);

            }
            finally
            {
                cmd.Dispose();
            }

            return retVal;
        }

        #endregion

    }
}
