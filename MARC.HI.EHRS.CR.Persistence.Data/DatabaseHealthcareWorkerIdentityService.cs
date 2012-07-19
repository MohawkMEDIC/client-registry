using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MARC.HI.EHRS.SVC.HealthWorkerIdentity;
using MARC.HI.EHRS.CR.Persistence.Data.ComponentPersister;
using System.Data;

namespace MARC.HI.EHRS.CR.Persistence.Data
{
    
    /// <summary>
    /// Just a quick implementation of a healthcare worker identity service that reads from the SHR database.
    /// </summary>
    public class DatabaseHealthcareWorkerIdentityService : IHealthcareWorkerIdentityService
    {
        #region IHealthcareWorkerIdentityService Members

        /// <summary>
        /// Find a participant based on an external identifier
        /// </summary>
        /// <param name="identifier"></param>
        /// <returns></returns>
        public SVC.Core.ComponentModel.Components.HealthcareParticipant FindParticipant(SVC.Core.DataTypes.DomainIdentifier identifier)
        {
            HealthcareParticipantPersister persister = new HealthcareParticipantPersister();
            // HACK: I norder to work around Client Registry Hack
            ApplicationContext.CurrentContext = Context;
            // First we want to find the appropriate helper
            IDbConnection conn = DatabasePersistenceService.ReadOnlyConnectionManager.GetConnection();
            try
            {
                return persister.GetProvider(conn, null, identifier);
            }
            finally
            {
                DatabasePersistenceService.ConnectionManager.ReleaseConnection(conn);
            }
        }

        #endregion

        #region IUsesHostContext Members

        /// <summary>
        /// Gets or sets the context within which this service operates
        /// </summary>
        public SVC.Core.HostContext Context { get; set;  }
        #endregion
    }
}
