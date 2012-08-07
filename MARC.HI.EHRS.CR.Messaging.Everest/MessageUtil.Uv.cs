/* 
 * Copyright 2008-2011 Mohawk College of Applied Arts and Technology
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
 * User: Justin Fyfe
 * Date: 08-24-2011
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MARC.Everest.Connectors;
using System.Reflection;
using MARC.HI.EHRS.SVC.Core.Services;
using MARC.Everest.DataTypes;
using MARC.Everest.Exceptions;
using MARC.HI.EHRS.SVC.Core.DataTypes;
using MARC.Everest.Interfaces;
using MARC.HI.EHRS.CR.Core.ComponentModel;
using MARC.HI.EHRS.SVC.Core.Issues;
using MARC.HI.EHRS.SVC.Core;
using MARC.Everest.RMIM.UV.NE2008.MCCI_MT100200UV01;
using MARC.Everest.RMIM.UV.NE2008.Vocabulary;
using MARC.Everest.RMIM.UV.NE2008.Interactions;
using MARC.Everest.RMIM.UV.NE2008.QUQI_MT020001UV01;

namespace MARC.HI.EHRS.CR.Messaging.Everest
{
    /// <summary>
    /// Message utilities
    /// </summary>
    public static partial class MessageUtil
    {


        
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
                new Device(
                    new SET<II>(new II() { NullFlavor = MARC.Everest.DataTypes.NullFlavor.NoInformation })
                ) : new Device()
                {
                    Id = sender.Device.Id ?? new SET<II>(new II() { NullFlavor = MARC.Everest.DataTypes.NullFlavor.NoInformation }),
                    Name = sender.Device.Name == null ? null : sender.Device.Name
                }
            };
        }

        /// <summary>
        /// Create a sender node from this application's configuration
        /// </summary>
        /// <returns></returns>
        public static Sender CreateSenderUv(Uri receiveEndpoint, ISystemConfigurationService configService)
        {
            return new Sender()
                {
                    Telecom = receiveEndpoint == null ? new TEL() { NullFlavor = NullFlavor.NoInformation } : (TEL)receiveEndpoint.ToString(),
                    Device = new Device()
                    {
                        Id = new SET<II>(new II(configService.DeviceIdentifier)),
                        SoftwareName = SoftwareName.Product,
                        Desc = SoftwareDescription.Description,
                        ManufacturerModelName = SoftwareVersion.ToString()
                    }
                };
        }

        /// <summary>
        /// Create a list of ack details from the supplied list of details
        /// </summary>
        public static List<AcknowledgementDetail> CreateAckDetailsUv(IResultDetail[] details)
        {
            List<AcknowledgementDetail> retVal = new List<AcknowledgementDetail>(10);
            foreach (IResultDetail dtl in details ?? new IResultDetail[0])
            {

                // Acknowledgement detail
                var ackDetail = new AcknowledgementDetail()
                {
                    TypeCode =
                    dtl.Type == ResultDetailType.Error ? AcknowledgementDetailType.Error :
                    dtl.Type == ResultDetailType.Warning ? AcknowledgementDetailType.Warning : AcknowledgementDetailType.Information
                };

                // Determine the type of acknowledgement
                if (dtl is InsufficientRepetionsResultDetail)
                    ackDetail.Code = new CE<string>(Util.ToWireFormat(MARC.Everest.RMIM.CA.R020402.Vocabulary.AcknowledgementDetailCode.InsufficientRepetitions), "2.16.840.1.113883.5.1100");
                else if (dtl is MandatoryElementMissingResultDetail)
                    ackDetail.Code = new CE<string>(Util.ToWireFormat(MARC.Everest.RMIM.CA.R020402.Vocabulary.AcknowledgementDetailCode.MandatoryElementWithNullValue), "2.16.840.1.113883.5.1100");
                else if (dtl is NotImplementedElementResultDetail)
                    ackDetail.Code = new CE<string>(Util.ToWireFormat(MARC.Everest.RMIM.CA.R020402.Vocabulary.AcknowledgementDetailCode.SyntaxError), "2.16.840.1.113883.5.1100");
                else if (dtl is RequiredElementMissingResultDetail)
                    ackDetail.Code = new CE<string>(Util.ToWireFormat(MARC.Everest.RMIM.CA.R020402.Vocabulary.AcknowledgementDetailCode.RequiredElementMissing), "2.16.840.1.113883.5.1100");
                else if (dtl is PersistenceResultDetail)
                    ackDetail.Code = new CE<string>(Util.ToWireFormat(MARC.Everest.RMIM.CA.R020402.Vocabulary.AcknowledgementDetailCode.NoStorageSpaceForMessage), "2.16.840.1.113883.5.1100");
                else if (dtl is VocabularyIssueResultDetail)
                    ackDetail.Code = new CE<string>(Util.ToWireFormat(MARC.Everest.RMIM.CA.R020402.Vocabulary.AcknowledgementDetailCode.TerminologyError), "2.16.840.1.113883.5.1100");
                else if (dtl is FixedValueMisMatchedResultDetail)
                    ackDetail.Code = new CE<string>(Util.ToWireFormat(MARC.Everest.RMIM.CA.R020402.Vocabulary.AcknowledgementDetailCode.ValueDoesNotMatchFixedValue), "2.16.840.1.113883.5.1100");
                else if (dtl is UnsupportedProcessingModeResultDetail)
                    ackDetail.Code = new CE<string>(Util.ToWireFormat(MARC.Everest.RMIM.CA.R020402.Vocabulary.AcknowledgementDetailCode.UnsupportedProcessingMode), "2.16.840.1.113883.5.1100");
                else if (dtl is UnsupportedResponseModeResultDetail)
                    ackDetail.Code = new CE<string>(Util.ToWireFormat(MARC.Everest.RMIM.CA.R020402.Vocabulary.AcknowledgementDetailCode.UnsupportedProcessingId), "2.16.840.1.113883.5.1100");
                else if (dtl is UnsupportedVersionResultDetail)
                    ackDetail.Code = new CE<string>(Util.ToWireFormat(MARC.Everest.RMIM.CA.R020402.Vocabulary.AcknowledgementDetailCode.UnsupportedVersionId), "2.16.840.1.113883.5.1100");
                else if (dtl.Exception is NotImplementedException)
                    ackDetail.Code = new CE<string>(Util.ToWireFormat(MARC.Everest.RMIM.CA.R020402.Vocabulary.AcknowledgementDetailCode.UnsupportedInteraction), "2.16.840.1.113883.5.1100");
                else if (dtl is UnrecognizedSenderResultDetail)
                    ackDetail.Code = new CE<string>(Util.ToWireFormat(MARC.Everest.RMIM.CA.R020402.Vocabulary.AcknowledgementDetailCode.UnknownSender), "2.16.840.1.113883.5.1100");
                else if (dtl is DetectedIssueResultDetail) // Don't handle these
                    continue;
                else
                    ackDetail.Code = new CE<string>(Util.ToWireFormat(MARC.Everest.RMIM.CA.R020402.Vocabulary.AcknowledgementDetailCode.InternalSystemError), "2.16.840.1.113883.5.1100");
                
                // Mesage
                ackDetail.Location = dtl.Location == null ? null : new SET<ST>((ST)dtl.Location, (a,b) => ST.Comparator(a, b));
                ackDetail.Text = dtl.Message;
                if (dtl.Exception != null)
                    ackDetail.Location = new SET<ST>((ST)String.Format("({0})", dtl.Exception.StackTrace));

                retVal.Add(ackDetail);
            }

            return retVal;
        }

        /// <summary>
        /// Generate altnerative ack detilas for the MCCI message set
        /// </summary>
        //public static IEnumerable<AcknowledgementDetail> CreateGenAckDetails(IResultDetail[] details)
        //{
        //    List<AcknowledgementDetail> retVal = new List<AcknowledgementDetail>(10);
        //    foreach (var item in MessageUtil.CreateAckDetails(details))
        //        retVal.Add(new AcknowledgementDetail(
        //            item.TypeCode, item.Code, item.Text, item.Location));

        //    return retVal;
        //}

        /// <summary>
        /// Create a domain identifier list
        /// </summary>
        //internal static List<DomainIdentifier> CreateDomainIdentifierList(List<MARC.Everest.RMIM.CA.R020402.REPC_MT500006CA.RecordId> list)
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
        public static void ValidateTransportWrapperUv(IInteraction interaction, ISystemConfigurationService config, List<IResultDetail> dtls)
        {
            // Check the response mode code
            string procMode = Util.ToWireFormat((interaction as IImplementsProcessingCode<ProcessingID>).ProcessingCode);

            var profile = interaction as IImplementsProfileId;

            // Check processing id
            if (procMode != "P" && procMode != "D")
                dtls.Add(new UnsupportedProcessingModeResultDetail(procMode));

            // Check version identifier
            if (interaction.VersionCode != null && !interaction.VersionCode.CodeValue.Equals("V3PR1"))
                dtls.Add(new UnsupportedVersionResultDetail(String.Format("Version '{0}' is not supported by this endpoint", interaction.VersionCode)));
            
            if(profile == null)
                dtls.Add(new FixedValueMisMatchedResultDetail(String.Empty, String.Format("{1}^^^&{0}&ISO", MCCI_IN000002UV01.GetProfileId()[0].Root, MCCI_IN000002UV01.GetProfileId()[0].Extension), false, "//urn:hl7-org:v3#profileId"));
            else if (profile == null || profile.ProfileId.Count(o => II.Comparator(o, MCCI_IN000002UV01.GetProfileId()[0]) == 0) == 0)
                dtls.Add(new UnsupportedVersionResultDetail(String.Format("Supplied profile identifier does not match any profile identifier this endpoint can reliably process")));

            Sender sndr = interaction.GetType().GetProperty("Sender").GetValue(interaction, null) as Sender;
            if(sndr == null || sndr.NullFlavor != null || sndr.Device == null || sndr.Device.NullFlavor != null || sndr.Device.Id == null || sndr.Device.Id.IsNull)
                dtls.Add(new MandatoryElementMissingResultDetail(ResultDetailType.Error, "Sender information is missing from message", null));
            else if(sndr.Device.Id.Find(o=>config.IsRegisteredDevice(new DomainIdentifier() { Domain = o.Root, Identifier = o.Extension })) == null)
                dtls.Add(new UnrecognizedSenderResultDetail(sndr));

        }

        /// <summary>
        /// Create a custodianship node
        /// </summary>
        //public static Custodian CreateCustodian(ISystemConfigurationService configService)
        //{
            
        //    var retVal = new MARC.Everest.RMIM.CA.R020402.REPC_MT230003CA.Custodian();
        //    retVal.AssignedDevice = new MARC.Everest.RMIM.CA.R020402.COCT_MT090310CA.AssignedDevice(
        //        new II(configService.Custodianship.Id.Domain, configService.Custodianship.Id.Identifier),
        //        new MARC.Everest.RMIM.CA.R020402.COCT_MT090310CA.Repository(configService.Custodianship.Name),
        //        new MARC.Everest.RMIM.CA.R020402.COCT_MT090310CA.RepositoryJurisdiction(
        //            configService.JurisdictionData.Name)
        //            );
        //    return retVal;
        //}

        /// <summary>
        /// Create query data structure 
        /// </summary>
        public static DataUtil.QueryData CreateQueryDataUv<T>(QueryByParameter<T> queryByParameter, string originator)
        {
            return  new DataUtil.QueryData()
            {
                QueryId = new Guid(queryByParameter.QueryId.Root),
                IncludeHistory = false,
                IncludeNotes = false,
                Quantity = (int)(queryByParameter.InitialQuantity ?? new INT(100)),
                Originator = originator
            };
        }

        /// <summary>
        /// Create ack details
        /// </summary>
        internal static IEnumerable<MARC.Everest.RMIM.UV.NE2008.MCCI_MT100200UV01.AcknowledgementDetail> CreateAckDetailsUv(DetectedIssue[] detectedIssue)
        {
            List<AcknowledgementDetail> retVal = new List<AcknowledgementDetail>(10);
            foreach (DetectedIssue dtl in detectedIssue ?? new DetectedIssue[0])
            {

                // Acknowledgement detail
                var ackDetail = new AcknowledgementDetail()
                {
                    TypeCode =
                    dtl.Priority == IssuePriorityType.Error ? AcknowledgementDetailType.Error :
                    dtl.Priority == IssuePriorityType.Warning ? AcknowledgementDetailType.Warning : AcknowledgementDetailType.Information
                };

                // Determine the type of acknowledgement
                var typ = TranslateDetectedIssueCode(dtl.Type);
                ackDetail.Code = new CE<string>(Util.ToWireFormat(typ.Code), typ.CodeSystem);
                ackDetail.Text = dtl.Text;
                // Mesage
                retVal.Add(ackDetail);
            }

            return retVal;
        }

    }
}
