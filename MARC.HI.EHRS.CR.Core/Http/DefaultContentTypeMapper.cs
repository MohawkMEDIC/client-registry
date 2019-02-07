using SanteDB.Core.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MARC.HI.EHRS.CR.Core.Http
{
    /// <summary>
	/// Default body binder.
	/// </summary>
	public class DefaultContentTypeMapper : IContentTypeMapper
    {
        #region IBodySerializerBinder implementation

        /// <summary>
        /// Gets the body serializer based on the content type
        /// </summary>
        /// <param name="contentType">Content type.</param>
        /// <param name="typeHint">The type hint.</param>
        /// <returns>The serializer.</returns>
        /// <exception cref="System.ArgumentOutOfRangeException">contentType - Not supported</exception>
        public IBodySerializer GetSerializer(string contentType, Type typeHint)
        {
            switch (contentType)
            {
                case "text/xml":
                case "application/xml":
                case "application/xml; charset=utf-8":
                case "application/xml; charset=UTF-8":
                    return new XmlBodySerializer(typeHint);

                case "application/json":
                case "application/json; charset=utf-8":
                case "application/json; charset=UTF-8":
                    return new JsonBodySerializer(typeHint);

                case "application/x-www-form-urlencoded":
                    return new FormBodySerializer();

                case "application/octet-stream":
                    return new BinaryBodySerializer();

                default:
                    if (contentType.StartsWith("multipart/form-data"))
                        return new MultipartBinarySerializer(contentType);

                    throw new ArgumentOutOfRangeException(nameof(contentType), contentType, "Not supported");
            }
        }

        #endregion IBodySerializerBinder implementation
    }
}
