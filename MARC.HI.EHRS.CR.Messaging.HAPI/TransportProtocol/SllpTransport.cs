/**
 * Copyright 2012-2013 Mohawk College of Applied Arts and Technology
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
 * Date: 18-9-2012
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Net;
using System.Diagnostics;
using System.Threading;
using System.IO;

namespace MARC.HI.EHRS.CR.Messaging.HL7.TransportProtocol
{
    /// <summary>
    /// Secure LLP transport
    /// </summary>
    public class SllpTransport : LlpTransport
    {

        // Certificate
        private X509Certificate2 m_certificate;

        // Client certs required
        private bool m_clientCertRequired;

        
        /// <summary>
        /// Protocol name
        /// </summary>
        public override string ProtocolName
        {
            get
            {
                return "sllp";
            }
        }

        /// <summary>
        /// Start the transport
        /// </summary>
        public override void Start(IPEndPoint bind, ServiceHandler handler)
        {

            this.m_timeout = handler.Definition.ReceiveTimeout;
            this.m_listener = new TcpListener(bind);
            this.m_listener.Start();
            Trace.TraceInformation("SLLP Transport bound to {0}", bind);

            // Setup certificate
            var certFile = handler.Definition.Attributes.Find(o => o.Key == "x509.cert").Value;
            if (String.IsNullOrEmpty(certFile))
                throw new InvalidOperationException("Cannot start secure LLP node with no certificate");

            if (File.Exists(certFile))
                this.m_certificate = new X509Certificate2(certFile);
            else
            {
                X509Store store = null;
                if (handler.Definition.Attributes.Exists(o => o.Key == "x509.store"))
                    store = new X509Store(handler.Definition.Attributes.Find(o => o.Key == "x509.store").Value);
                else
                    throw new InvalidOperationException("Must specify x509.store parameter!");
                try
                {
                    store.Open(OpenFlags.ReadOnly | OpenFlags.OpenExistingOnly);
                    this.m_certificate = store.Certificates.Find(X509FindType.FindByThumbprint, certFile, true)[0];

                }
                finally
                {
                    store.Close();
                }
            }

            // Client cert config
            if (handler.Definition.Attributes.Exists(o => o.Key == "client.cert" || o.Key == "client.castore"))
            {
                this.m_clientCertRequired = true;

            }

            while (m_run) // run the service
            {
                var client = this.m_listener.AcceptTcpClient();
                Thread clientThread = new Thread(OnReceiveMessage);
                clientThread.IsBackground = true;
                clientThread.Start(client);

            }
        }
        
        /// <summary>
        /// Validation for certificates
        /// </summary>
        private bool RemoteCertificateValidation(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            // TODO: Validate client certificates 
            return false;
        }

        /// <summary>
        /// Receive a message
        /// </summary>
        protected override void OnReceiveMessage(object client)
        {
            TcpClient tcpClient = client as TcpClient;
            SslStream stream = new SslStream(tcpClient.GetStream(), false, new RemoteCertificateValidationCallback(RemoteCertificateValidation));
            try
            {
                stream.AuthenticateAsServer(this.m_certificate, this.m_clientCertRequired, System.Security.Authentication.SslProtocols.Tls, true);

                // Now read to a string
                NHapi.Base.Parser.PipeParser parser = new NHapi.Base.Parser.PipeParser();
                DateTime lastReceive = DateTime.Now;

                while (DateTime.Now.Subtract(lastReceive) < this.m_timeout)
                {

                    // Standard stream stuff, read until the stream is exhausted
                    StringBuilder messageData = new StringBuilder();
                    byte[] buffer = new byte[1024];
                    int br = 0; // bytes read
                    do
                    {
                        br = stream.Read(buffer, 0, 1024);
                        messageData.Append(Encoding.ASCII.GetString(buffer, 0, br));
                    } while (br != 0);

                    // Read LLP head byte
                    int llpByte = messageData[0];
                    if (llpByte != 0x0B) // first byte must be HT
                        throw new InvalidOperationException("Invalid LLP First Byte");

                    // Use the nHAPI parser to process the data
                    var message = parser.Parse(messageData.ToString());

                    // Setup local and remote receive endpoint data for auditing
                    var localEp = tcpClient.Client.LocalEndPoint as IPEndPoint;
                    var remoteEp = tcpClient.Client.RemoteEndPoint as IPEndPoint;
                    Uri localEndpoint = new Uri(String.Format("sllp://{0}:{1}", localEp.Address, localEp.Port));
                    Uri remoteEndpoint = new Uri(String.Format("sllp://{0}:{1}", remoteEp.Address, remoteEp.Port));
                    var messageArgs = new Hl7MessageReceivedEventArgs(message, localEndpoint, remoteEndpoint, DateTime.Now);

                    // Call any bound event handlers that there is a message available
                    OnMessageReceived(messageArgs);

                    // Send the response back
                    stream.WriteByte(0xb); // header
                    StreamWriter writer = new StreamWriter(stream);
                    if (messageArgs.Response != null)
                    {
                        // Since nHAPI only emits a string we just send that along the stream
                        writer.Write(parser.Encode(messageArgs.Response));
                        writer.Flush();
                    }
                    stream.Write(new byte[] { 0x1c, 0x0d }, 0, 2); // Finish the stream with FSCR
                    lastReceive = DateTime.Now; // Update the last receive time so the timeout function works 
                }
            }
            catch (Exception e)
            {
                Trace.TraceError(e.ToString());
                // TODO: NACK
            }
            finally
            {
                stream.Close();
                tcpClient.Close();
            }
        }
    }
}
