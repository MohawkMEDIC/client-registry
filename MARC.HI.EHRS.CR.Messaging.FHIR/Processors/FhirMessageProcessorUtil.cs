/**
 * Copyright 2012-2015 Mohawk College of Applied Arts and Technology
 * 
 * Licensed under the Apache License, Version 2.0 (the "License"); you 
 * may not use this file except in compliance with the License. You may 
 * obtain a copy of the License at 
 * 
 * http://www.apache.org/licenses/LICENSE-2.0 
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS, WITHOUT
 * WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the 
 * License for the specific language governing permissions and limitations under 
 * the License.
 * 
 * User: Justin
 * Date: 12-7-2015
 */
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
using System.Diagnostics;

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
                Trace.TraceInformation("Added processor {0} for type {1}({2})", t.FullName, processor.ComponentType.FullName, processor.ResourceName);
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
            var retVal = s_messageProcessors.Find(o => o.ComponentType == componentType);
            if (retVal == null)
                Trace.TraceWarning("Could not find processor for {0}", componentType);
            return retVal;
        }

    }
}
