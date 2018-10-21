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
using MARC.HI.EHRS.SVC.Core.ComponentModel;

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
        public List<IResultDetail> Details { get; private set; }

        /// <summary>
        /// Registry result
        /// </summary>
        public RegistryResult()
        {
            this.Details = new List<IResultDetail>();
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
        public List<HealthServiceRecordContainer> Results { get; set; }
        /// <summary>
        /// Gets or sets the total results for the query
        /// </summary>
        public int TotalResults { get; set; }

        /// <summary>
        /// Original request identifier to be loaded form message persistence
        /// </summary>
        public String OriginalRequestId { get; set; }

    }

    /// <summary>
    /// Query data structure
    /// </summary>
    [XmlRoot("qd")]
    [Serializable]
    public class RegistryQueryRequest
    {

        /// <summary>
        /// Constructor
        /// </summary>
        public RegistryQueryRequest()
        {
            this.MatchingAlgorithm = MatchAlgorithm.Default;
        }

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
        /// Matching algorithms
        /// </summary>
        [XmlAttribute("matchAlgorithm")]
        public MatchAlgorithm MatchingAlgorithm { get; set; }

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
        [XmlAttribute("limit")]
        public int Limit { get; set; }

        /// <summary>
        /// Represents the original query component that is being used to query
        /// </summary>
        [XmlIgnore]
        public QueryEvent QueryRequest { get; set; }

        /// <summary>
        /// Original Request
        /// </summary>
        [XmlAttribute("originalConvo")]
        public string OriginalMessageQueryId { get; set; }


        /// <summary>
        /// Offset of the query result
        /// </summary>
        [XmlAttribute("offset")]
        public int Offset { get; set; }
        
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
        /// <summary>
        /// Gets or sets the version identifier created
        /// </summary>
        public VersionedDomainIdentifier VersionId { get; set; }
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
        /// Retrieve a registration record by id
        /// </summary>
        RegistryQueryResult Get(VersionedDomainIdentifier[] regEvtId, RegistryQueryRequest qd);

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
        /// <remarks>
        /// Performs the necessary steps to link the subject of the mergeEvent to the obsolete records. The structure 
        /// of these components shall be:
        /// 
        /// RegistrationEvent
        ///  -- (subjectOf) -> Person
        ///                      -- (replacementOf) -> PersonRegistrationRef
        ///                      -- (replacementOf) -> PersonRegistrationRef
        ///  -- (subjectOf) -> Person
        ///                      -- (replacementOf) -> PersonRegistrationRef
        ///                      -- (replacementOf) -> PersonRegistrationRef
        /// </remarks>
        RegistryStoreResult Merge(RegistrationEvent mergeEvent, DataPersistenceMode mode);

        /// <summary>
        /// Unmerge the registry store
        /// </summary>
        /// <remarks>
        /// Separates the subjectOf in the unmergeEvent from an original version association on a person object in the client registry database 
        /// </remarks>
        RegistryStoreResult UnMerge(RegistrationEvent unmergeEvent, DataPersistenceMode mode);

    }
}
