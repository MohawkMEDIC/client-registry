using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NHapi.Base.Model;
using System.Net;

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
        void Start(IPEndPoint bind);

        /// <summary>
        /// Stop listening
        /// </summary>
        void Stop();

        /// <summary>
        /// Message has been received
        /// </summary>
        event EventHandler<Hl7MessageReceivedEventArgs> MessageReceived;

    }
}
