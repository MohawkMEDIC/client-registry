using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MARC.HI.EHRS.SVC.Core.Services;
using NHapi.Base.Util;
using MARC.HI.EHRS.CR.Core.ComponentModel;

namespace MARC.HI.EHRS.CR.Messaging.PixPdqv2
{
    /// <summary>
    /// Decomponentization utility
    /// </summary>
    public class DeComponentUtility : IUsesHostContext
    {
        
        #region IUsesHostContext Members

        // Host context
        private MARC.HI.EHRS.SVC.Core.HostContext m_context;

        // Localization service
        private ILocalizationService m_locale;

        // Config
        private ISystemConfigurationService m_config;

        /// <summary>
        /// Gets or sets the application context of this component
        /// </summary>
        public SVC.Core.HostContext Context
        {
            get { return this.m_context; }
            set
            {
                this.m_context = value;
                this.m_locale = value.GetService(typeof(ILocalizationService)) as ILocalizationService;
                this.m_config = value.GetService(typeof(ISystemConfigurationService)) as ISystemConfigurationService;
            }
        }

        #endregion

        /// <summary>
        /// Create the RSP_K23 mesasge
        /// </summary>
        internal NHapi.Model.V25.Message.RSP_K23 CreateRSP_K23(QueryResultData result, List<Everest.Connectors.IResultDetail> dtls)
        {
            // Return value
            var retVal = new NHapi.Model.V25.Message.RSP_K23();

            var qak = retVal.QAK;
            var msa = retVal.MSA;
            
            qak.QueryTag.Value = result.QueryTag;
            msa.AcknowledgmentCode.Value = "AA";
            if (result.Results == null || result.Results.Length == 0)
            {
                qak.QueryResponseStatus.Value = "NF";
            }
            else
            {
                // Create the pid
                qak.QueryResponseStatus.Value = "OK";
                UpdatePID(result.Results[0], retVal.QUERY_RESPONSE.PID);
            }

            return retVal;
        }

        /// <summary>
        /// Update the specified PID
        /// </summary>
        private void UpdatePID(Core.ComponentModel.RegistrationEvent registrationEvent, NHapi.Model.V25.Segment.PID pid)
        {

            var subject = registrationEvent.FindComponent(SVC.Core.ComponentModel.HealthServiceRecordSiteRoleType.SubjectOf) as Person;

            // Alternate identifiers
            foreach (var altId in subject.AlternateIdentifiers)
            {
                var id = pid.GetPatientIdentifierList(pid.PatientIdentifierListRepetitionsUsed);
                UpdateCX(altId, id);
            }

            // IHE: This first repetition should be null
            pid.GetPatientName(0);
            pid.GetPatientName(1).NameTypeCode.Value = "S";

        }

        /// <summary>
        /// Update a CX instance
        /// </summary>
        private void UpdateCX(SVC.Core.DataTypes.DomainIdentifier altId, NHapi.Model.V25.Datatype.CX cx)
        {
            // Get oid data
            var oidData = this.m_config.OidRegistrar.FindData(altId.Domain);
            cx.AssigningAuthority.UniversalID.Value = oidData.Oid;
            cx.AssigningAuthority.UniversalIDType.Value = "ISO";
            cx.AssigningAuthority.NamespaceID.Value = oidData.Attributes.Find(o => o.Key.Equals("AssigningAuthorityName")).Value;
            cx.IDNumber.Value = altId.Identifier;
        }
    }
}
