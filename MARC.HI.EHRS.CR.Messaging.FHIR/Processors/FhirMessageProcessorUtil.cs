using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel.Syndication;
using System.ServiceModel.Web;
using MARC.HI.EHRS.SVC.Core.DataTypes;
using MARC.Everest.Connectors;
using System.Xml.Serialization;
using MARC.HI.EHRS.CR.Messaging.FHIR.Resources;
using MARC.HI.EHRS.SVC.Core.ComponentModel;

namespace MARC.HI.EHRS.CR.Messaging.FHIR.Processors
{
    /// <summary>
    /// Message processing tool
    /// </summary>
    public static class FhirMessageProcessorUtil
    {

        // XHTML
        public const string NS_XHTML = "http://www.w3.org/1999/xhtml";

        // Message processors
        private static List<IFhirMessageProcessor> s_messageProcessors = new List<IFhirMessageProcessor>();

        /// <summary>
        /// FHIR message processing utility
        /// </summary>
        static FhirMessageProcessorUtil()
        {

            foreach (var t in typeof(FhirMessageProcessorUtil).Assembly.GetTypes().Where(o => o.GetInterface(typeof(IFhirMessageProcessor).FullName) != null))
            {
                var ctor = t.GetConstructor(Type.EmptyTypes);
                if (ctor == null)
                    continue; // cannot construct
                var processor = ctor.Invoke(null) as IFhirMessageProcessor;
                s_messageProcessors.Add(processor);
            }

        }

        /// <summary>
        /// Populate a domain identifier from a FHIR token
        /// </summary>
        public static DomainIdentifier IdentifierFromToken(string token)
        {
            string[] tokens = token.Split('!');
            if (tokens.Length == 1)
                return new DomainIdentifier() { Identifier = tokens[0] };
            else
                return new DomainIdentifier()
                {
                    Domain = TranslateFhirDomain(tokens[0]),
                    Identifier = tokens[1]
                };
        }

        /// <summary>
        /// Attempt to translate fhir domain
        /// </summary>
        public static string TranslateFhirDomain(string fhirDomain)
        {
            if (fhirDomain.StartsWith("urn:oid:"))
                return fhirDomain.Replace("urn:oid:", "");
            else if (fhirDomain.StartsWith("urn:ietf:rfc:3986"))
                return fhirDomain;
            else
            {
                var oid = ApplicationContext.ConfigurationService.OidRegistrar.FindData(new Uri(fhirDomain));
                if (oid == null)
                    throw new InvalidOperationException(String.Format(ApplicationContext.LocalizationService.GetString("MSGE076"), fhirDomain));
                return oid.Oid;
            }
        }

        /// <summary>
        /// Attempt to translate fhir domain
        /// </summary>
        public static string TranslateCrDomain(string crDomain)
        {
            // Attempt to lookup the OID
            var oid = ApplicationContext.ConfigurationService.OidRegistrar.FindData(crDomain);
            if (oid == null)
                return String.Format("urn:oid:{0}", crDomain);
            else if (crDomain == "urn:ietf:rfc:3986")
                return crDomain;
            else
                return oid.Ref != null ? oid.Ref.ToString() : string.Format("urn:oid:{0}", crDomain);
        }

        /// <summary>
        /// Populate a domain identifier from a FHIR token
        /// </summary>
        public static CodeValue CodeFromToken(string token)
        {
            string[] tokens = token.Split('!');
            if (tokens.Length == 1)
                return new CodeValue() { Code = tokens[0] };
            else
                return new CodeValue()
                {
                    CodeSystem = tokens[0],
                    Code = tokens[1]
                };
        }


        /// <summary>
        /// Create a feed
        /// </summary>
        public static SyndicationFeed CreateFeed(MARC.HI.EHRS.CR.Messaging.FHIR.Util.DataUtil.FhirQueryResult result, List<IResultDetail> details)
        {

            SyndicationFeed retVal = new SyndicationFeed();

            int pageNo = result.Query.Start / result.Query.Quantity,
                nPages = (result.TotalResults / result.Query.Quantity) + 1;

            if(details.Exists(o=>o.Type == ResultDetailType.Error))
                retVal.Title = new TextSyndicationContent(String.Format("Search Error", pageNo));
            else
                retVal.Title = new TextSyndicationContent(String.Format("Search Page {0}", pageNo));
            retVal.Id = String.Format("urn:uuid:{0}", Guid.NewGuid());

            // Make the Self uri
            String baseUri = WebOperationContext.Current.IncomingRequest.UriTemplateMatch.RequestUri.AbsoluteUri;
            if (baseUri.Contains("?"))
                baseUri = baseUri.Substring(0, baseUri.IndexOf("?") + 1);

            // Self uri
            for (int i = 0; i < result.Query.ActualParameters.Count; i++)
                foreach (var itm in result.Query.ActualParameters.GetValues(i))
                    baseUri += string.Format("{0}={1}&", result.Query.ActualParameters.GetKey(i), itm);
            
            baseUri += String.Format("stateid={0}&", result.Query.QueryId);

            // Self URI
            if(nPages > 1)
            {
                retVal.Links.Add(new SyndicationLink(new Uri(String.Format("{0}&page={1}", baseUri, pageNo)), "self", null, null, 0));
                if (pageNo > 0)
                {
                    retVal.Links.Add(new SyndicationLink(new Uri(String.Format("{0}&page=0", baseUri)), "first", ApplicationContext.LocalizationService.GetString("FHIR001"), null, 0));
                    retVal.Links.Add(new SyndicationLink(new Uri(String.Format("{0}&page={1}", baseUri, pageNo - 1)), "previous", ApplicationContext.LocalizationService.GetString("FHIR002"), null, 0));
                }
                if (pageNo < nPages - 1)
                {
                    retVal.Links.Add(new SyndicationLink(new Uri(String.Format("{0}&page={1}", baseUri, pageNo + 1)), "next", ApplicationContext.LocalizationService.GetString("FHIR003"), null, 0));
                    retVal.Links.Add(new SyndicationLink(new Uri(String.Format("{0}&page={1}", baseUri, nPages)), "last", ApplicationContext.LocalizationService.GetString("FHIR004"), null, 0));
                }
            }
            else
                retVal.Links.Add(new SyndicationLink(new Uri(baseUri), "self", null, null, 0));

            // Updated
            retVal.LastUpdatedTime = DateTime.Now;
            retVal.Generator = "http://te.marc-hi.ca";
            
            //retVal.
            // Results
            if (result.TotalResults != 0)
            {
                var feedItems = new List<SyndicationItem>();
                foreach (HealthServiceRecordComponent itm in result.Results)
                {
                    var processor = FhirMessageProcessorUtil.GetComponentProcessor(itm.GetType());
                    if (processor == null)
                        details.Add(new NotImplementedResultDetail(ResultDetailType.Error, String.Format("Component type '{0}' cannot be translated to FHIR", itm.GetType().Name), null));
                    else
                    {
                        var resource = processor.ProcessComponent(itm, details);
                        Uri resourceUrl = new Uri(String.Format("{0}/{1}", WebOperationContext.Current.IncomingRequest.UriTemplateMatch.BaseUri, String.Format("{0}/@{1}/history/@{2}", processor.ResourceName, resource.Id, resource.VersionId)));
                        SyndicationItem feedResult = new SyndicationItem(String.Format("{0} id {1} version {2}", processor.ResourceName, resource.Id, resource.VersionId), null, resourceUrl);
                        feedResult.Summary = new TextSyndicationContent(resource.Text.Div.OuterXml, TextSyndicationContentKind.Html);
                        feedResult.Content = new XmlSyndicationContent("application/fhir+xml", new SyndicationElementExtension(resource, new XmlSerializer(resource.GetType())));
                        feedResult.LastUpdatedTime = itm.Timestamp;
                        feedResult.PublishDate = DateTime.Now;
                        // TODO: author
                        feedItems.Add(feedResult);
                    }
                }
                retVal.Items = feedItems;
            }

            // Outcome
            if (details.Count > 0 || result.Issues != null && result.Issues.Count > 0)
            {
                var outcome = CreateOutcomeResource(result, details);
                retVal.ElementExtensions.Add(outcome, new XmlSerializer(typeof(OperationOutcome)));
                retVal.Description = new TextSyndicationContent(outcome.Text.Div.OuterXml, TextSyndicationContentKind.Html);
            }
            return retVal;

        }

     

        /// <summary>
        /// Create an operation outcome resource
        /// </summary>
        private static OperationOutcome CreateOutcomeResource(Util.DataUtil.FhirQueryResult result, List<IResultDetail> details)
        {
            var retVal = new OperationOutcome();

            Uri fhirIssue = new Uri("http://hl7.org/fhir/issue-type");

            // Add issues for each of the details
            foreach (var dtl in details)
            {
                Issue issue = new Issue()
                {
                    Details = new DataTypes.FhirString(dtl.Message),
                    Severity = new DataTypes.PrimitiveCode<string>(dtl.Type.ToString().ToLower())
                };

                if (!String.IsNullOrEmpty(dtl.Location))
                    issue.Location.Add(new DataTypes.FhirString(dtl.Location));

                // Type
                if (dtl.Exception is TimeoutException)
                    issue.Type = new DataTypes.Coding(fhirIssue, "timeout");
                else if (dtl is FixedValueMisMatchedResultDetail)
                    issue.Type = new DataTypes.Coding(fhirIssue, "value");
                else if (dtl is PersistenceResultDetail)
                    issue.Type = new DataTypes.Coding(fhirIssue, "no-store");
                else
                    issue.Type = new DataTypes.Coding(fhirIssue, "exception");

                retVal.Issue.Add(issue);
            }

            // Add detected issues
            if (result.Issues != null)
                foreach (var iss in result.Issues)
                    retVal.Issue.Add(new Issue()
                    {
                        Details = new DataTypes.FhirString(iss.Text),
                        Severity = new DataTypes.PrimitiveCode<string>(iss.Severity.ToString().ToLower()),
                        Type = new DataTypes.Coding(fhirIssue, "business-rule")
                    });

            return retVal;
        }

        /// <summary>
        /// Get the message processor type based on resource name
        /// </summary>
        public static IFhirMessageProcessor GetMessageProcessor(String resourceName)
        {
            return s_messageProcessors.Find(o => o.ResourceName == resourceName);
        }

        /// <summary>
        /// Get the message processor based on resource type
        /// </summary>
        /// <param name="resourceType"></param>
        /// <returns></returns>
        public static IFhirMessageProcessor GetMessageProcessor(Type resourceType)
        {
            return s_messageProcessors.Find(o => o.ResourceType == resourceType);
        }

        /// <summary>
        /// Get the component processor type based component type
        /// </summary>
        public static IFhirMessageProcessor GetComponentProcessor(Type componentType)
        {
            return s_messageProcessors.Find(o => o.ComponentType == componentType);
        }

    }
}
