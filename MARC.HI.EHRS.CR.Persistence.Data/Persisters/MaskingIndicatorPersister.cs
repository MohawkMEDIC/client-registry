/**
 * Copyright 2012-2012 Mohawk College of Applied Arts and Technology
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
 * Date: 19-7-2012
 */

using System;
using System.Data;
using MARC.HI.EHRS.SVC.Core.ComponentModel;
using MARC.HI.EHRS.SVC.Core.ComponentModel.Components;
using MARC.HI.EHRS.SVC.Core.Services;
using MARC.HI.EHRS.CR.Core.ComponentModel;

namespace MARC.HI.EHRS.CR.Persistence.Data.ComponentPersister
{
    /// <summary>
    /// Persister for masking indicators
    /// </summary>
    internal class MaskingIndicatorPersister : IComponentPersister
    {
        #region IComponentPersister Members


        /// <summary>
        /// Gets the component that this persister handles
        /// </summary>
        public Type HandlesComponent
        {
            get { return typeof(MaskingIndicator);  }
        }

        /// <summary>
        /// Persist the MI
        /// </summary>
        public MARC.HI.EHRS.SVC.Core.DataTypes.VersionedDomainIdentifier Persist(System.Data.IDbConnection conn, System.Data.IDbTransaction tx, System.ComponentModel.IComponent data, bool isUpdate)
        {
            // Masking indicator
            MaskingIndicator mi = data as MaskingIndicator;

            // Health service record event identifier that this MI applies to
            Person psnParent = mi.Site.Container as Person;

            // Is there an HSR parent that this masking indicator applies to?
            if (psnParent == null)
                throw new ConstraintException(ApplicationContext.LocaleService.GetString("DBCF002"));

            // Now to create the comment
            IDbCommand cmd = DbUtil.CreateCommandStoredProc(conn, tx);
            try
            {
                // Call the create masking indicator function in DB
                cmd.CommandText = "crt_psn_msk_ind";
                cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "psn_id_in", DbType.Decimal, psnParent.Id));
                cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "psn_vrsn_id_in", DbType.Decimal, psnParent.VersionId));
                cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "msk_cs_in", DbType.String, mi.MaskingCode.Code));
                return new MARC.HI.EHRS.SVC.Core.DataTypes.VersionedDomainIdentifier()
                {
                    Identifier = cmd.ExecuteScalar().ToString()
                };
            }
            finally
            {
                cmd.Dispose();
            }
        }

        /// <summary>
        /// De-persist the MI
        /// </summary>
        public System.ComponentModel.IComponent DePersist(System.Data.IDbConnection conn, decimal identifier, System.ComponentModel.IContainer container, HealthServiceRecordSiteRoleType? roleType, bool loadFast)
        {
            // De-persist the masking record
            ISystemConfigurationService sysConfig = ApplicationContext.ConfigurationService;

            // Load the observation event
            MaskingIndicator retVal = new MaskingIndicator();
            using (IDbCommand cmd = DbUtil.CreateCommandStoredProc(conn, null))
            {
                cmd.CommandText = "get_psn_msk_ind";
                cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "msk_id_in", DbType.Decimal, identifier));

                // Execute the reader
                using (IDataReader rdr = cmd.ExecuteReader())
                {
                    if (rdr.Read())
                        retVal.MaskingCode = new MARC.HI.EHRS.SVC.Core.DataTypes.CodeValue(Convert.ToString(rdr["msk_cs"]));
                }

                // Append to the container
                if (container is Person)
                    (container as Person).Add(retVal, Guid.NewGuid().ToString(), MARC.HI.EHRS.SVC.Core.ComponentModel.HealthServiceRecordSiteRoleType.FilterOf, null);

            }

            return retVal;
        }

        #endregion

    }
}
