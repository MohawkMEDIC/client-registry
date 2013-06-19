using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MARC.HI.EHRS.SVC.Core.DataTypes;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.ServiceModel.Channels;
using System.Net;

namespace MARC.HI.EHRS.CR.Messaging.FHIR.Util
{
    /// <summary>
    /// Auditing utility
    /// </summary>
    public static class AuditUtil
    {

        /// <summary>
        /// Create audit data
        /// </summary>
        public static AuditData CreateAuditData(IEnumerable<VersionedDomainIdentifier> patientRecord)
        {
            // Audit data
            AuditData retVal = null;

            AuditableObjectLifecycle lifecycle = AuditableObjectLifecycle.Access;

            // Get the actor information
            string userId = String.Empty;
            if (OperationContext.Current.Channel.RemoteAddress != null && OperationContext.Current.Channel.RemoteAddress.Uri != null)
                userId = OperationContext.Current.Channel.RemoteAddress.Uri.OriginalString;
            else if (OperationContext.Current.ServiceSecurityContext != null && OperationContext.Current.ServiceSecurityContext.PrimaryIdentity != null)
                userId = OperationContext.Current.ServiceSecurityContext.PrimaryIdentity.Name;

            MessageProperties properties = OperationContext.Current.IncomingMessageProperties;
            RemoteEndpointMessageProperty endpoint = properties[RemoteEndpointMessageProperty.Name] as RemoteEndpointMessageProperty;
            string remoteEndpoint = "http://anonymous";
            if(endpoint != null)
                remoteEndpoint = endpoint.Address;

            switch (WebOperationContext.Current.IncomingRequest.Method)
            {
                case "GET":
                    {
                        retVal = new AuditData(DateTime.Now, ActionType.Read, OutcomeIndicator.Success, EventIdentifierType.Query, new CodeValue(
                            String.Format("GET {0}", WebOperationContext.Current.IncomingRequest.UriTemplateMatch.RequestUri.OriginalString), "http://marc-hi.ca/fhir/actions"));

                        // Audit actor for Patient Identity Source
                        retVal.Actors.Add(new AuditActorData()
                        {
                            UserIsRequestor = true,
                            UserIdentifier = remoteEndpoint,
                            ActorRoleCode = new List<CodeValue>() {
                            new  CodeValue("110153", "DCM") { DisplayName = "Source" }
                        },
                            NetworkAccessPointId = remoteEndpoint,
                            NetworkAccessPointType = NetworkAccessPointType.IPAddress,
                            UserName = userId
                        });
                        // Audit actor for FHIR service
                        retVal.Actors.Add(new AuditActorData()
                        {
                            UserIdentifier = WebOperationContext.Current.IncomingRequest.UriTemplateMatch.RequestUri.ToString(),
                            UserIsRequestor = false,
                            ActorRoleCode = new List<CodeValue>() { new CodeValue("110152", "DCM") { DisplayName = "Destination" } },
                            NetworkAccessPointType = NetworkAccessPointType.MachineName,
                            NetworkAccessPointId = Dns.GetHostName()
                        });

                        break;
                    }
                default:
                    {
                        retVal = new AuditData(DateTime.Now, ActionType.Execute, OutcomeIndicator.Success, EventIdentifierType.ApplicationActivity, new CodeValue(
                            String.Format("GET {0}", WebOperationContext.Current.IncomingRequest.UriTemplateMatch.RequestUri.OriginalString), "http://marc-hi.ca/fhir/actions"));

                        break;
                    }
            }

            if(patientRecord != null)
                foreach (var pat in patientRecord)
                {
                    // Construct the audit object
                    AuditableObject aud = new AuditableObject()
                    {
                        IDTypeCode = AuditableObjectIdType.PatientNumber,
                        Role = AuditableObjectRole.Patient,
                        Type = AuditableObjectType.Person
                    };

                    // Lifecycle
                    switch (retVal.ActionCode.Value)
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

                    aud.ObjectId = String.Format("{1}^^^&{0}&ISO", pat.Domain, pat.Identifier);
                    retVal.AuditableObjects.Add(aud);

                }
            return retVal;
        }

    }
}
