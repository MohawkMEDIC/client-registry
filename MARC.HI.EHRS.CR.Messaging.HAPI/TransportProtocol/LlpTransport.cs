using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using NHapi.Base.validation.impl;
using System.IO;
using System.Diagnostics;
using System.Threading;

namespace MARC.HI.EHRS.CR.Messaging.HL7.TransportProtocol
{
    /// <summary>
    /// HL7 llp transport
    /// </summary>
    public class LlpTransport : ITransportProtocol
    {
        #region ITransportProtocol Members

        // Timeout
        private TimeSpan m_timeout;

        // The socket
        private TcpListener m_listener;

        // Will run while true
        private bool m_run = true;

        /// <summary>
        /// Gets the name of the protocol
        /// </summary>
        public string ProtocolName
        {
            get { return "llp"; }
        }

        /// <summary>
        /// Start the transport
        /// </summary>
        public void Start(IPEndPoint bind, ServiceHandler handler)
        {

            this.m_timeout = handler.Definition.ReceiveTimeout;
            this.m_listener = new TcpListener(bind);
            this.m_listener.Start();
            Trace.TraceInformation("LLP Transport bound to {0}", bind);

            while (m_run) // run the service
            {
                var client = this.m_listener.AcceptTcpClient();
                Thread clientThread = new Thread(ReceiveMessage);
                clientThread.IsBackground = true;
                clientThread.Start(client);
                
            }
        }

        /// <summary>
        /// Receive and process message
        /// </summary>
        private void ReceiveMessage(object client)
        {
            TcpClient tcpClient = client as TcpClient;
            NetworkStream stream = tcpClient.GetStream();
            try
            {
                // Now read to a string
                NHapi.Base.Parser.PipeParser parser = new NHapi.Base.Parser.PipeParser();
                DateTime lastReceive = DateTime.Now;

                while (DateTime.Now.Subtract(lastReceive) < this.m_timeout)
                {

                    if (!stream.DataAvailable)
                    {
                        Thread.Sleep(10);
                        continue;
                    }

                    // Read LLP head byte
                    int llpByte = stream.ReadByte();
                    if (llpByte != 0x0B) // first byte must be HT
                        throw new InvalidOperationException("Invalid LLP First Byte");

                    StringBuilder messageData = new StringBuilder();
                    byte[] buffer = new byte[1024];
                    while (stream.DataAvailable)
                    {
                        int br = stream.Read(buffer, 0, 1024);
                        messageData.Append(Encoding.ASCII.GetString(buffer, 0, br));
                    }

                    var message = parser.Parse(messageData.ToString());
                    var localEp = tcpClient.Client.LocalEndPoint as IPEndPoint;
                    var remoteEp = tcpClient.Client.RemoteEndPoint as IPEndPoint;
                    Uri localEndpoint = new Uri(String.Format("llp://{0}:{1}", localEp.Address, localEp.Port));
                    Uri remoteEndpoint = new Uri(String.Format("llp://{0}:{1}", remoteEp.Address, remoteEp.Port));
                    var messageArgs = new Hl7MessageReceivedEventArgs(message, localEndpoint, remoteEndpoint, DateTime.Now);

                    this.MessageReceived(this, messageArgs);

                    // Send the response back
                    stream.WriteByte(0xb);
                    StreamWriter writer = new StreamWriter(stream);
                    if (messageArgs.Response != null)
                    {
                        writer.Write(parser.Encode(messageArgs.Response));
                        writer.Flush();
                    }
                    stream.Write(new byte[] { 0x1c, 0x0d }, 0, 2);
                    lastReceive = DateTime.Now;
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

        /// <summary>
        /// Stop the thread
        /// </summary>
        public void Stop()
        {
            this.m_run = false;
            this.m_listener.Stop();
            Trace.TraceInformation("LLP Transport stopped");
        }

        /// <summary>
        /// Message has been received
        /// </summary>
        public event EventHandler<Hl7MessageReceivedEventArgs> MessageReceived;

        #endregion
    }
}
