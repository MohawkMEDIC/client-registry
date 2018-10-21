﻿/**
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
using NHapi.Base.Model;
using NHapi.Base.Parser;
using System.Net.Sockets;
using System.Diagnostics;
using NHapi.Base;
using System.Security.Cryptography.X509Certificates;
using System.IO;
using System.Net.Security;

namespace MARC.HI.EHRS.CR.Notification.PixPdq
{
    /// <summary>
    /// MLLP Message Sender
    /// </summary>
    public class MllpMessageSender
    {

        // Endpoint
        private Uri m_endpoint = null;
        private X509Certificate2 m_clientCert = null;
        private X509Certificate2 m_serverCertChain = null;

        /// <summary>
        /// Creates a new message sender
        /// </summary>
        /// <param name="endpoint">The endpoint in the form : llp://ipaddress:port</param>
        public MllpMessageSender(Uri endpoint, X509Certificate2 clientCert, X509Certificate2 serverCertChain)
        {
            this.m_endpoint = endpoint;
            this.m_clientCert = clientCert;
            this.m_serverCertChain = serverCertChain;
        }

        /// <summary>
        /// Certificate selection callback
        /// </summary>
        public X509Certificate CertificateSelectionCallback(object sender, string targetHost, X509CertificateCollection localCertificates, X509Certificate remoteCertificate, string[] acceptableIssuers)
        {
            return this.m_clientCert;
        }

        /// <summary>
        /// Validation for certificates
        /// </summary>
        private bool RemoteCertificateValidation(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {

#if DEBUG
            if (certificate != null)
                Trace.TraceInformation("Received client certificate with subject {0}", certificate.Subject);
            if (chain != null)
            {
                Trace.TraceInformation("Client certificate is chained with {0}", chain.ChainElements.Count);

                foreach (var el in chain.ChainElements)
                    Trace.TraceInformation("\tChain Element : {0}", el.Certificate.Subject);
            }
            if (sslPolicyErrors != SslPolicyErrors.None)
            {
                Trace.TraceError("SSL Policy Error : {0}", sslPolicyErrors);

            }

#endif

            // First Validate the chain

            if (certificate == null || chain == null)
                return this.m_serverCertChain == null;
            else
            {

                bool isValid = false;
                foreach (var cer in chain.ChainElements)
                    if (cer.Certificate.Thumbprint == this.m_serverCertChain.Thumbprint)
                        isValid = true;
                if (!isValid)
                    Trace.TraceError("Certification authority from the supplied certificate doesn't match the expected thumbprint of the CA");
                foreach (var stat in chain.ChainStatus)
                    Trace.TraceWarning("Certificate chain validation error: {0}", stat.StatusInformation);
                //isValid &= chain.ChainStatus.Length == 0;
                return isValid;
            }
        }


        /// <summary>
        /// Send a message and receive the message
        /// </summary>
        public IMessage SendAndReceive(IMessage message)
        {
            
            // Encode the message
            var parser = new PipeParser();
            string strMessage = String.Empty;

            try
            {
                strMessage = parser.Encode(message);
                Console.WriteLine(strMessage);
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.ToString());
                throw new HL7Exception(e.Message);
            }

            // Open a TCP port
            using (TcpClient client = new TcpClient(AddressFamily.InterNetwork))
            {

                try
                {
                    // Connect on the socket
                    client.Connect(this.m_endpoint.Host, this.m_endpoint.Port);
                    // Get the stream
                    using (Stream stream = client.GetStream() )
                    {
                        Stream realStream = stream;

                        if (this.m_clientCert != null)
                        {
                            realStream = new SslStream(stream, false, new RemoteCertificateValidationCallback(this.RemoteCertificateValidation));
                            X509CertificateCollection collection = new X509CertificateCollection() {
                                this.m_clientCert
                            };
                            (realStream as SslStream).AuthenticateAsClient(this.m_endpoint.ToString(), collection, System.Security.Authentication.SslProtocols.Tls, true);
                        }


                        // Write message in UTF8 encoding
#if DEBUG
                        Trace.TraceInformation("Sending message to {0}: \r\n{1}", this.m_endpoint, strMessage);
#endif
                        byte[] buffer = new byte[System.Text.Encoding.UTF8.GetByteCount(strMessage) + 3];
                        buffer[0] = 0x0b;
                        Array.Copy(System.Text.Encoding.UTF8.GetBytes(strMessage), 0, buffer, 1, buffer.Length - 3);
                        buffer[buffer.Length - 2] = 0x1c;
                        buffer[buffer.Length - 1] = 0x0d;

                        realStream.Write(buffer, 0, buffer.Length);
                        // Write end message
                        realStream.Flush(); // Ensure all bytes get sent down the wire

                        // Now read the response
                        StringBuilder response = new StringBuilder();
                        buffer = new byte[1024];
                        while (!buffer.Contains((byte)0x1c)) // HACK: Keep reading until the buffer has the FS character
                        {
                            int br = realStream.Read(buffer, 0, 1024);

                            int ofs = 0;
                            if (buffer[ofs] == '\v')
                            {
                                ofs = 1;
                                br = br - 1;
                            }
                            response.Append(System.Text.Encoding.UTF8.GetString(buffer, ofs, br));
                        }

                        // Parse the response
#if DEBUG
                        Trace.TraceInformation("Received message from {0}: \r\n{1}", this.m_endpoint, response);
#endif

                        return parser.Parse(response.ToString());
                    }

                }
                catch (Exception e)
                {
                    Debug.WriteLine(e.ToString());
                    throw;
                }
            }

        }


    }
}
