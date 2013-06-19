using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using MARC.Everest.Connectors;

namespace MARC.HI.EHRS.CR.Messaging.FHIR.Processors
{
    /// <summary>
    /// FHIR message processor base
    /// </summary>
    public abstract class MessageProcessorBase : IFhirMessageProcessor
    {

        #region IFhirMessageProcessor Members

        /// <summary>
        /// Name of resource
        /// </summary>
        public abstract string ResourceName { get; }

        /// <summary>
        /// Type of resource
        /// </summary>
        public abstract Type ResourceType { get; }

        /// <summary>
        /// Type of component
        /// </summary>
        public abstract Type ComponentType { get; }

        /// <summary>
        /// Parse query
        /// </summary>
        public virtual Util.DataUtil.FhirQuery ParseQuery(System.Collections.Specialized.NameValueCollection parameters, List<Everest.Connectors.IResultDetail> dtls)
        {

            MARC.HI.EHRS.CR.Messaging.FHIR.Util.DataUtil.FhirQuery retVal = new Util.DataUtil.FhirQuery();
            retVal.ActualParameters = new System.Collections.Specialized.NameValueCollection();

             for(int i = 0; i < parameters.Count; i++)
                 try
                 {
                     switch (parameters.GetKey(i))
                     {
                         case "stateid":
                             retVal.QueryId = Guid.Parse(parameters.GetValues(i)[0]);
                             retVal.ActualParameters.Add("queryid", retVal.QueryId.ToString());
                             break;
                         case "_count":
                             retVal.Quantity = Int32.Parse(parameters.GetValues(i)[0]);
                             break;
                         case "page":
                             retVal.Start = retVal.Quantity * Int32.Parse(parameters.GetValues(i)[0]);
                             break;
                     }
                 }
                 catch (Exception e)
                 {
                     Trace.TraceError(e.ToString());
                     dtls.Add(new ResultDetail(ResultDetailType.Error, e.Message, e));
                 }
             return retVal;
        }

        public abstract System.ComponentModel.IComponent ProcessResource(Resources.ResourceBase resource, List<Everest.Connectors.IResultDetail> dtls);
        
        public abstract Resources.ResourceBase ProcessComponent(System.ComponentModel.IComponent component, List<Everest.Connectors.IResultDetail> dtls);

        #endregion
    }
}
