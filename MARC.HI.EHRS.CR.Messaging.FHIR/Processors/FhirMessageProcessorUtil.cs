using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel.Syndication;
using System.ServiceModel.Web;
using MARC.HI.EHRS.SVC.Core.DataTypes;
using MARC.Everest.Connectors;
using System.Xml.Serialization;
using MARC.HI.EHRS.SVC.Messaging.FHIR.Resources;
using MARC.HI.EHRS.SVC.Core.ComponentModel;
using System.Xml;

namespace MARC.HI.EHRS.CR.Messaging.FHIR.Processors
{
    /// <summary>
    /// Message processing tool
    /// </summary>
    public static class FhirMessageProcessorUtil
    {

      
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
