﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MARC.HI.EHRS.CR.Messaging.HL7;
using MARC.HI.EHRS.SVC.Core.Services;
using NHapi.Base.Model;
using NHapi.Model.V25.Segment;
using NHapi.Model.V25.Message;
using MARC.Everest.Connectors;
using MARC.HI.EHRS.SVC.Core.DataTypes;
using MARC.HI.EHRS.CR.Messaging.HL7.TransportProtocol;

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
                    case "Q22":
                        response = HandlePdqQuery(e.Message as QBP_Q21, e);
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
        /// Handle the pdq query
        /// </summary>
        private IMessage HandlePdqQuery(QBP_Q21 request, Hl7MessageReceivedEventArgs evt)
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
            DataUtil dataUtil = new DataUtil() { Context = this.Context };
            // Construct appropriate audit
            AuditData audit = null;

            try
            {

                // Create Query Data
                ComponentUtility cu = new ComponentUtility() { Context = this.Context };
                DeComponentUtility dcu = new DeComponentUtility() { Context = this.Context };
                var data = cu.CreateQueryComponentsPdq(request, dtls);
                if (data.Equals(QueryData.Empty))
                    throw new InvalidOperationException(locale.GetString("MSGE00A"));

                QueryResultData result = dataUtil.Query(data, dtls);
                audit = dataUtil.CreateAuditData("ITI-21", ActionType.Execute, OutcomeIndicator.Success, evt, result);

                // Now process the result
                response = dcu.CreateRSP_K21(result, dtls);
                MessageUtil.UpdateMSH(new NHapi.Base.Util.Terser(response), request, config);

            }
            catch (Exception e)
            {
                if (!dtls.Exists(o => o.Message == e.Message || o.Exception == e))
                    dtls.Add(new ResultDetail(ResultDetailType.Error, e.Message, e));
                response = MessageUtil.CreateNack(request, dtls, this.Context);
                audit = dataUtil.CreateAuditData("ITI-21", ActionType.Execute, OutcomeIndicator.EpicFail, evt, QueryResultData.Empty);
            }
            finally
            {
                IAuditorService auditSvc = this.Context.GetService(typeof(IAuditorService)) as IAuditorService;
                if (auditSvc != null)
                    auditSvc.SendAudit(audit);
            }

            return response;
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