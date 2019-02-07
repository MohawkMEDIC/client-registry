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
using System.Security.Cryptography.X509Certificates;

namespace MARC.HI.EHRS.CR.Security.OAuth
{
    /// <summary>
    /// Represents an OAuthIdentity provider
    /// </summary>
    public class OAuthDeviceIdentityProvider : IDeviceIdentityProviderService
    {
        
        private OAuthSecurityConfigurationSection m_configuration = ConfigurationManager.GetSection("marc.hi.ehrs.cr.security.oauth") as OAuthSecurityConfigurationSection;

        /// <summary>
        /// Authenticate the device by device id and secret
        /// </summary>
        public IPrincipal Authenticate(string deviceId, string deviceSecret)
        {
            return this.Authenticate(deviceId, deviceSecret, this.m_configuration.ClientId, this.m_configuration.ClientSecret);
        }

        public IPrincipal Authenticate(string deviceId, string deviceSecret, string clientId, string clientSecret) { 
            // Authenticate
            IPrincipal retVal = null;

            using (IRestClient restClient = new RestClient(this.m_configuration.GetIdpDescription()))
            {

                try
                {
                    // Create grant information
                    OAuthTokenRequest request = new OAuthTokenRequest(clientId, clientSecret);
                    request.Scope = "*";

                    restClient.Requesting += (o, p) =>
                    {
                        p.AdditionalHeaders.Add("X-Device-Authorization", $"BASIC {Convert.ToBase64String(Encoding.UTF8.GetBytes(String.Format("{0}:{1}", deviceId, deviceSecret)))}");
                    };

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
                    throw;
                }

                return retVal;
            }
        }

        public IPrincipal Authenticate(X509Certificate2 deviceCertificate)
        {
            throw new NotImplementedException();
        }

        public IIdentity GetIdentity(string name)
        {
            throw new NotImplementedException();
        }
    }
}

