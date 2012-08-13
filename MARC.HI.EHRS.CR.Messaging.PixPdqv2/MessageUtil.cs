using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NHapi.Base.Util;
using MARC.HI.EHRS.SVC.Core.Services;
using NHapi.Base.Model;
using MARC.Everest.Connectors;

namespace MARC.HI.EHRS.CR.Messaging.PixPdqv2
{
    /// <summary>
    /// Message Utility
    /// </summary>
    public class MessageUtil
    {
        
        /// <summary>
        /// Create an MSH in the specified terser
        /// </summary>
        public static void UpdateMSH(Terser terser, IMessage inboundMsh, ISystemConfigurationService config)
        {
            Terser inboundTerser = new Terser(inboundMsh);

            terser.Set("/MSH-10", Guid.NewGuid().ToString());
            terser.Set("/MSH-3", config.Custodianship.Name);
            terser.Set("/MSH-4", config.JurisdictionData.Name);
            terser.Set("/MSH-5", inboundTerser.Get("/MSH-3"));
            terser.Set("/MSH-6", inboundTerser.Get("/MSH-4"));
            terser.Set("/MSH-7", DateTime.Now.ToString("yyyyMMddHHmm"));
            terser.Set("/MSA-2", inboundTerser.Get("/MSH-10"));
            
        }

        /// <summary>
        /// Create NACK
        /// </summary>
        internal static IMessage CreateNack(IMessage request, string responseCode, string errCode, string errDescription, ISystemConfigurationService config)
        {
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
        internal static IMessage CreateNack(IMessage request, List<IResultDetail> errors, MARC.HI.EHRS.SVC.Core.HostContext context )
        {
            var config = context.GetService(typeof(ISystemConfigurationService)) as ISystemConfigurationService;

            NHapi.Model.V25.Message.ACK ack = new NHapi.Model.V25.Message.ACK();
            Terser terser = new Terser(ack);
            MessageUtil.UpdateMSH(terser, request, config);
            int errLevel = 0;

            foreach (var dtl in errors)
            {
                var err = ack.GetERR(ack.ERRRepetitionsUsed);
                var tErr = MessageUtil.UpdateERR(err, dtl, context);
                if (tErr > errLevel)
                    errLevel = tErr;
            }

            terser.Set("/MSA-1", errLevel == 0 ? "AA" : errLevel == 1 ? "AE" : "AR");

            return ack;
        }

        /// <summary>
        /// Update err
        /// </summary>
        public static int UpdateERR(NHapi.Model.V25.Segment.ERR err, IResultDetail dtl, MARC.HI.EHRS.SVC.Core.HostContext context)
        {
            var locale = context.GetService(typeof(ILocalizationService)) as ILocalizationService;

            err.Severity.Value = dtl.Type.ToString()[0].ToString();

            // Determine the type of acknowledgement
            string errCode = String.Empty;
            string errSys = "2.16.840.1.113883.5.1100";
            if (dtl is InsufficientRepetionsResultDetail)
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

            err.HL7ErrorCode.Identifier.Value = errCode;
            err.HL7ErrorCode.Text.Value = locale.GetString(String.Format("HL7{0}"));

            // Mesage
            err.UserMessage.Value = dtl.Message;

            return Int32.Parse(errCode[0].ToString());

        }

    }
}
