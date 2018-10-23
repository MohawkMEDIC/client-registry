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
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Newtonsoft.Json;
using System.Security.Principal;
using System.Globalization;
using System.Xml.Serialization;
using System.ComponentModel;
using System.Threading;
using System.Security;
using MARC.HI.EHRS.CR.Security.Services;
using System.Diagnostics;
using System.Security.Claims;

namespace MARC.HI.EHRS.CR.Security
{
    /// <summary>
    /// Session information
    /// </summary>
    [JsonObject("SessionInfo"), XmlType("SessionInfo", Namespace = "http://santedb.org/model")]
    public class SessionInfo 
    {
        
        // Lock
        private object m_syncLock = new object();

        /// <summary>
        /// Default ctor
        /// </summary>
        public SessionInfo()
        {
            this.Key = Guid.NewGuid();
        }

        /// <summary>
        /// Gets or sets the key
        /// </summary>
        [XmlElement("key"), JsonProperty("key")]
        public Guid Key { get; set; }

        /// <summary>
        /// Create the session object from the principal
        /// </summary>
        public SessionInfo(IPrincipal principal)
        {
            this.Key = Guid.NewGuid();
            this.ProcessPrincipal(principal);
        }

        private object ApplicationContextIDataPersistenceService<T>()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Gets the principal of the session
        /// </summary>
        [JsonIgnore, XmlIgnore]
        public IPrincipal Principal { get; private set; }
       
        /// <summary>
        /// Gets the user name
        /// </summary>
        [JsonProperty("username")]
        public string UserName { get; set; }

        /// <summary>
        /// Gets the roles to which the identity belongs
        /// </summary>
        [JsonProperty("roles")]
        public List<String> Roles { get; set; }

        /// <summary>
        /// True if authenticated
        /// </summary>
        [JsonProperty("isAuthenticated")]
        public bool IsAuthenticated { get; set; }

        /// <summary>
        /// Gets or sets the mechanism
        /// </summary>
        [JsonProperty("method")]
        public String AuthenticationType { get; set; }

        /// <summary>
        /// Expiry time
        /// </summary>
        [JsonProperty("exp")]
        public DateTime Expiry { get; set; }

        /// <summary>
        /// Issued time
        /// </summary>
        [JsonProperty("nbf")]
        public DateTime Issued { get; set; }

        /// <summary>
        /// Gets or sets the access token
        /// </summary>
        [JsonProperty("token")]
        public String Token { get; set; }

        /// <summary>
        /// Gets or sets the JWT token
        /// </summary>
        [JsonProperty("idToken")]
        public string IdentityToken { get; set; }

        /// <summary>
        /// Gets or sets the refresh token
        /// </summary>
        [JsonProperty("refresh_token")]
        public String RefreshToken { get; set; }
        
        /// <summary>
        /// Extends the session
        /// </summary>
        public bool Extend()
        {
            try
            {
                lock (this.m_syncLock)
                {
                    if (this.Expiry > DateTime.Now.AddMinutes(5)) // session will still be valid in 5 mins so no auth
                        return true;
                    this.ProcessPrincipal(ApplicationContext.Current.GetService<IIdentityProviderService>().Authenticate(this.Principal, null));
                    return this.Principal != null;
                }
            }
            catch (Exception e)
            {
                Trace.TraceError("Error extending session: {0}", e);
                return false;
            }
        }

        /// <summary>
        /// Process a principal
        /// </summary>
        /// <param name="principal"></param>
        private void ProcessPrincipal(IPrincipal principal)
        {
            this.UserName = principal.Identity.Name;
            this.IsAuthenticated = principal.Identity.IsAuthenticated;
            this.AuthenticationType = principal.Identity.AuthenticationType;
            this.Principal = principal;
            if (principal is ClaimsPrincipal)
                this.Token = principal.ToString();

            // Expiry / etc
            if (principal is ClaimsPrincipal)
            {
                var cp = principal as ClaimsPrincipal;

                this.Issued = ((cp.FindFirst(ClaimTypes.AuthenticationInstant) ?? cp.FindFirst("nbf"))?.AsDateTime().ToLocalTime() ?? DateTime.Now);
                this.Expiry = ((cp.FindFirst(ClaimTypes.Expiration) ?? cp.FindFirst("exp"))?.AsDateTime().ToLocalTime() ?? DateTime.MaxValue);
                this.Roles = cp.Claims.Where(o => o.Type == ClaimsIdentity.DefaultRoleClaimType)?.Select(o => o.Value)?.ToList();
                this.AuthenticationType = cp.FindFirst(ClaimTypes.AuthenticationMethod)?.Value;

                var subKey = Guid.Empty;
                if (cp.HasClaim(o => o.Type == ClaimTypes.Sid))
                    Guid.TryParse(cp.FindFirst(ClaimTypes.Sid)?.Value, out subKey);
            }
            else
            {
                IRoleProviderService rps = ApplicationContext.Current.GetService<IRoleProviderService>();
                this.Roles = rps.GetAllRoles(this.UserName).ToList();
                this.Issued = DateTime.Now;
                this.Expiry = DateTime.MaxValue;
            }
            
        }

    }
}