using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MARC.HI.EHRS.CR.Messaging.HL7;
using NHapi.Base.Util;
using NHapi.Base.Model;
using MARC.HI.EHRS.SVC.Core.Services;

namespace MARC.HI.EHRS.CR.Messaging.PixPdqv2
{
    public class NotSupportedHandler : IHL7MessageHandler
    {
        #region IHL7MessageHandler Members

        /// <summary>
        /// Handle message 
        /// </summary>
        public NHapi.Base.Model.IMessage HandleMessage(HL7.TransportProtocol.Hl7MessageReceivedEventArgs e)
        {
            // Get the config service
            ISystemConfigurationService config = Context.GetService(typeof(ISystemConfigurationService)) as ISystemConfigurationService;
            return MessageUtil.CreateNack(e.Message, "AR", "200", "Unsupported message type", config);
            
        }

        #endregion

        #region IUsesHostContext Members

        /// <summary>
        /// Gets or sets the context of this handler
        /// </summary>
        public SVC.Core.HostContext Context
        {
            get;
            set;
        }

        #endregion
    }
}
