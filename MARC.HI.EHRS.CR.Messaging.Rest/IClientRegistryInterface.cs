using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel;
using System.ServiceModel.Syndication;
using System.ServiceModel.Web;
using MARC.HI.EHRS.CR.Core.ComponentModel;

namespace MARC.HI.EHRS.CR.Messaging.Rest
{
    /// <summary>
    /// Client registry interface contract
    /// </summary>
    [ServiceContract]
    [XmlSerializerFormat]
    public interface IClientRegistryInterface
    {

        /// <summary>
        /// Get all clients matching criteria
        /// </summary>
        [WebGet(UriTemplate = "clients/")]
        Atom10FeedFormatter GetClients();

        /// <summary>
        /// Get client with the specified local identifier;
        /// </summary>
        [WebGet(UriTemplate = "clients/{id}")]
        Person GetClient(string id);
    }
}
