using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using SanteDB.Core.Http;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MARC.HI.EHRS.CR.Core.Http
{
    /// <summary>
	/// Represents a body serializer that uses JSON
	/// </summary>
	internal class JsonBodySerializer : IBodySerializer
    {
        // Serializer
        private JsonSerializer m_serializer;

        // The type
        private Type m_type;

        /// <summary>
        /// Initializes a new instance of the <see cref="SanteDB.Core.Http.JsonBodySerializer"/> class.
        /// </summary>
        public JsonBodySerializer(Type type)
        {
            this.m_serializer = new JsonSerializer()
            {
                DateFormatHandling = DateFormatHandling.IsoDateFormat,
                NullValueHandling = NullValueHandling.Ignore,
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                TypeNameHandling = TypeNameHandling.Auto
            };

            this.m_serializer.Converters.Add(new StringEnumConverter());
            this.m_type = type;
        }

        #region IBodySerializer implementation

        /// <summary>
        /// Serialize
        /// </summary>
        public void Serialize(System.IO.Stream s, object o)
        {
            using (TextWriter tw = new StreamWriter(s, System.Text.Encoding.UTF8, 2048, true))
            using (JsonTextWriter jw = new JsonTextWriter(tw))
                this.m_serializer.Serialize(jw, o);
        }

        /// <summary>
        /// De-serialize the body
        /// </summary>
        public object DeSerialize(System.IO.Stream s)
        {
            using (TextReader tr = new StreamReader(s, System.Text.Encoding.UTF8, true, 2048, true))
            using (JsonTextReader jr = new JsonTextReader(tr))
                return this.m_serializer.Deserialize(jr, this.m_type);
        }

        #endregion IBodySerializer implementation
    }
}
