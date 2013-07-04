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
 * Date: 23-7-2012
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
    /// Extended attribute persister
    /// </summary>
    public class ExtendedAttributePersister : IComponentPersister
    {
        #region IComponentPersister Members

        /// <summary>
        /// Gets the type of component
        /// </summary>
        public Type HandlesComponent
        {
            get { return typeof(ExtendedAttribute); }
        }

        /// <summary>
        /// Persist the data
        /// </summary>
        public SVC.Core.DataTypes.VersionedDomainIdentifier Persist(System.Data.IDbConnection conn, System.Data.IDbTransaction tx, System.ComponentModel.IComponent data, bool isUpdate)
        {
            ExtendedAttribute instance = data as ExtendedAttribute;

            using (IDbCommand cmd = DbUtil.CreateCommandStoredProc(conn, tx))
            { 
                // persist
                cmd.CommandText = "crt_ext";

                // parameters
                cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "ext_rep_in", DbType.String, "BIN"));
                cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "ext_typ_in", DbType.String, instance.Value.GetType().AssemblyQualifiedName));
                cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "ext_data_in", DbType.Binary, instance.ValueData));
                cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "ext_name_in", DbType.String, instance.Name));
                cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "ext_path_in", DbType.String, instance.PropertyPath));

                // Create the response
                instance.Id = Convert.ToDecimal(cmd.ExecuteScalar());

            }

            // Return constructed version pointer
            return new SVC.Core.DataTypes.VersionedDomainIdentifier()
            {
                Identifier = instance.Id.ToString(),
                Domain = ApplicationContext.ConfigurationService.OidRegistrar.GetOid(ClientRegistryOids.EVENT_OID).Oid
            };
        }

        /// <summary>
        /// Depersist the 
        /// </summary>
        public System.ComponentModel.IComponent DePersist(System.Data.IDbConnection conn, decimal identifier, System.ComponentModel.IContainer container, SVC.Core.ComponentModel.HealthServiceRecordSiteRoleType? role, bool loadFast)
        {

            // De-persist
            using (IDbCommand cmd = DbUtil.CreateCommandStoredProc(conn, null))
            {
                cmd.CommandText = "get_ext";
                cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "ext_id_in", DbType.Decimal, identifier));

                using(IDataReader rdr = cmd.ExecuteReader())
                    if (rdr.Read())
                    {
                        ExtendedAttribute ext = new ExtendedAttribute()
                        {
                            Id = Convert.ToDecimal(rdr["ext_id"]),
                            Name = Convert.ToString(rdr["ext_name"]),
                            PropertyPath = Convert.ToString(rdr["ext_path"]),
                            ValueData = (byte[])rdr["ext_data"]
                        };
                          

                        // Sanity check
                        if (ext.Value.GetType().AssemblyQualifiedName != Convert.ToString(rdr["ext_typ"]))
                            throw new ConstraintException();

                        return ext;
                    }
            }
            return null;

        }

        #endregion
    }
}
