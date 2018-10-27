/*
 * Copyright 2015-2018 Mohawk College of Applied Arts and Technology
 *
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
 * Date: 2017-9-1
 */
using MARC.HI.EHRS.CR.Security.OAuth.Configuration;
using MARC.HI.EHRS.CR.Security.Services;
using SanteDB.Core.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Diagnostics;
using System.IdentityModel.Configuration;
using System.IdentityModel.Policy;
using System.IdentityModel.Tokens;
using System.Linq;
using System.Security.Authentication;
using System.Security.Claims;
using System.Security.Principal;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Text;
using System.Threading.Tasks;

namespace MARC.HI.EHRS.CR.Security.OAuth.Wcf
{
    /// <summary>
    /// JwtToken SAM
    /// </summary>
    public class TokenServiceAuthorizationManager : ServiceAuthorizationManager
    {

        // Configuration from main SanteDB
        private OAuthSecurityConfigurationSection m_configuration = ConfigurationManager.GetSection("marc.hi.ehrs.cr.security.oauth") as OAuthSecurityConfigurationSection;

        /// <summary>
        /// Check access core
        /// </summary>
        protected override bool CheckAccessCore(OperationContext operationContext)
        {
            Trace.TraceInformation("CheckAccessCore");
            Trace.TraceInformation("User {0} already authenticated", AuthenticationContext.Current.Principal.Identity.Name);
            return base.CheckAccessCore(operationContext);
        }

        /// <summary>
        /// Check access
        /// </summary>
        public override bool CheckAccess(OperationContext operationContext)
        {
            RemoteEndpointMessageProperty remoteEndpoint = (RemoteEndpointMessageProperty)operationContext.IncomingMessageProperties[RemoteEndpointMessageProperty.Name];

            try
            {
                Trace.TraceInformation("CheckAccess");

                // Http message inbound
                HttpRequestMessageProperty httpMessage = (HttpRequestMessageProperty)operationContext.IncomingMessageProperties[HttpRequestMessageProperty.Name];

                // Get the authorize header
                String authorization = httpMessage.Headers[System.Net.HttpRequestHeader.Authorization];
                if (authorization == null)
                {
                    if (httpMessage.Method == "OPTIONS" || httpMessage.Method == "PING")
                    {
                        //operationContext.ServiceSecurityContext.AuthorizationContext.Properties["Identities"] = identities;
                        operationContext.ServiceSecurityContext.AuthorizationContext.Properties["Principal"] = AuthenticationContext.AnonymousPrincipal;
                        AuthenticationContext.Current = new AuthenticationContext(AuthenticationContext.AnonymousPrincipal);

                        return true; // OPTIONS is non PHI infrastructural
                    }
                    else
                    {
                        throw new UnauthorizedAccessException("Missing Authorization header");
                    }
                }

                // Authorization method
                var auth = authorization.Split(' ').Select(o=>o.Trim()).ToArray();
                switch(auth[0].ToLowerInvariant())
                {
                    case "bearer":
                        return this.CheckBearerAccess(operationContext, auth[1]);
                    case "urn:ietf:params:oauth:token-type:jwt": // Will use JWT authorization
                        return this.CheckJwtAccess(operationContext, auth[1]);
                    default:
                        throw new SecurityTokenException("Invalid authentication scheme");
                }

            }
            catch(UnauthorizedAccessException e) {
                Trace.TraceError("JWT Token Error (From: {0}) : {1}", remoteEndpoint?.Address, e);

                throw;
            }
            catch(SecurityTokenException e) {
                Trace.TraceError("JWT Token Error (From: {0}) : {1}", remoteEndpoint?.Address, e);
                throw;
            }
            catch(Exception e)
            {
                Trace.TraceError("JWT Token Error (From: {0}) : {1}", remoteEndpoint?.Address, e);
                throw new SecurityTokenException(e.Message, e);
            }
        }

        /// <summary>
        /// Checks bearer access token
        /// </summary>
        /// <param name="operationContext">The operation context within which the access token should be validated</param>
        /// <param name="authorization">The authorization data </param>
        /// <returns>True if authorization is successful</returns>
        private bool CheckBearerAccess(OperationContext operationContext, string authorization)
        {
            var session = ApplicationContext.Current.GetService<ISessionManagerService>()?.Get(
                authorization
            );

            if(session == null)
            {
                Trace.TraceWarning("Bearer token sesison could not be found");
                throw new SecurityTokenExpiredException("Session does not exist or is expired");
            }
            IPrincipal principal = session.Principal;

            operationContext.ServiceSecurityContext.AuthorizationContext.Properties["Identities"] = (principal as ClaimsPrincipal).Identities;
            operationContext.ServiceSecurityContext.AuthorizationContext.Properties["Principal"] = principal;
            AuthenticationContext.Current = new AuthenticationContext(principal);

            Trace.TraceInformation("User {0} authenticated via SESSION BEARER", principal.Identity.Name);

            return base.CheckAccess(operationContext);
        }

        /// <summary>
        /// Validates the authorization header as a JWT token
        /// </summary>
        /// <param name="operationContext">The operation context within which this should be checked</param>
        /// <param name="authorization">The authorization data</param>
        /// <returns>True when authorization is successful</returns>
        private bool CheckJwtAccess(OperationContext operationContext, string authorization)
        {
            authorization = authorization.Trim();
            String authorizationToken = authorization.Substring(authorization.IndexOf(" ")).Trim();
            JwtSecurityTokenHandler handler = new JwtSecurityTokenHandler();

            if (!handler.CanReadToken(authorizationToken))
                throw new SecurityTokenException("Token is not in a valid format");

            SecurityToken token = null;
            var identities = handler.ValidateToken(authorizationToken, this.m_configuration.ToConfigurationObject(), out token);

            // Validate token expiry
            if (token.ValidTo < DateTime.Now.ToUniversalTime())
                throw new SecurityTokenException("Token expired");
            else if (token.ValidFrom > DateTime.Now.ToUniversalTime())
                throw new SecurityTokenException("Token not yet valid");

            operationContext.ServiceSecurityContext.AuthorizationContext.Properties["Identities"] = identities.Identities;
            operationContext.ServiceSecurityContext.AuthorizationContext.Properties["Principal"] = identities;
            AuthenticationContext.Current = new AuthenticationContext(identities);

            Trace.TraceInformation("User {0} authenticated via JWT", identities.Identity.Name);

            return base.CheckAccess(operationContext);
        }
    }
}
