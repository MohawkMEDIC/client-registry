using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MARC.HI.EHRS.CR.Messaging.HL7;
using MARC.HI.EHRS.SVC.Core.Services;

namespace MARC.HI.EHRS.CR.Messaging.PixPdqv2
{
    /// <summary>
    /// PDQ Handler
    /// </summary>
    public class PdqHandler : IHL7MessageHandler
    {
        #region IHL7MessageHandler Members

        /// <summary>
        /// Handle HL7 message
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        public NHapi.Base.Model.IMessage HandleMessage(HL7.TransportProtocol.Hl7MessageReceivedEventArgs e)
        {
            throw new NotImplementedException();
        }

        #endregion



        #region IUsesHostContext Members

        /// <summary>
        /// Get or sets the context
        /// </summary>
        public SVC.Core.HostContext Context
        {
            get;
            set;
        }

        #endregion
    }
}
