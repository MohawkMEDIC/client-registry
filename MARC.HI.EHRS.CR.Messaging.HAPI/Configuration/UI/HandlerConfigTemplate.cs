using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using System.Xml;

namespace MARC.HI.EHRS.CR.Messaging.HL7.Configuration.UI
{
    /// <summary>
    /// HAPI revision template
    /// </summary>
    [XmlType("HapiHandlerTemplate", Namespace = "urn:marc-hi:ca/svc")]
    [XmlRoot("hapiHandlerTemplate", Namespace = "urn:marc-hi:ca/svc")]
    public class HandlerConfigTemplate
    {
        /// <summary>
        /// Name of the template
        /// </summary>
        [XmlElement("name")]
        public string Name { get; set; }

        /// <summary>
        /// Revision section configuration
        /// </summary>
        [XmlElement("handlerConfiguration")]
        public HandlerDefinition HandlerConfiguration { get; set; }

        /// <summary>
        /// Get as string
        /// </summary>
        public override string ToString()
        {
            return this.Name;
        }
    }
}
