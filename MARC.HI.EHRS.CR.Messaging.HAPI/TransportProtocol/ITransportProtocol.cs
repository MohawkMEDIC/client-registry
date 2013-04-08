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
 * Date: 13-8-2012
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NHapi.Base.Model;
using System.Net;
using MARC.HI.EHRS.CR.Messaging.HL7.Configuration;

namespace MARC.HI.EHRS.CR.Messaging.HL7.TransportProtocol
{

    /// <summary>
    /// Event args 
    /// </summary>
    public class Hl7MessageReceivedEventArgs : EventArgs
    {

        /// <summary>
        /// Creates a new instance of the Hl7MessageReceivedEventArgs 
        /// </summary>
        public Hl7MessageReceivedEventArgs(IMessage message, Uri solicitorEp, Uri receiveEp, DateTime timestamp)
        {
            this.Message = message;
            this.SolicitorEndpoint = solicitorEp;
            this.ReceiveEndpoint = receiveEp;
            this.Timestamp = timestamp;
        }

        /// <summary>
        /// Gets the message that was received by the transport protocol
        /// </summary>
        public IMessage Message { get; private set; }

        /// <summary>
        /// Gets or sets the response message
        /// </summary>
        public IMessage Response { get; set; }

        /// <summary>
        /// The endpoint of the solicitor
        /// </summary>
        public Uri SolicitorEndpoint { get; private set; }

        /// <summary>
        /// The endpoint of the received message
        /// </summary>
        public Uri ReceiveEndpoint { get; private set; }

        /// <summary>
        /// The timestamp the message was received
        /// </summary>
        public DateTime Timestamp { get; private set; }
    }

    /// <summary>
    /// Transport protocol
    /// </summary>
    public interface ITransportProtocol
    {

        /// <summary>
        /// Gets the name of the protocol . Example "mllp", "tcp", etc..
        /// </summary>
        string ProtocolName { get; }

        /// <summary>
        /// Start the transport protocol
        /// </summary>
        void Start(IPEndPoint bind, ServiceHandler handler);

        /// <summary>
        /// Stop listening
        /// </summary>
        void Stop();

        /// <summary>
        /// Message has been received
        /// </summary>
        event EventHandler<Hl7MessageReceivedEventArgs> MessageReceived;

        /// <summary>
        /// Setup the configuration object
        /// </summary>
        void SetupConfiguration(ServiceDefinition definition);

        /// <summary>
        /// Serialize the configuration
        /// </summary>
        List<KeyValuePair<String, String>> SerializeConfiguration();

        /// <summary>
        /// Gets the extension attributes that are allowed on the receive endpoint
        /// </summary>
        Object ConfigurationObject { get; }
    }
}
