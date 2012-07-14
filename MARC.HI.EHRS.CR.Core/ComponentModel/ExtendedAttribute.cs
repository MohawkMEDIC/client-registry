using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MARC.HI.EHRS.SVC.Core.ComponentModel;
using System.Xml.Serialization;
using System.ComponentModel;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

namespace MARC.HI.EHRS.CR.Core.ComponentModel
{
    /// <summary>
    /// Represents a component which is an extension to the existing data model
    /// </summary>
    [Serializable]
    public class ExtendedAttribute : HealthServiceRecordComponent
    {

        /// <summary>
        /// The path within the parent container to which this extension applies
        /// </summary>
        [XmlAttribute("property")]
        public string PropertyPath { get; set; }

        /// <summary>
        /// The value of the extension
        /// </summary>
        [XmlIgnore]
        public object Value { get; set; }

        /// <summary>
        /// Data of the value
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Browsable(false)]
        [XmlElement("data")]
        public byte[] ValueData
        {
            get
            {
                if (this.Value == null) return null;

                // Serialize
                BinaryFormatter bf = new BinaryFormatter();
                MemoryStream ms = new MemoryStream();
                try
                {
                    bf.Serialize(ms, this.Value);
                    byte[] retVal = new byte[ms.Length];
                    ms.Read(retVal, 0, (int)ms.Length);
                    return retVal;
                }
                finally
                {
                    ms.Dispose();
                }
            }
            set
            {
                if (value == null) return;

                // Deserialize
                BinaryFormatter bf = new BinaryFormatter();
                MemoryStream ms = new MemoryStream(value);
                try
                {
                    this.Value = bf.Deserialize(ms);
                }
                finally
                {
                    ms.Dispose();
                }
            }
        }
    }
}
