using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using System.Xml;

namespace MARC.HI.EHRS.CR.Messaging.FHIR.Resources
{
    /// <summary>
    /// Raw XML Wrapper
    /// </summary>
    public class RawXmlWrapper
    {
        /// <summary>
        /// Represents any elements
        /// </summary>
        [XmlAnyElement]
        public XmlElement[] Elements { get; set; }

        /// <summary>
        /// Raw xml wrapper
        /// </summary>
        /// <param name="wrapper"></param>
        /// <returns></returns>
        public static implicit operator XmlElement[](RawXmlWrapper wrapper)
        {
            return wrapper.Elements;
        }

        /// <summary>
        /// Raw xml wrapper
        /// </summary>
        /// <param name="wrapper"></param>
        /// <returns></returns>
        public static implicit operator RawXmlWrapper(XmlElement[] elements)
        {
            return new RawXmlWrapper() { Elements = elements };
        }
    }
}
