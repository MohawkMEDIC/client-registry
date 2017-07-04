using MARC.HI.EHRS.CR.Messaging.Authentication.Security;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Linq;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security;
using System.Security.Authentication;
using System.Security.Claims;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

namespace MARC.HI.EHRS.CR.Messaging.Authentication.OAuthIdentityConfiguration
{
    public class OAuthClaimsIdentity : IIdentity
    {
        // The authentication type
        private String m_authenticationType;

        // Whether the user is authenticated
        private bool m_isAuthenticated;

        // Issued on
        private DateTimeOffset m_issuedOn = DateTimeOffset.Now;

        /// <summary>
        /// Gets the time of issuance
        /// </summary>
        public DateTimeOffset IssuedOn { get { return this.m_issuedOn; } }

        /// <summary>
        /// Gets the time of issuance
        /// </summary>
        public DateTimeOffset Expiry { get { return this.m_issuedOn.AddMinutes(30); } }
        
        // The security user
        private string m_securityUserName;

        public string AuthenticationType
        {
            get
            {
                return this.m_authenticationType;
            }
        }

        public bool IsAuthenticated
        {
            get
            {
                return this.m_isAuthenticated;
            }
        }

        public string Name
        {
            get
            {
                return this.m_securityUserName;
            }
        }
        
        private OAuthClaimsIdentity(bool isAuthenticated)
        {
            this.m_isAuthenticated = isAuthenticated;
            this.m_authenticationType = "Password";
        }

        /// <summary>
        /// Creates a principal based on username and password
        /// </summary>
        internal static OAuthClaimsIdentity Create(String userName, String password)
        {
            try
            {
                if (userName == AuthenticationContext.AnonymousPrincipal.Identity.Name ||
                    userName == AuthenticationContext.SystemPrincipal.Identity.Name)
                {
                    //throw new PolicyViolationException(PermissionPolicyIdentifiers.Login, PolicyDecisionOutcomeType.Deny);
                    throw new Exception();
                }

                Guid? userId = Guid.Empty;
                
                var userIdentity = new OAuthClaimsIdentity(true) { m_authenticationType = "Password", m_securityUserName = userName };

                return userIdentity;
            }
            catch (Exception e)
            {
                // TODO: Audit this
                throw;
            }
        }

        /// <summary>
        /// Creates a principal based on username and password
        /// </summary>
        internal static OAuthClaimsIdentity Create(byte[] refreshToken)
        {
            try
            {
                Guid? userId = Guid.Empty;

                using (var dataContext = new DataContext("MEMPI"))
                {
                    var userIdentity = new OAuthClaimsIdentity(true) { m_authenticationType = "Refresh" };

                    return userIdentity;
                }
            }
            catch (AuthenticationException)
            {
                // TODO: Audit this
                throw;
            }
            catch (SecurityException)
            {
                // TODO: Audit this
                throw;
            }
            catch (SqlException e)
            {
                switch (e.Number)
                {
                    case 51900:
                        throw new AuthenticationException("Account is locked");
                    case 51901:
                        throw new AuthenticationException("Invalid username/password");
                    case 51902:
                        throw new AuthenticationException("User requires two-factor authentication");
                    default:
                        throw e;
                }
            }
            catch (Exception e)
            {
                throw new Exception("Creating identity failed", e);
            }
        }

        /// <summary>
        /// Creates an identity from a hash
        /// </summary>
        internal static OAuthClaimsIdentity Create(String userName)
        {
            try
            {
                using (var dataContext = new DataContext("MEMPI"))
                {
                    return new OAuthClaimsIdentity(false);
                }
            }
            catch (Exception e)
            {
                throw new Exception("Creating unauthorized identity failed", e);
            }
        }

        /// <summary>
        /// Create an authorization context
        /// </summary>
        public ClaimsPrincipal CreateClaimsPrincipal()
        {
            if (!this.m_isAuthenticated)
                throw new SecurityException("Principal is not authenticated");

            try
            {

                // System claims
                List<Claim> claims = new List<Claim>(
                    //this.m_roles.Select(r => new Claim(ClaimsIdentity.DefaultRoleClaimType, r.Name))
                )
                {
                    new Claim(ClaimTypes.Authentication, nameof(OAuthClaimsIdentity)),
                    new Claim(ClaimTypes.AuthorizationDecision, this.m_isAuthenticated ? "GRANT" : "DENY"),
                    new Claim(ClaimTypes.AuthenticationInstant, this.IssuedOn.ToString("o")), // TODO: Fix this
                    new Claim(ClaimTypes.AuthenticationMethod, this.m_authenticationType),
                    new Claim(ClaimTypes.Expiration, this.Expiry.ToString("o")), // TODO: Move this to configuration
                    new Claim(ClaimTypes.Name, this.m_securityUserName)
                };

                // TODO: Demographic data for the user
                var retVal = new ClaimsPrincipal(
                        new ClaimsIdentity[] { new ClaimsIdentity(this, claims.AsReadOnly()) }
                    );
                return retVal;
            }
            catch (Exception e)
            {
                throw new Exception("Creating principal from identity failed", e);
            }
        }

        /// <summary>
        /// Represent principal as a string
        /// </summary>
        private static String PrincipalToString(ClaimsPrincipal retVal)
        {
            using (StringWriter sw = new StringWriter())
            {
                sw.Write("{{ Identity = {0}, Claims = [", retVal.Identity);
                foreach (var itm in retVal.Claims)
                {
                    sw.Write("{{ Type = {0}, Value = {1} }}", itm.Type, itm.Value);
                    if (itm != retVal.Claims.Last()) sw.Write(",");
                }
                sw.Write("] }");
                return sw.ToString();
            }

        }
    }
}
