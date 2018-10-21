﻿/**
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
using NHapi.Model.V25.Message;
using NHapi.Base.Parser;
using MARC.HI.EHRS.CR.Core.Data;
using MARC.HI.EHRS.SVC.Core.ComponentModel;

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
                    Trace.TraceInformation("Message is of type {0} {1}", e.Message.GetType().FullName, trigger);

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
                            else if(e.Message is NHapi.Model.V231.Message.ADT_A08)
                                response = HandlePixUpdate(e.Message as NHapi.Model.V231.Message.ADT_A08, e);
                            else
                                response = MessageUtil.CreateNack(e.Message, "AR", "200", locale.GetString("MSGE074"), config);
                            break;
                        case "A40":
                            if (e.Message is NHapi.Model.V231.Message.ADT_A39)
                                response = HandlePixMerge(e.Message as NHapi.Model.V231.Message.ADT_A39, e);
                            else if (e.Message is NHapi.Model.V231.Message.ADT_A40)
                                response = HandlePixMerge(e.Message as NHapi.Model.V231.Message.ADT_A40, e);
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

        private IMessage HandlePixMerge(NHapi.Model.V231.Message.ADT_A40 aDT_A40, Hl7MessageReceivedEventArgs e)
        {
            ILocalizationService locale = this.Context.GetService(typeof(ILocalizationService)) as ILocalizationService;
            ISystemConfigurationService config = this.Context.GetService(typeof(ISystemConfigurationService)) as ISystemConfigurationService;

            PipeParser parser = new PipeParser();
            aDT_A40.MSH.MessageType.MessageStructure.Value = "ADT_A39";
            var message = parser.Parse(parser.Encode(aDT_A40));
            if (message is NHapi.Model.V231.Message.ADT_A39)
                return this.HandlePixMerge(message as NHapi.Model.V231.Message.ADT_A39, e);
            else
                return MessageUtil.CreateNack(e.Message, "AR", "200", locale.GetString("MSGE074"), config);
        }

        /// <summary>
        /// Handle PIX Update
        /// </summary>
        private IMessage HandlePixUpdate(NHapi.Model.V231.Message.ADT_A08 aDT_A08, Hl7MessageReceivedEventArgs e)
        {
            ILocalizationService locale = this.Context.GetService(typeof(ILocalizationService)) as ILocalizationService;
            ISystemConfigurationService config = this.Context.GetService(typeof(ISystemConfigurationService)) as ISystemConfigurationService;

            PipeParser parser = new PipeParser();
            aDT_A08.MSH.MessageType.MessageStructure.Value = "ADT_A01";
            var message = parser.Parse(parser.Encode(aDT_A08));
            if (message is NHapi.Model.V231.Message.ADT_A01)
                return this.HandlePixUpdate(message as NHapi.Model.V231.Message.ADT_A01, e);
            else
                return MessageUtil.CreateNack(e.Message, "AR", "200", locale.GetString("MSGE074"), config);
        }

        /// <summary>
        /// Handle the PIX merge request
        /// </summary>
        private IMessage HandlePixMerge(NHapi.Model.V231.Message.ADT_A39 request, Hl7MessageReceivedEventArgs evt)
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
            List<AuditData> audit = new List<AuditData>();
            try
            {

                // Create Query Data
                ComponentUtility cu = new ComponentUtility() { Context = this.Context };
                DeComponentUtility dcu = new DeComponentUtility() { Context = this.Context };
                var data = cu.CreateComponents(request, dtls);
                if (data == null)
                    throw new InvalidOperationException(locale.GetString("MSGE00A"));

                // Merge
                var result = dataService.Merge(data, request.MSH.ProcessingID.ProcessingID.Value == "P" ? DataPersistenceMode.Production : DataPersistenceMode.Debugging);

                if (result == null || result.VersionId == null)
                    throw new InvalidOperationException(locale.GetString("DTPE001"));

                List<VersionedDomainIdentifier> deletedRecordIds = new List<VersionedDomainIdentifier>(),
                    updatedRecordIds = new List<VersionedDomainIdentifier>();

                // Subjects
                var oidData = config.OidRegistrar.GetOid("CR_CID").Oid;
                foreach (Person subj in data.FindAllComponents(SVC.Core.ComponentModel.HealthServiceRecordSiteRoleType.SubjectOf))
                {
                    PersonRegistrationRef replcd = subj.FindComponent(SVC.Core.ComponentModel.HealthServiceRecordSiteRoleType.ReplacementOf) as PersonRegistrationRef;
                    deletedRecordIds.Add(new VersionedDomainIdentifier() { 
                        Identifier = replcd.Id.ToString(),
                        Domain = oidData
                    });
                    updatedRecordIds.Add(new VersionedDomainIdentifier() {
                        Identifier = subj.Id.ToString(),
                        Domain = oidData
                    });
                }

                // Now audit
                audit.Add(auditUtil.CreateAuditData("ITI-8", ActionType.Delete, OutcomeIndicator.Success, evt, deletedRecordIds));
                audit.Add(auditUtil.CreateAuditData("ITI-8", ActionType.Update, OutcomeIndicator.Success, evt, updatedRecordIds));
                // Now process the result
                response = MessageUtil.CreateNack(request, dtls, this.Context, typeof(NHapi.Model.V231.Message.ACK));
                MessageUtil.UpdateMSH(new NHapi.Base.Util.Terser(response), request, config);
                (response as NHapi.Model.V231.Message.ACK).MSH.MessageType.TriggerEvent.Value = request.MSH.MessageType.TriggerEvent.Value;
                (response as NHapi.Model.V231.Message.ACK).MSH.MessageType.MessageType.Value = "ACK";
            }
            catch (Exception e)
            {
                Trace.TraceError(e.ToString());
                if (!dtls.Exists(o => o.Message == e.Message || o.Exception == e))
                    dtls.Add(new ResultDetail(ResultDetailType.Error, e.Message, e));
                response = MessageUtil.CreateNack(request, dtls, this.Context, typeof(NHapi.Model.V231.Message.ACK));
                audit.Add(auditUtil.CreateAuditData("ITI-8", ActionType.Delete, OutcomeIndicator.EpicFail, evt, new List<VersionedDomainIdentifier>()));
            }
            finally
            {
                IAuditorService auditSvc = this.Context.GetService(typeof(IAuditorService)) as IAuditorService;
                if (auditSvc != null)
                    foreach(var aud in audit) 
                        auditSvc.SendAudit(aud);
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
                var data = cu.CreateComponents(request, dtls);
                if (data == null)
                    throw new InvalidOperationException(locale.GetString("MSGE00A"));

                var result = dataService.Update(data, request.MSH.ProcessingID.ProcessingID.Value == "P" ? DataPersistenceMode.Production : DataPersistenceMode.Debugging);

                if (result == null || result.VersionId == null)
                    throw new InvalidOperationException(locale.GetString("DTPE001"));

                dtls.AddRange(result.Details);
                audit = auditUtil.CreateAuditData("ITI-8", result.VersionId.UpdateMode == UpdateModeType.Update ? ActionType.Update : ActionType.Create, OutcomeIndicator.Success, evt, new List<VersionedDomainIdentifier>() { result.VersionId });
                // Now process the result
                response = MessageUtil.CreateNack(request, dtls, this.Context, typeof(NHapi.Model.V231.Message.ACK));
                MessageUtil.UpdateMSH(new NHapi.Base.Util.Terser(response), request, config);
                (response as NHapi.Model.V231.Message.ACK).MSH.MessageType.TriggerEvent.Value = request.MSH.MessageType.TriggerEvent.Value;
                (response as NHapi.Model.V231.Message.ACK).MSH.MessageType.MessageType.Value = "ACK";
            }
            catch (Exception e)
            {
                Trace.TraceError(e.ToString());
                if (!dtls.Exists(o => o.Message == e.Message || o.Exception == e))
                    dtls.Add(new ResultDetail(ResultDetailType.Error, e.Message, e));
                response = MessageUtil.CreateNack(request, dtls, this.Context, typeof(NHapi.Model.V231.Message.ACK));
                audit = auditUtil.CreateAuditData("ITI-8", ActionType.Create, OutcomeIndicator.EpicFail, evt, new List<VersionedDomainIdentifier>());
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
            AuditUtil auditUtil = new AuditUtil() { Context = this.Context };
            //DataUtil dataUtil = new DataUtil() { Context = this.Context };

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

                var result = dataService.Register(data, request.MSH.ProcessingID.ProcessingID.Value == "P" ? DataPersistenceMode.Production : DataPersistenceMode.Debugging);
                if (result == null || result.VersionId == null)
                    throw new InvalidOperationException(locale.GetString("DTPE001"));

                dtls.AddRange(result.Details);

                audit = auditUtil.CreateAuditData("ITI-8", result.VersionId.UpdateMode == UpdateModeType.Update ? ActionType.Update : ActionType.Create, OutcomeIndicator.Success, evt, new List<VersionedDomainIdentifier>() { result.VersionId });

                // Now process the result
                response = MessageUtil.CreateNack(request, dtls, this.Context, typeof(NHapi.Model.V231.Message.ACK));
                MessageUtil.UpdateMSH(new NHapi.Base.Util.Terser(response), request, config);
                (response as NHapi.Model.V231.Message.ACK).MSH.MessageType.TriggerEvent.Value = request.MSH.MessageType.TriggerEvent.Value;
                (response as NHapi.Model.V231.Message.ACK).MSH.MessageType.MessageType.Value = "ACK";
            }
            catch (Exception e)
            {
                Trace.TraceError(e.ToString());
                if (!dtls.Exists(o => o.Message == e.Message || o.Exception == e))
                    dtls.Add(new ResultDetail(ResultDetailType.Error, e.Message, e));
                response = MessageUtil.CreateNack(request, dtls, this.Context, typeof(NHapi.Model.V231.Message.ACK));
                audit = auditUtil.CreateAuditData("ITI-8", ActionType.Create, OutcomeIndicator.EpicFail, evt, new List<VersionedDomainIdentifier>());
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
            var dataService = this.Context.GetService(typeof(IClientRegistryDataService)) as IClientRegistryDataService;

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
            AuditUtil auditUtil = new AuditUtil() { Context = this.Context };
            //DataUtil dataUtil = new DataUtil() { Context = this.Context };

            try
            {

                // Create Query Data
                ComponentUtility cu = new ComponentUtility() { Context = this.Context };
                DeComponentUtility dcu = new DeComponentUtility() { Context = this.Context };
                var data = cu.CreateQueryComponents(request, dtls);
                
                if (data == null)
                    throw new InvalidOperationException(locale.GetString("MSGE00A"));

                
                RegistryQueryResult result = dataService.Query(data);
                dtls.AddRange(result.Details);

                // Update locations?
                foreach (var dtl in dtls)
                    if (dtl is PatientNotFoundResultDetail)
                        dtl.Location = "QPD^1^3^1^1";
                    else if (dtl is UnrecognizedPatientDomainResultDetail)
                        dtl.Location = "QPD^1^3^1^4";
                    else if (dtl is UnrecognizedTargetDomainResultDetail)
                        dtl.Location = "QPD^1^4^";


                audit = auditUtil.CreateAuditData("ITI-9", ActionType.Execute, OutcomeIndicator.Success, evt, result);

                // Now process the result
                response = dcu.CreateRSP_K23(result, dtls);
                //var r = dcu.CreateRSP_K23(null, null);
                // Copy QPD
                try
                {
                    (response as NHapi.Model.V25.Message.RSP_K23).QPD.MessageQueryName.Identifier.Value = request.QPD.MessageQueryName.Identifier.Value;
                    Terser reqTerser = new Terser(request),
                        rspTerser = new Terser(response);
                    rspTerser.Set("/QPD-1", reqTerser.Get("/QPD-1"));
                    rspTerser.Set("/QPD-2", reqTerser.Get("/QPD-2"));
                    rspTerser.Set("/QPD-3-1", reqTerser.Get("/QPD-3-1"));
                    rspTerser.Set("/QPD-3-4-1", reqTerser.Get("/QPD-3-4-1"));
                    rspTerser.Set("/QPD-3-4-2", reqTerser.Get("/QPD-3-4-2"));
                    rspTerser.Set("/QPD-3-4-3", reqTerser.Get("/QPD-3-4-3"));
                    rspTerser.Set("/QPD-4-1", reqTerser.Get("/QPD-4-1"));
                    rspTerser.Set("/QPD-4-4-1", reqTerser.Get("/QPD-4-4-1"));
                    rspTerser.Set("/QPD-4-4-2", reqTerser.Get("/QPD-4-4-2"));
                    rspTerser.Set("/QPD-4-4-3", reqTerser.Get("/QPD-4-4-3"));

                }
                catch(Exception e)
                {
                    Trace.TraceError(e.ToString());
                }
                //MessageUtil.((response as NHapi.Model.V25.Message.RSP_K23).QPD, request.QPD);
                
                MessageUtil.UpdateMSH(new NHapi.Base.Util.Terser(response), request, config);
            }
            catch (Exception e)
            {
                Trace.TraceError(e.ToString());
                if (!dtls.Exists(o => o is UnrecognizedPatientDomainResultDetail || o is UnrecognizedTargetDomainResultDetail || o.Message == e.Message || o.Exception == e))
                    dtls.Add(new ResultDetail(ResultDetailType.Error, e.Message, e));
                response = MessageUtil.CreateNack(request, dtls, this.Context, typeof(RSP_K23));
                Terser errTerser = new Terser(response);
                // HACK: Fix the generic ACK with a real ACK for this message
                errTerser.Set("/MSH-9-2", "K23");
                errTerser.Set("/MSH-9-3", "RSP_K23");
                errTerser.Set("/QAK-2", "AE");
                errTerser.Set("/MSA-1", "AE");
                errTerser.Set("/QAK-1", request.QPD.QueryTag.Value);
                audit = auditUtil.CreateAuditData("ITI-9", ActionType.Execute, OutcomeIndicator.EpicFail, evt, new List<VersionedDomainIdentifier>());
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
