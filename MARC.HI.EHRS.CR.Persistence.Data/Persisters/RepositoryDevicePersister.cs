/**
 * Copyright 2012-2013 Mohawk College of Applied Arts and Technology
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
 * User: fyfej
 * Date: 7-8-2012
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MARC.HI.EHRS.CR.Core.ComponentModel;
using System.Data;

namespace MARC.HI.EHRS.CR.Persistence.Data.ComponentPersister
{
    /// <summary>
    /// Repository device persister
    /// </summary>
    public class RepositoryDevicePersister : IComponentPersister
    {
        #region IComponentPersister Members

        /// <summary>
        /// Gets the type of component that this persister handles
        /// </summary>
        public Type HandlesComponent
        {
            get { return typeof(RepositoryDevice); }
        }

        /// <summary>
        /// Persists the component
        /// </summary>
        public SVC.Core.DataTypes.VersionedDomainIdentifier Persist(System.Data.IDbConnection conn, System.Data.IDbTransaction tx, System.ComponentModel.IComponent data, bool isUpdate)
        {
            RepositoryDevice device = data as RepositoryDevice;
            using (IDbCommand cmd = DbUtil.CreateCommandStoredProc(conn, tx))
            {
                if (device.AlternateIdentifier == null ||
                    String.IsNullOrEmpty(device.AlternateIdentifier.Domain))
                    throw new ConstraintException(ApplicationContext.LocaleService.GetString("DTPE009"));
                
                // create parmaeters
                cmd.CommandText = "crt_dev";
                cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "dev_root_in", DbType.String, device.AlternateIdentifier.Domain));
                cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "dev_ext_in", DbType.String, String.IsNullOrEmpty(device.AlternateIdentifier.Identifier) ? DBNull.Value : (object)device.AlternateIdentifier.Identifier));
                cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "dev_name_in", DbType.String, device.Name));
                cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "dev_jur_in", DbType.String, device.Jurisdiction));

                // Versioned domain identifier
                return new SVC.Core.DataTypes.VersionedDomainIdentifier()
                {
                    Identifier = Convert.ToString(cmd.ExecuteScalar()),
                    Domain = ApplicationContext.ConfigurationService.OidRegistrar.GetOid(ClientRegistryOids.DEVICE_CRID).Oid
                };
            }
        }

        /// <summary>
        /// De-persist the portion
        /// </summary>
        public System.ComponentModel.IComponent DePersist(System.Data.IDbConnection conn, decimal identifier, System.ComponentModel.IContainer container, SVC.Core.ComponentModel.HealthServiceRecordSiteRoleType? role, bool loadFast)
        {
            using (IDbCommand cmd = DbUtil.CreateCommandStoredProc(conn, null))
            {
                cmd.CommandText = "get_dev";
                cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "dev_id_in", DbType.Decimal, identifier));

                // Execute reader
                using (IDataReader rdr = cmd.ExecuteReader())
                    if (rdr.Read())
                        return new RepositoryDevice()
                        {
                            AlternateIdentifier = new SVC.Core.DataTypes.DomainIdentifier()
                            {
                                Identifier = Convert.ToString(rdr["dev_ext"]),
                                Domain = Convert.ToString(rdr["dev_root"])
                            },
                            Jurisdiction = rdr["dev_jur"] != DBNull.Value ? Convert.ToString(rdr["dev_jur"]) : null,
                            Name = rdr["dev_name"] != DBNull.Value ? Convert.ToString(rdr["dev_name"]) : null
                        };
            }
            return null;
        }

        #endregion
    }
}
