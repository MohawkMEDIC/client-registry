using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MARC.HI.EHRS.CR.Messaging.FHIR.DataTypes;
using System.Xml.Serialization;
using System.Net;
using System.Diagnostics;

namespace MARC.HI.EHRS.CR.Messaging.FHIR.Resources
{
    /// <summary>
    /// Identifies a resource link
    /// </summary>
    public class Resource<T> : Shareable
        where T : Shareable
    {
        /// <summary>
        /// Gets or sets the type
        /// </summary>
        [XmlElement("type")]
        public PrimitiveCode<String> Type 
        {
            get
            {
                Object[] atts = typeof(T).GetCustomAttributes(typeof(XmlRootAttribute), true);
                if (atts.Length == 1)
                    return new PrimitiveCode<String>((atts[0] as XmlRootAttribute).ElementName);
                return new PrimitiveCode<string>(typeof(T).Name);
            }
            set
            {
                ;
            }        
        }

        /// <summary>
        /// Gets or sets the reference
        /// </summary>
        [XmlElement("reference")]
        public PrimitiveCode<String> Reference { get; set; }

        /// <summary>
        /// Gets or sets the display
        /// </summary>
        [XmlElement("display")]
        public FhirString Display { get; set; }


        /// <summary>
        /// Fetch the resource described by this item
        /// </summary>
        public T FetchResource(Uri baseUri)
        {
            return this.FetchResource(baseUri, null);
        }

        /// <summary>
        /// Fetch a resource from the specified uri with the specified credentials
        /// </summary>
        public T FetchResource(Uri baseUri, ICredentials credentials)
        {
            // Request uri
            Uri requestUri = null;

            if (!Uri.TryCreate(this.Reference.Value, UriKind.Absolute, out requestUri))
                requestUri = new Uri(baseUri, this.Reference.Value);

            // Make request to URI
            Trace.TraceInformation("Fetching from {0}...", requestUri);
            var webReq = HttpWebRequest.Create(requestUri);
            webReq.Method = "GET";
            webReq.Credentials = credentials;

            // Fetch
            try
            {
                using (var response = webReq.GetResponse() as HttpWebResponse)
                {
                    if (response.StatusCode != HttpStatusCode.Accepted)
                        throw new WebException(String.Format("Server responded with {0}", response.StatusCode));

                    // Get the response stream
                    XmlSerializer xsz = new XmlSerializer(typeof(T));
                    return xsz.Deserialize(response.GetResponseStream()) as T;
                }
            }
            catch (Exception e)
            {
                Trace.TraceError(e.ToString());
                throw;
            }

        }

    }
}
