/**
 * Copyright 2013-2013 Mohawk College of Applied Arts and Technology
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
 * Date: 6-2-2013
 */

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
using System.Diagnostics;
using MARC.HI.EHRS.SVC.Core.DataTypes;
using MARC.HI.EHRS.CR.Messaging.HL7.TransportProtocol;
using MARC.HI.EHRS.CR.Core.Services;

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
            try
            {

                if (e.Message.Version == "2.5" || e.Message.Version == "2.3.1")
                {
                    // Get the MSH segment
                    var terser = new Terser(e.Message);
                    var trigger = terser.Get("/MSH-9-2");
                    switch (trigger)
                    {
                        case "Q23":
                            if(e.Message is NHapi.Model.V25.Message.QBP_Q21)
                                response = HandlePixQuery(e.Message as NHapi.Model.V25.Message.QBP_Q21, e);
                            else
                                response = MessageUtil.CreateNack(e.Message, "AR", "200", locale.GetString("MSGE074"), config);
                            break;
                        case "A01":
                        case "A04":
                        case "A05":
                            if(e.Message is NHapi.Model.V231.Message.ADT_A01)
                                response = HandlePixAdmit(e.Message as NHapi.Model.V231.Message.ADT_A01, e);
                            else
                                response = MessageUtil.CreateNack(e.Message, "AR", "200", locale.GetString("MSGE074"), config);
                            break;
                        case "A08":
                            if(e.Message is NHapi.Model.V231.Message.ADT_A01)
                                response = HandlePixUpdate(e.Message as NHapi.Model.V231.Message.ADT_A01, e);
                            else
                                response = MessageUtil.CreateNack(e.Message, "AR", "200", locale.GetString("MSGE074"), config);
                            break;
                        default:
                            response = MessageUtil.CreateNack(e.Message, "AR", "201", locale.GetString("HL7201"), config);
                            Trace.TraceError("{0} is not a supported trigger", trigger);
                            break;
                    }
                }

                // response still null?
                if (response == null)
                    response = MessageUtil.CreateNack(e.Message, "AR", "203", locale.GetString("HL7203"), config);
            }
            catch (Exception ex)
            {
                Trace.TraceError(ex.ToString());
                response = MessageUtil.CreateNack(e.Message, "AR", "207", ex.Message, config);
            }

            return response;
        }

        /// <summary>
        /// Handle PIX update
        /// </summary>
        private IMessage HandlePixUpdate(NHapi.Model.V231.Message.ADT_A01 request, Hl7MessageReceivedEventArgs evt)
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
                var data = cu.CreateComponents(request, dtls);
                if (data == null)
                    throw new InvalidOperationException(locale.GetString("MSGE00A"));

                var vid = dataUtil.Update(data, dtls, request.MSH.ProcessingID.ProcessingID.Value == "P" ? DataPersistenceMode.Production : DataPersistenceMode.Debugging);
                audit = dataUtil.CreateAuditData("ITI-8", vid.UpdateMode == UpdateModeType.Update ? ActionType.Update : ActionType.Create, OutcomeIndicator.Success, evt, new List<VersionedDomainIdentifier>() { vid });

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
                Trace.TraceError(e.ToString());
                if (!dtls.Exists(o => o.Message == e.Message || o.Exception == e))
                    dtls.Add(new ResultDetail(ResultDetailType.Error, e.Message, e));
                response = MessageUtil.CreateNack(request, dtls, this.Context);
                audit = dataUtil.CreateAuditData("ITI-8", ActionType.Create, OutcomeIndicator.EpicFail, evt, QueryResultData.Empty);
            }
            finally
            {
                IAuditorService auditSvc = this.Context.GetService(typeof(IAuditorService)) as IAuditorService;
                if (auditSvc != null)
                    auditSvc.SendAudit(audit);
            }
            return response;
        }

        /// <summary>
        /// Handle a PIX admission
        /// </summary>
        private IMessage HandlePixAdmit(NHapi.Model.V231.Message.ADT_A01 request, Hl7MessageReceivedEventArgs evt)
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
                var data = cu.CreateComponents(request, dtls);
                if (data == null)
                    throw new InvalidOperationException(locale.GetString("MSGE00A"));

                var vid = dataUtil.Register(data, dtls, request.MSH.ProcessingID.ProcessingID.Value == "P" ? DataPersistenceMode.Production : DataPersistenceMode.Debugging);

                if (vid == null)
                    throw new InvalidOperationException(locale.GetString("DTPE001"));
              
                audit = dataUtil.CreateAuditData("ITI-8", vid.UpdateMode == UpdateModeType.Update ? ActionType.Update : ActionType.Create, OutcomeIndicator.Success, evt, new List<VersionedDomainIdentifier>() { vid });

                // Now process the result
                response = MessageUtil.CreateNack(request, dtls, this.Context);
                MessageUtil.UpdateMSH(new NHapi.Base.Util.Terser(response), request, config);
                (response as NHapi.Model.V231.Message.ACK).MSH.MessageType.TriggerEvent.Value = request.MSH.MessageType.TriggerEvent.Value;
                (response as NHapi.Model.V231.Message.ACK).MSH.MessageType.MessageType.Value = "ACK";
            }
            catch (Exception e)
            {
                Trace.TraceError(e.ToString());
                if (!dtls.Exists(o => o.Message == e.Message || o.Exception == e))
                    dtls.Add(new ResultDetail(ResultDetailType.Error, e.Message, e));
                response = MessageUtil.CreateNack(request, dtls, this.Context);
                audit = dataUtil.CreateAuditData("ITI-8", ActionType.Create, OutcomeIndicator.EpicFail, evt, QueryResultData.Empty);
            }
            finally
            {
                IAuditorService auditSvc = this.Context.GetService(typeof(IAuditorService)) as IAuditorService;
                if (auditSvc != null)
                    auditSvc.SendAudit(audit);
            }
            return response;
        }


        /// <summary>
        /// Handle a PIX query
        /// </summary>
        private IMessage HandlePixQuery(NHapi.Model.V25.Message.QBP_Q21 request, Hl7MessageReceivedEventArgs evt)
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

            // Construct appropriate audit
            AuditData audit = null;

            // Data controller
            DataUtil dataUtil = new DataUtil() { Context = this.Context };

            try
            {

                // Create Query Data
                ComponentUtility cu = new ComponentUtility() { Context = this.Context };
                DeComponentUtility dcu = new DeComponentUtility() { Context = this.Context };
                var data = cu.CreateQueryComponents(request, dtls);
                if (data.Equals(QueryData.Empty))
                    throw new InvalidOperationException(locale.GetString("MSGE00A"));

                
                QueryResultData result = dataUtil.Query(data, dtls);
                audit = dataUtil.CreateAuditData("ITI-9", ActionType.Execute, OutcomeIndicator.Success, evt, result);

                // Now process the result
                response = dcu.CreateRSP_K23(result, dtls);
                MessageUtil.UpdateMSH(new NHapi.Base.Util.Terser(response), request, config);
            }
            catch (Exception e)
            {
                Trace.TraceError(e.ToString());
                if (!dtls.Exists(o => o.Message == e.Message || o.Exception == e))
                    dtls.Add(new ResultDetail(ResultDetailType.Error, e.Message, e));
                response = MessageUtil.CreateNack(request, dtls, this.Context);
                audit = dataUtil.CreateAuditData("ITI-9", ActionType.Execute, OutcomeIndicator.EpicFail, evt, QueryResultData.Empty);
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
        /// Gets or sets the context
        /// </summary>
        public IServiceProvider Context
        {
            get;
            set;
        }

        #endregion
    }
}
