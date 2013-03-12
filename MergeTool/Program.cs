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
 * Date: 19-2-2013
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;
using MohawkCollege.Util.Console.Parameters;
using System.IO;
using System.Reflection;
using MARC.HI.EHRS.CR.Persistence.Data.Configuration;
using System.Data;
using MARC.HI.EHRS.CR.Persistence.Data;
using MARC.HI.EHRS.SVC.Core.ComponentModel;
using MARC.HI.EHRS.SVC.Core.DataTypes;
using MARC.HI.EHRS.CR.Core.ComponentModel;
using System.ComponentModel;
using System.Xml.Serialization;
using System.Collections;
using MARC.HI.EHRS.SVC.Core.Services;
using MARC.HI.EHRS.CR.Core.Services;
using System.Threading;

namespace MergeTool
{
    class Program
    {

        private static string ExeName = "ClientRegistry.exe";

        static void Main(string[] args)
        {

            // Parameter parser
            ParameterParser<Parameters> parser = new ParameterParser<Parameters>();
            try
            {
                var parameters = parser.Parse(args);

                if (parameters.Help)
                {
                    Console.WriteLine("MARC-HI Client Registry Reference Implementation Merge Tool");
                    Console.WriteLine("Copyright (C) 2013, Mohawk College of Applied Arts and Technology");
                    parser.WriteHelp(Console.Out);
                    Console.WriteLine();
                    Console.WriteLine("Examples:\r\n\r\nList all marked potential matches:\r\nmergetool --list\r\n");
                    Console.WriteLine("Display detail information for id1..n\r\nmergetool --info <id 1> [<id 2> .. <id n>]\r\n");
                    Console.WriteLine("Merge id1..n into idX\r\nmergetool --merge <id 1> [<id 2> .. <id n>] --target=idX");
                }
                else if (parameters.List)
                    ListMergeCandidates();
                else if (parameters.Info)
                    InfoMergeCandidates(parameters.Pids);
                else if (parameters.Merge)
                    MergeCandidates(parameters.Pids, parameters.Target);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);

            }
            Console.ReadKey();
        }

        
        /// <summary>
        /// Merge candidates together
        /// </summary>
        private static void MergeCandidates(System.Collections.Specialized.StringCollection pids, string target)
        {
            // HACK: This method is a hack and can be done better, as a matter of fact the whole tool should be re-written
            // but this does the trick for now.
            ConfigXmlDocument xmlDocument = new ConfigXmlDocument();
            xmlDocument.Load(Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), "ClientRegistry.exe.config"));
            ConfigurationSectionHandler config = new ConfigurationSectionHandler();
            config = config.Create(null, null, xmlDocument.SelectSingleNode("//*[local-name() = 'marc.hi.ehrs.cr.persistence.data']")) as ConfigurationSectionHandler;
            ApplicationContext.CurrentContext = new MARC.HI.EHRS.SVC.Core.HostContext();
            List<Person> pid = new List<Person>();
            Person pidTarget = null;
            // Now create the db connection
            MARC.HI.EHRS.CR.Persistence.Data.DatabasePersistenceService persister = new MARC.HI.EHRS.CR.Persistence.Data.DatabasePersistenceService();
            persister.Context = ApplicationContext.CurrentContext;

            // De-persist victims
            foreach (var id in pids)
            {
                var registration = persister.GetContainer(new VersionedDomainIdentifier()
                {
                    Domain = ApplicationContext.ConfigurationService.OidRegistrar.GetOid("REG_EVT").Oid,
                    Identifier = id
                }
                    , true) as RegistrationEvent;
                if (registration == null)
                    throw new Exception(String.Format("Could not find registration {0}", id));
                pid.Add(registration.FindComponent(HealthServiceRecordSiteRoleType.SubjectOf) as Person);
            }

            // De-persist target
            var targetReg = persister.GetContainer(new VersionedDomainIdentifier()
            {
                Domain = ApplicationContext.ConfigurationService.OidRegistrar.GetOid("REG_EVT").Oid,
                Identifier = target
            }
                    , true) as RegistrationEvent;

            if (targetReg == null)
                throw new Exception(String.Format("Could not find registration {0}", target));
            pidTarget = targetReg.FindComponent(HealthServiceRecordSiteRoleType.SubjectOf) as Person;

            // Now create the merge message\
            RegistrationEvent merge = new RegistrationEvent()
            {
                EventClassifier = RegistrationEventType.Register,
                EventType = new CodeValue("REG"),
                Status = MARC.HI.EHRS.SVC.Core.ComponentModel.Components.StatusType.Active
            };
            merge.LanguageCode = targetReg.LanguageCode;
            merge.Id = targetReg.Id;
            merge.Add(pidTarget, "SUBJ", HealthServiceRecordSiteRoleType.SubjectOf, null);
            foreach (var pi in pid)
            {
                PersonRegistrationRef re = new PersonRegistrationRef()
                {
                    AlternateIdentifiers = new List<DomainIdentifier>() { 
                            new DomainIdentifier() { Identifier = pi.Id.ToString(),
                                Domain = ApplicationContext.ConfigurationService.OidRegistrar.GetOid("CR_CID").Oid
                            }
                        }
                };
                pidTarget.Add(re, Guid.NewGuid().ToString(), HealthServiceRecordSiteRoleType.ReplacementOf, re.AlternateIdentifiers);
            }

            var vid = persister.UpdateContainer(merge, DataPersistenceMode.Debugging);
            
            if (vid != null)
            {
                Console.WriteLine("Duplicates merged successfully, notifying targets...");
                IClientNotificationService notifierService = ApplicationContext.CurrentContext.GetService(typeof(IClientNotificationService)) as IClientNotificationService;
                if (notifierService != null)
                    notifierService.NotifyDuplicatesResolved(merge);
                Thread.Sleep(1000); // HACK: Give time for the targets to be notified as this is async..
            }
        }

        /// <summary>
        /// Get information about the candidates
        /// </summary>
        private static void InfoMergeCandidates(System.Collections.Specialized.StringCollection pids)
        {
            ConfigXmlDocument xmlDocument = new ConfigXmlDocument();
            xmlDocument.Load(Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), "ClientRegistry.exe.config"));
            ConfigurationSectionHandler config = new ConfigurationSectionHandler();
            config = config.Create(null, null, xmlDocument.SelectSingleNode("//*[local-name() = 'marc.hi.ehrs.cr.persistence.data']")) as ConfigurationSectionHandler;
            ApplicationContext.CurrentContext = new MARC.HI.EHRS.SVC.Core.HostContext();
            // Now create the db connection
            using (IDbConnection conn = config.ReadonlyConnectionManager.GetConnection())
            {
                foreach(var id in pids)
                {
                    MARC.HI.EHRS.CR.Persistence.Data.ComponentPersister.RegistrationEventPersister persister = new MARC.HI.EHRS.CR.Persistence.Data.ComponentPersister.RegistrationEventPersister();
                    var registration = persister.DePersist(conn, Decimal.Parse(id), null, null, true) as RegistrationEvent;

                    if (registration == null)
                        Console.WriteLine("Could not find '{0}'", id);
                    else
                    {
                        // Print detailed information about event
                        Console.WriteLine("{0}\r\nREG EVT # {1}\r\n{0}", new String('-', 50), id);
                        Console.WriteLine("CREATED : {0}\r\nVER ID : {1}\r\nALT ID(S) : {2}^^^{4}&{3}&ISO\r\nCLS ID : {5}\r\nEVT ID : {6}", registration.Timestamp, registration.VersionIdentifier, registration.AlternateIdentifier.Identifier, registration.AlternateIdentifier.Domain, registration.AlternateIdentifier.AssigningAuthority, registration.EventClassifier, registration.EventType.Code);

                        foreach (var cmp in registration.Components)
                            DisplayComponentData((HealthServiceRecordComponent)cmp);
                    }
                }
            }
        }

        /// <summary>
        /// Display component data
        /// </summary>
        private static void DisplayComponentData(HealthServiceRecordComponent cmp)
        {

            if ((cmp.Site as HealthServiceRecordSite).SiteRoleType != HealthServiceRecordSiteRoleType.SubjectOf &&
                (cmp.Site as HealthServiceRecordSite).SiteRoleType != HealthServiceRecordSiteRoleType.AuthorOf)
                return;

            Console.WriteLine("{0}\r\n {1} ({2})\r\n{0}", new String('-', 25), (cmp.Site as HealthServiceRecordSite).SiteRoleType.ToString().ToUpper(), cmp.GetType().Name.ToUpper());

            foreach (PropertyInfo pi in cmp.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                object[] xele = pi.GetCustomAttributes(typeof(XmlElementAttribute), true),
                    xatt = pi.GetCustomAttributes(typeof(XmlAttributeAttribute), true);
                if (xele.Length + xatt.Length > 0 && pi.CanRead && !pi.Name.StartsWith( "Xml"))
                {

                    if (pi.GetValue(cmp, null) is ICollection)
                    {
                        int i = 0;
                        Console.WriteLine("{0} :", pi.Name.ToUpper());

                        foreach (var v in pi.GetValue(cmp, null) as ICollection)
                        {
                            string display = ShowPrimitive(v);
                            Console.WriteLine("\t{0}", display);
                        }
                    }
                    else
                    {
                        String display = ShowPrimitive(pi.GetValue(cmp, null));
                        if (!String.IsNullOrEmpty(display))
                            Console.WriteLine("{0} : {1}", pi.Name.ToUpper(), display);
                    }
                }
            }
        }

        /// <summary>
        /// Show a primitive type
        /// </summary>
        private static string ShowPrimitive(object v)
        {
            if (v == null)
                return null;
            else if (v is DomainIdentifier)
                return String.Format("{0}^^^{1}&{2}&ISO", (v as DomainIdentifier).Identifier, (v as DomainIdentifier).AssigningAuthority, (v as DomainIdentifier).Domain);
            else if (v is TimestampSet)
            {
                StringBuilder sbr = new StringBuilder();
                foreach (var s in (v as TimestampSet).Parts)
                    sbr.AppendFormat("{0}..", ShowPrimitive(s));
                return sbr.ToString();
            }   
            else if (v is TimestampPart)
                return (v as TimestampPart).Value.ToString((v as TimestampPart).Precision == "D" ? "yyyy-MM-dd" : "yyyy-MM-dd HH:mm:ss Z");
            else if (v is NameSet)
                return String.Format("({0}) {1}", (v as NameSet).Use, v.ToString());
            else if (v is TelecommunicationsAddress)
                return String.Format("({0}) {1}", (v as TelecommunicationsAddress).Use, (v as TelecommunicationsAddress).Value);
            else if (v is AddressSet)
            {
                StringBuilder sbr = new StringBuilder();
                sbr.AppendFormat("({0}) ", (v as AddressSet).Use);
                foreach (var pt in (v as AddressSet).Parts)
                    sbr.AppendFormat("({0}) {1}", pt.PartType, pt.AddressValue);
                return sbr.ToString();
            }
            else if (v.GetType().GetProperty("Value") != null)
                return ShowPrimitive(v.GetType().GetProperty("Value").GetValue(v, null));
            return v.ToString();

        }

        /// <summary>
        /// List all merge candidates
        /// </summary>
        private static void ListMergeCandidates()
        {

            ConfigXmlDocument xmlDocument = new ConfigXmlDocument();
            xmlDocument.Load(Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), "ClientRegistry.exe.config"));
            ConfigurationSectionHandler config = new ConfigurationSectionHandler();
            config = config.Create(null, null, xmlDocument.SelectSingleNode("//*[local-name() = 'marc.hi.ehrs.cr.persistence.data']")) as ConfigurationSectionHandler;
            ApplicationContext.CurrentContext = new MARC.HI.EHRS.SVC.Core.HostContext();
            // Now create the db connection
            using (IDbConnection conn = config.ReadonlyConnectionManager.GetConnection())
            {
                using (IDbCommand cmd = DbUtil.CreateCommandStoredProc(conn, null))
                {
                    cmd.CommandText = "ADM_FIND_CNTRS_WITH_CMP_ROLE";
                    cmd.Parameters.Add(DbUtil.CreateParameterIn(cmd, "ROL_TYP_IN", DbType.Decimal, HealthServiceRecordSiteRoleType.AlternateTo | HealthServiceRecordSiteRoleType.TargetOf));
                    Dictionary<Decimal, List<Decimal>> reconciliationList = new Dictionary<decimal, List<decimal>>();
                    MARC.HI.EHRS.CR.Persistence.Data.ComponentPersister.RegistrationEventPersister persister = new MARC.HI.EHRS.CR.Persistence.Data.ComponentPersister.RegistrationEventPersister();

                    using (IDataReader rdr = cmd.ExecuteReader())
                    {
                        while (rdr.Read())
                        {
                            List<Decimal> replList = null;
                            if (!reconciliationList.TryGetValue(Convert.ToDecimal(rdr["new_id"]), out replList))
                            {
                                replList = new List<decimal>();
                                reconciliationList.Add(Convert.ToDecimal(rdr["new_id"]), replList);
                            }

                            // Add to replacement list
                            replList.Add(Convert.ToDecimal(rdr["cand_id"]));

                        }
                     
                    }

                    // Process each replacement
                    foreach (var kv in reconciliationList)
                    {
                        Console.WriteLine(GetSummaryInfo(persister.DePersist(conn, kv.Key, null, null, true) as RegistrationEvent));
                        foreach(var id in kv.Value)
                            Console.WriteLine("\tPOTENTIAL MATCH : {0} ", GetSummaryInfo(persister.DePersist(conn, id, null, null, true) as RegistrationEvent));

                    }
                }
            }
        }

        /// <summary>
        /// Get summary information for the registration event
        /// </summary>
        private static string GetSummaryInfo(RegistrationEvent registrationEvent)
        {
            StringBuilder retVal = new StringBuilder();

            retVal.AppendFormat("#{0} - ", registrationEvent.Id);

            Person subject = registrationEvent.FindComponent(HealthServiceRecordSiteRoleType.SubjectOf) as Person;
            retVal.AppendFormat("{0} ({1})", subject.Names[0], subject.GenderCode);

            return retVal.ToString();
        }

    }
}
