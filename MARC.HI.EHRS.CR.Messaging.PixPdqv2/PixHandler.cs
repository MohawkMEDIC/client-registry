using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MARC.HI.EHRS.CR.Messaging.HL7;
using NHapi.Base.Model;
using NHapi.Model.V25.Segment;
using NHapi.Model.V25.Message;
using MARC.HI.EHRS.SVC.Core.Services;
using MARC.Everest.Connectors;

namespace MARC.HI.EHRS.CR.Messaging.PixPdqv2
{
    /// <summary>
    /// PIX Handler
    /// </summary>
    public class PixHandler : IHL7MessageHandler
    {
        #region IHL7MessageHandler Members

        /// <summary>
        /// Handle a received message
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        public NHapi.Base.Model.IMessage HandleMessage(HL7.TransportProtocol.Hl7MessageReceivedEventArgs e)
        {
            ISystemConfigurationService config = this.Context.GetService(typeof(ISystemConfigurationService)) as ISystemConfigurationService;
            ILocalizationService locale = this.Context.GetService(typeof(ILocalizationService)) as ILocalizationService;

            // Get the message type
            IMessage response = null;
            if (e.Message.Version == "2.5")
            {
                // Get the MSH segment
                var msh = e.Message.GetStructure("MSH") as MSH;
                switch (msh.MessageType.TriggerEvent.Value)
                {
                    case "Q23":
                        response = HandlePixQuery(e.Message as QBP_Q21);
                        break;
                    default:
                        response = MessageUtil.CreateNack(e.Message, "AR", "201", locale.GetString("HL7201"), config);
                        break;
                }
            }

            // response still null?
            if (response == null)
                response = MessageUtil.CreateNack(e.Message, "AR", "203", locale.GetString("HL7203"), config);
            return response;
        }


        /// <summary>
        /// Handle a PIX query
        /// </summary>
        private IMessage HandlePixQuery(QBP_Q21 request)
        {
            // Get config
            var config = this.Context.GetService(typeof(ISystemConfigurationService)) as ISystemConfigurationService;
            // Create a details array
            List<IResultDetail> dtls = new List<IResultDetail>();

            // Validate the inbound message
            MessageUtil.Validate((IMessage)request, config, dtls, this.Context);

            // Control 
            if (request == null)
                return null;

            return MessageUtil.CreateNack(request, dtls, this.Context);

        }

        #endregion

        #region IUsesHostContext Members

        /// <summary>
        /// Gets or sets the context
        /// </summary>
        public SVC.Core.HostContext Context
        {
            get;
            set;
        }

        #endregion
    }
}
