using System;
using System.Configuration;
using System.Diagnostics;
using System.IdentityModel.Configuration;
using System.IdentityModel.Policy;
using System.IdentityModel.Tokens;
using System.Linq;
using System.Security.Authentication;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Text;
using System.Threading.Tasks;

namespace MARC.HI.EHRS.CR.Messaging.Authentication.Security
{
    public class JwtTokenServiceAuthorizationManager : ServiceAuthorizationManager
    {
        // Configuration from main OAuth
        private OAuthConfiguration.OAuthConfiguration m_configuration = ConfigurationManager.GetSection("marc.hi.ehrs.cr.messaging.authentication") as OAuthConfiguration.OAuthConfiguration;

        // Trace source
        private TraceSource m_traceSource = new TraceSource("MARC.HI.EHRS.CR.Messaging.Authentication.Security");

        /// <summary>
        /// Check access core
        /// </summary>
        protected override bool CheckAccessCore(OperationContext operationContext)
        {
            this.m_traceSource.TraceInformation("CheckAccessCore");
            this.m_traceSource.TraceInformation("User {0} already authenticated", AuthenticationContext.Current.Principal.Identity.Name);

            return base.CheckAccessCore(operationContext);
        }

        /// <summary>
        /// Check access
        /// </summary>
        public override bool CheckAccess(OperationContext operationContext)
        {
            return true;
            try
            {
                this.m_traceSource.TraceInformation("CheckAccess");

                // Http message inbound
                HttpRequestMessageProperty httpMessage = (HttpRequestMessageProperty)operationContext.IncomingMessageProperties[HttpRequestMessageProperty.Name];

                // Get the authorize header
                String authorization = httpMessage.Headers[System.Net.HttpRequestHeader.Authorization];
                if (authorization == null)
                {
                    if (httpMessage.Method == "OPTIONS") return true; // OPTIONS is non PHI infrastructural
                    else
                        throw new Exception();
                    //throw new Exception("Missing Authorization header", "Bearer", this.m_configuration.Security.ClaimsAuth.Realm, this.m_configuration.Security.ClaimsAuth.Audiences.FirstOrDefault());
                }
                else if (!authorization.Trim().StartsWith("bearer", StringComparison.InvariantCultureIgnoreCase))
                    throw new Exception();
                    //throw new Exception("Invalid authentication scheme", "Bearer", this.m_configuration.Security.ClaimsAuth.Realm, this.m_configuration.Security.ClaimsAuth.Audiences.FirstOrDefault());

                String authorizationToken = authorization.Substring(6).Trim();
                JwtSecurityTokenHandler handler = new JwtSecurityTokenHandler();

                var identityModelConfig = ConfigurationManager.GetSection("system.identityModel") as SystemIdentityModelSection;

                if (!handler.CanReadToken(authorizationToken))
                    throw new SecurityTokenException("Token is not in a vlaid format");

                SecurityToken token = null;
                var identities = handler.ValidateToken(authorizationToken, this.m_configuration?.Security?.ClaimsAuth?.ToConfigurationObject(), out token);

                // Validate token expiry
                if (token.ValidTo < DateTime.Now.ToUniversalTime())
                    throw new SecurityTokenException("Token expired");
                else if (token.ValidFrom > DateTime.Now.ToUniversalTime())
                    throw new SecurityTokenException("Token not yet valid");

                operationContext.ServiceSecurityContext.AuthorizationContext.Properties["Identities"] = identities.Identities;
                operationContext.ServiceSecurityContext.AuthorizationContext.Properties["Principal"] = identities;
                AuthenticationContext.Current = new AuthenticationContext(identities);

                this.m_traceSource.TraceInformation("User {0} authenticated via JWT", identities.Identity.Name);

                return base.CheckAccess(operationContext);
            }
            catch (UnauthorizedAccessException) { throw; }
            catch (Exception e)
            {
                throw new SecurityTokenException(e.Message, e);
            }
        }
    }
}
