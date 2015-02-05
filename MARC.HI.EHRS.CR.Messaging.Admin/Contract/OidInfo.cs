using MARC.HI.EHRS.SVC.Core.DataTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace MARC.HI.EHRS.CR.Messaging.Admin.Contract
{
    /// <summary>
    /// Represents serialized oid information
    /// </summary>
    [XmlType("OidInfo")]
    [XmlRoot("OidInfo")]
    public class OidInfo
    {

        /// <summary>
        /// Description of the oid
        /// </summary>
        [XmlText]
        public string Description { get; set; }
        /// <summary>
        /// The OID itself
        /// </summary>
        [XmlAttribute("oid")]
        public string Oid { get; set; }
        /// <summary>
        /// The URL of the oid
        /// </summary>
        [XmlAttribute("url")]
        public string Url { get; set; }
        /// <summary>
        /// Name of the OID
        /// </summary>
        [XmlAttribute("key")]
        public string Name { get; set; }
        /// <summary>
        /// Attributes
        /// </summary>
        [XmlElement("attribute")]
        public List<AttributeData> Attributes { get; set; }

        /// <summary>
        /// OID info default ctor
        /// </summary>
        public OidInfo()
        {

        }

        /// <summary>
        /// OID info copy constructor
        /// </summary>
        public OidInfo(OidRegistrar.OidData data)
        {
            this.Name = data.Name;
            this.Oid = data.Oid;
            this.Description = data.Description;
            this.Url = data.Ref.ToString();
            this.Attributes = new List<AttributeData>();
            foreach (var att in data.Attributes)
                this.Attributes.Add(new AttributeData()
                {
                    Key = att.Key,
                    Value = att.Value
                });
        }
    }

    /// <summary>
    /// Attribute data
    /// </summary>
    [XmlType("AttributeData")]
    public class AttributeData
    {
        /// <summary>
        /// Key of the attribute
        /// </summary>
        [XmlAttribute("key")]
        public String Key { get; set; }
        /// <summary>
        /// Value of the attribute
        /// </summary>
        [XmlAttribute("value")]
        public String Value { get; set; }
    }
}
