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
using System;
using System.Linq;
using System.Security.Principal;
using Newtonsoft.Json;
using System.Security;
using SanteDB.Core.Model.Security;
using SanteDB.Core.Http;
using System.Net;
using SanteDB.Messaging.AMI.Client;
using SanteDB.Core.Services;
using SanteDB.Core.Model.AMI.Auth;
using System.Text;
using SanteDB.Core.Interfaces;
using SanteDB.Core.Model.Constants;
using MARC.HI.EHRS.CR.Security.Services;
using System.Security.Claims;
using MARC.HI.EHRS.CR.Security.OAuth.Configuration;
using System.Configuration;
using MARC.HI.EHRS.CR.Core.Http;
using MARC.HI.EHRS.CR.Security.OAuth.Token;
using System.Diagnostics;

namespace MARC.HI.EHRS.CR.Security.OAuth
{
    /// <summary>
    /// Represents an OAuthIdentity provider
    /// </summary>
    public class OAuthIdentityProvider : IIdentityProviderService
    {
        #region IIdentityProviderService implementation

        private OAuthSecurityConfigurationSection m_configuration = ConfigurationManager.GetSection("marc.hi.ehrs.cr.security.oauth") as OAuthSecurityConfigurationSection;

        /// <summary>
        /// Authenticate the user
        /// </summary>
        /// <param name="userName">User name.</param>
        /// <param name="password">Password.</param>
        public System.Security.Principal.IPrincipal Authenticate(string userName, string password)
        {
            return this.Authenticate(new GenericPrincipal(new GenericIdentity(userName), null), password);
        }

        /// <summary>
        /// Perform authentication with specified password
        /// </summary>
        public System.Security.Principal.IPrincipal Authenticate(System.Security.Principal.IPrincipal principal, string password)
        {
            return this.Authenticate(principal, password, null);
        }

        /// <summary>
        /// Authenticate the user
        /// </summary>
        /// <param name="principal">Principal.</param>
        /// <param name="password">Password.</param>
        public System.Security.Principal.IPrincipal Authenticate(System.Security.Principal.IPrincipal principal, string password, String tfaSecret)
        {

            // Get the scope being requested
            String scope = "*";
            if (principal is ClaimsPrincipal)
                scope = (principal as ClaimsPrincipal).Claims.FirstOrDefault(o => o.Type == "scope")?.Value ?? scope;
            else
                scope = "*";

            // Authenticate
            IPrincipal retVal = null;

            using (IRestClient restClient = new RestClient(this.m_configuration.GetIdpDescription()))
            {

                try
                {
                    // Create grant information
                    OAuthTokenRequest request = null;
                    if (!String.IsNullOrEmpty(password))
                        request = new OAuthTokenRequest(principal.Identity.Name, password, scope);
                    else if (principal is TokenClaimsPrincipal)
                        request = new OAuthTokenRequest(principal as TokenClaimsPrincipal, scope);
                    else
                        request = new OAuthTokenRequest(principal.Identity.Name, null, scope);

                    // Set credentials
                    request.ClientId = this.m_configuration.ClientId;
                    request.ClientSecret = this.m_configuration.ClientSecret;

                    OAuthTokenResponse response = restClient.Post<OAuthTokenRequest, OAuthTokenResponse>("oauth2_token", "application/x-www-form-urlencoded", request);
                    retVal = new TokenClaimsPrincipal(response.AccessToken, response.IdToken ?? response.AccessToken, response.TokenType, response.RefreshToken);

                }
                catch (RestClientException<OAuthTokenResponse> ex)
                {
                    Trace.TraceError("REST client exception: {0}", ex.Message);
                    var se = new SecurityException($"Error executing OAuth request: {ex.Result.Error}", ex);
                    se.Data.Add("detail", ex.Result);
                    throw se;
                }
                catch (SecurityException ex)
                {
                    Trace.TraceError("TOKEN exception: {0}", ex.Message);
                    throw new SecurityException($"Security error: {ex.Message}", ex);
                }
                catch (Exception ex)
                {
                    Trace.TraceError("Generic exception: {0}", ex);
                }

                return retVal;
            }
        }

        /// <summary>
        /// Gets the specified identity
        /// </summary>
        public System.Security.Principal.IIdentity GetIdentity(string userName)
        {
            throw new NotImplementedException();
        }
        /// <summary>
        /// Authenticates the specified user
        /// </summary>
        public System.Security.Principal.IPrincipal Authenticate(string userName, string password, string tfaSecret)
        {
            return this.Authenticate(new GenericPrincipal(new GenericIdentity(userName), null), password, tfaSecret);
        }

        /// <summary>
        /// Changes the users password.
        /// </summary>
        /// <param name="userName">The username of the user.</param>
        /// <param name="newPassword">The new password of the user.</param>
        /// <param name="principal">The authentication principal (the user that is changing the password).</param>
        public void ChangePassword(string userName, string newPassword, System.Security.Principal.IPrincipal principal)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Changes the users password.
        /// </summary>
        /// <param name="userName">The username of the user.</param>
        /// <param name="password">The new password of the user.</param>
        public void ChangePassword(string userName, string password)
        {
            this.ChangePassword(userName, password, AuthenticationContext.Current.Principal);
        }

        /// <summary>
        /// Creates an identity
        /// </summary>
        public IIdentity CreateIdentity(string userName, string password)
        {
            throw new NotImplementedException();
        }
        /// <summary>
        /// Sets the user's lockout status
        /// </summary>
        public void SetLockout(string userName, bool v)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Deletes the specified identity
        /// </summary>
        public void DeleteIdentity(string userName)
        {
            throw new NotImplementedException();
        }

        public IIdentity CreateIdentity(Guid sid, string userName, string password)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}

