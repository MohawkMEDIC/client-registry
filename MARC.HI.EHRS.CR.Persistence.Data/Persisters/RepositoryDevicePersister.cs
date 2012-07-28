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
                    String.IsNullOrEmpty(device.AlternateIdentifier.Domain) ||
                    String.IsNullOrEmpty(device.AlternateIdentifier.Identifier))
                    throw new ConstraintException(ApplicationContext.LocaleService.GetString("DTPE009"));
                
                // create parmaeters
                cmd.CommandText = "crt_dev";
                cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "dev_root_in", DbType.String, device.AlternateIdentifier.Domain));
                cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "dev_ext_in", DbType.String, device.AlternateIdentifier.Identifier));
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
