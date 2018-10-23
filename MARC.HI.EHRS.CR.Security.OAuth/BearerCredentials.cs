using System.Collections.Generic;
using SanteDB.Core.Http;

namespace MARC.HI.EHRS.CR.Security.OAuth
{
    /// <summary>
    /// Represents bearer credentials
    /// </summary>
    public class BearerCredentials : Credentials
    {
        private string m_sessionToken;

        /// <summary>
        /// Creates a new bearer credentials
        /// </summary>
        public BearerCredentials(string sessionToken) : base(null)
        {
            this.m_sessionToken = sessionToken;
        }

        /// <summary>
        /// Get HTTP headers
        /// </summary>
        public override Dictionary<string, string> GetHttpHeaders()
        {
            return new Dictionary<string, string>()
            {
                { "Authorization", $"BEARER {this.m_sessionToken}" }
            };
        }
    }
}