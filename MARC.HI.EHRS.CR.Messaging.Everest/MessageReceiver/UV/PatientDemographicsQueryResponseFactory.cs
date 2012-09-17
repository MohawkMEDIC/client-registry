using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MARC.Everest.RMIM.UV.NE2008.Interactions;
using MARC.HI.EHRS.SVC.Core.Services;
using MARC.HI.EHRS.SVC.Core.DataTypes;
using MARC.Everest.Exceptions;
using MARC.Everest.DataTypes;
using MARC.HI.EHRS.CR.Core.ComponentModel;
using MARC.Everest.Connectors;

namespace MARC.HI.EHRS.CR.Messaging.Everest.MessageReceiver.UV
{
    /// <summary>
    /// Patient demographics query response factory
    /// </summary>
    public class PatientDemographicsQueryResponseFactory : IQueryResponseFactory
    {
        #region IQueryResponseFactory Members

        /// <summary>
        /// Get the type that this creates
        /// </summary>
        public Type CreateType
        {
            get { return typeof(PRPA_IN201306UV02); }
        }

        /// <summary>
        /// Create filter data
        /// </summary>
        public DataUtil.QueryData CreateFilterData(MARC.Everest.Interfaces.IInteraction request, List<MARC.Everest.Connectors.IResultDetail> dtls)
        {
            ILocalizationService locale = Context.GetService(typeof(ILocalizationService)) as ILocalizationService;
            ISystemConfigurationService config = Context.GetService(typeof(ISystemConfigurationService)) as ISystemConfigurationService;

            // Componentize the message into the data model
            UvComponentUtil compUtil = new UvComponentUtil();
            compUtil.Context = this.Context;
            PRPA_IN201305UV02 rqst = request as PRPA_IN201305UV02;

            List<DomainIdentifier> ids = new List<DomainIdentifier>();
            var queryData = compUtil.CreateQueryMatch(rqst.controlActProcess, dtls, ref ids);


            if (ids == null || queryData == null)
                throw new MessageValidationException(locale.GetString("MSGE00A"), request);

            var filter = new DataUtil.QueryData()
            {
                QueryId = String.Format("{1}^^^&{0}&ISO", rqst.controlActProcess.queryByParameter.QueryId.Root, rqst.controlActProcess.queryByParameter.QueryId.Extension),
                IncludeHistory = false,
                IncludeNotes = false,
                Quantity = (int)rqst.controlActProcess.queryByParameter.InitialQuantity,
                Originator = String.Format("^^^&{0}&ISO",
                    rqst.Sender.Device.Id[0].Root),
                OriginalMessageQuery = request,
                QueryRequest = queryData,
                TargetDomains = ids,
                IsSummary = true
            };

            // Filter parameters
            var qbp = rqst.controlActProcess.queryByParameter;
            if (qbp != null && qbp.MatchCriterionList != null && qbp.MatchCriterionList.NullFlavor == null)
            {
                if (qbp.MatchCriterionList.MatchAlgorithm != null && qbp.MatchCriterionList.MatchAlgorithm.NullFlavor == null &&
                    qbp.MatchCriterionList.MatchAlgorithm.Value is ST)
                {
                    try
                    {
                        filter.MatchingAlgorithm = (MatchAlgorithm)Enum.Parse(typeof(MatchAlgorithm), qbp.MatchCriterionList.MatchAlgorithm.Value as ST);
                    }
                    catch
                    {
                        dtls.Add(new VocabularyIssueResultDetail(ResultDetailType.Warning, String.Format(locale.GetString("MSGE071"), qbp.MatchCriterionList.MatchAlgorithm.Value as ST), null));
                    }
                }
                else
                    filter.MatchingAlgorithm = MatchAlgorithm.Default;

                // Match degree match
                if (qbp.MatchCriterionList.MinimumDegreeMatch != null && qbp.MatchCriterionList.MinimumDegreeMatch.NullFlavor == null &&
                    qbp.MatchCriterionList.MinimumDegreeMatch.Value != null && !qbp.MatchCriterionList.MinimumDegreeMatch.Value.IsNull)
                {
                    var match = qbp.MatchCriterionList.MinimumDegreeMatch.Value as INT;
                    if (match == null || match < 0 || match > 100)
                        dtls.Add(new NotSupportedChoiceResultDetail(ResultDetailType.Warning, locale.GetString("MSGE072"), null, null));
                    else
                        filter.MinimumDegreeMatch = (float)((float)match / 100);
                }
            }

            // Ensure that the target domains are understood by this service
            if (filter.TargetDomains != null)
                foreach (var id in filter.TargetDomains)
                    if (String.IsNullOrEmpty(id.Domain) || config.OidRegistrar.FindData(id.Domain) == null || !config.OidRegistrar.FindData(id.Domain).Attributes.Exists(p => p.Key.Equals("AssigningAuthorityName")))
                        dtls.Add(new UnrecognizedTargetDomainResultDetail(locale, id.Domain));
            return filter;
        }

        /// <summary>
        /// Create the interaction type
        /// </summary>
        public MARC.Everest.Interfaces.IInteraction Create(MARC.Everest.Interfaces.IInteraction request, DataUtil.QueryResultData results, List<MARC.Everest.Connectors.IResultDetail> details, List<SVC.Core.Issues.DetectedIssue> issues)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region IUsesHostContext Members

        /// <summary>
        /// Gets or sets the context
        /// </summary>
        public SVC.Core.HostContext Context { get; set; }

        #endregion
    }
}
