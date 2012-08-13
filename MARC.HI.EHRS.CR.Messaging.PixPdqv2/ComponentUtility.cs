using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MARC.HI.EHRS.SVC.Core.Services;
using MARC.HI.EHRS.SVC.Core.DataTypes;
using MARC.Everest.Connectors;

namespace MARC.HI.EHRS.CR.Messaging.PixPdqv2
{
    /// <summary>
    /// Component utility
    /// </summary>
    public class ComponentUtility : IUsesHostContext
    {
        #region IUsesHostContext Members

        // Host context
        private MARC.HI.EHRS.SVC.Core.HostContext m_context;

        // Localization service
        private ILocalizationService m_locale;

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
            }
        }

        #endregion

        /// <summary>
        /// Create domain identifier
        /// </summary>
        internal SVC.Core.DataTypes.DomainIdentifier CreateDomainIdentifier(NHapi.Model.V25.Datatype.HD id, List<Everest.Connectors.IResultDetail> dtls)
        {
            DomainIdentifier retVal = new DomainIdentifier();
            if (!String.IsNullOrEmpty(id.NamespaceID.Value))
                retVal.Identifier = id.NamespaceID.Value;
            if (!String.IsNullOrEmpty(id.UniversalID.Value))
                retVal.Domain = id.UniversalID.Value;
            if (!String.IsNullOrEmpty(id.UniversalIDType.Value) && id.UniversalIDType.Value != "ISO")
                dtls.Add(new NotImplementedResultDetail(ResultDetailType.Warning, m_locale.GetString("MSGW016"), null, null));
            if (!MARC.Everest.DataTypes.II.IsValidOidFlavor(new MARC.Everest.DataTypes.II(retVal.Domain)))
                dtls.Add(new FormalConstraintViolationResultDetail(ResultDetailType.Error, m_locale.GetString("MSGE05B"), null, null));
            return retVal;
        }

        /// <summary>
        /// Create domain identifier
        /// </summary>
        internal DomainIdentifier CreateDomainIdentifier(NHapi.Model.V231.Datatype.HD id, List<IResultDetail> dtls)
        {
            DomainIdentifier retVal = new DomainIdentifier();
            if (!String.IsNullOrEmpty(id.NamespaceID.Value))
                retVal.Identifier = id.NamespaceID.Value;
            if (!String.IsNullOrEmpty(id.UniversalID.Value))
                retVal.Domain = id.UniversalID.Value;
            if (!String.IsNullOrEmpty(id.UniversalIDType.Value) && id.UniversalIDType.Value != "ISO")
                dtls.Add(new NotImplementedResultDetail(ResultDetailType.Warning, m_locale.GetString("MSGW016"), null, null));
            if (!MARC.Everest.DataTypes.II.IsValidOidFlavor(new MARC.Everest.DataTypes.II(retVal.Domain)))
                dtls.Add(new FormalConstraintViolationResultDetail(ResultDetailType.Error, m_locale.GetString("MSGE05B"), null, null));
            return retVal;
        }
    }
}
