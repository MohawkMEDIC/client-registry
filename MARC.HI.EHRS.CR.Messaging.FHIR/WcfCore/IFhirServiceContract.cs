using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel;
using MARC.HI.EHRS.CR.Messaging.FHIR.Resources;
using System.ServiceModel.Web;
using System.ServiceModel.Syndication;
using System.Xml;
using MARC.HI.EHRS.CR.Messaging.FHIR.DataTypes;

namespace MARC.HI.EHRS.CR.Messaging.FHIR.WcfCore
{
    /// <summary>
    /// FHIR Service Contract
    /// </summary>
    [XmlSerializerFormat]
    [ServiceContract]
    [ServiceKnownType(typeof(Patient))]
    [ServiceKnownType(typeof(Organization))]
    [ServiceKnownType(typeof(Picture))]
    [ServiceKnownType(typeof(Practictioner))]
    [ServiceKnownType(typeof(OperationOutcome))]
    public interface IFhirServiceContract
    {

        /// <summary>
        /// Read a resource
        /// </summary>
        [WebGet(UriTemplate = "/{resourceType}/@{id}?_format={mimeType}", ResponseFormat = WebMessageFormat.Xml, RequestFormat = WebMessageFormat.Xml, BodyStyle = WebMessageBodyStyle.Bare)]
        Shareable ReadResource(string resourceType, string id, string mimeType);

        /// <summary>
        /// Version read a resource
        /// </summary>
        [WebGet(UriTemplate = "/{resourceType}/@{id}/history/@{vid}?_format={mimeType}", ResponseFormat = WebMessageFormat.Xml, RequestFormat = WebMessageFormat.Xml, BodyStyle = WebMessageBodyStyle.Bare)]
        Shareable VReadResource(string resourceType, string id, string vid, string mimeType);

        /// <summary>
        /// Update a resource
        /// </summary>
        [WebInvoke(UriTemplate = "/{resourceType}/@{id}?_format={mimeType}", Method = "PUT", ResponseFormat = WebMessageFormat.Xml, RequestFormat = WebMessageFormat.Xml, BodyStyle = WebMessageBodyStyle.Bare)]
        void UpdateResource(string resourceType, string id, string mimeType, Shareable target);

        /// <summary>
        /// Delete a resource
        /// </summary>
        [WebInvoke(UriTemplate = "/{resourceType}/@{id}?_format={mimeType}", Method = "DELETE", ResponseFormat = WebMessageFormat.Xml, RequestFormat = WebMessageFormat.Xml, BodyStyle = WebMessageBodyStyle.Bare)]
        void DeleteResource(string resourceType, string id, string mimeType);

        /// <summary>
        /// Create a resource
        /// </summary>
        [WebInvoke(UriTemplate = "/{resourceType}?_format={mimeType}", Method = "POST", ResponseFormat = WebMessageFormat.Xml, RequestFormat = WebMessageFormat.Xml, BodyStyle = WebMessageBodyStyle.Bare)]
        void CreateResource(string resourceType, string mimeType, Shareable target);

        /// <summary>
        /// Validate a resource
        /// </summary>
        [WebInvoke(UriTemplate = "/{resourceType}/validate/@{id}", Method = "POST", ResponseFormat = WebMessageFormat.Xml, RequestFormat = WebMessageFormat.Xml, BodyStyle = WebMessageBodyStyle.Bare)]
        void ValidateResource(string resourceType, string id, Shareable target);

        /// <summary>
        /// Version read a resource
        /// </summary>
        [WebGet(UriTemplate = "/{resourceType}", ResponseFormat = WebMessageFormat.Xml, RequestFormat = WebMessageFormat.Xml, BodyStyle = WebMessageBodyStyle.Bare)]
        Atom10FeedFormatter SearchResource(string resourceType);

        /// <summary>
        /// Options for this service
        /// </summary>
        [WebInvoke(UriTemplate = "/", Method = "OPTIONS", ResponseFormat = WebMessageFormat.Xml, RequestFormat = WebMessageFormat.Xml, BodyStyle = WebMessageBodyStyle.Bare)]
        XmlElement GetOptions();

        /// <summary>
        /// Post a transaction
        /// </summary>
        [WebInvoke(UriTemplate = "/", Method = "POST", ResponseFormat = WebMessageFormat.Xml, RequestFormat = WebMessageFormat.Xml, BodyStyle = WebMessageBodyStyle.Bare)]
        Atom10FeedFormatter PostTransaction(Atom10FeedFormatter feed);

        /// <summary>
        /// Get history
        /// </summary>
        [WebGet(UriTemplate = "/{resourceType}/@{id}/history?_format={mimeType}", ResponseFormat = WebMessageFormat.Xml, RequestFormat = WebMessageFormat.Xml, BodyStyle = WebMessageBodyStyle.Bare)]
        Atom10FeedFormatter GetResourceInstanceHistory(string resourceType, string id, string mimeType);

        /// <summary>
        /// Get history
        /// </summary>
        [WebGet(UriTemplate = "/{resourceType}/history?_format={mimeType}", ResponseFormat = WebMessageFormat.Xml, RequestFormat = WebMessageFormat.Xml, BodyStyle = WebMessageBodyStyle.Bare)]
        Atom10FeedFormatter GetResourceHistory(string resourceType, string mimeType);

        /// <summary>
        /// Get history for all
        /// </summary>
        [WebGet(UriTemplate = "/history?_format={mimeType}", ResponseFormat = WebMessageFormat.Xml, RequestFormat = WebMessageFormat.Xml, BodyStyle = WebMessageBodyStyle.Bare)]
        Atom10FeedFormatter GetHistory(string mimeType);

    }
}
