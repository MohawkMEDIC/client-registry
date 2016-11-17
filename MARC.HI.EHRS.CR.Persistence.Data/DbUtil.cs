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
using System.ComponentModel;
using System.Data;
using System.Text;
using MARC.HI.EHRS.CR.Core.ComponentModel;
using MARC.HI.EHRS.CR.Persistence.Data.ComponentPersister;
using MARC.HI.EHRS.SVC.Core;
using MARC.HI.EHRS.SVC.Core.ComponentModel;
using MARC.HI.EHRS.SVC.Core.ComponentModel.Components;
using MARC.HI.EHRS.SVC.Core.DataTypes;
using MARC.HI.EHRS.SVC.Core.Services;
using System.Diagnostics;

namespace MARC.HI.EHRS.CR.Persistence.Data
{
    /// <summary>
    /// Database utilities
    /// </summary>
    public static class DbUtil
    {

        [ThreadStatic]
        private static List<HealthServiceRecordComponent> m_alreadyDepersisted = new List<HealthServiceRecordComponent>(100);

        //private static Object s_syncLock = new object();

        /// <summary>
        /// DAta about components
        /// </summary>
        private struct ComponentData
        {
            public Type m_componentType;
            public Decimal m_componentId;
            public Decimal m_componentVersionId;
            public HealthServiceRecordSiteRoleType m_roleType;
        }

        /// <summary>
        /// Timestamp part type
        /// </summary>
        private static Dictionary<TimestampPart.TimestampPartType, string> m_timestampPartMap;

        /// <summary>
        /// Database utility
        /// </summary>
        static DbUtil()
        {
            m_timestampPartMap = new Dictionary<TimestampPart.TimestampPartType, string>() 
            { 
                { TimestampPart.TimestampPartType.HighBound, "U" } ,
                { TimestampPart.TimestampPartType.LowBound, "L" } ,
                { TimestampPart.TimestampPartType.Standlone, "S" } ,
                { TimestampPart.TimestampPartType.Width, "W" }
            };
        }

        /// <summary>
        /// Get the root event
        /// </summary>
        public static RegistrationEvent GetRegistrationEvent(IComponent child)
        {
            if (child.Site == null)
                return null;

            // Get the HealthServiceRecord that this document belongs to
            IContainer hsrContainer = child.Site.Container;
            while (!(hsrContainer is RegistrationEvent) && hsrContainer != null && (hsrContainer as IComponent).Site != null)
                hsrContainer = (hsrContainer as IComponent).Site.Container;
            return hsrContainer as RegistrationEvent;

        }

        /// <summary>
        /// Create a command
        /// </summary>
        public static IDbCommand CreateCommandStoredProc(IDbConnection conn, IDbTransaction tx)
        {
            IDbCommand cmd = conn.CreateCommand();
            cmd.CommandType = CommandType.StoredProcedure;
            if (tx != null)
                cmd.Transaction = tx;
            return cmd;
        }

        /// <summary>
        /// Create an input parameter 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="type"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static IDbDataParameter CreateParameterIn(IDbCommand owner, string name, DbType type, object value)
        {
            IDbDataParameter retVal = owner.CreateParameter();
            retVal.DbType = type;
            retVal.ParameterName = name;
            retVal.Value = value;
            retVal.Direction = ParameterDirection.Input;
            return retVal;
        }

        /// <summary>
        /// Create a coded value
        /// </summary>
        public static decimal CreateCodedValue(IDbConnection conn, IDbTransaction tx, MARC.HI.EHRS.SVC.Core.DataTypes.CodeValue codeValue)
        {

            IDbCommand cmd = CreateCommandStoredProc(conn, tx);

            try
            {
                cmd.CommandText = "crt_code";

                // Add parameters
                cmd.Parameters.Add(CreateParameterIn(cmd, "cd_val_in", DbType.StringFixedLength, codeValue.Code));
                cmd.Parameters.Add(CreateParameterIn(cmd, "cd_domain_in", DbType.StringFixedLength, codeValue.CodeSystem));
                cmd.Parameters.Add(CreateParameterIn(cmd, "org_cnt_typ_in", DbType.StringFixedLength, "text/plain"));
                cmd.Parameters.Add(CreateParameterIn(cmd, "org_text_in", DbType.Binary, codeValue.OriginalText == null ? DBNull.Value : (object)Encoding.UTF8.GetBytes(codeValue.OriginalText)));
                cmd.Parameters.Add(CreateParameterIn(cmd, "cd_vrsn_id", DbType.StringFixedLength, codeValue.CodeSystemVersion));
                cmd.Parameters.Add(CreateParameterIn(cmd, "cd_qlfys_cd_id_in", DbType.Decimal, DBNull.Value));
                cmd.Parameters.Add(CreateParameterIn(cmd, "cd_qlfys_as_in", DbType.StringFixedLength, DBNull.Value));
                cmd.Parameters.Add(CreateParameterIn(cmd, "cd_qlfys_kv_id_in", DbType.StringFixedLength, DBNull.Value));
                cmd.Parameters.Add(CreateParameterIn(cmd, "can_share", DbType.Boolean, codeValue.Qualifies == null || codeValue.Qualifies.Count == 0));
                decimal codeId = Convert.ToDecimal(cmd.ExecuteScalar());

                // Create qualifiers
                if (codeValue.Qualifies != null)
                    foreach (var kv in codeValue.Qualifies)
                        CreateCodedValue(conn, tx, codeId, kv);


                // Return the code identifier
                return codeId;
            }
            finally
            {
                cmd.Dispose();
            }
        }

        /// <summary>
        /// Get coded value from the database
        /// </summary>
        public static CodeValue GetCodedValue(IDbConnection conn, IDbTransaction tx, decimal? codeId)
        {
            if (!codeId.HasValue) return null;

            IDbCommand cmd = CreateCommandStoredProc(conn, tx);
            try
            {
                cmd.CommandText = "get_code";
                cmd.Parameters.Add(CreateParameterIn(cmd, "cd_id_in", DbType.Decimal, codeId));

                // Get code
                IDataReader reader = cmd.ExecuteReader();
                CodeValue retVal = null;
                try
                {
                    if (reader.Read())
                    {
                        retVal = new CodeValue()
                        {
                            Code = Convert.ToString(reader["cd_val"]),
                            CodeSystem = Convert.ToString(reader["cd_domain"]),
                            OriginalText = reader["org_text"] == DBNull.Value ? null : Encoding.UTF8.GetString((byte[])reader["org_text"]),
                            CodeSystemVersion = reader["cd_vrsn"] == DBNull.Value ? null :  Convert.ToString(reader["cd_vrsn"]),
                            Key = codeId.Value,
                            UpdateMode = UpdateModeType.Ignore
                        };
                    }
                }
                finally
                {
                    reader.Close();
                    reader.Dispose();
                }


                // Coded Value
                if(retVal != null)
                    foreach (KeyValuePair<CodeValue, CodeValue> kv in GetCodedValueQualifiers(conn, tx, codeId))
                    {
                        if (retVal.Qualifies == null)
                            retVal.Qualifies = new Dictionary<CodeValue, CodeValue>();
                        retVal.Qualifies.Add(kv.Key, kv.Value);
                    }

                return retVal;

            }
            finally
            {
                cmd.Dispose();
            }
        }

        /// <summary>
        /// Get all codified qualifiers
        /// </summary>
        private static IEnumerable<KeyValuePair<CodeValue, CodeValue>> GetCodedValueQualifiers(IDbConnection conn, IDbTransaction tx, decimal? codeId)
        {
            IDbCommand cmd = CreateCommandStoredProc(conn, tx);
            try
            {
                cmd.CommandText = "get_code_qlfys";
                cmd.Parameters.Add(CreateParameterIn(cmd, "cd_id_in", DbType.Decimal, codeId));

                // Define and read the return qualifiers into the dictionary
                // The reason we use this method is to keep track of the kv_id in the table
                // TODO: JF - This is messy, need a better solution
                Dictionary<String, Decimal?> tKeys = new Dictionary<string, Decimal?>(),
                    tValues = new Dictionary<string,Decimal?>();
                List<string> kvIds = new List<string>();
                List<KeyValuePair<CodeValue, CodeValue>> retVal = new List<KeyValuePair<CodeValue,CodeValue>>();

                IDataReader reader = cmd.ExecuteReader();
                try
                {
                    while (reader.Read())
                    {
                        string kvRec = Convert.ToString(reader["cd_qlfys_kv_id"]);
                        if(!kvIds.Contains(kvRec))
                            kvIds.Add(kvRec);

                        // Populate the key or value
                        if (Convert.ToChar(reader["cd_qlfys_as"]) == 'K')
                            tKeys.Add(kvRec, Convert.ToDecimal(reader["cd_id"]));
                        else
                            tValues.Add(kvRec, Convert.ToDecimal(reader["cd_id"]));
                    }
                }
                finally
                {
                    reader.Close();
                }
                
                // Now create the return value
                foreach (var kid in kvIds)
                {
                    decimal? key = null, value = null;
                    tKeys.TryGetValue(kid, out key);
                    tValues.TryGetValue(kid, out value);
                    retVal.Add(new KeyValuePair<CodeValue, CodeValue>(GetCodedValue(conn, tx, key.Value), GetCodedValue(conn, tx, value.Value)));
                }

                return retVal;
            }
            finally
            {
                cmd.Dispose();
            }
        }

        /// <summary>
        /// Create a sub-coded value
        /// </summary>
        private static void CreateCodedValue(IDbConnection conn, IDbTransaction tx, decimal codeId, KeyValuePair<CodeValue, CodeValue> kv)
        {
            IDbCommand cmd = CreateCommandStoredProc(conn, tx);

            try
            {
                Guid kvGuid = Guid.NewGuid();
                cmd.CommandText = "crt_code";

                // Add parameters
                if (kv.Key != null)
                {
                    cmd.Parameters.Add(CreateParameterIn(cmd, "cd_val_in", DbType.StringFixedLength, kv.Key.Code));
                    cmd.Parameters.Add(CreateParameterIn(cmd, "cd_domain_in", DbType.StringFixedLength, kv.Key.CodeSystem));
                    cmd.Parameters.Add(CreateParameterIn(cmd, "org_cnt_typ_in", DbType.StringFixedLength, kv.Key.OriginalText == null ? DBNull.Value : (object)"text/plain"));
                    cmd.Parameters.Add(CreateParameterIn(cmd, "org_text_in", DbType.Binary, kv.Key.OriginalText == null ? DBNull.Value : (object)Encoding.UTF8.GetBytes(kv.Key.OriginalText)));
                    cmd.Parameters.Add(CreateParameterIn(cmd, "cd_vrsn_id", DbType.StringFixedLength, kv.Key.CodeSystemVersion));
                    cmd.Parameters.Add(CreateParameterIn(cmd, "cd_qlfys_cd_id_in", DbType.Decimal, codeId));
                    cmd.Parameters.Add(CreateParameterIn(cmd, "cd_qlfys_as_in", DbType.AnsiStringFixedLength, "K"));
                    cmd.Parameters.Add(CreateParameterIn(cmd, "cd_qlfys_cd_id_in", DbType.StringFixedLength, kvGuid.ToString()));
                    cmd.Parameters.Add(CreateParameterIn(cmd, "can_share", DbType.Boolean, false));
                    decimal cdId = Convert.ToDecimal(cmd.ExecuteScalar());

                    // Create qualifiers
                    if (kv.Key.Qualifies != null)
                        foreach (var skv in kv.Key.Qualifies)
                            CreateCodedValue(conn, tx, cdId, skv);
                }
                if (kv.Value != null)
                {
                    cmd = conn.CreateCommand();
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.CommandText = "crt_code";

                    if (tx != null)
                        cmd.Transaction = tx;

                    cmd.Parameters.Add(CreateParameterIn(cmd, "cd_val_in", DbType.StringFixedLength, kv.Value.Code));
                    cmd.Parameters.Add(CreateParameterIn(cmd, "cd_domain_in", DbType.StringFixedLength, kv.Value.CodeSystem));
                    cmd.Parameters.Add(CreateParameterIn(cmd, "org_cnt_typ_in", DbType.StringFixedLength, "text/plain"));
                    cmd.Parameters.Add(CreateParameterIn(cmd, "org_text_in", DbType.Binary, kv.Value.OriginalText == null ? DBNull.Value : (object)Encoding.UTF8.GetBytes(kv.Value.OriginalText)));
                    cmd.Parameters.Add(CreateParameterIn(cmd, "cd_vrsn_id", DbType.String, kv.Value.CodeSystemVersion));
                    cmd.Parameters.Add(CreateParameterIn(cmd, "cd_qlfys_cd_id_in", DbType.Decimal, codeId));
                    cmd.Parameters.Add(CreateParameterIn(cmd, "cd_qlfys_as_in", DbType.AnsiStringFixedLength, "V"));
                    cmd.Parameters.Add(CreateParameterIn(cmd, "cd_qlfys_cd_id_in", DbType.StringFixedLength, kvGuid.ToString()));
                    decimal cdId = Convert.ToDecimal(cmd.ExecuteScalar());

                    // Create qualifiers
                    if (kv.Value.Qualifies != null)
                        foreach (var skv in kv.Value.Qualifies)
                            CreateCodedValue(conn, tx, cdId, skv);
                }
            }
            finally
            {
                cmd.Dispose();
            }
        }
        /// <summary>
        /// Create a timeset
        /// </summary>
        public static decimal CreateTimeset(IDbConnection conn, IDbTransaction tx, TimestampSet dateTime)
        {

            decimal? tsSetId = null;

            if (dateTime.Parts.Count == 0)
                throw new ConstraintException("Timestamp set must contain at least one part");

            // Create the parts
            foreach (var tsPart in dateTime.Parts)
            {
                // database commands
                if (tsSetId == null)
                    tsSetId = CreateTimestamp(conn, tx, tsPart, tsSetId);
                else
                    CreateTimestamp(conn, tx, tsPart, tsSetId);
            }

            return tsSetId.Value;
        }

        /// <summary>
        /// Create timestamp part
        /// </summary>
        public static decimal CreateTimestamp(IDbConnection conn, IDbTransaction tx, TimestampPart tsPart, decimal? tsSetId)
        {
            IDbCommand cmd = CreateCommandStoredProc(conn, tx);
            try
            {
                cmd.CommandText = "crt_ts";

                // Create a time-set, we'll use the identifier of the first time-component
                // as the site identifier
                cmd.Parameters.Add(CreateParameterIn(cmd, "ts_value_in", DbType.DateTime, tsPart.Precision == "D" ? tsPart.Value.Date : tsPart.Value));
                cmd.Parameters.Add(CreateParameterIn(cmd, "ts_precision_in", DbType.StringFixedLength, tsPart.Precision));
                cmd.Parameters.Add(CreateParameterIn(cmd, "ts_cls_in", DbType.StringFixedLength, m_timestampPartMap[tsPart.PartType]));
                cmd.Parameters.Add(CreateParameterIn(cmd, "ts_set_id_in", DbType.Decimal, (object)tsSetId ?? DBNull.Value));

                // Get the identifier
                decimal tsId = Convert.ToDecimal(cmd.ExecuteScalar());
                if (tsSetId == null)
                    tsSetId = tsId;
                return tsId;
            }
            finally
            {
                cmd.Dispose();
            }
            
        }

        /// <summary>
        /// Get an address set from the database
        /// </summary>
        public static AddressSet GetAddress(IDbConnection conn, IDbTransaction tx, decimal? addrSetId, bool loadFast)
        {
            IDbCommand cmd = CreateCommandStoredProc(conn, tx);

            try
            {
                cmd.CommandText = "get_addr_set";
                if (!loadFast)
                    cmd.CommandText += "_efft";
                cmd.Parameters.Add(CreateParameterIn(cmd, "addr_set_id_in", DbType.Decimal, addrSetId));

                // Execute a reader
                IDataReader reader = cmd.ExecuteReader();
                AddressSet retVal = new AddressSet() { Use = AddressSet.AddressSetUse.PhysicalVisit, Key = addrSetId.Value };

                // Populate set
                while (reader.Read())
                {
                    AddressPart part = new AddressPart();
                    part.AddressValue = Convert.ToString(reader["addr_cmp_value"]);
                    part.PartType = (AddressPart.AddressPartType)Convert.ToInt32(reader["addr_cmp_cls"]);
                    retVal.Parts.Add(part);

                    // Effective time
                    if(!loadFast && retVal.EffectiveTime == default(DateTime) && reader["efft_utc"] != DBNull.Value)
                    {
                        retVal.EffectiveTime = Convert.ToDateTime(reader["efft_utc"]);
                        if (reader["obslt_utc"] != DBNull.Value)
                            retVal.EffectiveTime = Convert.ToDateTime(reader["obslt_utc"]);
                    }
                }

                return retVal;
            }
            finally
            {
                cmd.Dispose();
            }
        }

        /// <summary>
        /// Get a name from the database
        /// </summary>
        public static NameSet GetName(IDbConnection conn, IDbTransaction tx, decimal? nsSetId, bool loadFast)
        {
            IDbCommand cmd = CreateCommandStoredProc(conn, tx);

            try
            {
                cmd.CommandText = "get_name_set";
                if (!loadFast)
                    cmd.CommandText += "_efft";
                cmd.Parameters.Add(CreateParameterIn(cmd, "name_set_id_in", DbType.Decimal, nsSetId));

                // Execute a reader
                IDataReader reader = cmd.ExecuteReader();
                NameSet retVal = new NameSet() { Use = NameSet.NameSetUse.Legal };

                // Populate set
                while (reader.Read())
                {
                    NamePart part = new NamePart();
                    part.Value = Convert.ToString(reader["name_cmp_value"]);
                    part.Type = (NamePart.NamePartType)Convert.ToInt32(reader["name_cmp_cls"]);
                    retVal.Parts.Add(part);

                    // Effective time
                    if (!loadFast && retVal.EffectiveTime == default(DateTime) && reader["efft_utc"] != DBNull.Value)
                    {
                        retVal.EffectiveTime = Convert.ToDateTime(reader["efft_utc"]);
                        if (!loadFast && reader["obslt_utc"] != DBNull.Value)
                            retVal.EffectiveTime = Convert.ToDateTime(reader["obslt_utc"]);
                    }
                }

                return retVal;
            }
            finally
            {
                cmd.Dispose();
            }
        }

        /// <summary>
        /// Create a name set record
        /// </summary>
        public static decimal CreateNameSet(IDbConnection conn, IDbTransaction tx, NameSet nameSet)
        {
            IDbCommand cmd = CreateCommandStoredProc(conn, tx);

            try
            {
                
                cmd.CommandText = "crt_name_cmp";
                cmd.Parameters.Add(CreateParameterIn(cmd, "name_value_in", DbType.StringFixedLength, null));
                cmd.Parameters.Add(CreateParameterIn(cmd, "name_cls_in", DbType.Decimal, null));
                cmd.Parameters.Add(CreateParameterIn(cmd, "name_set_id_in", DbType.Decimal, DBNull.Value));

                // Name set parts
                foreach (var cmp in nameSet.Parts)
                {
                    ((IDataParameter)cmd.Parameters["name_value_in"]).Value = cmp.Value;
                    ((IDataParameter)cmd.Parameters["name_cls_in"]).Value = (decimal)cmp.Type;
                    ((IDataParameter)cmd.Parameters["name_set_id_in"]).Value = nameSet.Key == default(decimal) ? (object)DBNull.Value : nameSet.Key;
                    // Execute 
                    decimal retVal = Convert.ToDecimal(cmd.ExecuteScalar());
                    if (nameSet.Key == default(decimal))
                        nameSet.Key = retVal;
                }
                return nameSet.Key;
            }
            finally
            {
                cmd.Dispose();
            }
        }

        /// <summary>
        /// Create an address set
        /// </summary>
        public static decimal CreateAddressSet(IDbConnection conn, IDbTransaction tx, AddressSet addrSet)
        {
            IDbCommand cmd = CreateCommandStoredProc(conn, tx);

            try
            {
                cmd.CommandText = "crt_addr_cmp";
                cmd.Parameters.Add(CreateParameterIn(cmd, "addr_value_in", DbType.StringFixedLength, null));
                cmd.Parameters.Add(CreateParameterIn(cmd, "addr_cls_in", DbType.Decimal, null));
                cmd.Parameters.Add(CreateParameterIn(cmd, "addr_set_id_in", DbType.Decimal, DBNull.Value));

                // Address set parts
                foreach (var cmp in addrSet.Parts)
                {
                    ((IDataParameter)cmd.Parameters["addr_value_in"]).Value = cmp.AddressValue;
                    ((IDataParameter)cmd.Parameters["addr_cls_in"]).Value = (decimal)cmp.PartType;
                    ((IDataParameter)cmd.Parameters["addr_set_id_in"]).Value = addrSet.Key == default(decimal) ? (object)DBNull.Value : addrSet.Key;
                    // Execute 
                    decimal retVal = Convert.ToDecimal(cmd.ExecuteScalar());
                    if (addrSet.Key == default(decimal))
                        addrSet.Key = retVal;
                }

                return addrSet.Key;
            }
            finally
            {
                cmd.Dispose();
            }
        }

        /// <summary>
        /// Persist the components of the specified container
        /// </summary>
        public static void PersistComponents(IDbConnection conn, IDbTransaction tx, bool isUpdate, IComponentPersister persister, IContainer cont)
        {
            // Now time for sub-components
            foreach (IComponent cmp in cont.Components)
            {

                IComponentPersister cmpp = DatabasePersistenceService.GetPersister(cmp.GetType());
                if (cmpp != null)
                {
                    var vid = cmpp.Persist(conn, tx, cmp, isUpdate);
                    // Add this component to the registered components?
                    if(vid != null)
                        RegisterComponent(conn, tx, Decimal.Parse(vid.Identifier), cmp, (cmp.Site.Container as HealthServiceRecordContainer).Id, cmp.Site.Container);
                }
                else
                    throw new InvalidOperationException(String.Format("Cannot find persister for '{0}'", cmp.GetType().FullName));
            }
        }

        /// <summary>
        /// Register component
        /// </summary>
        public static void RegisterComponent(IDbConnection conn, IDbTransaction tx, Decimal componentId, IComponent component, Decimal containerId, IContainer container)
        {
            IDbCommand cmd = CreateCommandStoredProc(conn, tx);
            try
            {
                decimal? versionId = null, componentVersionId = null;
                if (container is RegistrationEvent)
                    versionId = (container as RegistrationEvent).VersionIdentifier;
                else if (container is Person)
                    versionId = (container as Person).VersionId;

                if(component is RegistrationEvent)
                    componentVersionId = (component as RegistrationEvent).VersionIdentifier;
                else if (component is Person)
                    componentVersionId = (component as Person).VersionId;

                cmd.CommandText = "crt_comp";
                cmd.Parameters.Add(CreateParameterIn(cmd, "cntr_typ_in", DbType.String, container.GetType().FullName));
                cmd.Parameters.Add(CreateParameterIn(cmd, "cntr_tbl_id_in", DbType.Decimal, containerId));
                cmd.Parameters.Add(CreateParameterIn(cmd, "cntr_vrsn_id_in", DbType.Decimal, versionId.HasValue ? (object)versionId.Value : DBNull.Value));
                cmd.Parameters.Add(CreateParameterIn(cmd, "cmp_typ_in", DbType.String, component.GetType().FullName));
                cmd.Parameters.Add(CreateParameterIn(cmd, "cmp_tbl_id_in", DbType.Decimal, componentId));
                cmd.Parameters.Add(CreateParameterIn(cmd, "cmp_tbl_id_in", DbType.Decimal, componentVersionId.HasValue ? (object)componentVersionId.Value : DBNull.Value));
                cmd.Parameters.Add(CreateParameterIn(cmd, "cmp_rol_typ_in", DbType.Decimal, (decimal)(component.Site as HealthServiceRecordSite).SiteRoleType));
                cmd.ExecuteNonQuery();
            }
            finally
            {
                cmd.Dispose();
            }
        }

        /// <summary>
        /// Verifies that the database being used for the persistence engine will work as expected
        /// </summary>
        public static Version GetSchemaVersion(IDbConnection conn)
        {
            IDbCommand cmd = CreateCommandStoredProc(conn, null);
            try
            {
                cmd.CommandText = "get_sch_ver";
                return new Version(Convert.ToString(cmd.ExecuteScalar()));
            }
            finally
            {
                cmd.Dispose();
            }
        }

        /// <summary>
        /// Get an effective time set
        /// </summary>
        public static TimestampSet GetEffectiveTimestampSet(IDbConnection conn, IDbTransaction tx, decimal tsId)
        {
            // Load the timestampset
            TimestampSet retVal = new TimestampSet();

            IDbCommand cmd = CreateCommandStoredProc(conn, tx);
            try
            {
                cmd.CommandText = "get_ts_set";
                cmd.Parameters.Add(CreateParameterIn(cmd, "ts_set_id_in", DbType.Decimal, tsId));
                IDataReader reader = cmd.ExecuteReader();
                try
                {
                    // Read all components
                    while (reader.Read())
                    {
                        TimestampPart pt = new TimestampPart();

                        // Classifier
                        switch (Convert.ToChar(reader["ts_cls"]))
                        {
                            case 'L':
                                pt.PartType = TimestampPart.TimestampPartType.LowBound;
                                break;
                            case 'U':
                                pt.PartType = TimestampPart.TimestampPartType.HighBound;
                                break;
                            case 'S':
                                pt.PartType = TimestampPart.TimestampPartType.Standlone;
                                break;
                            case 'W':
                                pt.PartType = TimestampPart.TimestampPartType.Width;
                                break;
                        }

                        // Value
                        pt.Precision = Convert.ToString(reader["ts_precision"]);
                            Trace.TraceInformation("{0} - {1}", reader["ts_id"].ToString(), reader["ts_date"].ToString());
                        pt.Value = pt.Precision == "D" || pt.Precision == "Y" || pt.Precision == "M" ? DateTime.Parse(reader["ts_date"].ToString()).Date : Convert.ToDateTime(reader["ts_value"]);
                        retVal.Parts.Add(pt);

                    }
                }
                finally
                {
                    reader.Close();
                }
            }
            finally
            {
                cmd.Dispose();
            }

            return retVal;
        }

        /// <summary>
        /// Clear persisted cache
        /// </summary>
        public static void ClearPersistedCache()
        {
            m_alreadyDepersisted = new List<HealthServiceRecordComponent>();
        }

        /// <summary>
        /// De-Persist components
        /// </summary>
        public static void DePersistComponents(IDbConnection conn, IContainer cont, IComponentPersister parent, bool loadFast)
        {
            List<ComponentData> tComponents = GetComponents(conn, cont, false);
            
            // Include Inverse roles?
            if (cont is RegistrationEvent)
                tComponents.AddRange(GetComponents(conn, new HealthServiceRecordComponentRef() { Id = (cont as RegistrationEvent).Id }, true));

            // Reconstruct components
            foreach (var cmp in tComponents)
            {
#if PERFMON
                Trace.TraceInformation("{0} : {1}", DateTime.Now.ToString("HH:mm:ss.ffff"), cmp.m_roleType.ToString());
#endif
                IComponentPersister cmpp = DatabasePersistenceService.GetPersister(cmp.m_componentType);
                if (cmpp != null)
                {

                    // Determine if we've already de-persisted the object
                    HealthServiceRecordComponent dcComp = m_alreadyDepersisted.Find(o => o != null && o.GetType().Equals(cmp.m_componentType) && o is IIdentifiable && (o as IIdentifiable).Identifier == cmp.m_componentId && (cmp.m_componentVersionId == default(Decimal) || (o as IIdentifiable).VersionIdentifier == cmp.m_componentVersionId));
                    //Trace.TraceInformation("DePersist {0}:{1}", cmp.m_componentType, cmp.m_componentId);
                    if (dcComp != null)
                        dcComp = dcComp.Clone() as HealthServiceRecordComponent;
                    else
                    {
                        if(cmpp is IVersionComponentPersister)
                            dcComp = (cmpp as IVersionComponentPersister).DePersist(conn, cmp.m_componentId, cmp.m_componentVersionId, cont, cmp.m_roleType, loadFast) as HealthServiceRecordComponent;
                        else
                            dcComp = cmpp.DePersist(conn, cmp.m_componentId, cont, cmp.m_roleType, loadFast) as HealthServiceRecordComponent;
                        if(dcComp != null) m_alreadyDepersisted.Add(dcComp);
                    }

                    // Add to the graph
                    if (dcComp != null && dcComp.Site != null)
                        (dcComp.Site as HealthServiceRecordSite).SiteRoleType = cmp.m_roleType;
                    else if(dcComp != null)
                        (cont as HealthServiceRecordContainer).Add(dcComp, Guid.NewGuid().ToString(), cmp.m_roleType, null);
                }
                else
                    throw new InvalidOperationException(String.Format("Cannot find persister for '{0}'", cmp.m_componentType.FullName));
#if PERFMON
                Trace.TraceInformation("EO {0} : {1}", DateTime.Now.ToString("HH:mm:ss.ffff"), cmp.m_roleType.ToString());
#endif
            }

       
        }

        /// <summary>
        /// Get Components
        /// </summary>
        private static List<ComponentData> GetComponents(IDbConnection conn, IContainer cont, bool invert)
        {
            IDbCommand cmd = CreateCommandStoredProc(conn, null);
            try
            {
                decimal? versionId = null;
                if (cont is RegistrationEvent)
                    versionId = (cont as RegistrationEvent).VersionIdentifier;
                else if (cont is Person)
                    versionId = (cont as Person).VersionId;

                cmd.CommandText = "get_comps";
                cmd.Parameters.Add(CreateParameterIn(cmd, "cntr_typ_in", DbType.String, cont.GetType().FullName));
                cmd.Parameters.Add(CreateParameterIn(cmd, "cntr_tbl_id_in", DbType.Decimal, (cont as HealthServiceRecordContainer).Id));
                cmd.Parameters.Add(CreateParameterIn(cmd, "cntr_vrsn_id_in", DbType.Decimal, versionId.HasValue ? (object)versionId.Value : DBNull.Value));
                cmd.Parameters.Add(CreateParameterIn(cmd, "invrt_in", DbType.Boolean, invert));
                // Need to read components that this container has, then we can reconstruct the object graph
                IDataReader reader = cmd.ExecuteReader();
                List<ComponentData> tComponents = new List<ComponentData>(10);
                try
                {
                    while (reader.Read())
                        tComponents.Add(new ComponentData()
                        {
                            m_componentId = Convert.ToDecimal(reader["cmp_tbl_id"]),
                            m_componentVersionId = reader["cmp_vrsn_id"] == DBNull.Value ? default(Decimal) : Convert.ToDecimal(reader["cmp_vrsn_id"]),
                            m_componentType = typeof(HealthServiceRecordSiteRoleType).Assembly.GetType(Convert.ToString(reader["cmp_typ"])) ?? typeof(RegistrationEvent).Assembly.GetType(Convert.ToString(reader["cmp_typ"])),
                            m_roleType = (HealthServiceRecordSiteRoleType)Convert.ToDecimal(reader["cmp_rol_typ"])
                        });
                }
                finally
                {
                    reader.Close();
                }
                return tComponents;
            }
            finally
            {
                cmd.Dispose();
            }
        }

        /// <summary>
        /// Build the query expression
        /// </summary>
        public static string BuildQueryFilter(IComponent queryComponent, IServiceProvider context, bool forceExact)
        {
            
            StringBuilder retVal = new StringBuilder();


            // Find the query persister for our current component
            var register = DatabasePersistenceService.GetQueryPersister(queryComponent.GetType());
            if (register == null)
                return "";

            // Set the host context
            if (register is IUsesHostContext)
                (register as IUsesHostContext).Context = context;

            // Build the intersect statement
            retVal.Append(register.BuildFilter(queryComponent, forceExact));
            // Container needs components to be queried
            if(queryComponent is HealthServiceRecordContainer)
            {
                var queryContainer = queryComponent as HealthServiceRecordContainer;
                if (queryContainer.Components.Count > 0)
                {
                    //retVal.Append(" AND HSR_ID IN (");
                    int ccomp = 0;
                    queryContainer.SortComponentsByRole();
                    bool needsClose = false;
                    foreach (HealthServiceRecordComponent comp in queryContainer.Components)
                    {
                        string verb = " AND HSR_VRSN_ID IN (";
                        if (queryContainer.FindAllComponents((comp.Site as HealthServiceRecordSite).SiteRoleType).Count > 1)
                            verb = " OR HSR_VRSN_ID IN (";

                        if (ccomp++ < queryContainer.Components.Count)
                        {
                            string subFilter = BuildQueryFilter(comp, context, forceExact);
                            
                            if (!String.IsNullOrEmpty(subFilter))
                            {
                                if (retVal.Length > 0)
                                {
                                    // Scrub multi column selects that some comps do
                                    subFilter = subFilter.Replace("HSR_ID, HSR_VRSN_ID", "HSR_VRSN_ID");
                                    retVal.AppendFormat(" {0} ", verb);
                                    needsClose = true;
                                }
                                retVal.AppendFormat(" {0} ", subFilter);
                                //needsClose = true;
                            }
                        }
                        else
                        {
                            string subFilter = BuildQueryFilter(comp, context, forceExact);
                            if (!String.IsNullOrEmpty(subFilter)) retVal.Append(subFilter);
                        }
                    }

                    // Clean up 
                    if (retVal.ToString().EndsWith(" AND HSR_VRSN_ID IN ( "))
                        retVal.Remove(retVal.Length - 11, 11);

                    if(needsClose)
                        retVal.Append(")");

                    // Clean Up
                    if (retVal.ToString().EndsWith("INTERSECT ()"))
                        retVal.Remove(retVal.Length - 12, 12);

                    // Clean Up
                    retVal.Replace("AND () ", "");
                }
            }
            retVal.Append(register.BuildControlClauses(queryComponent));
            return retVal.ToString();
        }
        

    }
}
