using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MARC.Everest.RMIM.UV.NE2008.Interactions;

namespace MARC.HI.EHRS.CR.Messaging.Everest.MessageReceiver.UV
{
    /// <summary>
    /// Query response factory
    /// </summary>
    public class GetIdentifiersQueryResponseFactory : IQueryResponseFactory
    {
        #region IQueryResponseFactory Members

        /// <summary>
        /// Get the type of message this query creates
        /// </summary>
        public Type CreateType
        {
            get { return typeof(PRPA_IN201310UV02); }
        }

        /// <summary>
        /// Create filter data
        /// </summary>
        public DataUtil.QueryData CreateFilterData(MARC.Everest.Interfaces.IInteraction request, List<MARC.Everest.Connectors.IResultDetail> dtls)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Create the response message
        /// </summary>
        public MARC.Everest.Interfaces.IInteraction Create(MARC.Everest.Interfaces.IInteraction request, DataUtil.QueryResultData results, List<MARC.Everest.Connectors.IResultDetail> details, List<SVC.Core.Issues.DetectedIssue> issues)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region IUsesHostContext Members

        /// <summary>
        /// GEts or sets the host context
        /// </summary>
        public SVC.Core.HostContext Context
        {
            get;
            set;
        }

        #endregion
    }
}
