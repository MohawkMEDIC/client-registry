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
using NHapi.Base.Util;
using MARC.HI.EHRS.SVC.Core.Services;
using NHapi.Base.Model;
using MARC.Everest.Connectors;
using MARC.HI.EHRS.SVC.Core.DataTypes;
using NHapi.Base.validation.impl;
using NHapi.Base.Parser;
using System.Text.RegularExpressions;
using System.Diagnostics;
using MARC.HI.EHRS.CR.Core.Data;
using MARC.HI.EHRS.CR.Security.Services;
using MARC.HI.EHRS.CR.Security;

namespace MARC.HI.EHRS.CR.Messaging.PixPdqv2
{
    /// <summary>
    /// Message Utility
    /// </summary>
    public class MessageUtil
    {

        /// <summary>
        /// Telecommunications address use map
        /// </summary>
        private static Dictionary<String, MARC.Everest.DataTypes.Interfaces.TelecommunicationAddressUse?> s_v2TelUseConvert = new Dictionary<string, MARC.Everest.DataTypes.Interfaces.TelecommunicationAddressUse?>()
        {
            { "ASN", MARC.Everest.DataTypes.Interfaces.TelecommunicationAddressUse.AnsweringService },
            { "BPN", MARC.Everest.DataTypes.Interfaces.TelecommunicationAddressUse.Pager },
            { "EMR", MARC.Everest.DataTypes.Interfaces.TelecommunicationAddressUse.EmergencyContact },
            { "PRN", MARC.Everest.DataTypes.Interfaces.TelecommunicationAddressUse.PrimaryHome },
            { "VHN", MARC.Everest.DataTypes.Interfaces.TelecommunicationAddressUse.VacationHome },
            { "WPN", MARC.Everest.DataTypes.Interfaces.TelecommunicationAddressUse.WorkPlace }
        };


        /// <summary>
        /// Transform a telephone number
        /// </summary>
        public static TelecommunicationsAddress TelFromXTN(NHapi.Model.V231.Datatype.XTN v2XTN)
        {
            Regex re = new Regex(@"([+0-9A-Za-z]{1,4})?\((\d{3})\)?(\d{3})\-(\d{4})X?(\d{1,6})?");
            MARC.Everest.DataTypes.TEL retVal = new Everest.DataTypes.TEL();

            if (v2XTN.Get9999999X99999CAnyText.Value == null)
            {
                StringBuilder sb = new StringBuilder("tel:");

                try
                {
                    if (v2XTN.CountryCode.Value != null)
                        sb.AppendFormat("{0}-", v2XTN.CountryCode);
                    if (v2XTN.PhoneNumber != null && v2XTN.PhoneNumber.Value != null && !v2XTN.PhoneNumber.Value.Contains("-"))
                        v2XTN.PhoneNumber.Value = v2XTN.PhoneNumber.Value.Insert(3, "-");
                    sb.AppendFormat("{0}-{1}", v2XTN.AreaCityCode, v2XTN.PhoneNumber);
                    if (v2XTN.Extension.Value != null)
                        sb.AppendFormat(";ext={0}", v2XTN.Extension);
                }
                catch { }
                
                if (sb.ToString().EndsWith("tel:") ||
                    sb.ToString()== "tel:-")
                    retVal = "tel:" + v2XTN.AnyText.Value;
                else
                    retVal = sb.ToString();
                
            }
            else
            {
                var match = re.Match(v2XTN.Get9999999X99999CAnyText.Value);
                StringBuilder sb = new StringBuilder("tel:");

                for (int i = 1; i < 5; i++)
                    if (!String.IsNullOrEmpty(match.Groups[i].Value))
                        sb.AppendFormat("{0}{1}", match.Groups[i].Value, i == 4 ? "" : "-");
                if (!string.IsNullOrEmpty(match.Groups[5].Value))
                    sb.AppendFormat(";ext={0}", match.Groups[5].Value);

                retVal = sb.ToString();
            }

            // Use code conversion
            MARC.Everest.DataTypes.Interfaces.TelecommunicationAddressUse? use = null;
            if (!String.IsNullOrEmpty(v2XTN.TelecommunicationUseCode.Value) && !s_v2TelUseConvert.TryGetValue(v2XTN.TelecommunicationUseCode.Value, out use))
                throw new InvalidOperationException(string.Format("{0} is not a known use code", v2XTN.TelecommunicationUseCode.Value));

            // Capability
            retVal.Capabilities = new Everest.DataTypes.SET<Everest.DataTypes.CS<Everest.DataTypes.Interfaces.TelecommunicationCabability>>();
            switch (v2XTN.TelecommunicationEquipmentType.Value)
            {
                case "BP":
                    retVal.Capabilities.Add(MARC.Everest.DataTypes.Interfaces.TelecommunicationCabability.Text);
                    break;
                case "CP":
                    retVal.Capabilities.Add(MARC.Everest.DataTypes.Interfaces.TelecommunicationCabability.SMS);
                    retVal.Capabilities.Add(MARC.Everest.DataTypes.Interfaces.TelecommunicationCabability.Voice);
                    break;
                case "FX":
                    retVal.Capabilities.Add(MARC.Everest.DataTypes.Interfaces.TelecommunicationCabability.Fax);
                    break;
                case "PH":
                    retVal.Capabilities.Add(MARC.Everest.DataTypes.Interfaces.TelecommunicationCabability.Voice);
                    break;
                case "X.400":
                    retVal.Capabilities.Add(MARC.Everest.DataTypes.Interfaces.TelecommunicationCabability.Text);
                    retVal.Capabilities.Add(MARC.Everest.DataTypes.Interfaces.TelecommunicationCabability.Data);
                    break;
            }

            return new TelecommunicationsAddress()
            {
                Capability = Util.ToWireFormat(retVal.Capabilities),
                Use = Util.ToWireFormat(retVal.Use),
                Value = retVal.Value
            };
        }

        /// <summary>
        /// XTN from telephone number
        /// </summary>
        /// <param name="tel"></param>
        /// <param name="instance"></param>
        public static void XTNFromTel(MARC.Everest.DataTypes.TEL tel, NHapi.Model.V25.Datatype.XTN instance)
        {
            Regex re = new Regex(@"^(?<s1>(?<s0>[^:/\?#]+):)?(?<a1>//(?<a0>[^/\;#]*))?(?<p0>[^\;#]*)(?<q1>\;(?<q0>[^#]*))?(?<f1>#(?<f0>.*))?");

            // Match 
            var match = re.Match(tel.Value);
            if (match.Groups[1].Value != "tel:")
            {
                instance.AnyText.Value = tel.Value;
                //instance.TelephoneNumber.Value = tel.Value;
                return;
            }

            // Telephone
            string[] comps = match.Groups[5].Value.Split('-');
            StringBuilder sb = new StringBuilder(),
                phone = new StringBuilder();
            for (int i = 0; i < comps.Length; i++)
                if (i == 0 && comps[i].Contains("+"))
                {
                    sb.Append(comps[i]);
                    instance.CountryCode.Value = comps[i];
                }
                else if (sb.Length == 0 && comps.Length == 3 ||
                    comps.Length == 4 && i == 1) // area code?
                {
                    sb.AppendFormat("({0})", comps[i]);
                    instance.AreaCityCode.Value = comps[i];
                }
                else if (i != comps.Length - 1)
                {
                    sb.AppendFormat("{0}-", comps[i]);
                    phone.AppendFormat("{0}", comps[i]);
                }
                else
                {
                    sb.Append(comps[i]);
                    phone.Append(comps[i]);
                }

            instance.LocalNumber.Value = phone.ToString().Replace("-","");

            // Extension?
            string[] parms = match.Groups[7].Value.Split(';');
            foreach (var parm in parms)
            {
                string[] pData = parm.Split('=');
                if (pData[0] == "extension" || pData[0] == "ext" || pData[0] == "postd")
                {
                    sb.AppendFormat("X{0}", pData[1]);
                    instance.Extension.Value = pData[1];
                }
            }

            instance.TelephoneNumber.Value = sb.ToString();

            // Tel use
            if (tel.Use != null)
                foreach (var tcu in s_v2TelUseConvert)
                    if (tcu.Value.Value == tel.Use.First.Code)
                        instance.TelecommunicationUseCode.Value = tcu.Key;
            if (tel.Capabilities != null)
            {
                String cap = Util.ToWireFormat(tel.Capabilities);
                if (cap.Contains("sms"))
                    instance.TelecommunicationEquipmentType.Value = "CP";
                else if (cap.Contains("voice"))
                    instance.TelecommunicationEquipmentType.Value = "PH";
            }
        }

        /// <summary>
        /// Create an MSH in the specified terser
        /// </summary>
        public static void UpdateMSH(Terser terser, IMessage inboundMsh, ISystemConfigurationService config)
        {
            Terser inboundTerser = new Terser(inboundMsh);

            terser.Set("/MSH-10", Guid.NewGuid().ToString());
            terser.Set("/MSH-3", config.DeviceName);
            terser.Set("/MSH-4", config.JurisdictionData.Name);
            terser.Set("/MSH-5", inboundTerser.Get("/MSH-3"));
            terser.Set("/MSH-6", inboundTerser.Get("/MSH-4"));
            terser.Set("/MSH-7", DateTime.Now.ToString("yyyyMMddHHmm"));
            terser.Set("/MSA-2", inboundTerser.Get("/MSH-10"));
            if(String.IsNullOrEmpty(terser.Get("/MSH-9-2")))
                terser.Set("/MSH-9-2", inboundTerser.Get("/MSH-9-2"));
            terser.Set("/MSH-11", inboundTerser.Get("/MSH-11"));
        }

        /// <summary>
        /// Create NACK
        /// </summary>
        internal static IMessage CreateNack(IMessage request, string responseCode, string errCode, string errDescription, ISystemConfigurationService config)
        {
            System.Diagnostics.Trace.TraceWarning(String.Format("NACK Condition : {0}", errDescription));

            if (request.Version == "2.3.1")
            {
                NHapi.Model.V231.Message.ACK ack = new NHapi.Model.V231.Message.ACK();
                Terser terser = new Terser(ack);
                terser.Set("/MSA-1", responseCode);
                terser.Set("/MSA-3", "Error occurred");
                terser.Set("/MSA-6-1", errCode);
                terser.Set("/MSA-6-2", errDescription);
                MessageUtil.UpdateMSH(terser, request, config);
                return ack;
            }
            else
            {
                NHapi.Model.V25.Message.ACK ack = new NHapi.Model.V25.Message.ACK();
                Terser terser = new Terser(ack);
                terser.Set("/MSA-1", responseCode);
                MessageUtil.UpdateMSH(terser, request, config);
                terser.Set("/ERR-3-1", errCode);
                terser.Set("/ERR-3-2", errDescription);
                return ack;
            }
        }

        /// <summary>
        /// Create NACK
        /// </summary>
        internal static IMessage CreateNack(IMessage request, List<IResultDetail> errors, IServiceProvider context, Type errType)
        {
            var config = context.GetService(typeof(ISystemConfigurationService)) as ISystemConfigurationService;

            IMessage ack = errType.GetConstructor(Type.EmptyTypes).Invoke(null) as IMessage;

            Trace.TraceError("Validation Errors:");
            errors.ForEach(o => Trace.TraceError("\t{0} : {1}", o.Type, o.Message));

            Terser terser = new Terser(ack);
            MessageUtil.UpdateMSH(terser, request, config);
            int errLevel = 0;

            int ec = 0;
            foreach (var dtl in errors)
            {
                try
                {
                    ISegment errSeg;
                    if (ack.Version == "2.5")
                        errSeg = terser.getSegment(String.Format("/ERR({0})", ec++));
                    else
                        errSeg = terser.getSegment(String.Format("/ERR", ec++));

                    if (errSeg is NHapi.Model.V231.Segment.ERR)
                    {
                        var tErr = MessageUtil.UpdateERR(errSeg as NHapi.Model.V231.Segment.ERR, dtl, context);
                        if (tErr > errLevel)
                            errLevel = tErr;
                    }
                    else if (errSeg is NHapi.Model.V25.Segment.ERR)
                    {
                        var tErr = MessageUtil.UpdateERR(errSeg as NHapi.Model.V25.Segment.ERR, dtl, context);
                        if (tErr > errLevel)
                            errLevel = tErr;
                    }
                }
                catch (Exception e)
                {
                    Trace.TraceError(e.ToString());
                }
            }

            terser.Set("/MSA-1", errLevel == 0 ? "AA" : errLevel == 1 ? "AE" : "AR");

            return ack;
        }

        /// <summary>
        /// Update an ERR
        /// </summary>
        private static int UpdateERR(NHapi.Model.V231.Segment.ERR err, IResultDetail dtl, IServiceProvider context)
        {
            var locale = context.GetService(typeof(ILocalizationService)) as ILocalizationService;

            

            // Determine the type of acknowledgement
            string errCode = String.Empty;
            string errSys = "2.16.840.1.113883.5.1100";
            if (dtl is InsufficientRepetitionsResultDetail)
                errCode = "100";
            else if (dtl is MandatoryElementMissingResultDetail)
                errCode = "101";
            else if (dtl is NotImplementedElementResultDetail)
                errCode = "207";
            else if (dtl is RequiredElementMissingResultDetail)
                errCode = "101";
            else if (dtl is PersistenceResultDetail)
                errCode = "207";
            else if (dtl is VocabularyIssueResultDetail)
                errCode = "103";
            else if (dtl is FixedValueMisMatchedResultDetail)
                errCode = "103";
            else if (dtl is UnsupportedProcessingModeResultDetail)
                errCode = "202";
            else if (dtl is UnsupportedResponseModeResultDetail)
                errCode = "207";
            else if (dtl is UnsupportedVersionResultDetail)
                errCode = "203";
            else if (dtl.Exception is NotImplementedException)
                errCode = "200";
            else if (dtl is UnrecognizedTargetDomainResultDetail ||
                dtl is UnrecognizedPatientDomainResultDetail ||
                dtl is PatientNotFoundResultDetail)
                errCode = "204";
            else if (dtl is UnrecognizedSenderResultDetail)
                errCode = "901";
            else
                errCode = "207";

            var eld = err.GetErrorCodeAndLocation(err.ErrorCodeAndLocationRepetitionsUsed);
            eld.CodeIdentifyingError.Text.Value = locale.GetString(String.Format("HL7{0}", errCode));
            eld.CodeIdentifyingError.AlternateText.Value = dtl.Message;
            eld.CodeIdentifyingError.Identifier.Value = errCode;
            
            if (dtl.Location != null && dtl.Location.Contains("^"))
            {
                var cmp = dtl.Location.Split('^');
                for (int i = 0; i < cmp.Length; i++)
                {
                    var st = eld.SegmentID as NHapi.Model.V231.Datatype.ST;
                    if (string.IsNullOrEmpty(st.Value))
                        st.Value = cmp[i];
                    else
                    {
                        var nm = eld.FieldPosition as NHapi.Model.V231.Datatype.NM;
                        if (nm != null)
                            nm.Value = cmp[i];
                    }
                }
            }
            
            return Int32.Parse(errCode[0].ToString());
        }

        /// <summary>
        /// Update err
        /// </summary>
        public static int UpdateERR(NHapi.Model.V25.Segment.ERR err, IResultDetail dtl, IServiceProvider context)
        {
            var locale = context.GetService(typeof(ILocalizationService)) as ILocalizationService;

            err.Severity.Value = dtl.Type.ToString()[0].ToString();

            // Determine the type of acknowledgement
            string errCode = String.Empty;
            string errSys = "2.16.840.1.113883.5.1100";
            if (dtl is InsufficientRepetitionsResultDetail)
                errCode = "100";
            else if (dtl is MandatoryElementMissingResultDetail)
                errCode = "101";
            else if (dtl is NotImplementedElementResultDetail)
                errCode = "207";
            else if (dtl is RequiredElementMissingResultDetail)
                errCode = "101";
            else if (dtl is PersistenceResultDetail)
                errCode = "207";
            else if (dtl is VocabularyIssueResultDetail)
                errCode = "103";
            else if (dtl is FixedValueMisMatchedResultDetail)
                errCode = "103";
            else if (dtl is UnsupportedProcessingModeResultDetail)
                errCode = "202";
            else if (dtl is UnsupportedResponseModeResultDetail)
                errCode = "207";
            else if (dtl is UnsupportedVersionResultDetail)
                errCode = "203";
            else if (dtl.Exception is NotImplementedException)
                errCode = "200";
            else if (dtl is UnrecognizedTargetDomainResultDetail ||
                dtl is UnrecognizedPatientDomainResultDetail ||
                dtl is PatientNotFoundResultDetail)
                errCode = "204";
            else
                errCode = "207";

            err.HL7ErrorCode.Identifier.Value = errCode;
            err.HL7ErrorCode.Text.Value = locale.GetString(String.Format("HL7{0}", errCode));

            if (dtl.Location != null && dtl.Location.Contains("^"))
            {
                var cmp = dtl.Location.Split('^');
                for (int i = 0; i < cmp.Length; i++)
                {
                    var st = err.GetErrorLocation(0).Components[i] as NHapi.Model.V25.Datatype.ST;
                    if (st != null)
                        st.Value = cmp[i];
                    else
                    {
                        var nm = err.GetErrorLocation(0).Components[i] as NHapi.Model.V25.Datatype.NM;
                        if (nm != null)
                            nm.Value = cmp[i];
                    }
                }
            }

            // Mesage
            err.UserMessage.Value = dtl.Message;


            return Int32.Parse(errCode[0].ToString());

        }


        /// <summary>
        /// Validate the message
        /// </summary>
        internal static void Validate(IMessage message, ISystemConfigurationService config, List<IResultDetail> dtls, IServiceProvider context)
        {
            
            // Structure validation
            PipeParser pp = new PipeParser() { ValidationContext = new DefaultValidation() };
            try
            {
                pp.Encode(message);
            }
            catch (Exception e)
            {
                dtls.Add(new ValidationResultDetail(ResultDetailType.Error, e.Message, e));
            }

            // Validation of sending application
            try
            {
                Terser msgTerser = new Terser(message);
                object obj = msgTerser.getSegment("MSH") as NHapi.Model.V25.Segment.MSH;
                if (obj != null)
                {
                    var msh = obj as NHapi.Model.V25.Segment.MSH;
                    var domainId = new ComponentUtility() { Context = context }.CreateDomainIdentifier(msh.SendingApplication, dtls);
                    if (!config.IsRegisteredDevice(domainId))
                        dtls.Add(new UnrecognizedSenderResultDetail(domainId));

                    String secret = msh.Security.Value,
                        deviceId = $"{msh.SendingApplication.NamespaceID}|{msh.SendingFacility.NamespaceID}";
                    ValidateSession(deviceId, secret, dtls, context);
                }
                else
                {
                    obj = msgTerser.getSegment("MSH") as NHapi.Model.V231.Segment.MSH;
                    if (obj != null)
                    {
                        var msh = obj as NHapi.Model.V231.Segment.MSH;
                        var domainId = new ComponentUtility() { Context = context }.CreateDomainIdentifier(msh.SendingApplication, dtls);
                        if (!config.IsRegisteredDevice(domainId))
                            dtls.Add(new UnrecognizedSenderResultDetail(domainId));

                        String secret = msh.Security.Value,
                            deviceId = $"{msh.SendingApplication.NamespaceID}|{msh.SendingFacility.NamespaceID}";
                        ValidateSession(deviceId, secret, dtls, context);
                    }
                    else
                        dtls.Add(new MandatoryElementMissingResultDetail(ResultDetailType.Error, "Missing MSH", "MSH"));
                }
            }
            catch (Exception e)
            {
                dtls.Add(new ResultDetail(ResultDetailType.Error, e.Message, e));
            }

        }

        /// <summary>
        /// Validate the session
        /// </summary>
        private static void ValidateSession(string deviceId, string secret, List<IResultDetail> dtls, IServiceProvider context)
        {
            var idp = context.GetService(typeof(IDeviceIdentityProviderService)) as IDeviceIdentityProviderService;
            var iss = context.GetService(typeof(ISessionManagerService)) as ISessionManagerService;
            if (idp == null) return;

            // Perform validation
            if (String.IsNullOrEmpty(secret))
                dtls.Add(new ValidationResultDetail(ResultDetailType.Error, "Missing MSH-8 security. This server requires MSH-8 security", "MSH^8", null));
            else
            {
                // Attempt to find an existing session for this user
                var currentSession = iss?.GetActive(deviceId)?.FirstOrDefault();
                if (currentSession == null)
                {
                    var principal = idp.Authenticate(deviceId, secret);
                    AuthenticationContext.Current = new AuthenticationContext(principal);
                    var session = iss?.Establish(principal);
                    Trace.TraceInformation("Established session {0} with {1}", session?.Key ?? Guid.Empty, principal.Identity.Name);
                }
                else
                    Trace.TraceInformation("Using existing session {0} with {1}", currentSession?.Key, deviceId);
            }
        }

        ///// <summary>
        ///// Copy QPD segment
        ///// </summary>
        //internal static void CopyQPD(NHapi.Model.V25.Segment.QPD dest, NHapi.Model.V25.Segment.QPD source,  QueryData queryParms)
        //{
        //    dest.MessageQueryName.Identifier.Value = source.MessageQueryName.Identifier.Value;
        //    dest.MessageQueryName.Text.Value = source.MessageQueryName.Text.Value;
        //    dest.QueryTag.Value = source.QueryTag.Value;
        //    var qps = source.GetField(3);

        //    // Create the actual qparms
        //    Dictionary<String, Object> actualQParms = new Dictionary<string, object>();
        //    var qPerson = queryParms.QueryRequest.FindComponent(SVC.Core.ComponentModel.HealthServiceRecordSiteRoleType.SubjectOf);
        //    if (qPerson == null)
        //        return;


        //    //dest.UserParametersInsuccessivefields.Data = source.UserParametersInsuccessivefields.Data;
        //}

        /// <summary>
        /// XTN from telephone number
        /// </summary>
        public static void XTNFromTel(Everest.DataTypes.TEL tel, NHapi.Model.V231.Datatype.XTN instance)
        {
            NHapi.Model.V25.Datatype.XTN v25instance = new NHapi.Model.V25.Datatype.XTN(instance.Message); 
            XTNFromTel(tel, v25instance);
            for (int i = 0; i < v25instance.Components.Length; i++)
                if(v25instance.Components[i] is AbstractPrimitive && i < instance.Components.Length)
                    (instance.Components[i] as AbstractPrimitive).Value = (v25instance.Components[i] as AbstractPrimitive).Value;

        }
    }
}
