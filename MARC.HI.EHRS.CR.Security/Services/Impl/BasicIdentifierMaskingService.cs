using MARC.HI.EHRS.SVC.DecisionSupport;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MARC.HI.EHRS.SVC.Core.DataTypes;
using MARC.HI.EHRS.SVC.Core.Issues;
using MARC.HI.EHRS.SVC.Core.Services;
using MARC.HI.EHRS.SVC.Core.ComponentModel;
using MARC.HI.EHRS.CR.Core.ComponentModel;
using System.Security.Claims;

namespace MARC.HI.EHRS.CR.Security.Services.Impl
{
    /// <summary>
    /// Decision support service for masking specific identifiers based on OAUTH policy
    /// </summary>
    /// <remarks>TODO: 1.4 test service, experiment and remove before production release</remarks>
    class BasicIdentifierMaskingService : IDecisionSupportService
    {
        
        /// <summary>
        /// Persisted record
        /// </summary>
        public List<DetectedIssue> RecordPersisted(SVC.Core.ComponentModel.HealthServiceRecordComponent hsr)
        {
            return new List<DetectedIssue>();
        }

        /// <summary>
        /// Record is about to be persisted
        /// </summary>
        public List<DetectedIssue> RecordPersisting(SVC.Core.ComponentModel.HealthServiceRecordComponent hsr)
        {
            return new List<DetectedIssue>();
        }

        /// <summary>
        /// Record retrieved
        /// </summary>
        public List<DetectedIssue> RetrievedRecord(SVC.Core.ComponentModel.HealthServiceRecordComponent hsr)
        {
            var iconfig = ApplicationContext.Current.GetService<ISystemConfigurationService>();
            var isessionService = ApplicationContext.Current.GetService<ISessionManagerService>();
            var subject = (hsr as HealthServiceRecordContainer).FindComponent(HealthServiceRecordSiteRoleType.SubjectOf) as Person;
            List<DetectedIssue> retVal = new List<DetectedIssue>();

            if(isessionService != null && subject != null)
                for(int i = subject.AlternateIdentifiers.Count - 1; i >= 0; i--)
                {
                    var itm = subject.AlternateIdentifiers[i];
                    var oid = iconfig.OidRegistrar.FindData(itm.Domain);
                    if (oid == null) continue;
                    else
                    {
                        var demand = oid.Attributes.FirstOrDefault(o => o.Key == "demand");
                        if (!String.IsNullOrEmpty(demand.Value))
                        {
                            // User must have permission
                            var scopeClaim = (AuthenticationContext.Current?.Principal as ClaimsPrincipal)?.FindAll("scope");
                            if (scopeClaim == null || scopeClaim.Count() == 0 ||
                                !scopeClaim.Any(c=>c.Type == "scope" && c.Value == demand.Value))
                            {
                                subject.AlternateIdentifiers.RemoveAt(i);
                                retVal.Add(new DetectedIssue()
                                {
                                    MitigatedBy = ManagementType.OtherActionTaken,
                                    Severity = IssueSeverityType.Moderate,
                                    Type = IssueType.InsufficientAuthorization,
                                    Priority = IssuePriorityType.Informational
                                });
                            }
                        }
                    }
                }

            return retVal;
        }

        /// <summary>
        /// Record is being retrieved
        /// </summary>
        public List<DetectedIssue> RetrievingRecord(DomainIdentifier recordId)
        {
            return new List<DetectedIssue>();
        }
    }
}
