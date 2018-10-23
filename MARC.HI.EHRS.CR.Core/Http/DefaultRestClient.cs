/*
 * Copyright 2015-2018 Mohawk College of Applied Arts and Technology
 *
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
 * User: fyfej
 * Date: 2017-9-1
 */
using SanteDB.Core.Http.Description;
using SanteDB.Core.Model.Query;
using SharpCompress.Compressors.BZip2;
using SharpCompress.Compressors.LZMA;
using SharpCompress.Compressors;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Security;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using SharpCompress.Compressors.Deflate;
using MARC.HI.EHRS.SVC.Core;
using MARC.HI.EHRS.SVC.Core.Services;
using SanteDB.Core.Http;
using MARC.HI.EHRS.CR.Core.Util;

namespace MARC.HI.EHRS.CR.Core.Http
{
    /// <summary>
    /// Represents an android enabled rest client
    /// </summary>
    public class RestClient : RestClientBase
    {
        /// <summary>
        /// The trace source.
        /// </summary>
        private readonly TraceSource traceSource = new TraceSource("SanteDB.Core.Http");

        /// <summary>
        /// Initializes a new instance of the <see cref="RestClient"/> class.
        /// </summary>
        public RestClient() : base()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RestClient"/> class.
        /// </summary>
        /// <param name="config">The configuration.</param>
        /// <exception cref="System.Security.SecurityException"></exception>
        public RestClient(IRestClientDescription config) : base(config)
        {
            // Find the specified certificate
            if (config.Binding?.Security?.ClientCertificate != null)
            {
                this.ClientCertificates = new X509Certificate2Collection();
                var cert = SecurityUtils.FindCertificate(config.Binding.Security.ClientCertificate.FindType,
                              config.Binding.Security.ClientCertificate.StoreLocation,
                              config.Binding.Security.ClientCertificate.StoreName,
                              config.Binding.Security.ClientCertificate.FindValue);
                if (cert == null)
                    throw new SecurityException(String.Format("Certificate described by {0} could not be found in {1}/{2}",
                        config.Binding.Security.ClientCertificate.FindValue,
                        config.Binding.Security.ClientCertificate.StoreLocation,
                        config.Binding.Security.ClientCertificate.StoreName));
                this.ClientCertificates.Add(cert);
            }
        }

        /// <summary>
        /// Gets or sets the client certificate
        /// </summary>
        /// <value>The client certificate.</value>
        public X509Certificate2Collection ClientCertificates
        {
            get;
            set;
        }

        /// <summary>
        /// Create HTTP Request object
        /// </summary>
        protected override WebRequest CreateHttpRequest(string url, NameValueCollection query)
        {
            var retVal = (HttpWebRequest)base.CreateHttpRequest(url, query);

            // Certs?
            if (this.ClientCertificates != null)
                retVal.ClientCertificates.AddRange(this.ClientCertificates);


            if (this.Description?.Binding?.Security?.CertificateValidator != null)
                retVal.ServerCertificateValidationCallback += (sender, certificate, chain, sslPolicyErrors) =>
                {
                    return this.Description.Binding.Security.CertificateValidator.ValidateCertificate(certificate, chain);
                };
            


            if (this.Description.Binding.Optimize)
                retVal.Headers[HttpRequestHeader.AcceptEncoding] = "gzip, deflate";
            return retVal;
        }

        /// <summary>
        /// Invokes the specified method against the url provided
        /// </summary>
        /// <param name="method">Method.</param>
        /// <param name="url">URL.</param>
        /// <param name="contentType">Content type.</param>
        /// <param name="body">Body.</param>
        /// <param name="query">Query.</param>
        /// <typeparam name="TBody">The 1st type parameter.</typeparam>
        /// <typeparam name="TResult">The 2nd type parameter.</typeparam>
        protected override TResult InvokeInternal<TBody, TResult>(string method, string url, string contentType, WebHeaderCollection requestHeaders, out WebHeaderCollection responseHeaders, TBody body, NameValueCollection query)
        {
            if (String.IsNullOrEmpty(method))
                throw new ArgumentNullException(nameof(method));
            if (String.IsNullOrEmpty(url))
                throw new ArgumentNullException(nameof(url));

            // Three times:
            // 1. With provided credential
            // 2. With challenge
            // 3. With challenge again
            for (int i = 0; i < 2; i++)
            {
                // Credentials provided ?
                HttpWebRequest requestObj = this.CreateHttpRequest(url, query) as HttpWebRequest;
                if (!String.IsNullOrEmpty(contentType))
                    requestObj.ContentType = contentType;
                requestObj.Method = method;

                // Additional headers
                if (requestHeaders != null)
                    foreach (var hdr in requestHeaders.AllKeys)
                    {
                        if (hdr == "If-Modified-Since")
                            requestObj.IfModifiedSince = DateTime.Parse(requestHeaders[hdr]);
                        else
                            requestObj.Headers.Add(hdr, requestHeaders[hdr]);
                    }

                // Body was provided?
                try
                {
                    // Try assigned credentials
                    IBodySerializer serializer = null;
                    if (body != null)
                    {
                        // GET Stream,
                        Stream requestStream = null;
                        Exception requestException = null;

                        try
                        {
                            //requestStream = requestObj.GetRequestStream();
                            var requestTask = requestObj.GetRequestStreamAsync().ContinueWith(r =>
                            {
                                if (r.IsFaulted)
                                    requestException = r.Exception.InnerExceptions.First();
                                else
                                    requestStream = r.Result;
                            }, TaskContinuationOptions.LongRunning);

                            if (!requestTask.Wait(this.Description.Endpoint[0].Timeout))
                            {
                                throw new TimeoutException();
                            }
                            else if (requestException != null) throw requestException;

                            if (contentType == null)
                                throw new ArgumentNullException(nameof(contentType));

                            serializer = this.Description.Binding.ContentTypeMapper.GetSerializer(contentType, typeof(TBody));
                            // Serialize and compress with deflate
                            if (this.Description.Binding.Optimize)
                            {
#if HTTP_LZMA
                                requestObj.Headers.Add("Content-Encoding", "lzma");
								using (var df = new LZipStream(requestStream, CompressionMode.Compress, leaveOpen: true))
									serializer.Serialize(df, body);
#elif HTTP_GZ
                                requestObj.Headers.Add("Content-Encoding", "gzip");
								using (var df = new GZipStream(requestStream, CompressionMode.Compress, leaveOpen: true))
									serializer.Serialize(df, body);
#else

                                requestObj.Headers.Add("Content-Encoding", "deflate");
                                using (var df = new DeflateStream(requestStream, CompressionMode.Compress, leaveOpen: true))
                                    serializer.Serialize(df, body);
#endif
                            }
                            else
                                serializer.Serialize(requestStream, body);
                        }
                        finally
                        {
                            if (requestStream != null)
                                requestStream.Dispose();
                        }
                    }

                    // Response
                    HttpWebResponse response = null;
                    Exception responseError = null;

                    try
                    {
                        var responseTask = requestObj.GetResponseAsync().ContinueWith(r =>
                        {
                            if (r.IsFaulted)
                                responseError = r.Exception.InnerExceptions.First();
                            else
                                response = r.Result as HttpWebResponse;
                        }, TaskContinuationOptions.LongRunning);

                        if (!responseTask.Wait(this.Description.Endpoint[0].Timeout))
                            throw new TimeoutException();
                        else
                        {
                            if (responseError != null)
                            {
                                responseHeaders = new WebHeaderCollection();
                                if (((responseError as WebException)?.Response as HttpWebResponse)?.StatusCode == HttpStatusCode.NotModified)
                                    return default(TResult);
                                else
                                    throw responseError;
                            }
                            responseHeaders = response.Headers;

                        }

                        var validationResult = this.ValidateResponse(response);
                        if (validationResult != ServiceClientErrorType.Valid)
                        {
                            this.traceSource.TraceEvent(TraceEventType.Error, 0, "Response failed validation : {0}", validationResult);
                            throw new WebException("Response failed validation", null, WebExceptionStatus.Success, response);
                        }
                        // De-serialize
                        var responseContentType = response.ContentType;
                        if (responseContentType.Contains(";"))
                            responseContentType = responseContentType.Substring(0, responseContentType.IndexOf(";"));

                        if (response.StatusCode == HttpStatusCode.NotModified ||
                            response.StatusCode == HttpStatusCode.NoContent ||
                            typeof(TResult) == typeof(object))
                            return default(TResult);
                        serializer = this.Description.Binding.ContentTypeMapper.GetSerializer(responseContentType, typeof(TResult));

                        TResult retVal = default(TResult);
                        using (var ms = new MemoryStream())
                        {
                            response.GetResponseStream().CopyTo(ms);
                            ms.Seek(0, SeekOrigin.Begin);
                            // Compression?
                            switch (response.Headers[HttpResponseHeader.ContentEncoding])
                            {
                                case "deflate":
                                    using (DeflateStream df = new DeflateStream(ms, CompressionMode.Decompress, leaveOpen: true))
                                        retVal = (TResult)serializer.DeSerialize(df);
                                    break;

                                case "gzip":
                                    using (GZipStream df = new GZipStream(ms, CompressionMode.Decompress, leaveOpen: true))
                                        retVal = (TResult)serializer.DeSerialize(df);
                                    break;
                                case "bzip2":
                                    using (var bzs = new BZip2Stream(ms, CompressionMode.Decompress, leaveOpen: true))
                                        retVal = (TResult)serializer.DeSerialize(bzs);
                                    break;
                                case "lzma":
                                    using (var lzmas = new LZipStream(ms, CompressionMode.Decompress, leaveOpen: true))
                                        retVal = (TResult)serializer.DeSerialize(lzmas);

                                    break;
                                default:
                                    retVal = (TResult)serializer.DeSerialize(ms);
                                    break;
                            }
                        }

                        return retVal;
                    }
                    finally
                    {
                        if (response != null)
                            response.Dispose();
                    }
                }
                catch (TimeoutException e)
                {
                    this.traceSource.TraceEvent(TraceEventType.Error, 0, "Request timed out:{0}", e);
                    throw;
                }
                catch (WebException e)
                {
                    this.traceSource.TraceEvent(TraceEventType.Error, 0, e.ToString());

                    // status
                    switch (e.Status)
                    {
                        case WebExceptionStatus.ProtocolError:

                            // Deserialize
                            TResult result = default(TResult);
                            var errorResponse = (e.Response as HttpWebResponse);
                            var responseContentType = errorResponse.ContentType;
                            if (responseContentType.Contains(";"))
                                responseContentType = responseContentType.Substring(0, responseContentType.IndexOf(";"));
                            try
                            {
                                var serializer = this.Description.Binding.ContentTypeMapper.GetSerializer(responseContentType, typeof(TResult));
                                switch (errorResponse.Headers[HttpResponseHeader.ContentEncoding])
                                {
                                    case "deflate":
                                        using (DeflateStream df = new DeflateStream(errorResponse.GetResponseStream(), CompressionMode.Decompress, leaveOpen: true))
                                            result = (TResult)serializer.DeSerialize(df);
                                        break;

                                    case "gzip":
                                        using (GZipStream df = new GZipStream(errorResponse.GetResponseStream(), CompressionMode.Decompress, leaveOpen: true))
                                            result = (TResult)serializer.DeSerialize(df);
                                        break;
                                    case "bzip2":
                                        using (var bzs = new BZip2Stream(errorResponse.GetResponseStream(), CompressionMode.Decompress, leaveOpen: true))
                                            result = (TResult)serializer.DeSerialize(bzs);
                                        break;
                                    case "lzma":
                                        using (var lzmas = new LZipStream(errorResponse.GetResponseStream(), CompressionMode.Decompress, leaveOpen: true))
                                            result = (TResult)serializer.DeSerialize(lzmas);

                                        break;
                                    default:
                                        result = (TResult)serializer.DeSerialize(errorResponse.GetResponseStream());
                                        break;
                                }
                            }
                            catch (Exception dse)
                            {
                                this.traceSource.TraceEvent(TraceEventType.Error, 0, "Could not de-serialize error response! {0}", dse);
                            }

                            switch (errorResponse.StatusCode)
                            {
                                case HttpStatusCode.Unauthorized: // Validate the response
                                    if (this.ValidateResponse(errorResponse) != ServiceClientErrorType.Valid)
                                        throw new RestClientException<TResult>(
                                            result,
                                            e,
                                            e.Status,
                                            e.Response);

                                    break;

                                default:
                                    throw new RestClientException<TResult>(
                                        result,
                                        e,
                                        e.Status,
                                        e.Response);
                            }
                            break;

                        default:
                            throw;
                    }
                }
            }

            responseHeaders = new WebHeaderCollection();
            return default(TResult);
        }
    }
}