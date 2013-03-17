using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MARC.HI.EHRS.SVC.Core.DataTypes;
using MARC.HI.EHRS.CR.Core.ComponentModel;
using MARC.HI.EHRS.SVC.Core.Services;

namespace MARC.HI.EHRS.CR.Core.Services
{
    /// <summary>
    /// Represents a service that controls merges
    /// </summary>
    public interface IClientRegistryMergeService : IUsesHostContext
    {

        /// <summary>
        /// Mark the candidates as potential matches to a record but do not merge
        /// </summary>
        void MarkConflicts(VersionedDomainIdentifier recordId, IEnumerable<VersionedDomainIdentifier> matches);

        /// <summary>
        /// Merges the specified victim records into the survivor record
        /// </summary>
        void Resolve(IEnumerable<VersionedDomainIdentifier> victimIds, VersionedDomainIdentifier survivorId, DataPersistenceMode mode);

        /// <summary>
        /// Mark a conflict as resolved
        /// </summary>
        /// <param name="recordId"></param>
        void MarkResolved(VersionedDomainIdentifier recordId);

        /// <summary>
        /// Get exact match conflicts
        /// </summary>
        IEnumerable<VersionedDomainIdentifier> FindIdConflicts(RegistrationEvent registration);

        /// <summary>
        /// Get potential conflicts for the provided registration
        /// </summary>
        IEnumerable<VersionedDomainIdentifier> FindFuzzyConflicts(RegistrationEvent registration);

        /// <summary>
        /// Gets the conflicts that have been marked for the specified record identifier
        /// </summary>
        IEnumerable<VersionedDomainIdentifier> GetConflicts(VersionedDomainIdentifier recordId);

        /// <summary>
        /// Gets all HSR identifiers where a merge is possible
        /// </summary>
        IEnumerable<VersionedDomainIdentifier> GetOutstandingConflicts();
    }
}
