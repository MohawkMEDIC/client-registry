﻿/**
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
 * Date: 13-8-2012
 */

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
using NHapi.Base.Model;

namespace MARC.HI.EHRS.CR.Messaging.HL7.TransportProtocol
{
    /// <summary>
    /// HL7 llp transport
    /// </summary>
    public class LlpTransport : ITransportProtocol
    {
        #region ITransportProtocol Members

        // Timeout
        protected TimeSpan m_timeout;

        // The socket
        protected TcpListener m_listener;

        // Will run while true
        protected bool m_run = true;

        /// <summary>
        /// Gets the name of the protocol
        /// </summary>
        public virtual string ProtocolName
        {
            get { return "llp"; }
        }

        /// <summary>
        /// Start the transport
        /// </summary>
        public virtual void Start(IPEndPoint bind, ServiceHandler handler)
        {

            this.m_timeout = handler.Definition.ReceiveTimeout;
            this.m_listener = new TcpListener(bind);
            this.m_listener.Start();
            Trace.TraceInformation("LLP Transport bound to {0}", bind);
            
            while (m_run) // run the service
            {
                var client = this.m_listener.AcceptTcpClient();
                Thread clientThread = new Thread(OnReceiveMessage);
                clientThread.IsBackground = true;
                clientThread.Start(client);
                
            }
        }

        /// <summary>
        /// Receive and process message
        /// </summary>
        protected virtual void OnReceiveMessage(object client)
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

                    // Standard stream stuff, read until the stream is exhausted
                    StringBuilder messageData = new StringBuilder();
                    byte[] buffer = new byte[1024];
                    bool receivedEOF = false;
                    
                    while (!receivedEOF)
                    {
                        int br = stream.Read(buffer, 0, 1024);
                        messageData.Append(System.Text.Encoding.UTF8.GetString(buffer, 0, br));
                        // HACK: should look for FSCR but ... meh
                        receivedEOF = Array.Exists(buffer, o => o == 0x1c);
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
                        stream.Write(new byte[] { 0xb }, 0, 1); // header
                        if (messageArgs != null && messageArgs.Response != null)
                        {
                            // Since nHAPI only emits a string we just send that along the stream
                            writer.Write(parser.Encode(messageArgs.Response));
                            writer.Flush();
                        }
                        stream.Write(new byte[] { 0x1c, 0x0d }, 0, 2); // Finish the stream with FSCR
                        lastReceive = DateTime.Now; // Update the last receive time so the timeout function works 
                    }
                }
            }
            catch (Exception e)
            {
                Trace.TraceError(e.ToString());

                
            }
            finally
            {
                stream.Close();
                tcpClient.Close();
            }
        }

        /// <summary>
        /// Message received
        /// </summary>
        /// <param name="message"></param>
        protected void OnMessageReceived(Hl7MessageReceivedEventArgs messageArgs)
        {
            if (this.MessageReceived != null)
                this.MessageReceived(this, messageArgs);
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
