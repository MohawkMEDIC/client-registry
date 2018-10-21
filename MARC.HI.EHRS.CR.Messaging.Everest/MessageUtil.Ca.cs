﻿/**
 * Copyright 2015-2015 Mohawk College of Applied Arts and Technology
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
 * User: Justin
 * Date: 12-7-2015
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MARC.Everest.Connectors;
using System.Reflection;
using MARC.HI.EHRS.SVC.Core.Services;
using MARC.Everest.DataTypes;
using MARC.Everest.RMIM.CA.R020403.MCCI_MT102001CA;
using MARC.Everest.RMIM.CA.R020403.Vocabulary;
using MARC.Everest.Exceptions;
using MARC.Everest.RMIM.CA.R020403.MCCI_MT002200CA;
using MARC.HI.EHRS.SVC.Core.DataTypes;
using MARC.Everest.Interfaces;
using MARC.Everest.RMIM.CA.R020403.Interactions;
using MARC.Everest.RMIM.CA.R020403.MCCI_MT002200CA;
using MARC.HI.EHRS.CR.Core.ComponentModel;
using MARC.HI.EHRS.SVC.Core.Issues;
using MARC.Everest.RMIM.CA.R020403.QUQI_MT120008CA;
using MARC.HI.EHRS.SVC.Core;
using MARC.HI.EHRS.CR.Core.Services;
using MARC.HI.EHRS.CR.Core.Data;

namespace MARC.HI.EHRS.CR.Messaging.Everest
{
    /// <summary>
    /// Message utilities
    /// </summary>
    public static partial class MessageUtil
    {
        /// <summary>
        /// The name of the software
        /// </summary>
        static AssemblyProductAttribute SoftwareName = null;
        /// <summary>
        /// The description of the software
        /// </summary>
        static AssemblyDescriptionAttribute SoftwareDescription = null;
        /// <summary>
        /// Version of the software
        /// </summary>
        static Version SoftwareVersion = null;

        /// <summary>
        /// Static constructor for the not supported exception
        /// </summary>
        static MessageUtil()
        {
            SoftwareName = Assembly.GetEntryAssembly().GetCustomAttributes(typeof(AssemblyProductAttribute), false)[0] as AssemblyProductAttribute;
            SoftwareDescription = Assembly.GetEntryAssembly().GetCustomAttributes(typeof(AssemblyDescriptionAttribute), false)[0] as AssemblyDescriptionAttribute;
            SoftwareVersion = Assembly.GetEntryAssembly().GetName().Version;
        }

        /// <summary>
        /// Determine if the message is valid
        /// </summary>
        public static bool IsValid(IReceiveResult receivedMessage)
        {
            
            var result = receivedMessage.Code == ResultCode.Accepted ||
                receivedMessage.Code == ResultCode.AcceptedNonConformant &&
                receivedMessage.Details.Count(o=>o.Type == ResultDetailType.Error) == 0;

            

            return result;
        }
        
        /// <summary>
        /// Turns the specified sender node into a receiver node
        /// </summary>
        public static Receiver CreateReceiver(Sender sender)
        {
            if (sender == null)
                throw new ArgumentException("Can't determine sender from transmission wrapper");

            return new Receiver()
            {
                Telecom = sender.Telecom ?? new TEL() { NullFlavor = MARC.Everest.DataTypes.NullFlavor.NoInformation },
                Device = sender.Device == null ?
                new Device2(
                    new II() { NullFlavor = MARC.Everest.DataTypes.NullFlavor.NoInformation }
                ) : new Device2()
                {
                    Id = sender.Device.Id ?? new II() { NullFlavor = MARC.Everest.DataTypes.NullFlavor.NoInformation },
                    Name = sender.Device.Name == null ? null : sender.Device.Name
                }
            };
        }

        /// <summary>
        /// Create a sender node from this application's configuration
        /// </summary>
        /// <returns></returns>
        public static Sender CreateSender(Uri receiveEndpoint, ISystemConfigurationService configService)
        {
            return new Sender()
                {
                    Telecom = receiveEndpoint == null ? new TEL() { NullFlavor = NullFlavor.NoInformation } : (TEL)receiveEndpoint.ToString(),
                    Device = new Device1()
                    {
                        Id = new II(configService.DeviceIdentifier),
                        Name = configService.DeviceName,
                        SoftwareName = SoftwareName.Product,
                        Desc = SoftwareDescription.Description,
                        ManufacturerModelName = SoftwareVersion.ToString()
                    }
                };
        }

        /// <summary>
        /// Create a list of ack details from the supplied list of details
        /// </summary>
        public static List<AcknowledgementDetail> CreateAckDetails(IEnumerable<IResultDetail> details)
        {
            List<AcknowledgementDetail> retVal = new List<AcknowledgementDetail>(10);
            foreach (IResultDetail dtl in details ?? new IResultDetail[0])
            {

                // Acknowledgement detail
                var ackDetail = new AcknowledgementDetail(
                    dtl.Type == ResultDetailType.Error ? AcknowledgementDetailType.Error :
                    dtl.Type == ResultDetailType.Warning ? AcknowledgementDetailType.Warning : AcknowledgementDetailType.Information);

                // Determine the type of acknowledgement
                if (dtl is InsufficientRepetitionsResultDetail)
                    ackDetail.Code =Util.ToWireFormat(MARC.Everest.RMIM.CA.R020402.Vocabulary.AcknowledgementDetailCode.InsufficientRepetitions);
                else if (dtl is MandatoryElementMissingResultDetail)
                    ackDetail.Code = Util.ToWireFormat(MARC.Everest.RMIM.CA.R020402.Vocabulary.AcknowledgementDetailCode.MandatoryElementWithNullValue);
                else if (dtl is NotImplementedElementResultDetail)
                    ackDetail.Code = Util.ToWireFormat(MARC.Everest.RMIM.CA.R020402.Vocabulary.AcknowledgementDetailCode.SyntaxError);
                else if (dtl is RequiredElementMissingResultDetail)
                    ackDetail.Code = Util.ToWireFormat(MARC.Everest.RMIM.CA.R020402.Vocabulary.AcknowledgementDetailCode.RequiredElementMissing);
                else if (dtl is PersistenceResultDetail)
                    ackDetail.Code = Util.ToWireFormat(MARC.Everest.RMIM.CA.R020402.Vocabulary.AcknowledgementDetailCode.NoStorageSpaceForMessage);
                else if (dtl is VocabularyIssueResultDetail)
                    ackDetail.Code = Util.ToWireFormat(MARC.Everest.RMIM.CA.R020402.Vocabulary.AcknowledgementDetailCode.TerminologyError);
                else if (dtl is FixedValueMisMatchedResultDetail)
                    ackDetail.Code = Util.ToWireFormat(MARC.Everest.RMIM.CA.R020402.Vocabulary.AcknowledgementDetailCode.ValueDoesNotMatchFixedValue);
                else if (dtl is UnsupportedProcessingModeResultDetail)
                    ackDetail.Code = Util.ToWireFormat(MARC.Everest.RMIM.CA.R020402.Vocabulary.AcknowledgementDetailCode.UnsupportedProcessingMode);
                else if (dtl is UnsupportedResponseModeResultDetail)
                    ackDetail.Code = Util.ToWireFormat(MARC.Everest.RMIM.CA.R020402.Vocabulary.AcknowledgementDetailCode.UnsupportedProcessingId);
                else if (dtl is UnsupportedVersionResultDetail)
                    ackDetail.Code = Util.ToWireFormat(MARC.Everest.RMIM.CA.R020402.Vocabulary.AcknowledgementDetailCode.UnsupportedVersionId);
                else if (dtl.Exception is NotImplementedException)
                    ackDetail.Code = Util.ToWireFormat(MARC.Everest.RMIM.CA.R020402.Vocabulary.AcknowledgementDetailCode.UnsupportedInteraction);
                else if (dtl is UnrecognizedSenderResultDetail)
                    ackDetail.Code = Util.ToWireFormat(MARC.Everest.RMIM.CA.R020402.Vocabulary.AcknowledgementDetailCode.UnknownSender);
                else if (dtl is DetectedIssueResultDetail) // Don't handle these
                    continue;
                else
                    ackDetail.Code = Util.ToWireFormat(MARC.Everest.RMIM.CA.R020402.Vocabulary.AcknowledgementDetailCode.InternalSystemError);
                
                // Mesage
                ackDetail.Location = dtl.Location == null ? null : new SET<ST>((ST)dtl.Location, (a,b) => ST.Comparator(a, b));
                ackDetail.Text = dtl.Message;
                if (dtl.Exception != null)
                    ackDetail.Text += String.Format("({0})", dtl.Exception.Message);

                retVal.Add(ackDetail);
            }

            return retVal;
        }

        /// <summary>
        /// Generate altnerative ack detilas for the MCCI message set
        /// </summary>
        public static IEnumerable<AcknowledgementDetail> CreateGenAckDetails(IEnumerable<IResultDetail> details)
        {
            List<AcknowledgementDetail> retVal = new List<AcknowledgementDetail>(10);
            foreach (var item in MessageUtil.CreateAckDetails(details))
                retVal.Add(new AcknowledgementDetail(
                    item.TypeCode, item.Code, item.Text, item.Location));

            return retVal;
        }

        /// <summary>
        /// Create a domain identifier list
        /// </summary>
        //internal static List<DomainIdentifier> CreateDomainIdentifierList(List<MARC.Everest.RMIM.CA.R020403.REPC_MT500006CA.RecordId> list)
        //{
        //    List<DomainIdentifier> retVal = new List<DomainIdentifier>();
        //    foreach (var recId in list)
        //        retVal.Add(new DomainIdentifier()
        //        {
        //            Domain = recId.Value.Root,
        //            Identifier = recId.Value.Extension
        //        });
        //    return retVal;
        //}

        /// <summary>
        /// Validates common transport wrapper flags
        /// </summary>
        public static void ValidateTransportWrapper(IInteraction interaction, ISystemConfigurationService config, List<IResultDetail> dtls)
        {
            // Check the response mode code
            string rspMode = Util.ToWireFormat((interaction as IImplementsResponseModeCode<ResponseMode>).ResponseModeCode);
            string procMode = Util.ToWireFormat((interaction as IImplementsProcessingCode<ProcessingID>).ProcessingCode);

            var profile = interaction as IImplementsProfileId;

            // Check response mode
            if (rspMode != "I")
                dtls.Add(new UnsupportedResponseModeResultDetail(rspMode));
            //// Check processing id
            //if (procMode != "P" && procMode != "D")
            //    dtls.Add(new UnsupportedProcessingModeResultDetail(procMode));

            // Check version identifier
            if (!interaction.VersionCode.CodeValue.Equals("V3-2008N"))
                dtls.Add(new UnsupportedVersionResultDetail(String.Format("Version '{0}' is not supported by this endpoint", interaction.VersionCode)));
            else if (profile == null || profile.ProfileId.Count(o => II.Comparator(o, MCCI_IN000002CA.GetProfileId()[0]) == 0) == 0)
                dtls.Add(new UnsupportedVersionResultDetail(String.Format("Supplied profile identifier does not match any profile identifier this endpoint can reliably process")));


            Sender sndr = interaction.GetType().GetProperty("Sender").GetValue(interaction, null) as Sender;
            if (sndr == null || sndr.NullFlavor != null || sndr.Device == null || sndr.Device.NullFlavor != null || sndr.Device.Id == null || sndr.Device.Id.IsNull)
                dtls.Add(new MandatoryElementMissingResultDetail(ResultDetailType.Error, "Sender information is missing from message", null));
            else {
                var sndrId = new DomainIdentifier() { Domain = sndr.Device.Id.Root, Identifier = sndr.Device.Id.Extension };
                if (!config.IsRegisteredDevice(sndrId))
                    dtls.Add(new UnrecognizedSenderResultDetail(sndrId));
            }

        }

        /// <summary>
        /// Create a custodianship node
        /// </summary>
        //public static MARC.Everest.RMIM.CA.R020403.REPC_MT230003CA.Custodian CreateCustodian(ISystemConfigurationService configService)
        //{
            
        //    var retVal = new MARC.Everest.RMIM.CA.R020403.REPC_MT230003CA.Custodian();
        //    retVal.AssignedDevice = new MARC.Everest.RMIM.CA.R020403.COCT_MT090310CA.AssignedDevice(
        //        new II(configService.Custodianship.Id.Domain, configService.Custodianship.Id.Identifier),
        //        new MARC.Everest.RMIM.CA.R020403.COCT_MT090310CA.Repository(configService.Custodianship.Name),
        //        new MARC.Everest.RMIM.CA.R020403.COCT_MT090310CA.RepositoryJurisdiction(
        //            configService.JurisdictionData.Name)
        //            );
        //    return retVal;
        //}

        /// <summary>
        /// Create query data structure 
        /// </summary>
        public static RegistryQueryRequest CreateQueryData<T>(QueryByParameter<T> queryByParameter, string originator)
        {
            return new RegistryQueryRequest()
            {
                QueryId = String.Format("{1}^^^&{0}&ISO", queryByParameter.QueryId.Root, queryByParameter.QueryId.Extension),
                IsSummary = true,
                Limit = (int)(queryByParameter.InitialQuantity ?? new INT(100)),
                Originator = originator
            };
        }




        /// <summary>
        /// Create detected issue
        /// </summary>
        public static IEnumerable<MARC.Everest.RMIM.CA.R020403.MCAI_MT700220CA.Subject> CreateDetectedIssueEvents(List<DetectedIssue> issues)
        {
            List<MARC.Everest.RMIM.CA.R020403.MCAI_MT700220CA.Subject> retVal = new List<MARC.Everest.RMIM.CA.R020403.MCAI_MT700220CA.Subject>(10);
            foreach (var dtl in issues)
            {

                // Item value
                MARC.Everest.RMIM.CA.R020403.MCAI_MT700220CA.Subject rv = new MARC.Everest.RMIM.CA.R020403.MCAI_MT700220CA.Subject(
                    new MARC.Everest.RMIM.CA.R020403.COCT_MT260020CA.DetectedIssueEvent()
                );

                // Determine the code
                rv.DetectedIssueEvent.Code = TranslateDetectedIssueCode(dtl.Type);
                
                // Mitigation
                rv.DetectedIssueEvent.MitigatedBy.Add(new MARC.Everest.RMIM.CA.R020403.COCT_MT260020CA.Mitigates(
                    new MARC.Everest.RMIM.CA.R020403.COCT_MT260020CA.DetectedIssueManagement(
                        TranslateMitigationCode(dtl.MitigatedBy))));

                // Priority code
                rv.DetectedIssueEvent.PriorityCode =
                    dtl.Priority == IssuePriorityType.Error ? AcknowledgementDetailType.Error :
                    dtl.Priority == IssuePriorityType.Warning ? AcknowledgementDetailType.Warning :
                    AcknowledgementDetailType.Information;

                rv.DetectedIssueEvent.Text = dtl.Text;

                rv.DetectedIssueEvent.SubjectOf2 = new MARC.Everest.RMIM.CA.R020403.COCT_MT260030CA.Subject(
                    new MARC.Everest.RMIM.CA.R020403.REPC_MT000005CA.SeverityObservation(
                        TranslateSeverityCode(dtl.Severity)));

                retVal.Add(rv);
            }
            return retVal;
        }

        /// <summary>
        /// Translate severity code
        /// </summary>
        public static CV<SeverityObservation> TranslateSeverityCode(IssueSeverityType issueSeverityType)
        {
            switch (issueSeverityType)
            {
                case IssueSeverityType.High:
                    return SeverityObservation.High;
                default:
                    return new CV<SeverityObservation>() { NullFlavor = MARC.Everest.DataTypes.NullFlavor.Other, CodeSystem = "2.16.840.1.113883.5.1063", OriginalText = issueSeverityType.ToString() };
            }
        }

        /// <summary>
        /// Translate mitigation
        /// </summary>
        public static CV<String> TranslateMitigationCode(ManagementType? mitigation)
        {
            if (mitigation == null) return null;
            return new CV<String>(Util.ToWireFormat((MARC.Everest.RMIM.CA.R020402.Vocabulary.ActDetectedIssueManagementCode)Enum.Parse(typeof(MARC.Everest.RMIM.CA.R020402.Vocabulary.ActDetectedIssueManagementCode), mitigation.Value.ToString())));
        }

        /// <summary>
        /// Translate detected issue
        /// </summary>
        public static CV<String> TranslateDetectedIssueCode(IssueType issueType)
        {
            return new CV<String>(Util.ToWireFormat((MARC.Everest.RMIM.CA.R020402.Vocabulary.ActDetectedIssueCode)Enum.Parse(typeof(MARC.Everest.RMIM.CA.R020402.Vocabulary.ActDetectedIssueCode), issueType.ToString())));
        }

        /// <summary>
        /// Generate the detected issue for queries
        /// </summary>
        public static IEnumerable<MARC.Everest.RMIM.CA.R020403.MCAI_MT700221CA.Subject> CreateDetectedIssueEventsQuery(List<DetectedIssue> issues)
        {
            List<MARC.Everest.RMIM.CA.R020403.MCAI_MT700221CA.Subject> retVal = new List<MARC.Everest.RMIM.CA.R020403.MCAI_MT700221CA.Subject>(10);
            foreach (var dtl in issues)
            {

                // Item value
                MARC.Everest.RMIM.CA.R020403.MCAI_MT700221CA.Subject rv = new MARC.Everest.RMIM.CA.R020403.MCAI_MT700221CA.Subject(
                    new MARC.Everest.RMIM.CA.R020403.COCT_MT260022CA.DetectedIssueEvent()
                );

                // Determine the code
                rv.DetectedIssueEvent.Code = TranslateDetectedIssueCode(dtl.Type);

                // Mitigation
                if(dtl.MitigatedBy != null)
                    rv.DetectedIssueEvent.MitigatedBy.Add(new MARC.Everest.RMIM.CA.R020403.COCT_MT260020CA.Mitigates(
                        new MARC.Everest.RMIM.CA.R020403.COCT_MT260020CA.DetectedIssueManagement(
                            TranslateMitigationCode(dtl.MitigatedBy))));

                // Priority code
                rv.DetectedIssueEvent.PriorityCode = dtl.Priority == IssuePriorityType.Error ? AcknowledgementDetailType.Error :
                    dtl.Priority == IssuePriorityType.Warning ? AcknowledgementDetailType.Warning :
                    AcknowledgementDetailType.Information;

                rv.DetectedIssueEvent.Text = dtl.Text;

                retVal.Add(rv);
            }
            return retVal;
        }

        /// <summary>
        /// Get the default language code
        /// </summary>
        internal static CE<string> GetDefaultLanguageCode(IServiceProvider context)
        {
            ISystemConfigurationService configSvc = context.GetService(typeof(ISystemConfigurationService)) as ISystemConfigurationService;

            // Get the terminology service 
            var termSvc = context.GetService(typeof(ITerminologyService)) as ITerminologyService;
            if (termSvc == null)
                return new CE<string>() { NullFlavor = NullFlavor.NoInformation };
            
            // Convert
            var conversion = termSvc.Translate(new CodeValue(configSvc.JurisdictionData.DefaultLanguageCode, configSvc.OidRegistrar.GetOid("ISO639-1").Oid), configSvc.OidRegistrar.GetOid("ISO639-3").Oid);
            return new CE<string>(conversion.Code, conversion.CodeSystem);

        }

    }
}
