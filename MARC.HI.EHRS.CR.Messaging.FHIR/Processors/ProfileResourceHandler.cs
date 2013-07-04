using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MARC.HI.EHRS.SVC.Messaging.FHIR.Handlers;
using MARC.HI.EHRS.CR.Messaging.FHIR.Util;
using System.Diagnostics;

namespace MARC.HI.EHRS.CR.Messaging.FHIR.Processors
{
    /// <summary>
    /// A resource handler that can service requests related profiles
    /// </summary>
    public class ProfileResourceHandler : IFhirResourceHandler
    {
        #region IFhirResourceHandler Members

        /// <summary>
        /// Creates a profile (not supported)
        /// </summary>
        public SVC.Messaging.FHIR.FhirOperationResult Create(SVC.Messaging.FHIR.Resources.ResourceBase target, SVC.Core.Services.DataPersistenceMode mode)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Delete a profile
        /// </summary>
        public SVC.Messaging.FHIR.FhirOperationResult Delete(string id, SVC.Core.Services.DataPersistenceMode mode)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Query a profile
        /// </summary>
        public SVC.Messaging.FHIR.FhirQueryResult Query(System.Collections.Specialized.NameValueCollection parameters)
        {
            var result = new SVC.Messaging.FHIR.FhirQueryResult()
            {
                Outcome = Everest.Connectors.ResultCode.Accepted,
                Results = new List<SVC.Messaging.FHIR.Resources.ResourceBase>(),
                Query = new SVC.Messaging.FHIR.FhirQuery()
                {
                    ActualParameters = new System.Collections.Specialized.NameValueCollection(),
                    Quantity = 100,
                    Start = 0
                }
            };
            foreach(var prof in ProfileUtil.GetProfiles())
                result.Results.Add(prof);
            return result;
        }

        /// <summary>
        /// Read a profile
        /// </summary>
        public SVC.Messaging.FHIR.FhirOperationResult Read(string id, string versionId)
        {
            // Read a version
            var profile = ProfileUtil.GetProfile(id);
            if (!String.IsNullOrEmpty(versionId) && profile.VersionId != versionId)
                return new SVC.Messaging.FHIR.FhirOperationResult()
                {
                    Outcome = Everest.Connectors.ResultCode.TypeNotAvailable
                };
            else
                return new SVC.Messaging.FHIR.FhirOperationResult()
                {
                    Outcome = Everest.Connectors.ResultCode.Accepted,
                    Results = new List<SVC.Messaging.FHIR.Resources.ResourceBase>() { profile }
                };

        }

        /// <summary>
        /// Gets the resource name
        /// </summary>
        public string ResourceName
        {
            get { return "Profile"; }
        }

        /// <summary>
        /// Update a resource profile
        /// </summary>
        public SVC.Messaging.FHIR.FhirOperationResult Update(string id, SVC.Messaging.FHIR.Resources.ResourceBase target, SVC.Core.Services.DataPersistenceMode mode)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Validate a resource profile
        /// </summary>
        public SVC.Messaging.FHIR.FhirOperationResult Validate(string id, SVC.Messaging.FHIR.Resources.ResourceBase target)
        {
            throw new NotSupportedException();
        }

        #endregion
    }
}
