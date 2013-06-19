using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MARC.HI.EHRS.SVC.Core.Services;
using System.ServiceModel;
using System.ServiceModel.Web;
using MARC.HI.EHRS.CR.Messaging.FHIR.Processors;
using System.ServiceModel.Syndication;
using System.Diagnostics;
using MARC.HI.EHRS.SVC.Core.DataTypes;
using MARC.HI.EHRS.CR.Messaging.FHIR.Util;
using System.ComponentModel;
using MARC.Everest.Connectors;

namespace MARC.HI.EHRS.CR.Messaging.FHIR.WcfCore
{
    /// <summary>
    /// FHIR service behavior
    /// </summary>
    public class FhirServiceBehavior : IFhirServiceContract
    {

        #region IFhirServiceContract Members

        public DataTypes.Shareable ReadResource(string resourceType, string id, string mimeType)
        {
            throw new NotImplementedException();
        }

        public DataTypes.Shareable VReadResource(string resourceType, string id, string vid, string mimeType)
        {
            throw new NotImplementedException();
        }

        public void UpdateResource(string resourceType, string id, string mimeType, DataTypes.Shareable target)
        {
            throw new NotImplementedException();
        }

        public void DeleteResource(string resourceType, string id, string mimeType)
        {
            throw new NotImplementedException();
        }

        public void CreateResource(string resourceType, string mimeType, DataTypes.Shareable target)
        {
            throw new NotImplementedException();
        }

        public void ValidateResource(string resourceType, string id, DataTypes.Shareable target)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Searches a resource from the client registry datastore 
        /// </summary>
        public System.ServiceModel.Syndication.Atom10FeedFormatter SearchResource(string resourceType)
        {
            // Get the services from the service registry
            var auditService = ApplicationContext.CurrentContext.GetService(typeof(IAuditorService)) as IAuditorService;

            // Stuff for auditing and exception handling
            AuditData audit = null;
            List<IResultDetail> details = new List<IResultDetail>();
            MARC.HI.EHRS.CR.Messaging.FHIR.Util.DataUtil.FhirQueryResult result = new DataUtil.FhirQueryResult();

            try
            {

                // Get query parameters
                var queryParameters = WebOperationContext.Current.IncomingRequest.UriTemplateMatch.QueryParameters;
                var resourceProcessor = FhirMessageProcessorUtil.GetMessageProcessor(resourceType);

                // Setup outgoing content
                WebOperationContext.Current.OutgoingResponse.ContentType = "application/atom+xml";
                if (resourceProcessor == null) // Unsupported resource
                {
                    WebOperationContext.Current.OutgoingResponse.StatusCode = System.Net.HttpStatusCode.NotFound;
                    return null;
                }

                // Process incoming request
                result.Query = resourceProcessor.ParseQuery(WebOperationContext.Current.IncomingRequest.UriTemplateMatch.QueryParameters, details);

                // sanity check
                if (result.Query.ActualParameters.Count == 0)
                    throw new InvalidOperationException(ApplicationContext.LocalizationService.GetString("MSGE077"));
                else if (details.Exists(o => o.Type == ResultDetailType.Error))
                    throw new InvalidOperationException(ApplicationContext.LocalizationService.GetString("MSGE00A"));
                // Filter
                if (result.Query.Filter == null)
                    throw new InvalidOperationException("Could not process query parameters!");

                // Query?
                if (result.Query.QueryId == Guid.Empty)
                {
                    result.Query.QueryId = Guid.NewGuid();
                    result = DataUtil.Query(result.Query, details);
                }
                else
                    ; // todo: 

                // Create the Atom feed
                return new Atom10FeedFormatter(FhirMessageProcessorUtil.CreateFeed(result, details));

            }
            catch (InvalidOperationException e)
            {
                Trace.TraceError(e.ToString());
                details.Add(new ResultDetail(ResultDetailType.Error, e.Message, e));
                audit = AuditUtil.CreateAuditData(null);
                audit.Outcome = OutcomeIndicator.SeriousFail;
                throw new WebFaultException<Atom10FeedFormatter>(new Atom10FeedFormatter(FhirMessageProcessorUtil.CreateFeed(result, details)), (System.Net.HttpStatusCode)422);
            }
            catch (Exception e)
            {
                Trace.TraceError(e.ToString());
                details.Add(new ResultDetail(ResultDetailType.Error, e.Message, e)); 
                audit = AuditUtil.CreateAuditData(null);
                audit.Outcome = OutcomeIndicator.EpicFail;
                throw new WebFaultException<Atom10FeedFormatter>(new Atom10FeedFormatter(FhirMessageProcessorUtil.CreateFeed(result, details)), System.Net.HttpStatusCode.InternalServerError);
            }
            finally
            {
                if (auditService != null)
                    auditService.SendAudit(audit);
            }
            return null;

        }

        public System.Xml.XmlElement GetOptions()
        {
            throw new NotImplementedException();
        }

        public System.ServiceModel.Syndication.Atom10FeedFormatter PostTransaction(System.ServiceModel.Syndication.Atom10FeedFormatter feed)
        {
            throw new NotImplementedException();
        }

        public System.ServiceModel.Syndication.Atom10FeedFormatter GetResourceInstanceHistory(string resourceType, string id, string mimeType)
        {
            throw new NotImplementedException();
        }

        public System.ServiceModel.Syndication.Atom10FeedFormatter GetResourceHistory(string resourceType, string mimeType)
        {
            throw new NotImplementedException();
        }

        public System.ServiceModel.Syndication.Atom10FeedFormatter GetHistory(string mimeType)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
