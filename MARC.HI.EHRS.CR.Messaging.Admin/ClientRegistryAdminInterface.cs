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
 * Date: 25-2-2013
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MARC.HI.EHRS.CR.Messaging.Admin.Configuration;
using System.Configuration;
using MARC.HI.EHRS.SVC.Core.Services;
using System.Diagnostics;
using System.ServiceModel;
using MARC.HI.EHRS.CR.Core.ComponentModel;
using MARC.HI.EHRS.SVC.Core.DataTypes;
using MARC.Everest.Threading;
using System.ServiceModel.Channels;
using MARC.HI.EHRS.CR.Core.Services;
using MARC.HI.EHRS.SVC.Core.Logging;
using System.IO;
using MARC.HI.EHRS.CR.Messaging.Admin.Contract;
using System.Reflection;
using System.ComponentModel;

namespace MARC.HI.EHRS.CR.Messaging.Admin
{
    /// <summary>
    /// Client registry interface
    /// </summary>
    [ServiceBehavior()]
    public class ClientRegistryAdminInterface : IClientRegistryAdminInterface, IMessageHandlerService
    {

        // Configuration
        private ClientRegistryInterfaceConfiguration m_configuration;
        // Service host
        private ServiceHost m_serviceHost;
        // Context
        private IServiceProvider m_context;

        /// <summary>
        /// Creates a new instance of the client registry interface
        /// </summary>
        public ClientRegistryAdminInterface()
        {
            this.m_configuration = ConfigurationManager.GetSection("marc.hi.ehrs.cr.messaging.admin") as ClientRegistryInterfaceConfiguration;
        }


        #region IClientRegistryInterface Members


        /// <summary>
        /// Get all registrations matching the query prototype
        /// </summary>
        public RegistrationEventCollection GetRegistrations(Person queryPrototype, int offset, int count)
        {
            // Get all Services
            IAuditorService auditSvc = ApplicationContext.CurrentContext.GetService(typeof(IAuditorService)) as IAuditorService;
            IDataRegistrationService regSvc = ApplicationContext.CurrentContext.GetService(typeof(IDataRegistrationService)) as IDataRegistrationService;
            IDataPersistenceService repSvc = ApplicationContext.CurrentContext.GetService(typeof(IDataPersistenceService)) as IDataPersistenceService;

             // Audit message
            AuditData audit = this.ConstructAuditData(ActionType.Read, EventIdentifierType.Export);
            audit.EventTypeCode = new CodeValue("ADM_GetRegistrations");

            try
            {
                // Result identifiers
                VersionedDomainIdentifier[] vids = null;
                var dummyQuery = new QueryEvent();
                var regEvt = new RegistrationEvent() { EventClassifier = RegistrationEventType.Register };
                dummyQuery.Add(regEvt, "SUBJ", SVC.Core.ComponentModel.HealthServiceRecordSiteRoleType.SubjectOf, null);

                if (queryPrototype == null)
                    regEvt.Add(new Person(), "SUBJ", SVC.Core.ComponentModel.HealthServiceRecordSiteRoleType.SubjectOf, null);
                else
                    regEvt.Add(queryPrototype, "SUBJ", SVC.Core.ComponentModel.HealthServiceRecordSiteRoleType.SubjectOf, null);
                vids = regSvc.QueryRecord(dummyQuery);

                RegistrationEventCollection retVal = new RegistrationEventCollection();
                Object syncLock = new object();
                retVal.Count = vids.Length;
                // Now fetch each one asynchronously
                using (WaitThreadPool thdPool = new WaitThreadPool(Environment.ProcessorCount * 2))
                {
                    foreach (var id in vids.Skip(offset).Take(count))
                        thdPool.QueueUserWorkItem(
                            delegate(object state)
                            {
                                try
                                {
                                    var itm = repSvc.GetContainer(state as VersionedDomainIdentifier, true);

                                    lock (syncLock)
                                        retVal.Event.Add(itm as RegistrationEvent);
                                }
                                catch (Exception e)
                                {
                                    Trace.TraceError("Could not fetch result {0} : {1}", (state as VersionedDomainIdentifier).Identifier, e.ToString());
                                }
                            }
                            , id);

                    // Wait until fetch is done
                    thdPool.WaitOne(new TimeSpan(0, 0, 30), false);
                }
                //retVal.Event.Sort((a, b) => b.Timestamp.CompareTo(a.Timestamp));
                // Add audit data
                foreach(var res in retVal.Event)
                    audit.AuditableObjects.Add(new AuditableObject() {
                        IDTypeCode = AuditableObjectIdType.ReportNumber,
                        LifecycleType = AuditableObjectLifecycle.Export,
                        ObjectId = String.Format("{0}^^^&{1}&ISO", res.AlternateIdentifier.Identifier, res.AlternateIdentifier.Domain),
                        Role = AuditableObjectRole.MasterFile,
                        Type = AuditableObjectType.SystemObject,
                        QueryData = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("loadFast=true"))
                    });
                return retVal;
            }
            catch(Exception e)
            {
                Trace.TraceError("Could not execute GetRegistrations : {0}", e.ToString());
                audit.Outcome = OutcomeIndicator.EpicFail;
#if DEBUG
                throw new FaultException(new FaultReason(e.ToString()), new FaultCode(e.GetType().Name));
                
#else
                throw new FaultException(new FaultReason(e.Message), new FaultCode(e.GetType().Name));
#endif
            }
            finally
            {
                if(auditSvc != null)
                    auditSvc.SendAudit(audit);
            }
        }

        /// <summary>
        /// Get one registration event
        /// </summary>
        public Core.ComponentModel.RegistrationEvent GetRegistrationEvent(decimal id)
        {
            // Get all Services
            IAuditorService auditSvc = ApplicationContext.CurrentContext.GetService(typeof(IAuditorService)) as IAuditorService;
            IDataPersistenceService repSvc = ApplicationContext.CurrentContext.GetService(typeof(IDataPersistenceService)) as IDataPersistenceService;

            // Audit message
            AuditData audit = this.ConstructAuditData(ActionType.Read, EventIdentifierType.Export);
            audit.EventTypeCode = new CodeValue("ADM_GetRegistrationEvent");

            try
            {
                // Result identifiers
                var retVal = repSvc.GetContainer(new VersionedDomainIdentifier() {
                    Domain = ApplicationContext.ConfigurationService.OidRegistrar.GetOid("REG_EVT").Oid,
                    Identifier = id.ToString()
                }, false) as RegistrationEvent;

                // Add audit data
                if (retVal != null)
                {
                    audit.AuditableObjects.Add(new AuditableObject()
                    {
                        IDTypeCode = AuditableObjectIdType.ReportNumber,
                        LifecycleType = AuditableObjectLifecycle.Export,
                        ObjectId = String.Format("{0}^^^&{1}&ISO", retVal.AlternateIdentifier.Identifier, retVal.AlternateIdentifier.Domain),
                        Role = AuditableObjectRole.MasterFile,
                        Type = AuditableObjectType.SystemObject,
                        QueryData = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("loadFast=false"))
                    });
                }
                return retVal;
            }
            catch (Exception e)
            {
                Trace.TraceError("Could not execute GetRegistration : {0}", e.ToString());
                audit.Outcome = OutcomeIndicator.EpicFail;
#if DEBUG
                throw new FaultException(new FaultReason(e.ToString()), new FaultCode(e.GetType().Name));

#else
                throw new FaultException(new FaultReason(e.Message), new FaultCode(e.GetType().Name));
#endif
            }
            finally
            {
                if (auditSvc != null)
                    auditSvc.SendAudit(audit);
            }
        }

        /// <summary>
        /// Get a specific conflict
        /// </summary>
        public ConflictCollection GetConflict(decimal id)
        {
            // Get all Services
            IAuditorService auditSvc = ApplicationContext.CurrentContext.GetService(typeof(IAuditorService)) as IAuditorService;
            IDataPersistenceService repSvc = ApplicationContext.CurrentContext.GetService(typeof(IDataPersistenceService)) as IDataPersistenceService;
            IClientRegistryMergeService mergeSvc = ApplicationContext.CurrentContext.GetService(typeof(IClientRegistryMergeService)) as IClientRegistryMergeService;

            // Audit message
            AuditData audit = this.ConstructAuditData(ActionType.Read, EventIdentifierType.Export);
            audit.EventTypeCode = new CodeValue("ADM_GetConflict");
            try
            {

                var vid = new VersionedDomainIdentifier()
                {
                    Domain = ApplicationContext.ConfigurationService.OidRegistrar.GetOid("REG_EVT").Oid,
                    Identifier = id.ToString()
                };

                // Get all with a merge
                var mergeResults = mergeSvc.GetConflicts(vid);

                var retVal = new ConflictCollection();
                // Construct the return, and load match
                var conf = new Conflict()
                {
                    Source = repSvc.GetContainer(vid, true) as RegistrationEvent
                };

                // Add audit data
                audit.AuditableObjects.Add(new AuditableObject()
                {
                    IDTypeCode = AuditableObjectIdType.ReportNumber,
                    LifecycleType = AuditableObjectLifecycle.Export,
                    ObjectId = String.Format("{0}^^^&{1}&ISO", conf.Source.AlternateIdentifier.Identifier, conf.Source.AlternateIdentifier.Domain),
                    Role = AuditableObjectRole.MasterFile,
                    Type = AuditableObjectType.SystemObject,
                    QueryData = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("loadFast=false"))
                });

                // Load the matches
                foreach (var match in mergeResults)
                {
                    var matchRecord = repSvc.GetContainer(match, true) as RegistrationEvent;
                    conf.Match.Add(matchRecord);
                    // Add audit data
                    audit.AuditableObjects.Add(new AuditableObject()
                    {
                        IDTypeCode = AuditableObjectIdType.ReportNumber,
                        LifecycleType = AuditableObjectLifecycle.Export,
                        ObjectId = String.Format("{0}^^^&{1}&ISO", matchRecord.AlternateIdentifier.Identifier, matchRecord.AlternateIdentifier.Domain),
                        Role = AuditableObjectRole.MasterFile,
                        Type = AuditableObjectType.SystemObject,
                        QueryData = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("loadFast=false"))
                    });
                }

                retVal.Conflict.Add(conf);

                return retVal;
            }
            catch (Exception e)
            {
                Trace.TraceError("Could not execute GetConflicts : {0}", e.ToString());
                audit.Outcome = OutcomeIndicator.EpicFail;
#if DEBUG
                throw new FaultException(new FaultReason(e.ToString()), new FaultCode(e.GetType().Name));

#else
                throw new FaultException(new FaultReason(e.Message), new FaultCode(e.GetType().Name));
#endif
            }
            finally
            {
                if (auditSvc != null)
                    auditSvc.SendAudit(audit);
            }
        }

        /// <summary>
        /// Get all merge candidates
        /// </summary>
        public ConflictCollection GetConflicts(int offset, int count, bool identifierOnly)
        {
            // Get all Services
            IAuditorService auditSvc = ApplicationContext.CurrentContext.GetService(typeof(IAuditorService)) as IAuditorService;
            IDataPersistenceService repSvc = ApplicationContext.CurrentContext.GetService(typeof(IDataPersistenceService)) as IDataPersistenceService;
            IClientRegistryMergeService mergeSvc = ApplicationContext.CurrentContext.GetService(typeof(IClientRegistryMergeService)) as IClientRegistryMergeService;

            // Audit message
            AuditData audit = this.ConstructAuditData(ActionType.Read, EventIdentifierType.Export);
            audit.EventTypeCode = new CodeValue("ADM_GetConflicts");
            try
            {
                
                // Get all with a merge
                var mergeResults = mergeSvc.GetOutstandingConflicts();

                var retVal = new ConflictCollection();
                retVal.Count = mergeResults.Count();

                // Loop and load
                foreach (var merge in mergeResults.Skip(offset).Take(count))
                {
                    // Construct the return, and load match
                    Conflict conf = new Conflict();

                    if (!identifierOnly)
                    {
                        conf.Source = repSvc.GetContainer(merge, true) as RegistrationEvent;
                        // Add audit data
                        audit.AuditableObjects.Add(new AuditableObject()
                        {
                            IDTypeCode = AuditableObjectIdType.ReportNumber,
                            LifecycleType = AuditableObjectLifecycle.Export,
                            ObjectId = String.Format("{0}^^^&{1}&ISO", conf.Source.AlternateIdentifier.Identifier, conf.Source.AlternateIdentifier.Domain),
                            Role = AuditableObjectRole.MasterFile,
                            Type = AuditableObjectType.SystemObject,
                            QueryData = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("loadFast=false"))
                        });
                    }
                

                    // Load the matches
                    foreach (var match in mergeSvc.GetConflicts(merge))
                    {
                        if (!identifierOnly)
                        {
                            var matchRecord = repSvc.GetContainer(match, true) as RegistrationEvent;
                            conf.Match.Add(matchRecord);
                            // Add audit data
                            audit.AuditableObjects.Add(new AuditableObject()
                            {
                                IDTypeCode = AuditableObjectIdType.ReportNumber,
                                LifecycleType = AuditableObjectLifecycle.Export,
                                ObjectId = String.Format("{0}^^^&{1}&ISO", matchRecord.AlternateIdentifier.Identifier, matchRecord.AlternateIdentifier.Domain),
                                Role = AuditableObjectRole.MasterFile,
                                Type = AuditableObjectType.SystemObject,
                                QueryData = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("loadFast=false"))
                            });
                        }
                        else
                            conf.Match.Add(new RegistrationEvent()
                            {
                                AlternateIdentifier = match
                            });
                    }

                    retVal.Conflict.Add(conf);
                }
               
                return retVal;
            }
            catch (Exception e)
            {
                Trace.TraceError("Could not execute GetConflicts : {0}", e.ToString());
                audit.Outcome = OutcomeIndicator.EpicFail;
#if DEBUG
                throw new FaultException(new FaultReason(e.ToString()), new FaultCode(e.GetType().Name));

#else
                throw new FaultException(new FaultReason(e.Message), new FaultCode(e.GetType().Name));
#endif
            }
            finally
            {
                if (auditSvc != null)
                    auditSvc.SendAudit(audit);
            }
        }

        /// <summary>
        /// Construct audit data
        /// </summary>
        /// <returns></returns>
        private AuditData ConstructAuditData(ActionType action, EventIdentifierType identifier)
        {
            AuditData audit = new AuditData(DateTime.Now, action, OutcomeIndicator.Success, identifier, null);
            audit.Actors.Add(new AuditActorData()
            {
                NetworkAccessPointId = OperationContext.Current.IncomingMessageHeaders.To.ToString(),
                NetworkAccessPointType = NetworkAccessPointType.IPAddress,
                UserName = Environment.UserName,
                UserIdentifier = Environment.UserName,
                UserIsRequestor = false,
                ActorRoleCode = new List<CodeValue>() {
                            new  CodeValue("110152", "DCM") { DisplayName = "Destination" }
                        }, 
            });
            audit.Actors.Add(new AuditActorData()
            {
                NetworkAccessPointId = (OperationContext.Current.IncomingMessageProperties[RemoteEndpointMessageProperty.Name] as RemoteEndpointMessageProperty).Address,
                NetworkAccessPointType = NetworkAccessPointType.IPAddress,
                UserIsRequestor = true,
                UserIdentifier = (OperationContext.Current.IncomingMessageProperties[RemoteEndpointMessageProperty.Name] as RemoteEndpointMessageProperty).Address,
                ActorRoleCode = new List<CodeValue>() {
                            new  CodeValue("110153", "DCM") { DisplayName = "Source" }
                        }, 
            });
            return audit;
        }

        /// <summary>
        /// Resolve an item
        /// </summary>
        /// <param name="sourceId"></param>
        public void Resolve(decimal sourceId)
        {
            // Get all Services
            IAuditorService auditSvc = ApplicationContext.CurrentContext.GetService(typeof(IAuditorService)) as IAuditorService;
            IClientRegistryMergeService mergeSvc = ApplicationContext.CurrentContext.GetService(typeof(IClientRegistryMergeService)) as IClientRegistryMergeService;

            // Audit message
            AuditData auditMessage = this.ConstructAuditData(ActionType.Update, EventIdentifierType.ApplicationActivity);
            auditMessage.EventTypeCode = new CodeValue("ADM_Resolve");
            try
            {

                // Prepare merge
                VersionedDomainIdentifier resolveId = new VersionedDomainIdentifier()
                {
                    Domain = ApplicationContext.ConfigurationService.OidRegistrar.GetOid("REG_EVT").Oid,
                    Identifier = sourceId.ToString()
                };

                // Merge 
                mergeSvc.MarkResolved(resolveId);

                
                auditMessage.AuditableObjects.Add(new AuditableObject()
                {
                    IDTypeCode = AuditableObjectIdType.ReportNumber,
                    LifecycleType = AuditableObjectLifecycle.Verification,
                    ObjectId = string.Format("{0}^^^&{1}&ISO", resolveId.Identifier, resolveId.Domain),
                    Role = AuditableObjectRole.Job,
                    Type = AuditableObjectType.SystemObject
                });

            }
            catch (Exception e)
            {
                auditMessage.Outcome = OutcomeIndicator.EpicFail;
                Trace.TraceError("Could not execute Merge : {0}", e.ToString());
#if DEBUG
                throw new FaultException(new FaultReason(e.ToString()), new FaultCode(e.GetType().Name));

#else
                throw new FaultException(new FaultReason(e.Message), new FaultCode(e.GetType().Name));
#endif
            }
            finally
            {
                if (auditSvc != null)
                        auditSvc.SendAudit(auditMessage);
            }
        }

        /// <summary>
        /// Perform a merge
        /// </summary>
        public Core.ComponentModel.RegistrationEvent Merge(decimal[] sourceIds, decimal targetId)
        {
            // Get all Services
            IAuditorService auditSvc = ApplicationContext.CurrentContext.GetService(typeof(IAuditorService)) as IAuditorService;
            IDataPersistenceService repSvc = ApplicationContext.CurrentContext.GetService(typeof(IDataPersistenceService)) as IDataPersistenceService;
            IClientRegistryMergeService mergeSvc = ApplicationContext.CurrentContext.GetService(typeof(IClientRegistryMergeService)) as IClientRegistryMergeService;

            // Audit message
            List<AuditData> auditMessages = new List<AuditData>();

            try
            {

                // Prepare merge
                List<VersionedDomainIdentifier> domainId = new List<VersionedDomainIdentifier>(sourceIds.Length);
                foreach (var srcId in sourceIds)
                {
                    domainId.Add(new VersionedDomainIdentifier()
                    {
                        Domain = ApplicationContext.ConfigurationService.OidRegistrar.GetOid("REG_EVT").Oid,
                        Identifier = srcId.ToString()
                    });

                    var am = ConstructAuditData(ActionType.Delete, EventIdentifierType.Import);
                    am.EventTypeCode = new CodeValue("ADM_Merge");
                    am.AuditableObjects.Add(new AuditableObject()
                    {
                        IDTypeCode = AuditableObjectIdType.ReportNumber,
                        LifecycleType = AuditableObjectLifecycle.LogicalDeletion,
                        ObjectId = String.Format("{0}^^^&{1}&ISO", srcId, ApplicationContext.ConfigurationService.OidRegistrar.GetOid("REG_EVT").Oid),
                        Role = AuditableObjectRole.MasterFile,
                        Type = AuditableObjectType.SystemObject
                    });
                    auditMessages.Add(am);
                }
                VersionedDomainIdentifier survivorId = new VersionedDomainIdentifier()
                {
                    Domain = ApplicationContext.ConfigurationService.OidRegistrar.GetOid("REG_EVT").Oid,
                    Identifier = targetId.ToString()
                };
                var updateAudit = ConstructAuditData(ActionType.Update, EventIdentifierType.Import);
                updateAudit.EventTypeCode = new CodeValue("ADM_Merge");
                updateAudit.AuditableObjects.Add(new AuditableObject()
                {
                    IDTypeCode = AuditableObjectIdType.ReportNumber,
                    LifecycleType = AuditableObjectLifecycle.Amendment,
                    ObjectId = String.Format("{0}^^^&{1}&ISO", survivorId.Identifier, survivorId.Domain),
                    Role = AuditableObjectRole.MasterFile,
                    Type = AuditableObjectType.SystemObject
                });
                auditMessages.Add(updateAudit);

                // Merge 
                mergeSvc.Resolve(domainId, survivorId, DataPersistenceMode.Production);
                
                // Now load
                return repSvc.GetContainer(survivorId, true) as RegistrationEvent;
            }
            catch (Exception e)
            {
                foreach (var am in auditMessages)
                    am.Outcome = OutcomeIndicator.EpicFail;
                Trace.TraceError("Could not execute Merge : {0}", e.ToString());
#if DEBUG
                throw new FaultException(new FaultReason(e.ToString()), new FaultCode(e.GetType().Name));

#else
                throw new FaultException(new FaultReason(e.Message), new FaultCode(e.GetType().Name));
#endif
            }
            finally
            {
                if (auditSvc != null)
                    foreach(var am in auditMessages)
                        auditSvc.SendAudit(am);
            }
        }

        #endregion

        #region IMessageHandlerService Members

        /// <summary>
        /// Start the administrative interface
        /// </summary>
        public bool Start()
        {
            Trace.TraceInformation("Starting administrative service...");
            try
            {
                this.m_serviceHost = new MARC.Everest.Connectors.WCF.Core.WcfServiceHost(this.m_configuration.WcfServiceName, typeof(ClientRegistryAdminInterface));
                this.m_serviceHost.Open();
                return true;
            }
            catch (Exception e)
            {
                Trace.TraceError("Could not start administrative interface : {0}", e.ToString());
                return false;
            }
        }

        /// <summary>
        /// Stop the service host
        /// </summary>
        public bool Stop()
        {
            if (this.m_serviceHost != null)
                this.m_serviceHost.Close();
            return true;
        }

        #endregion

        #region IUsesHostContext Members

        /// <summary>
        /// Gets or sets the host context
        /// </summary>
        public IServiceProvider Context
        {
            get
            {
                return this.m_context;
            }
            set
            {
                this.m_context = value;
                ApplicationContext.CurrentContext = value;
            }
        }

        #endregion

        /// <summary>
        /// Get all log files
        /// </summary>
        public List<LogInfo> GetLogFiles()
        {
            // Get all log files
            foreach (var tl in Trace.Listeners)
            {
                if (tl is RollOverTextWriterTraceListener)
                {
                    var fname = (tl as RollOverTextWriterTraceListener).FileName;
                    List<LogInfo> retVal = new List<LogInfo>();
                    foreach (var fn in Directory.GetFiles(Path.GetDirectoryName(fname), Path.GetFileNameWithoutExtension(fname) + "*.*"))
                    {
                        FileInfo fi = new FileInfo(fn);
                        LogInfo itm = new LogInfo()
                        {
                            Id = Path.GetFileNameWithoutExtension(fn),
                            LastModified = fi.LastWriteTime,
                            Size = fi.Length
                        };
                        retVal.Add(itm);
                    }
                    return retVal;
                }
            }
            return new List<LogInfo>();
        }

        /// <summary>
        /// Get the specified log file
        /// </summary>
        public String GetLog(string id)
        {
            RollOverTextWriterTraceListener traceListener = null;
            foreach (var tl in Trace.Listeners)
            {
                if (tl is RollOverTextWriterTraceListener)
                    traceListener = tl as RollOverTextWriterTraceListener;
            }

            if (traceListener == null)
                return null;
            else
            {
                try {
                    using (FileStream fs = File.Open(Path.ChangeExtension(Path.Combine(Path.GetDirectoryName(traceListener.FileName), id), Path.GetExtension(traceListener.FileName)), FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    using (StreamReader sw = new StreamReader(fs))
                        return sw.ReadToEnd();
                }
                catch(Exception e)
                {
                    Trace.TraceError(e.ToString());
                }
                return null; 
            }

        }

        /// <summary>
        /// Services loaded
        /// </summary>
        public List<ServiceStatus> GetServices()
        {
            // First get all services
            List<ServiceStatus> stats = new List<ServiceStatus>();
            stats.Add(new ServiceStatus()
            {
                Contract = "MARC-HI Client Registry Core",
                Class = "Core Implementation CR",
                Version = Assembly.GetEntryAssembly().GetName().Version.ToString()
            });
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies().Where(o => { try { return Path.GetDirectoryName(o.Location) == Path.GetDirectoryName(Assembly.GetEntryAssembly().Location); } catch { return false; } }))
                foreach (var typ in asm.GetTypes().Where(o => o.IsInterface))
                {
                    if (typ.GetInterface(typeof(IUsesHostContext).FullName) != null ||
                        typ.Namespace != null && typ.Namespace.EndsWith("Services"))
                    {
                        string typName = typ.Name;
                        var desc = typ.GetCustomAttributes(typeof(DescriptionAttribute), false);
                        if (desc.Length > 0)
                            typName = (desc[0] as DescriptionAttribute).Description;

                        var stat = new ServiceStatus() { Contract = typName };
                        var obj = ApplicationContext.CurrentContext.GetService(typ);
                        if (obj != null)
                        {
                            typName = obj.GetType().FullName;
                            desc = obj.GetType().GetCustomAttributes(typeof(DescriptionAttribute), false);
                            if (desc.Length > 0)
                                typName = (desc[0] as DescriptionAttribute).Description;

                            stat.Class = typName;
                        }

                        stat.Version = asm.GetName().Version.ToString();
                        stats.Add(stat);
                    }
                }
            return stats;

            
        }

        /// <summary>
        /// Get OIDS
        /// </summary>
        public List<OidInfo> GetOids()
        {
            List<OidInfo> retVal = new List<OidInfo>();
            var conf = ApplicationContext.CurrentContext.GetService(typeof(ISystemConfigurationService)) as ISystemConfigurationService;
            foreach (MARC.HI.EHRS.SVC.Core.DataTypes.OidRegistrar.OidData data in conf.OidRegistrar)
            {
                retVal.Add(new OidInfo(data));
            }
            return retVal;
        }


        /// <summary>
        /// Get recent activity
        /// </summary>
        public RegistrationEventCollection GetRecentActivity(TimestampSet timeRange, int offset, int count, bool identifierOnly)
        {
            // Get all Services
            IAuditorService auditSvc = ApplicationContext.CurrentContext.GetService(typeof(IAuditorService)) as IAuditorService;
            IDataRegistrationService regSvc = ApplicationContext.CurrentContext.GetService(typeof(IDataRegistrationService)) as IDataRegistrationService;
            IDataPersistenceService repSvc = ApplicationContext.CurrentContext.GetService(typeof(IDataPersistenceService)) as IDataPersistenceService;

            // Audit message
            AuditData audit = this.ConstructAuditData(ActionType.Read, EventIdentifierType.Export);
            audit.EventTypeCode = new CodeValue("ADM_GetRegistrations");

            try
            {
                // Result identifiers
                VersionedDomainIdentifier[] vids = null;
                var dummyQuery = new QueryEvent();
                dummyQuery.Add(new RegistrationEvent() { EventClassifier = RegistrationEventType.Register, EffectiveTime = timeRange }, "SUBJ", SVC.Core.ComponentModel.HealthServiceRecordSiteRoleType.SubjectOf, null);
                vids = regSvc.QueryRecord(dummyQuery);

                RegistrationEventCollection retVal = new RegistrationEventCollection();
                Object syncLock = new object();

                retVal.Count = vids.Length;

                // Now fetch each one asynchronously
                if (!identifierOnly)
                {
                    using (WaitThreadPool thdPool = new WaitThreadPool(Environment.ProcessorCount * 2))
                    {
                        foreach (var id in vids.Skip(offset).Take(count))
                            thdPool.QueueUserWorkItem(
                                delegate(object state)
                                {
                                    try
                                    {
                                        var itm = repSvc.GetContainer(state as VersionedDomainIdentifier, true);
                                        lock (syncLock)
                                            retVal.Event.Add(itm as RegistrationEvent);
                                    }
                                    catch (Exception e)
                                    {
                                        Trace.TraceError("Could not fetch result {0} : {1}", (state as VersionedDomainIdentifier).Identifier, e.ToString());
                                    }
                                }
                                , id);

                        // Wait until fetch is done
                        thdPool.WaitOne(new TimeSpan(0, 0, 30), false);
                    }
                    //retVal.Event.Sort((a, b) => b.Timestamp.CompareTo(a.Timestamp));
                    // Add audit data
                    foreach (var res in retVal.Event)
                        audit.AuditableObjects.Add(new AuditableObject()
                        {
                            IDTypeCode = AuditableObjectIdType.ReportNumber,
                            LifecycleType = AuditableObjectLifecycle.Export,
                            ObjectId = String.Format("{0}^^^&{1}&ISO", res.AlternateIdentifier.Identifier, res.AlternateIdentifier.Domain),
                            Role = AuditableObjectRole.MasterFile,
                            Type = AuditableObjectType.SystemObject,
                            QueryData = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("loadFast=true"))
                        });
                }
                return retVal;
            }
            catch (Exception e)
            {
                Trace.TraceError("Could not execute GetRegistrations : {0}", e.ToString());
                audit.Outcome = OutcomeIndicator.EpicFail;
#if DEBUG
                throw new FaultException(new FaultReason(e.ToString()), new FaultCode(e.GetType().Name));

#else
                throw new FaultException(new FaultReason(e.Message), new FaultCode(e.GetType().Name));
#endif
            }
            finally
            {
                if (auditSvc != null)
                    auditSvc.SendAudit(audit);
            }
        }
    }
}
