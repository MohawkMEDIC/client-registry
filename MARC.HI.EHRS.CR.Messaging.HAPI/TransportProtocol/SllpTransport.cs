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
using System.Security;
using System.Security.Authentication;
using MARC.HI.EHRS.SVC.Core.DataTypes;
using MARC.HI.EHRS.SVC.Core.Services;
using System.ComponentModel;
using MARC.HI.EHRS.CR.Messaging.HL7.Configuration;
using System.Drawing.Design;

namespace MARC.HI.EHRS.CR.Messaging.HL7.TransportProtocol
{
    /// <summary>
    /// Secure LLP transport
    /// </summary>
    [Description("ER7 over Secure LLP")]
    public class SllpTransport : LlpTransport
    {

        /// <summary>
        /// SLLP configuration object
        /// </summary>
        public class SllpConfigurationObject 
        {
            /// <summary>
            /// Identifies the location of the server's certificate
            /// </summary>
            [Category("Server Certificate")]
            [Description("Identifies the location of the server's certificate")]
            public StoreLocation ServerCertificateLocation { get; set; }
            /// <summary>
            /// Identifies the store name of the server's certificate
            /// </summary>
            [Category("Server Certificate")]
            [Description("Identifies the store name of the server's certificate")]
            public StoreName ServerCertificateStore { get; set; }
            /// <summary>
            /// Identifies the certificate to be used
            /// </summary>
            [Category("Server Certificate")]
            [Description("Identifies the certificate to be used by the server")]
            [Editor(typeof(X509CertificateEditor), typeof(UITypeEditor))]
            [TypeConverter(typeof(ExpandableObjectConverter))]
            public X509Certificate2 ServerCertificate { get; set; }

            /// <summary>
            /// Identifies the location of the certificate which client certs should be issued from
            /// </summary>
            [Category("Trusted Client Certificate")]
            [Description("Identifies the location of a certificate used for client authentication")]
            public StoreLocation TrustedCaCertificateLocation { get; set; }
            /// <summary>
            /// Identifies the store name of the server's certificate
            /// </summary>
            [Category("Trusted Client Certificate")]
            [Description("Identifies the store of a certificate used for client authentication")]
            public StoreName TrustedCaCertificateStore { get; set; }
            /// <summary>
            /// Identifies the certificate to be used
            /// </summary>
            [Category("Trusted Client Certificate")]
            [Description("Identifies the certificate of the CA which clients must carry to be authenticated")]
            [Editor(typeof(X509CertificateEditor), typeof(UITypeEditor))]
            [TypeConverter(typeof(ExpandableObjectConverter))]
            public X509Certificate2 TrustedCaCertificate { get; set; }


            /// <summary>
            /// Enabling of the client cert negotiate
            /// </summary>
            [Description("When enabled, enforces client certificate negotiation")]
            public bool EnableClientCertNegotiation { get; set; }
        }

        // SLLP configuration object
        private SllpConfigurationObject m_configuration = new SllpConfigurationObject();
        
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
        /// Setup configuration 
        /// </summary>
        public override void SetupConfiguration(ServiceDefinition definition)
        {
            this.m_configuration = new SllpConfigurationObject();
            KeyValuePair<String, String> certThumb = definition.Attributes.Find(o => o.Key == "x509.cert"),
                certLocation = definition.Attributes.Find(o => o.Key == "x509.location"),
                certStore = definition.Attributes.Find(o => o.Key == "x509.store"),
                caCertThumb = definition.Attributes.Find(o => o.Key == "client.cacert"),
                caCertLocation = definition.Attributes.Find(o => o.Key == "client.calocation"),
                caCertStore = definition.Attributes.Find(o => o.Key == "client.castore");

            // Now setup the object 
            this.m_configuration = new SllpConfigurationObject()
            {
                EnableClientCertNegotiation = caCertThumb.Value != null,
                ServerCertificateLocation = (StoreLocation)Enum.Parse(typeof(StoreLocation), certLocation.Value ?? "LocalMachine"),
                ServerCertificateStore = (StoreName)Enum.Parse(typeof(StoreName), certStore.Value ?? "My"),
                TrustedCaCertificateLocation = (StoreLocation)Enum.Parse(typeof(StoreLocation), caCertLocation.Value ?? "LocalMachine"),
                TrustedCaCertificateStore = (StoreName)Enum.Parse(typeof(StoreName), caCertStore.Value ?? "Root")
            };

            // Now get the certificates
            if (!String.IsNullOrEmpty(certThumb.Value))
                this.m_configuration.ServerCertificate = this.GetCertificateFromStore(certThumb.Value, this.m_configuration.ServerCertificateLocation, this.m_configuration.ServerCertificateStore);
            if (this.m_configuration.EnableClientCertNegotiation)
            {
                if (!String.IsNullOrEmpty(caCertThumb.Value))
                    this.m_configuration.TrustedCaCertificate = this.GetCertificateFromStore(caCertThumb.Value, this.m_configuration.TrustedCaCertificateLocation, this.m_configuration.TrustedCaCertificateStore);
            }

            
        }

        /// <summary>
        /// Get certificate from store
        /// </summary>
        private X509Certificate2 GetCertificateFromStore(string certThumb, StoreLocation storeLocation, StoreName storeName)
        {
            X509Store store = new X509Store(storeName, storeLocation);
            try
            {
                store.Open(OpenFlags.ReadOnly | OpenFlags.OpenExistingOnly);
                var cert = store.Certificates.Find(X509FindType.FindByThumbprint, certThumb, true);
                if (cert.Count == 0)
                    throw new InvalidOperationException("Could not find certificate");
                return cert[0];
            }
            catch (Exception e)
            {
                Trace.TraceError("Could get certificate {0} from store {1}. Error was: {2}", certThumb, storeName, e.ToString());
                throw;
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
            this.SetupConfiguration(handler.Definition);
            if (this.m_configuration.ServerCertificate == null)
                throw new InvalidOperationException("Cannot start the secure LLP listener without a server certificate");

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

            // First Validate the chain
            
            if (certificate == null || chain == null)
                return !this.m_configuration.EnableClientCertNegotiation;
            else
            {

                bool isValid = false;
                foreach (var cer in chain.ChainElements)
                    if (cer.Certificate.Thumbprint == this.m_configuration.TrustedCaCertificate.Thumbprint)
                        isValid = true;
                if (!isValid)
                    Trace.TraceError("Certification authority from the supplied certificate doesn't match the expected thumbprint of the CA");
                foreach (var stat in chain.ChainStatus)
                    Trace.TraceWarning("Certificate chain validation error: {0}", stat.StatusInformation);
                isValid &= chain.ChainStatus.Length == 0;
                return isValid;
            }
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
                stream.AuthenticateAsServer(this.m_configuration.ServerCertificate, this.m_configuration.EnableClientCertNegotiation, System.Security.Authentication.SslProtocols.Tls, true);
                
                // Now read to a string
                NHapi.Base.Parser.PipeParser parser = new NHapi.Base.Parser.PipeParser();

                DateTime lastReceive = DateTime.Now;

                while (DateTime.Now.Subtract(lastReceive) < this.m_timeout)
                {

                    int llpByte = 0;
                    // Read LLP head byte
                    try
                    {
                        llpByte = stream.ReadByte();
                    }
                    catch (SocketException)
                    {
                        break;
                    }

                    if (llpByte != START_TX) // first byte must be HT
                        throw new InvalidOperationException("Invalid LLP First Byte");

                    // Standard stream stuff, read until the stream is exhausted
                    StringBuilder messageData = new StringBuilder();
                    byte[] buffer = new byte[1024];
                    bool receivedEOF = false, scanForCr = false;

                    while (!receivedEOF)
                    {
                        int br = stream.Read(buffer, 0, 1024);
                        messageData.Append(System.Text.Encoding.UTF8.GetString(buffer, 0, br));

                        // Need to check for CR?
                        if (scanForCr)
                            receivedEOF = buffer[0] == END_TXNL;
                        else
                        {
                            // Look for FS
                            int fsPos = Array.IndexOf(buffer, END_TX);

                            if (fsPos == -1) // not found
                                continue;
                            else if (fsPos < buffer.Length - 1) // more room to read
                                receivedEOF = buffer[fsPos + 1] == END_TXNL;
                            else
                                scanForCr = true; // Cannot check the end of message for CR because there is no more room in the message buffer
                            // so need to check on the next loop
                        }

                        // TODO: Timeout for this
                    }


                    // Use the nHAPI parser to process the data
                    Hl7MessageReceivedEventArgs messageArgs = null;
                    try
                    {

                        var message = parser.Parse(messageData.ToString());

                        // Setup local and remote receive endpoint data for auditing
                        var localEp = tcpClient.Client.LocalEndPoint as IPEndPoint;
                        var remoteEp = tcpClient.Client.RemoteEndPoint as IPEndPoint;
                        Uri localEndpoint = new Uri(String.Format("llp://{0}:{1}", localEp.Address, localEp.Port));
                        Uri remoteEndpoint = new Uri(String.Format("llp://{0}:{1}", remoteEp.Address, remoteEp.Port));
                        messageArgs = new Hl7MessageReceivedEventArgs(message, localEndpoint, remoteEndpoint, DateTime.Now);

                        // Call any bound event handlers that there is a message available
                        OnMessageReceived(messageArgs);
                    }
                    finally
                    {
                        // Send the response back
                        StreamWriter writer = new StreamWriter(stream);
                        stream.Write(new byte[] { START_TX }, 0, 1); // header
                        if (messageArgs != null && messageArgs.Response != null)
                        {
                            // Since nHAPI only emits a string we just send that along the stream
                            writer.Write(parser.Encode(messageArgs.Response));
                            writer.Flush();
                        }
                        stream.Write(new byte[] { END_TX, END_TXNL }, 0, 2); // Finish the stream with FSCR
                        stream.Flush();
                        lastReceive = DateTime.Now; // Update the last receive time so the timeout function works 
                    }
                }
            }
            catch (AuthenticationException e)
            {
                // Trace authentication error
                AuditData ad = new AuditData(
                    DateTime.Now,
                    ActionType.Execute,
                    OutcomeIndicator.MinorFail,
                    EventIdentifierType.ApplicationActivity,
                    new CodeValue("110113", "DCM") { DisplayName = "Security Alert" }
                );
                ad.Actors = new List<AuditActorData>() {
                    new AuditActorData()
                    {
                        NetworkAccessPointId = Dns.GetHostName(),
                        NetworkAccessPointType = SVC.Core.DataTypes.NetworkAccessPointType.MachineName,
                        UserName = Environment.UserName,
                        UserIsRequestor = false
                    },
                    new AuditActorData()
                    {   
                        NetworkAccessPointId = String.Format("sllp://{0}", tcpClient.Client.RemoteEndPoint.ToString()),
                        NetworkAccessPointType = NetworkAccessPointType.MachineName,
                        UserIsRequestor = true
                    }
                };
                ad.AuditableObjects = new List<AuditableObject>()
                {
                    new AuditableObject() {
                        Type = AuditableObjectType.SystemObject,
                        Role = AuditableObjectRole.SecurityResource,
                        IDTypeCode = AuditableObjectIdType.Uri,
                        ObjectId = String.Format("sllp://{0}", this.m_listener.LocalEndpoint)
                    }
                };

                var auditService = ApplicationContext.CurrentContext.GetService(typeof(IAuditorService)) as IAuditorService;
                if (auditService != null)
                    auditService.SendAudit(ad);
                Trace.TraceError(e.ToString());
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

        /// <summary>
        /// Configuration object override
        /// </summary>
        public override object ConfigurationObject
        {
            get
            {
                return this.m_configuration;
            }
        }

        public override List<KeyValuePair<string, string>> SerializeConfiguration()
        {
            // REturn value setup
            var retVal = new List<KeyValuePair<string, string>>()
            {
                new KeyValuePair<String,String>("x509.cert", this.m_configuration.ServerCertificate.Thumbprint),
                new KeyValuePair<String,String>("x509.store", this.m_configuration.ServerCertificateStore.ToString()),
                new KeyValuePair<String,String>("x509.location", this.m_configuration.ServerCertificateLocation.ToString())
            };

            if (this.m_configuration.EnableClientCertNegotiation)
                retVal.AddRange(new KeyValuePair<String, String>[] {
                    new KeyValuePair<String,String>("client.cacert", this.m_configuration.TrustedCaCertificate.Thumbprint),
                    new KeyValuePair<String,String>("client.castore", this.m_configuration.TrustedCaCertificateStore.ToString()),
                    new KeyValuePair<String,String>("client.calocation", this.m_configuration.TrustedCaCertificateLocation.ToString())
                });
            return retVal;
        }
    }
}
