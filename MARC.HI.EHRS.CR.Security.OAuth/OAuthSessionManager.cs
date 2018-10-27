using MARC.HI.EHRS.CR.Core.Http;
using MARC.HI.EHRS.CR.Security.OAuth.Configuration;
using MARC.HI.EHRS.CR.Security.OAuth.Token;
using MARC.HI.EHRS.CR.Security.Services.Impl;
using MARC.HI.EHRS.SVC.Core.Services;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MARC.HI.EHRS.CR.Security.OAuth
{
    /// <summary>
    /// Represents a memory based session manager that can reach out to an OAuth STS to get current session information
    /// </summary>
    public class OAuthSessionManager : MemorySessionManagerService, IUsesHostContext
    {

        // configuration
        private OAuthSecurityConfigurationSection m_configuration = ConfigurationManager.GetSection("marc.hi.ehrs.cr.security.oauth") as OAuthSecurityConfigurationSection;

        /// <summary>
        /// Gets or sets the host context
        /// </summary>
        public IServiceProvider Context { get => ApplicationContext.Current; set => ApplicationContext.Current = value; }

        /// <summary>
        /// Get session information from the provided token
        /// </summary>
        /// <param name="sessionToken"></param>
        /// <returns></returns>
        public override SessionInfo Get(string sessionToken)
        {
            var retVal = base.Get(sessionToken);

            if(retVal == null || retVal.Expiry < DateTime.Now)
            {
                if (retVal != null)
                    lock(base.m_session)
                        base.m_session.Remove(retVal.Key);

                using (var oauthClient = new RestClient(this.m_configuration.GetIdpDescription()))
                {
                    try
                    {
                        oauthClient.Credentials = new BearerCredentials(sessionToken);
                        var response = oauthClient.Get<OAuthTokenResponse>("session");
                        Trace.TraceInformation("Found session from session server");
                        retVal = base.Establish(new TokenClaimsPrincipal(response.AccessToken, response.IdToken, response.TokenType, null));
                    }
                    catch(Exception e)
                    {
                        Trace.TraceError("Could not retrieve session from session server: {0}", e);
                        return null;
                    }
                }
            }

            return retVal;
        }
    }
}
