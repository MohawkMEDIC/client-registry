using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MARC.HI.EHRS.CR.Core.ComponentModel;
using MARC.HI.EHRS.SVC.Core.Services;
using MARC.HI.EHRS.SVC.Core.Issues;
using MARC.Everest.Connectors;
using System.Xml.Serialization;
using MARC.HI.EHRS.SVC.Core.DataTypes;
using System.IO;

namespace MARC.HI.EHRS.CR.Core.Services
{
    /// <summary>
    /// Registry result data
    /// </summary>
    public abstract class RegistryResult
    {
        /// <summary>
        /// Gets a list of details 
        /// </summary>
        public List<IResultDetail> Detals { get; private set; }

        /// <summary>
        /// Issues detected
        /// </summary>
        public List<DetectedIssue> Issues { get; private set; }

        /// <summary>
        /// Registry result
        /// </summary>
        public RegistryResult()
        {
            this.Detals = new List<IResultDetail>();
            this.Issues = new List<DetectedIssue>();
        }
    }

    /// <summary>
    /// Registry query result
    /// </summary>
    public class RegistryQueryResult : RegistryResult
    {
        /// <summary>
        /// Identifies the first record number that is to be returned in the set
        /// </summary>
        public int StartRecordNumber { get; set; }
        /// <summary>
        /// Continuation pointer
        /// </summary>
        public string ContinuationPtr { get; set; }
        /// <summary>
        /// Gets or sets the identifier of the query the result set is for
        /// </summary>
        public string QueryTag { get; set; }
        /// <summary>
        /// Gets or sets the results for the query
        /// </summary>
        public RegistrationEvent[] Results { get; set; }
        /// <summary>
        /// Gets or sets the total results for the query
        /// </summary>
        public int TotalResults { get; set; }
       
    }

    /// <summary>
    /// Query data structure
    /// </summary>
    [XmlRoot("qd")]
    [Serializable]
    public class RegistryQueryRequest
    {

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
        /// True if the query data constructed is a continu
        /// </summary>
        [XmlAttribute("isContinue")]
        public bool IsContinue { get; set; }

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
        internal static RegistryQueryRequest ParseXml(string p)
        {
            StringReader sr = new StringReader(p);
            XmlSerializer xsz = new XmlSerializer(typeof(RegistryQueryRequest));
            RegistryQueryRequest retVal = (RegistryQueryRequest)xsz.Deserialize(sr);
            sr.Close();
            return retVal;
        }
    }

    /// <summary>
    /// Represents a registry store result
    /// </summary>
    public class RegistryStoreResult : RegistryResult
    {
    }

    /// <summary>
    /// Client registry data service is responsible for coordinating communications between the data 
    /// persistence and registration service and the messaging layers.
    /// </summary>
    public interface IClientRegistryDataService
    {

        /// <summary>
        /// Query the underlying data model based on the registry query request data
        /// </summary>
        RegistryQueryResult Query(RegistryQueryRequest query);

        /// <summary>
        /// Register the store result
        /// </summary>
        RegistryStoreResult Register(RegistrationEvent evt, DataPersistenceMode mode);

        /// <summary>
        /// Update the registry store with new data
        /// </summary>
        RegistryStoreResult Update(RegistrationEvent evt, DataPersistenceMode mode);

        /// <summary>
        /// Merge the registry store data together
        /// </summary>
        RegistryStoreResult Merge(RegistrationEvent survivor, List<RegistrationEvent> obsolete, DataPersistenceMode mode);

        /// <summary>
        /// Unmerge the registry store
        /// </summary>
        RegistryStoreResult UnMerge(RegistrationEvent evt, DataPersistenceMode mode);

    }
}
