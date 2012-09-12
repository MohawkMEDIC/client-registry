using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MARC.HI.EHRS.CR.Messaging.HL7;
using NHapi.Base.Model;
using MARC.HI.EHRS.SVC.Core.Services;
using MARC.Everest.Connectors;
using MARC.HI.EHRS.CR.Core.ComponentModel;
using NHapi.Base.Util;

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
            if (e.Message.Version == "2.5" || e.Message.Version == "2.3.1")
            {
                // Get the MSH segment
                var terser = new Terser(e.Message);
                var trigger = terser.Get("/MSH-9-2");
                switch (trigger)
                {
                    case "Q23":
                        response = HandlePixQuery(e.Message as NHapi.Model.V25.Message.QBP_Q21);
                        break;
                    case "A01":
                    case "A04":
                    case "A05":
                        response = HandlePixAdmit(e.Message as NHapi.Model.V231.Message.ADT_A01);
                        break;
                    case "A08":
                        response = HandlePixUpdate(e.Message as NHapi.Model.V231.Message.ADT_A01);
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

        private IMessage HandlePixUpdate(NHapi.Model.V231.Message.ADT_A01 aDT_A01)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Handle a PIX admission
        /// </summary>
        private IMessage HandlePixAdmit(NHapi.Model.V231.Message.ADT_A01 request)
        {
            // Get config
            var config = this.Context.GetService(typeof(ISystemConfigurationService)) as ISystemConfigurationService;
            var locale = this.Context.GetService(typeof(ILocalizationService)) as ILocalizationService;

            // Create a details array
            List<IResultDetail> dtls = new List<IResultDetail>();

            // Validate the inbound message
            MessageUtil.Validate((IMessage)request, config, dtls, this.Context);

            IMessage response = null;

            // Control 
            if (request == null)
                return null;

            // Data controller
            try
            {

                // Create Query Data
                ComponentUtility cu = new ComponentUtility() { Context = this.Context };
                DeComponentUtility dcu = new DeComponentUtility() { Context = this.Context };
                var data = cu.CreateComponents(request, dtls);
                if (data == null)
                    throw new InvalidOperationException(locale.GetString("MSGE00A"));

                DataUtil dataUtil = new DataUtil() { Context = this.Context };
                var vid = dataUtil.Register(data, dtls, request.MSH.ProcessingID.ProcessingID.Value == "P" ? DataPersistenceMode.Production : DataPersistenceMode.Debugging);

                if (vid == null)
                    throw new InvalidOperationException(locale.GetString("DTPE001"));

                // Now process the result
                response = MessageUtil.CreateNack(request, dtls, this.Context);
                MessageUtil.UpdateMSH(new NHapi.Base.Util.Terser(response), request, config);
                (response as NHapi.Model.V231.Message.ACK).MSH.MessageType.TriggerEvent.Value = request.MSH.MessageType.TriggerEvent.Value;
                (response as NHapi.Model.V231.Message.ACK).MSH.MessageType.MessageType.Value = "ACK";
            }
            catch (Exception e)
            {
                if (!dtls.Exists(o => o.Message == e.Message || o.Exception == e))
                    dtls.Add(new ResultDetail(ResultDetailType.Error, e.Message, e));
                response = MessageUtil.CreateNack(request, dtls, this.Context);
            }

            return response;
        }


        /// <summary>
        /// Handle a PIX query
        /// </summary>
        private IMessage HandlePixQuery(NHapi.Model.V25.Message.QBP_Q21 request)
        {
            // Get config
            var config = this.Context.GetService(typeof(ISystemConfigurationService)) as ISystemConfigurationService;
            var locale = this.Context.GetService(typeof(ILocalizationService)) as ILocalizationService;

            // Create a details array
            List<IResultDetail> dtls = new List<IResultDetail>();

            // Validate the inbound message
            MessageUtil.Validate((IMessage)request, config, dtls, this.Context);

            IMessage response = null;

            // Control 
            if (request == null)
                return null;

            // Data controller
            try
            {

                // Create Query Data
                ComponentUtility cu = new ComponentUtility() { Context = this.Context };
                DeComponentUtility dcu = new DeComponentUtility() { Context = this.Context };
                var data = cu.CreateQueryComponents(request, dtls);
                if (data.Equals(QueryData.Empty))
                    throw new InvalidOperationException(locale.GetString("MSGE00A"));

                DataUtil dataUtil = new DataUtil() { Context = this.Context };
                QueryResultData result = dataUtil.Query(data, dtls);

                // Now process the result
                response = dcu.CreateRSP_K23(result, dtls);
                MessageUtil.UpdateMSH(new NHapi.Base.Util.Terser(response), request, config);
            }
            catch (Exception e)
            {
                if (!dtls.Exists(o => o.Message == e.Message || o.Exception == e))
                    dtls.Add(new ResultDetail(ResultDetailType.Error, e.Message, e));
                response = MessageUtil.CreateNack(request, dtls, this.Context);
            }

            return response;

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
