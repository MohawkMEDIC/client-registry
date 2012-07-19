using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MARC.HI.EHRS.CR.Core.ComponentModel;

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
            return new SVC.Core.DataTypes.VersionedDomainIdentifier()
            {
                Identifier = "3"
            };
        }

        /// <summary>
        /// De-persist the portion
        /// </summary>
        public System.ComponentModel.IComponent DePersist(System.Data.IDbConnection conn, decimal identifier, System.ComponentModel.IContainer container, SVC.Core.ComponentModel.HealthServiceRecordSiteRoleType? role, bool loadFast)
        {
            return null;
        }

        #endregion
    }
}
