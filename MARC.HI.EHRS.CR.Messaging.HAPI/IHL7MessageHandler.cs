using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NHapi.Base.Model;
using MARC.HI.EHRS.CR.Messaging.HL7.TransportProtocol;
using MARC.HI.EHRS.SVC.Core.Services;

namespace MARC.HI.EHRS.CR.Messaging.HL7
{
    /// <summary>
    /// Handler for HL7 message
    /// </summary>
    public interface IHL7MessageHandler : IUsesHostContext
    {

        /// <summary>
        /// Handle a message
        /// </summary>
        IMessage HandleMessage(Hl7MessageReceivedEventArgs e);

    }
}
