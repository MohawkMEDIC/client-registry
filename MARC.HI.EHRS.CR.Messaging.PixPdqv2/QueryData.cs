/**
 * Copyright 2012-2013 Mohawk College of Applied Arts and Technology
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
 * Date: 16-8-2012
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using MARC.HI.EHRS.SVC.Core.DataTypes;
using MARC.HI.EHRS.CR.Core.ComponentModel;
using System.IO;

namespace MARC.HI.EHRS.CR.Messaging.PixPdqv2
{

    /// <summary>
    /// Query data structure
    /// </summary>
    [XmlRoot("qd")]
    [Serializable]
    public struct QueryData
    {

        /// <summary>
        /// Empty query data
        /// </summary>
        public static readonly QueryData Empty = new QueryData();

        /// <summary>
        /// The minimum degree of match
        /// </summary>
        [XmlAttribute("minDegreeMatch")]
        public float MinimumDegreeMatch { get; set; }

        // Target (filter) domains for clients
        [XmlElement("target")]
        public List<DomainIdentifier> TargetDomain { get; set; }
        
        /// <summary>
        /// True if the query is a summary query
        /// </summary>
        [XmlAttribute("isSummary")]
        public bool IsSummary { get; set; }

        /// <summary>
        /// Response message type
        /// </summary>
        [XmlAttribute("responseMessage")]
        public string ResponseMessageType { get; set; }
        /// <summary>
        /// The Query identifier
        /// </summary>
        [XmlAttribute("tag")]
        public string QueryTag { get; set; }
        /// <summary>
        /// Gets or sets the query id for the query 
        /// </summary>
        [XmlAttribute("id")]
        public String QueryId { get; set; }
        /// <summary>
        /// Gets or sets the originator of the request
        /// </summary>
        [XmlAttribute("orgn")]
        public string Originator { get; set; }
        /// <summary>
        /// Specifies the maximum number of query results to return fro mthe ffunction
        /// </summary>
        [XmlAttribute("qty")]
        public int Quantity { get; set; }
        /// <summary>
        /// Represents the original query component that is being used to query
        /// </summary>
        [XmlIgnore]
        public RegistrationEvent QueryRequest { get; set; }
        /// <summary>
        /// Original Request
        /// </summary>
        [XmlAttribute("originalConvo")]
        public string OriginalMessageQueryId { get; set; }

        /// <summary>
        /// Record Ids to be fetched
        /// </summary>
        [XmlIgnore]
        public List<VersionedDomainIdentifier> RecordIds { get; set; }

        /// <summary>
        /// Represent the QD as string
        /// </summary>
        public override string ToString()
        {
            StringWriter sb = new StringWriter();
            XmlSerializer xs = new XmlSerializer(this.GetType());
            xs.Serialize(sb, this);
            return sb.ToString();
        }

        /// <summary>
        /// Parse XML from the string
        /// </summary>
        internal static QueryData ParseXml(string p)
        {
            StringReader sr = new StringReader(p);
            XmlSerializer xsz = new XmlSerializer(typeof(QueryData));
            QueryData retVal = (QueryData)xsz.Deserialize(sr);
            sr.Close();
            return retVal;
        }
    }
}
