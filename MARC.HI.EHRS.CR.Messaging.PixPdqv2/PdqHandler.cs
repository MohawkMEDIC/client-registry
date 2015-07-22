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
 * Date: 21-8-2012
 */

using System;
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
using System.Diagnostics;
using NHapi.Base.Util;
using MARC.HI.EHRS.CR.Core.Services;

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
            var dataService = this.Context.GetService(typeof(IClientRegistryDataService)) as IClientRegistryDataService;

            // Create a details array
            List<IResultDetail> dtls = new List<IResultDetail>();

            // Validate the inbound message
            MessageUtil.Validate((IMessage)request, config, dtls, this.Context);

            IMessage response = null;

            // Control 
            if (request == null)
                return null;


            // Data controller
            //DataUtil dataUtil = new DataUtil() { Context = this.Context };
            AuditUtil auditUtil = new AuditUtil() { Context = this.Context };
            // Construct appropriate audit
            AuditData audit = null;

            try
            {

                // Create Query Data
                ComponentUtility cu = new ComponentUtility() { Context = this.Context };
                DeComponentUtility dcu = new DeComponentUtility() { Context = this.Context };
                var data = cu.CreateQueryComponentsPdq(request, dtls);
                if (data == null)
                    throw new InvalidOperationException(locale.GetString("MSGE00A"));

                // Is this a continue or new query?
                RegistryQueryResult result = dataService.Query(data);

                audit = auditUtil.CreateAuditData("ITI-21", ActionType.Execute, OutcomeIndicator.Success, evt, result);

                // Now process the result
                response = dcu.CreateRSP_K21(result, data, dtls);
                //MessageUtil.CopyQPD((response as RSP_K21).QPD, request.QPD, data);
                MessageUtil.UpdateMSH(new NHapi.Base.Util.Terser(response), request, config);
                Terser ters = new Terser(response);
                ters.Set("/MSH-9-2", "K22");

            }
            catch (Exception e)
            {
                Trace.TraceError(e.ToString());


                if (!dtls.Exists(o => o.Message == e.Message || o.Exception == e))
                {
                    if(dtls.Count == 0)
                        dtls.Add(new ResultDetail(ResultDetailType.Error, e.Message, e));
                }
                // HACK: Only one error allowed in nHAPI for some reason : 
                // TODO: Fix NHapi
                dtls.RemoveAll(o => o.Type != ResultDetailType.Error);
                while (dtls.Count > 1)
                    dtls.RemoveAt(1);
                response = MessageUtil.CreateNack(request, dtls, this.Context, typeof(RSP_K21));
                
                Terser errTerser = new Terser(response);
                // HACK: Fix the generic ACK with a real ACK for this message
                errTerser.Set("/MSH-9-2", "K22");
                errTerser.Set("/MSH-9-3", "RSP_K21");
                errTerser.Set("/QAK-2", "AE");
                errTerser.Set("/MSA-1", "AE");
                errTerser.Set("/QAK-1", request.QPD.QueryTag.Value);
                audit = auditUtil.CreateAuditData("ITI-21", ActionType.Execute, OutcomeIndicator.EpicFail, evt, new List<VersionedDomainIdentifier>());
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
        public IServiceProvider Context
        {
            get;
            set;
        }

        #endregion
    }
}
