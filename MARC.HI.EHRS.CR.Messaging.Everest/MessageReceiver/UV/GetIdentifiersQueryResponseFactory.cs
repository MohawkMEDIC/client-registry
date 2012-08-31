using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MARC.Everest.RMIM.UV.NE2008.Interactions;
using MARC.HI.EHRS.SVC.Core.Services;
using MARC.HI.EHRS.SVC.Core.DataTypes;
using MARC.Everest.Exceptions;

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
            ILocalizationService locale = Context.GetService(typeof(ILocalizationService)) as ILocalizationService;

            // Componentize the message into the data model
            UvComponentUtil compUtil = new UvComponentUtil();
            compUtil.Context = this.Context;
            PRPA_IN201309UV02 rqst = request as PRPA_IN201309UV02;

            List<DomainIdentifier> ids = new List<DomainIdentifier>();
            var queryData = compUtil.CreateQueryMatch(rqst.controlActProcess, dtls, ref ids);

            if (ids == null || queryData == null)
                throw new MessageValidationException(locale.GetString("MSG00A"), request);

            var filter = new DataUtil.QueryData()
            {
                QueryId = new Guid(rqst.controlActProcess.queryByParameter.QueryId.Root),
                IncludeHistory = false,
                IncludeNotes = false,
                Quantity = 100,
                Originator = String.Format("^^^&{0}&ISO",
                    rqst.Sender.Device.Id[0].Root),
                OriginalMessageQuery = request,
                QueryRequest = queryData,
                TargetDomains = ids,
                IsSummary = true
            };

            return filter;
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
