using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using MARC.HI.EHRS.CR.Core.ComponentModel;
using MARC.HI.EHRS.CR.Core.Services;
using MARC.HI.EHRS.CR.Messaging.HL7.TransportProtocol;
using MARC.HI.EHRS.SVC.Core.ComponentModel;
using MARC.HI.EHRS.SVC.Core.DataTypes;
using MARC.HI.EHRS.SVC.Core.Services;
using NHapi.Base.Parser;
using NHapi.Base.Util;

namespace MARC.HI.EHRS.CR.Messaging.PixPdqv2
{
    /// <summary>
    /// Tools for auditing
    /// </summary>
    public class AuditUtil : IUsesHostContext
    {

        // Config service
        private ISystemConfigurationService m_configService;
        private IDataPersistenceService m_dataPersistence;
        private IServiceProvider m_context;

        /// <summary>
        /// Create audit data
        /// </summary>
        public AuditData CreateAuditData(string itiName, ActionType action, OutcomeIndicator outcome, Hl7MessageReceivedEventArgs msgEvent, RegistryQueryResult result)
        {


            // Create the call to the other create audit data message by constructing the list of disclosed identifiers
            List<VersionedDomainIdentifier> vids = new List<VersionedDomainIdentifier>(result.Results.Count);
            foreach (var res in result.Results)
            {
                if (res == null) continue;
                var subj = res.FindComponent(SVC.Core.ComponentModel.HealthServiceRecordSiteRoleType.SubjectOf) as Person;
                if (subj == null)
                    continue;
                vids.Add(new VersionedDomainIdentifier()
                {
                    Domain = this.m_configService.OidRegistrar.GetOid("CR_CID").Oid,
                    Identifier = subj.Id.ToString()
                });
            }

            return CreateAuditData(itiName, action, outcome, msgEvent, vids);
        }

        /// <summary>
        /// Create audit data
        /// </summary>
        internal AuditData CreateAuditData(string itiName, ActionType action, OutcomeIndicator outcome, Hl7MessageReceivedEventArgs msgEvent, List<VersionedDomainIdentifier> identifiers)
        {
            // Audit data
            AuditData retVal = null;

            AuditableObjectLifecycle lifecycle = AuditableObjectLifecycle.Access;

            // Get the config service
            ISystemConfigurationService config = Context.GetService(typeof(ISystemConfigurationService)) as ISystemConfigurationService;

            Terser terser = new Terser(msgEvent.Message);

            // Source and dest
            string sourceData = String.Format("{0}|{1}", terser.Get("/MSH-3"), terser.Get("/MSH-4")),
                destData = String.Format("{0}|{1}", terser.Get("/MSH-5"), terser.Get("/MSH-6"));


            switch (itiName)
            {
                case "ITI-21":
                    {
                        retVal = new AuditData(DateTime.Now, action, outcome, EventIdentifierType.Query, new CodeValue(itiName, "IHE Transactions") { DisplayName = "Patient Demographics Query" });

                        // Audit actor for Patient Identity Source
                        retVal.Actors.Add(new AuditActorData()
                        {
                            UserIsRequestor = true,
                            UserIdentifier = sourceData,
                            ActorRoleCode = new List<CodeValue>() {
                            new  CodeValue("110153", "DCM") { DisplayName = "Source" }
                        },
                            NetworkAccessPointId = msgEvent.SolicitorEndpoint.Host,
                            NetworkAccessPointType = msgEvent.SolicitorEndpoint.HostNameType == UriHostNameType.Dns ? NetworkAccessPointType.MachineName : NetworkAccessPointType.IPAddress
                        });


                        // Add query parameters
                        retVal.AuditableObjects.Add(
                            new AuditableObject()
                            {
                                IDTypeCode = AuditableObjectIdType.Custom,
                                CustomIdTypeCode = new CodeValue(itiName, "IHE Transactions") { DisplayName = "Patient Demographics Query" },
                                QueryData = Convert.ToBase64String(CreateMessageSerialized(msgEvent.Message)),
                                Type = AuditableObjectType.SystemObject,
                                Role = AuditableObjectRole.Query,
                                ObjectId = terser.Get("/QPD-2"),
                                ObjectData = new Dictionary<string, byte[]>()
                                {
                                    { "MSH-10", System.Text.Encoding.ASCII.GetBytes(terser.Get("/MSH-10"))}
                                }
                            }
                        );

                        // Audit actor for PDQ
                        retVal.Actors.Add(new AuditActorData()
                        {
                            UserIdentifier = destData,
                            UserIsRequestor = false,
                            ActorRoleCode = new List<CodeValue>() { new CodeValue("110152", "DCM") { DisplayName = "Destination" } },
                            NetworkAccessPointType = NetworkAccessPointType.MachineName,
                            NetworkAccessPointId = Dns.GetHostName(),
                            AlternativeUserId = Process.GetCurrentProcess().Id.ToString()
                        });
                        break;
                    }
                case "ITI-8":
                    {
                        retVal = new AuditData(DateTime.Now, action, outcome, EventIdentifierType.PatientRecord, new CodeValue(itiName, "IHE Transactions") { DisplayName = "Patient Identity Feed" });

                        // Audit actor for Patient Identity Source
                        retVal.Actors.Add(new AuditActorData()
                        {
                            UserIsRequestor = true,
                            UserIdentifier = sourceData,
                            ActorRoleCode = new List<CodeValue>() {
                            new  CodeValue("110153", "DCM") { DisplayName = "Source" }
                        },
                            NetworkAccessPointId = msgEvent.SolicitorEndpoint.Host,
                            NetworkAccessPointType = msgEvent.SolicitorEndpoint.HostNameType == UriHostNameType.Dns ? NetworkAccessPointType.MachineName : NetworkAccessPointType.IPAddress
                        });

                        // Audit actor for PDQ
                        retVal.Actors.Add(new AuditActorData()
                        {
                            UserIdentifier = destData,
                            UserIsRequestor = false,
                            ActorRoleCode = new List<CodeValue>() { new CodeValue("110152", "DCM") { DisplayName = "Destination" } },
                            NetworkAccessPointType = NetworkAccessPointType.MachineName,
                            NetworkAccessPointId = Dns.GetHostName(),
                            AlternativeUserId = Process.GetCurrentProcess().Id.ToString()
                        });
                        break;
                    }
                case "ITI-9":
                    {
                        retVal = new AuditData(DateTime.Now, action, outcome, EventIdentifierType.Query, new CodeValue(itiName, "IHE Transactions") { DisplayName = "PIX Query" });

                        // Audit actor for Patient Identity Source
                        retVal.Actors.Add(new AuditActorData()
                        {
                            UserIsRequestor = true,
                            UserIdentifier = sourceData,
                            ActorRoleCode = new List<CodeValue>() {
                            new  CodeValue("110153", "DCM") { DisplayName = "Source" }
                        },
                            NetworkAccessPointId = msgEvent.SolicitorEndpoint.Host,
                            NetworkAccessPointType = msgEvent.SolicitorEndpoint.HostNameType == UriHostNameType.Dns ? NetworkAccessPointType.MachineName : NetworkAccessPointType.IPAddress
                        });

                        // Add query parameters
                        retVal.AuditableObjects.Add(
                            new AuditableObject()
                            {
                                IDTypeCode = AuditableObjectIdType.Custom,
                                CustomIdTypeCode = new CodeValue("ITI-9", "IHE Transactions") { DisplayName = "PIX Query" },
                                QueryData = Convert.ToBase64String(CreateMessageSerialized(msgEvent.Message)),
                                Type = AuditableObjectType.SystemObject,
                                Role = AuditableObjectRole.Query,
                                ObjectId = terser.Get("/QPD-2"),
                                ObjectData = new Dictionary<string, byte[]>()
                                {
                                    { "MSH-10", System.Text.Encoding.ASCII.GetBytes(terser.Get("/MSH-10"))}
                                }

                            }
                        );

                        // Audit actor for PDQ
                        retVal.Actors.Add(new AuditActorData()
                        {
                            UserIdentifier = destData,
                            UserIsRequestor = false,
                            ActorRoleCode = new List<CodeValue>() { new CodeValue("110152", "DCM") { DisplayName = "Destination" } },
                            NetworkAccessPointType = NetworkAccessPointType.MachineName,
                            NetworkAccessPointId = Dns.GetHostName(),
                            AlternativeUserId = Process.GetCurrentProcess().Id.ToString()
                        });
                        break;
                    }
            }

            var expDatOid = config.OidRegistrar.GetOid("CR_CID");

            // HACK: Use only patient identifiers in the output
            foreach (var id in identifiers.Where(o => o.Domain != expDatOid.Oid).ToArray())
            {
                RegistrationEvent evt = this.m_dataPersistence.GetContainer(id, true) as RegistrationEvent;
                if (evt != null)
                {
                    identifiers.Remove(id);
                    foreach (Person subj in evt.FindAllComponents(HealthServiceRecordSiteRoleType.SubjectOf))
                        identifiers.Add(new VersionedDomainIdentifier()
                        {
                            Identifier = subj.Id.ToString(),
                            Domain = expDatOid.Oid
                        });
                }
            }

            // Audit patients
            foreach (var id in identifiers)
            {
                // If the id is not a patient then
                // Construct the audit object
                AuditableObject aud = new AuditableObject()
                {
                    IDTypeCode = AuditableObjectIdType.PatientNumber,
                    Role = AuditableObjectRole.Patient,
                    Type = AuditableObjectType.Person
                };

                // Lifecycle
                switch (action)
                {
                    case ActionType.Create:
                        aud.LifecycleType = AuditableObjectLifecycle.Creation;
                        break;
                    case ActionType.Delete:
                        aud.LifecycleType = AuditableObjectLifecycle.LogicalDeletion;
                        break;
                    case ActionType.Execute:
                        aud.LifecycleType = AuditableObjectLifecycle.Access;
                        break;
                    case ActionType.Read:
                        aud.LifecycleType = AuditableObjectLifecycle.Disclosure;
                        break;
                    case ActionType.Update:
                        aud.LifecycleType = AuditableObjectLifecycle.Amendment;
                        break;
                }

                aud.ObjectData.Add("MSH-10", System.Text.Encoding.ASCII.GetBytes(terser.Get("/MSH-10")));
                aud.ObjectId = String.Format("{1}^^^{2}&{0}&ISO", expDatOid.Oid, id.Identifier, expDatOid.Attributes.Find(o => o.Key == "AssigningAuthorityName").Value);
                retVal.AuditableObjects.Add(aud);
            }


            return retVal;
        }

        /// <summary>
        /// Create a serialized message
        /// </summary>
        private byte[] CreateMessageSerialized(NHapi.Base.Model.IMessage iMessage)
        {
            MemoryStream ms = new MemoryStream();
            string msg = new PipeParser().Encode(iMessage);
            ms.Write(Encoding.ASCII.GetBytes(msg), 0, msg.Length);
            ms.Flush();
            return ms.GetBuffer();
            
        }

        /// <summary>
        /// Context
        /// </summary>
        public IServiceProvider Context
        {
            get { return this.m_context; }
            set
            {
                this.m_context = value;
                this.m_configService = this.m_context.GetService(typeof(ISystemConfigurationService)) as ISystemConfigurationService;
                this.m_dataPersistence = this.m_context.GetService(typeof(IDataPersistenceService)) as IDataPersistenceService;
            }
        }

    }
}
