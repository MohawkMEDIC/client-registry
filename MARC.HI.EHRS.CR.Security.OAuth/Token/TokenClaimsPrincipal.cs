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
using System.Security.Cryptography.X509Certificates;
using System.Security;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using System.Text;
using System.Security.Claims;
using MARC.HI.EHRS.CR.Core.Util;
using System.Configuration;
using MARC.HI.EHRS.CR.Security.OAuth.Configuration;
using System.IdentityModel.Tokens;

namespace MARC.HI.EHRS.CR.Security.OAuth.Token
{
	/// <summary>
	/// Token claims principal.
	/// </summary>
	public class TokenClaimsPrincipal : ClaimsPrincipal
	{

		// Claim map
		private readonly Dictionary<String, String> claimMap = new Dictionary<string, string>() {
			{ "unique_name", ClaimsIdentity.DefaultNameClaimType },
			{ "role", ClaimsIdentity.DefaultRoleClaimType },
			{ "sub", ClaimTypes.Sid },
			{ "authmethod", ClaimTypes.AuthenticationMethod },
			{ "exp", ClaimTypes.Expiration },
			{ "nbf", ClaimTypes.AuthenticationInstant },
			{ "email", ClaimTypes.Email }
		};

        private OAuthSecurityConfigurationSection m_configuration = ConfigurationManager.GetSection("marc.hi.ehrs.cr.security.oauth") as OAuthSecurityConfigurationSection;
		// The token
		private String m_idToken;

        // Access token
        private String m_accessToken;
        		
        /// <summary>
        /// Gets the refresh token
        /// </summary>
        public String RefreshToken { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SanteDB.DisconnectedClient.Xamarin.Security.TokenClaimsPrincipal"/> class.
        /// </summary>
        /// <param name="idToken">Token.</param>
        /// <param name="tokenType">Token type.</param>
        public TokenClaimsPrincipal (String accessToken, String idToken, String tokenType, String refreshToken) 
		{
			if (String.IsNullOrEmpty (idToken))
				throw new ArgumentNullException (nameof (idToken));
			else if (String.IsNullOrEmpty (tokenType))
				throw new ArgumentNullException (nameof (tokenType));
			else if (tokenType != "urn:ietf:params:oauth:token-type:jwt" &&
                tokenType != "bearer")
				throw new ArgumentOutOfRangeException (nameof (tokenType), "expected urn:ietf:params:oauth:token-type:jwt");

			// Token
			this.m_idToken = idToken;
            this.m_accessToken = accessToken;

			String[] tokenObjects = idToken.Split ('.');
            // Correct each token to be proper B64 encoding
            for (int i = 0; i < tokenObjects.Length; i++)
                tokenObjects[i] = tokenObjects[i].PadRight(tokenObjects[i].Length + (tokenObjects[i].Length % 4), '=').Replace("===","=");
			JObject headers = JObject.Parse (Encoding.UTF8.GetString (Convert.FromBase64String (tokenObjects [0]))),
				body = JObject.Parse (Encoding.UTF8.GetString (Convert.FromBase64String (tokenObjects [1])));

            // Attempt to get the certificate
            JwtSecurityTokenHandler handler = new JwtSecurityTokenHandler();
            if (!handler.CanReadToken(idToken))
                throw new SecurityTokenException("Token is not in a valid format");

            SecurityToken token = null;
            var identities = handler.ValidateToken(idToken, this.m_configuration.ToConfigurationObject(), out token);

            // Validate token expiry
            if (token.ValidTo < DateTime.Now.ToUniversalTime())
                throw new SecurityTokenException("Token expired");
            else if (token.ValidFrom > DateTime.Now.ToUniversalTime())
                throw new SecurityTokenException("Token not yet valid");

            this.RefreshToken = refreshToken;
			this.AddIdentities(identities.Identities);
		}
        
		/// <summary>
		/// Represent the token claims principal as a string (the access token itself)
		/// </summary>
		/// <returns>To be added.</returns>
		/// <remarks>To be added.</remarks>
		public override string ToString ()
		{
			return this.m_accessToken;
		}
	}
}

