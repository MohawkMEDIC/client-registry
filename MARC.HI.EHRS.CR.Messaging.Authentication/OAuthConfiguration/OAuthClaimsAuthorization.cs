using System;
using System.Collections.Generic;
using System.Configuration;
using System.IdentityModel.Selectors;
using System.IdentityModel.Tokens;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace MARC.HI.EHRS.CR.Messaging.Authentication.OAuthConfiguration
{
    class OAuthClaimsAuthorization
    {
        /// <summary>
        /// Creates a new claims
        /// </summary>
        public OAuthClaimsAuthorization()
        {
            this.Audiences = new List<string>();
            this.IssuerKeys = new Dictionary<string, SecurityKey>();
        }

        /// <summary>
        /// Custom validator type
        /// </summary>
        public Type CustomValidator { get; set; }

        /// <summary>
        /// Symmetric issuer keys
        /// </summary>
        public Dictionary<String, SecurityKey> IssuerKeys { get; set; }

        /// <summary>
        /// Gets or sets the allowed audiences 
        /// </summary>
        public List<String> Audiences { get; set; }

        /// <summary>
        /// Gets or sets the realm
        /// </summary>
        public string Realm { get; internal set; }


        /// <summary>
        /// Convert this to a STS handler config
        /// </summary>
        public TokenValidationParameters ToConfigurationObject()
        {

            TokenValidationParameters retVal = new TokenValidationParameters();

            retVal.ValidIssuers = this.IssuerKeys.Select(o => o.Key);
            retVal.RequireExpirationTime = true;
            retVal.RequireSignedTokens = true;
            retVal.ValidAudiences = this.Audiences;
            retVal.ValidateLifetime = true;
            retVal.ValidateIssuerSigningKey = true;
            retVal.ValidateIssuer = true;
            retVal.ValidateAudience = true;
            retVal.IssuerSigningTokens = this.IssuerKeys.Where(o => o.Value is X509SecurityKey).Select(o => new X509SecurityToken((o.Value as X509SecurityKey).Certificate));
            retVal.IssuerSigningKeys = this.IssuerKeys.Select(o => o.Value);
            retVal.IssuerSigningKeyResolver = (s, securityToken, identifier, parameters) =>
            {

                if (identifier.Count > 0)
                    return identifier.Select(o =>
                    {
                        // Lookup by thumbprint
                        SecurityKey candidateKey = null;

                        if (o is X509ThumbprintKeyIdentifierClause)
                            candidateKey = this.IssuerKeys.SingleOrDefault(ik => (ik.Value as X509SecurityKey).Certificate.Thumbprint == BitConverter.ToString((o as X509ThumbprintKeyIdentifierClause).GetX509Thumbprint()).Replace("-", "")).Value;

                        return candidateKey;
                    }).First(o => o != null);
                else
                {
                    SecurityKey candidateKey = null;
                    this.IssuerKeys.TryGetValue((securityToken as JwtSecurityToken).Issuer, out candidateKey);
                    return candidateKey;
                }
            };

            // Custom validator
            if (this.CustomValidator != null)
            {
                ConstructorInfo ci = this.CustomValidator.GetConstructor(Type.EmptyTypes);
                if (ci == null)
                    throw new ConfigurationException("No constructor found for custom validator");
                retVal.CertificateValidator = ci.Invoke(null) as X509CertificateValidator;
            }


            return retVal;
        }
    }
}
