using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using MARC.HI.EHRS.SVC.Core.DataTypes;
using MARC.Everest.Connectors;
using MARC.HI.EHRS.SVC.Core.Issues;
using System.Data.Common;
using MARC.HI.EHRS.SVC.Core.Exceptions;
using MARC.HI.EHRS.CR.Core.ComponentModel;
using MARC.Everest.Connectors.WCF;
using System.Net;
using MARC.HI.EHRS.SVC.Core.ComponentModel.Components;

namespace MARC.HI.EHRS.CR.Messaging.Everest.MessageReceiver.UV
{
    /// <summary>
    /// Data utility class specifically for IHE PIX transaction(s)
    /// </summary>
    public class IheDataUtil : DataUtil
    {

        /// <summary>
        /// Create audit data
        /// </summary>
        public AuditData CreateAuditData(string itiName, ActionType action, OutcomeIndicator outcome, UnsolicitedDataEventArgs msgEvent, IReceiveResult msgReceiveResult, IEnumerable<VersionedDomainIdentifier> patientRecord, HealthcareParticipant author)
        {
            // Audit data
            AuditData retVal = null;

            AuditableObjectLifecycle lifecycle = AuditableObjectLifecycle.Access;
            var wcfReceiveResult = msgReceiveResult as WcfReceiveResult;

            switch (itiName)
            {
                case "ITI-44":
                    retVal = new AuditData(DateTime.Now, action, outcome, EventIdentifierType.PatientRecord, new CodeValue(itiName, "IHE Transactions"));

                    var msgReplyTo = wcfReceiveResult == null || wcfReceiveResult.Headers == null || wcfReceiveResult.Headers.ReplyTo == null ? msgEvent.SolicitorEndpoint.ToString() : wcfReceiveResult.Headers.ReplyTo.Uri.ToString();
                    // Audit actor for Patient Identity Source
                    retVal.Actors.Add( new AuditActorData() {
                        UserIsRequestor = true, 
                        UserIdentifier = msgReplyTo,
                        ActorRoleCode = new List<CodeValue>() {
                            new  CodeValue("110153", "DCM") { DisplayName = "Source" }
                        }, 
                        NetworkAccessPointId = msgEvent.SolicitorEndpoint.Host,
                        NetworkAccessPointType = msgEvent.SolicitorEndpoint.HostNameType == UriHostNameType.Dns ? NetworkAccessPointType.MachineName : NetworkAccessPointType.IPAddress
                    });
                    // Audit actor for PIX manager
                    retVal.Actors.Add(new AuditActorData()
                    {
                        UserIdentifier = msgEvent.ReceiveEndpoint.ToString(),
                        UserIsRequestor = false,
                        ActorRoleCode = new List<CodeValue>() { new CodeValue("110152", "DCM") { DisplayName = "Destination" } },
                        NetworkAccessPointType = NetworkAccessPointType.MachineName,
                        NetworkAccessPointId = Dns.GetHostName()
                    });
                    break;
            }

            // Audit authors
            if (author != null)
            {
                retVal.AuditableObjects.Add(new AuditableObject()
                {
                    IDTypeCode = AuditableObjectIdType.UserIdentifier,
                    ObjectId = String.Format("{1}^^^&{0}&ISO", m_configService.OidRegistrar.GetOid("CR_PID"), author.Id),
                    Role = AuditableObjectRole.Provider,
                    Type = AuditableObjectType.Person,
                    LifecycleType = (AuditableObjectLifecycle?)(action == ActionType.Read ? (object)AuditableObjectLifecycle.ReceiptOfDisclosure : null)
                });

            }
            // Audit patients
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

                aud.ObjectId = String.Format("{1}^^^&{0}&ISO", pat.Domain, pat.Identifier);
                retVal.AuditableObjects.Add(aud);

            }
            return retVal;
        }

        /// <summary>
        /// Register a client in the registry
        /// </summary>
        internal override SVC.Core.DataTypes.VersionedDomainIdentifier Register(Core.ComponentModel.RegistrationEvent healthServiceRecord, List<MARC.Everest.Connectors.IResultDetail> dtls, List<SVC.Core.Issues.DetectedIssue> issues, SVC.Core.Services.DataPersistenceMode mode)
        {
            
            try
            {

                // Can't find persistence
                if (this.m_persistenceService == null)
                {
                    dtls.Add(new PersistenceResultDetail(ResultDetailType.Error, "Couldn't locate an implementation of a PersistenceService object, storage is aborted", null));
                    return null;
                }
                else if (healthServiceRecord == null)
                {
                    dtls.Add(new PersistenceResultDetail(ResultDetailType.Error, "Can't register null health service record data", null));
                    return null;
                }
                else if (dtls.Count(o => o.Type == ResultDetailType.Error) > 0)
                {
                    dtls.Add(new PersistenceResultDetail(ResultDetailType.Error, "Won't attempt to persist invalid message", null));
                    return null;
                }
               
                // First, IHE is a little different first we have to see if we can match any of the records for cross referencing
                // therefore we do a query, first with identifiers and then without identifiers, 100% match
                QueryParameters qp = new QueryParameters()
                {
                    Confidence = 1.0f,
                    MatchingAlgorithm = MatchAlgorithm.Exact,
                    MatchStrength = MatchStrength.Exact
                };
                var patientQuery = healthServiceRecord.Clone() as RegistrationEvent;
                patientQuery.Add(qp, "FLT", SVC.Core.ComponentModel.HealthServiceRecordSiteRoleType.FilterOf, null);
                // Perform the query
                var pid = this.m_docRegService.QueryRecord(patientQuery);
                if (pid.Length == 0)
                {
                    // Get rid of identifiers
                    var subject = (healthServiceRecord.FindComponent(SVC.Core.ComponentModel.HealthServiceRecordSiteRoleType.SubjectOf) as Person).Clone() as Person;
                    patientQuery.RemoveAllFromRole(SVC.Core.ComponentModel.HealthServiceRecordSiteRoleType.SubjectOf);
                    subject.AlternateIdentifiers = new List<DomainIdentifier>();
                    patientQuery.Add(subject, "SUBJ", SVC.Core.ComponentModel.HealthServiceRecordSiteRoleType.SubjectOf, subject.AlternateIdentifiers);
                    pid = this.m_docRegService.QueryRecord(patientQuery); // Try to cross reference again
                }
                    
                // Did we cross reference a patient?
                if (pid.Length == 1)
                {
                    // Add the pid to the list of registration event identifiers
                    healthServiceRecord.AlternateIdentifier = pid[0];
                    // Update
                    var vid = this.Update(healthServiceRecord, dtls, issues, mode);
                    return vid;
                }
                else if (pid.Length > 1) // Add a warning
                    issues.Add(new DetectedIssue()
                    {
                        Priority = IssuePriorityType.Warning,
                        Severity = IssueSeverityType.Moderate,
                        Text = m_localeService.GetString("DTPW001"), 
                        Type = IssueType.DetectedIssue
                    });

                // Call the dss
                if (this.m_decisionService != null)
                    issues.AddRange(this.m_decisionService.RecordPersisting(healthServiceRecord));

                // Any errors?
                if (issues.Count(o => o.Priority == IssuePriorityType.Error) > 0)
                    dtls.Add(new PersistenceResultDetail(ResultDetailType.Error, "Won't attempt to persist message due to detected issues", null));

                // Persist
                var retVal = this.m_persistenceService.StoreContainer(healthServiceRecord, mode);
                retVal.UpdateMode = UpdateModeType.Add;

                // Call the dss
                if (this.m_decisionService != null)
                    this.m_decisionService.RecordPersisted(healthServiceRecord);

                // Register the document set if it is a document
                if (retVal != null && this.m_docRegService != null && !this.m_docRegService.RegisterRecord(healthServiceRecord, mode))
                    dtls.Add(new PersistenceResultDetail(ResultDetailType.Warning, "Wasn't able to register event in the event registry, event exists in repository but not in registry. You may not be able to query for this event", null));

                return retVal;
            }
            catch (DuplicateNameException ex) // Already persisted stuff
            {
                dtls.Add(new PersistenceResultDetail(ResultDetailType.Error, m_localeService.GetString("DTPE005"), ex));
                issues.Add(new DetectedIssue()
                {
                    Severity = IssueSeverityType.High,
                    Type = IssueType.AlreadyPerformed,
                    Text = ex.Message,
                    Priority = IssuePriorityType.Error
                });
                return null;
            }
            catch (MissingPrimaryKeyException ex) // Already persisted stuff
            {
                dtls.Add(new PersistenceResultDetail(ResultDetailType.Error, m_localeService.GetString("DTPE005"), ex));
                issues.Add(new DetectedIssue()
                {
                    Severity = IssueSeverityType.High,
                    Type = IssueType.DetectedIssue,
                    Text = ex.Message,
                    Priority = IssuePriorityType.Error
                });
                return null;
            }
            catch (ConstraintException ex)
            {
                dtls.Add(new PersistenceResultDetail(ResultDetailType.Error, m_localeService.GetString("DTPE005"), ex));
                issues.Add(new DetectedIssue()
                {
                    Severity = IssueSeverityType.High,
                    Type = IssueType.DetectedIssue,
                    Text = ex.Message,
                    Priority = IssuePriorityType.Error
                });
                return null;
            }
            catch (IssueException ex)
            {
                issues.Add(ex.Issue);
                return null;
            }
            catch (Exception ex)
            {
                dtls.Add(new ResultDetail(ResultDetailType.Error, ex.Message, ex));
                return null;
            }
        }

        /// <summary>
        /// Update the version identifier
        /// </summary>
        internal override VersionedDomainIdentifier Update(RegistrationEvent healthServiceRecord, List<IResultDetail> dtls, List<DetectedIssue> issues, SVC.Core.Services.DataPersistenceMode mode)
        {
            try
            {

                // Can't find persistence
                if (this.m_persistenceService == null)
                {
                    dtls.Add(new PersistenceResultDetail(ResultDetailType.Error, "Couldn't locate an implementation of a PersistenceService object, storage is aborted", null));
                    return null;
                }
                else if (healthServiceRecord == null)
                {
                    dtls.Add(new PersistenceResultDetail(ResultDetailType.Error, "Can't register null health service record data", null));
                    return null;
                }
                else if (dtls.Count(o => o.Type == ResultDetailType.Error) > 0)
                {
                    dtls.Add(new PersistenceResultDetail(ResultDetailType.Error, "Won't attempt to persist invalid message", null));
                    return null;
                }
                
                // Call the dss
                if (this.m_decisionService != null)
                    issues.AddRange(this.m_decisionService.RecordPersisting(healthServiceRecord));

                // Any errors?
                if (issues.Count(o => o.Priority == IssuePriorityType.Error) > 0)
                    dtls.Add(new PersistenceResultDetail(ResultDetailType.Error, "Won't attempt to persist message due to detected issues", null));

                // Persist
                var retVal = this.m_persistenceService.UpdateContainer(healthServiceRecord, mode);
                retVal.UpdateMode = UpdateModeType.Update;

                // Call the dss
                if (this.m_decisionService != null)
                    this.m_decisionService.RecordPersisted(healthServiceRecord);

                // Register the document set if it is a document
                if (retVal != null && this.m_docRegService != null && !this.m_docRegService.RegisterRecord(healthServiceRecord, mode))
                    dtls.Add(new PersistenceResultDetail(ResultDetailType.Warning, "Wasn't able to register event in the event registry, event exists in repository but not in registry. You may not be able to query for this event", null));

                return retVal;
            }
            catch (DuplicateNameException ex) // Already persisted stuff
            {
                dtls.Add(new PersistenceResultDetail(ResultDetailType.Error, m_localeService.GetString("DTPE005"), ex));
                issues.Add(new DetectedIssue()
                {
                    Severity = IssueSeverityType.High,
                    Type = IssueType.AlreadyPerformed,
                    Text = ex.Message,
                    Priority = IssuePriorityType.Error
                });
                return null;
            }
            catch (MissingPrimaryKeyException ex) // Already persisted stuff
            {
                dtls.Add(new PersistenceResultDetail(ResultDetailType.Error, m_localeService.GetString("DTPE005"), ex));
                issues.Add(new DetectedIssue()
                {
                    Severity = IssueSeverityType.High,
                    Type = IssueType.DetectedIssue,
                    Text = ex.Message,
                    Priority = IssuePriorityType.Error
                });
                return null;
            }
            catch (ConstraintException ex)
            {
                dtls.Add(new PersistenceResultDetail(ResultDetailType.Error, m_localeService.GetString("DTPE005"), ex));
                issues.Add(new DetectedIssue()
                {
                    Severity = IssueSeverityType.High,
                    Type = IssueType.DetectedIssue,
                    Text = ex.Message,
                    Priority = IssuePriorityType.Error
                });
                return null;
            }
            catch (IssueException ex)
            {
                issues.Add(ex.Issue);
                return null;
            }
            catch (Exception ex)
            {
                dtls.Add(new ResultDetail(ResultDetailType.Error, ex.Message, ex));
                return null;
            }
        }

    }
}
