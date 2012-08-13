using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MARC.HI.EHRS.CR.Core.ComponentModel;
using MARC.HI.EHRS.SVC.Core.ComponentModel;
using System.Data;
using System.Reflection;
using MARC.HI.EHRS.CR.Core.Services;

namespace MARC.HI.EHRS.CR.Persistence.Data.ComponentPersister
{
    /// <summary>
    /// Person registration reference persister
    /// </summary>
    /// <remarks>
    /// This is a special persister in that it updates other records
    /// </remarks>
    public class PersonRegistrationRefPersister : IComponentPersister
    {
        #region IComponentPersister Members

        /// <summary>
        /// A person registration reference persister
        /// </summary>
        public Type HandlesComponent
        {
            get { return typeof(PersonRegistrationRef); }
        }

        /// <summary>
        /// Persist the person relationship
        /// </summary>
        public SVC.Core.DataTypes.VersionedDomainIdentifier Persist(System.Data.IDbConnection conn, System.Data.IDbTransaction tx, System.ComponentModel.IComponent data, bool isUpdate)
        {
            // Is this a replacement
            var pp = new PersonPersister();
            PersonRegistrationRef refr = data as PersonRegistrationRef;
            Person psn = pp.GetPerson(conn, tx, refr.AlternateIdentifiers[0], true);
            Person cntrPsn = refr.Site.Container as Person;

            if (psn == null || cntrPsn == null)
                throw new ConstraintException(ApplicationContext.LocaleService.GetString("DBCF00B"));

            var role = (refr.Site as HealthServiceRecordSite).SiteRoleType;
            var symbolic = (refr.Site as HealthServiceRecordSite).IsSymbolic; // If true, the replacement does not cascade and is a symbolic replacement of only the identifiers listed

            // Replacement?
            if (role == HealthServiceRecordSiteRoleType.ReplacementOf)
            {
                // First, we obsolete all records with the existing person
                foreach (var id in psn.AlternateIdentifiers.FindAll(o => refr.AlternateIdentifiers.Exists(a => a.Domain == o.Domain)))
                    id.UpdateMode = SVC.Core.DataTypes.UpdateModeType.Remove;
                psn.AlternateIdentifiers.RemoveAll(o => o.UpdateMode != SVC.Core.DataTypes.UpdateModeType.Remove);

                // Not symbolic, means that we do a hard replace
                if(!symbolic)
                {
                    // TODO: Support other obsoletion methods
                    // Remove the old person from the db
                    psn.Status = SVC.Core.ComponentModel.Components.StatusType.Obsolete; // obsolete the person
                    psn.Addresses= null;
                    psn.Citizenship= null;
                    psn.Employment= null;
                    psn.Language= null;
                    psn.Names= null;
                    psn.OtherIdentifiers= null;
                    psn.Race= null;
                    psn.TelecomAddresses= null;
                    psn.BirthTime = null;
                    psn.DeceasedTime = null;
                }

                // Now update the person
                psn.Site = refr.Site;
                pp.Persist(conn, tx, psn, true); // update the person record
                
            }

            // Create the link
            using (var cmd = DbUtil.CreateCommandStoredProc(conn, tx))
            {
                cmd.CommandText = "crt_psn_lnk";
                cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "psn_id_in", DbType.Decimal, cntrPsn.Id));
                cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "psn_vrsn_id_in", DbType.Decimal, cntrPsn.VersionId));
                cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "lnk_psn_id_in", DbType.Decimal, psn.Id));
                cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "lnk_cls_in", DbType.Decimal, (decimal)role));
                cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "symbolic_in", DbType.Boolean, symbolic));
                cmd.ExecuteNonQuery();
            }
            
            // Send notification that duplicates were resolved
            if (symbolic)
            {
                // Send an duplicates resolved message
                IClientNotificationService notificationService = ApplicationContext.CurrentContext.GetService(typeof(IClientNotificationService)) as IClientNotificationService;
                if (notificationService != null)
                    notificationService.NotifyDuplicatesResolved(cntrPsn, refr.AlternateIdentifiers[0]);
            }

            // Person identifier
            return new SVC.Core.DataTypes.VersionedDomainIdentifier()
            {
                Identifier = psn.Id.ToString(),
                Version = psn.VersionId.ToString()
            };
        }

        /// <summary>
        /// De-persist the component
        /// </summary>
        public System.ComponentModel.IComponent DePersist(System.Data.IDbConnection conn, decimal identifier, System.ComponentModel.IContainer container, SVC.Core.ComponentModel.HealthServiceRecordSiteRoleType? role, bool loadFast)
        {
            return new PersonPersister().DePersist(conn, identifier, container, role, loadFast); // Simply return the de-persisted person
        }

        #endregion
    }
}
