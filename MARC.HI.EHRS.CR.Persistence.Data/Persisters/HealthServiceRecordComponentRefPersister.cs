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
using System.ComponentModel;
using System.Data;
using MARC.HI.EHRS.CR.Core.ComponentModel;
using MARC.HI.EHRS.SVC.Core.ComponentModel;
using MARC.HI.EHRS.SVC.Core.ComponentModel.Components;
using MARC.HI.EHRS.SVC.Core.Services;

namespace MARC.HI.EHRS.CR.Persistence.Data.ComponentPersister
{
    /// <summary>
    /// Health service record component persister
    /// </summary>
    internal class HealthServiceRecordComponentRefPersister : IComponentPersister
    {
        #region IComponentPersister Members


        /// <summary>
        /// Gets the type of compoinent this item handles
        /// </summary>
        public Type HandlesComponent
        {
            get { return typeof(HealthServiceRecordComponentRef); }
        }

        /// <summary>
        /// Persists the item
        /// </summary>
        public MARC.HI.EHRS.SVC.Core.DataTypes.VersionedDomainIdentifier Persist(System.Data.IDbConnection conn, System.Data.IDbTransaction tx, System.ComponentModel.IComponent data, bool isUpdate)
        {
            ISystemConfigurationService configSvc = ApplicationContext.ConfigurationService; //ApplicationContext.Current.GetService(typeof(ISystemConfigurationService)) as ISystemConfigurationService;

            // In order to persist we first must get the id of the hsr
            var componentRef = data as HealthServiceRecordComponentRef;
            if (componentRef.AlternateIdentifier.Domain.Equals(configSvc.OidRegistrar.GetOid(ClientRegistryOids.EVENT_OID).Oid))
                componentRef.Id = Decimal.Parse(componentRef.AlternateIdentifier.Identifier);
            else
                throw new ConstraintException("Referenced record cannot be found in this repository");

            // Link the records if the record is a hard link
            if(!(componentRef.Site as HealthServiceRecordSite).IsSymbolic)
                LinkHSRRecord(conn, tx, componentRef);

            return new MARC.HI.EHRS.SVC.Core.DataTypes.VersionedDomainIdentifier()
            {
                Domain = ClientRegistryOids.EVENT_OID,
                Identifier = componentRef.Id.ToString()
            };
        }

        /// <summary>
        /// Link this HSR record to another
        /// </summary>
        private void LinkHSRRecord(IDbConnection conn, IDbTransaction tx, HealthServiceRecordComponentRef hsr)
        {

            // TODO: Check to ensure that we don't double replace or double succeed a record
            // An HSR can only be linked to another HSR, so ... 
            // first we need to find the HSR container to link to
            IContainer hsrContainer = hsr.Site.Container;
            while (!(hsrContainer is RegistrationEvent) && hsrContainer != null)
                hsrContainer = (hsrContainer as IComponent).Site.Container;
            RegistrationEvent parentHsr = hsrContainer as RegistrationEvent;

            // Get the root container
            HealthServiceRecordContainer parentContainer = hsr.Site.Container as HealthServiceRecordContainer;
            while (parentContainer.Site != null)
                parentContainer = parentContainer.Site.Container as HealthServiceRecordContainer;

            // Now we want to link
            if (parentHsr == null)
                throw new InvalidOperationException("Can only link a Health Service Record event to a Registration Event");
            else if (parentContainer.Id.Equals(hsr.Id))
                throw new InvalidOperationException("Can't link a record to itself");

            // Insert link
            IDbCommand cmd = DbUtil.CreateCommandStoredProc(conn, tx);
            try
            {
                cmd.CommandText = "add_hsr_lnk";
                cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "cmp_hsr_id_in", DbType.Decimal, hsr.Id));
                cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "cbc_hsr_id_in", DbType.Decimal, parentHsr.Id));
                cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "lnk_cls_in", DbType.Decimal, (decimal)(hsr.Site as HealthServiceRecordSite).SiteRoleType));
                cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "conduction_in", DbType.Boolean, (hsr.Site as HealthServiceRecordSite).ContextConduction));
                cmd.ExecuteNonQuery();

            }
            catch (Exception)
            {
                throw new ConstraintException(String.Format("Cannot locate referenced record for '{0}' relationship", (hsr.Site as HealthServiceRecordSite).SiteRoleType));
            }
            finally
            {
                cmd.Dispose();
            }
        }

        /// <summary>
        /// De-Persist the item
        /// </summary>
        public System.ComponentModel.IComponent DePersist(System.Data.IDbConnection conn, decimal identifier, System.ComponentModel.IContainer container, MARC.HI.EHRS.SVC.Core.ComponentModel.HealthServiceRecordSiteRoleType? role, bool loadFast)
        {
            // Configuration service
            ISystemConfigurationService configService = ApplicationContext.ConfigurationService; //ApplicationContext.Current.GetService(typeof(ISystemConfigurationService)) as ISystemConfigurationService;

            // HSR Persister
            RegistrationEventPersister hsrp = new RegistrationEventPersister();

            // JF: This fixes infinitely reading references
            IComponent parentCont = (IComponent)container;
            while (parentCont.Site != null)
            {
                // Are we de-persisting something that is already in the tree of the container

                if ((parentCont as HealthServiceRecordContainer).Id.Equals(identifier) &&
                    parentCont.GetType().Equals(typeof(RegistrationEvent)))
                    return null;
                parentCont = (IComponent)parentCont.Site.Container;
            }

            // Get the id of the item to de-persist
            if(!loadFast)
                return hsrp.DePersist(conn, identifier, container, role, loadFast);
            else
            {
                var retVal = new HealthServiceRecordComponentRef()
                {
                    AlternateIdentifier = new MARC.HI.EHRS.SVC.Core.DataTypes.DomainIdentifier() {
                        Domain = configService.OidRegistrar.GetOid(ClientRegistryOids.REGISTRATION_EVENT).Oid,
                        Identifier = identifier.ToString()
                    },
                    Id = identifier
                };
                (container as HealthServiceRecordContainer).Add(retVal, "CMP", role.Value, null);
                return retVal;
            }

            
        }

        #endregion

    }
}
