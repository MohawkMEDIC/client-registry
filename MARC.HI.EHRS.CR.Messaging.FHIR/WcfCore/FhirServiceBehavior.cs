using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MARC.HI.EHRS.SVC.Core.Services;
using System.ServiceModel;
using System.ServiceModel.Web;

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
            var dataRegistrationService = ApplicationContext.CurrentContext.GetService(typeof(IDataRegistrationService)) as IDataRegistrationService;
            var dataPersistenceService = ApplicationContext.CurrentContext.GetService(typeof(IDataPersistenceService)) as IDataPersistenceService;
            var auditService = ApplicationContext.CurrentContext.GetService(typeof(IAuditorService)) as IAuditorService;
            var queryService = ApplicationContext.CurrentContext.GetService(typeof(IQueryPersistenceService)) as IQueryPersistenceService;

            // Query parameters?
            
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
