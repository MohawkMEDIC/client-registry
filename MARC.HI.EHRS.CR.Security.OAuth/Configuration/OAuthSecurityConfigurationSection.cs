using SanteDB.Core.Http;
using SanteDB.Core.Http.Description;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens;
using System.Linq;
using System.Security.Cryptography;
using System.ServiceModel.Security.Tokens;
using System.Text;
using System.Threading.Tasks;

namespace MARC.HI.EHRS.CR.Security.OAuth.Configuration
{

    /// <summary>
    /// OAuth client description
    /// </summary>
    public class OAuthClientDescription : IRestClientDescription
    {
        /// <summary>
        /// Create a new endpoint description
        /// </summary>
        public OAuthClientDescription(String endpoint, String realm, ICredentialProvider credentialProvider, SecurityScheme scheme)
        {
            this.Endpoint = new List<IRestClientEndpointDescription>()
            {
                new OAuthRestClientEndpointDescription(endpoint)
            };

            this.Binding = new OAuthClientBindingDescription(new OAuthClientSecurityDescription(credentialProvider, scheme, realm));
        }

        /// <summary>
        /// True if tracing should be enabled
        /// </summary>
        public bool Trace => false;

        /// <summary>
        /// Gets or sets the endpoint
        /// </summary>
        public List<IRestClientEndpointDescription> Endpoint { get; private set; }

        /// <summary>
        /// Gets the binding
        /// </summary>
        public IRestClientBindingDescription Binding { get; private set; }
    }

    /// <summary>
    /// Represents a client binding
    /// </summary>
    public class OAuthClientBindingDescription : IRestClientBindingDescription
    {
        /// <summary>
        /// Create OAuth binding description
        /// </summary>
        public OAuthClientBindingDescription(OAuthClientSecurityDescription security)
        {
            this.Security = security;
        }

        /// <summary>
        /// Gets the content/type mapper
        /// </summary>
        public IContentTypeMapper ContentTypeMapper => new DefaultContentTypeMapper();

        /// <summary>
        /// Get or sets the rest client security 
        /// </summary>
        public IRestClientSecurityDescription Security { get; private set; }

        /// <summary>
        /// Gets or sets whether optimization should occur
        /// </summary>
        public bool Optimize => false;
    }

    /// <summary>
    /// Represents client security description
    /// </summary>
    public class OAuthClientSecurityDescription : IRestClientSecurityDescription
    {
        /// <summary>
        /// Creates a new client security description
        /// </summary>
        public OAuthClientSecurityDescription(ICredentialProvider credentialProvider, SecurityScheme scheme, String realm)
        {
            this.CredentialProvider = credentialProvider;
            this.Mode = scheme;
            this.AuthRealm = realm;
            this.PreemptiveAuthentication = true;
        }

        /// <summary>
        /// Gets the certificate validator
        /// </summary>
        public ICertificateValidator CertificateValidator => null;

        /// <summary>
        /// Gets or sets the credential provider
        /// </summary>
        public ICredentialProvider CredentialProvider { get; private set; }

        /// <summary>
        /// Security Scheme mode
        /// </summary>
        public SecurityScheme Mode { get; private set; }

        /// <summary>
        /// Gets or sets the client certificate
        /// </summary>
        /// TODO:
        public IRestClientCertificateDescription ClientCertificate => null;

        /// <summary>
        /// Authentication realm
        /// </summary>
        public string AuthRealm { get; private set; }

        /// <summary>
        /// Pre-empt authorization
        /// </summary>
        public bool PreemptiveAuthentication { get; set; }
    }

    /// <summary>
    /// Represents endpoint description
    /// </summary>
    public class OAuthRestClientEndpointDescription : IRestClientEndpointDescription
    {
        /// <summary>
        /// Create a new endpoint description
        /// </summary>
        public OAuthRestClientEndpointDescription(String address)
        {
            this.Address = address;
            this.Timeout = 20000;
        }

        /// <summary>
        /// Gets the address of the endpoint
        /// </summary>
        public string Address { get; private set; }

        /// <summary>
        /// Gets or sets the timeout
        /// </summary>
        public int Timeout { get; set; }
    }

    /// <summary>
    /// OAuth security configuration section
    /// </summary>
    public class OAuthSecurityConfigurationSection
    {
        /// <summary>
        /// Creates a new oauth security configuration
        /// </summary>
        public OAuthSecurityConfigurationSection(String endpoint, String clientId, String clientSecret, String realm, String jwtSecret, String issuer)
        {
            this.AuthenticationEndpoint = endpoint;
            this.ClientId = clientId;
            this.ClientSecret = clientSecret;
            this.AuthenticationRealm = realm;
            this.JwtSecret = jwtSecret;
            this.Issuer = issuer;
        }

        /// <summary>
        /// Gets or sets the authentication realm
        /// </summary>
        public String AuthenticationRealm { get; private set; }

        /// <summary>
        /// Authentication endpoint
        /// </summary>
        public String AuthenticationEndpoint { get; private set; }

        /// <summary>
        /// Get the client secret of this application
        /// </summary>
        public String ClientSecret { get; private set; }

        /// <summary>
        /// Get the client identifier of this application
        /// </summary>
        public String ClientId { get; private set; }

        /// <summary>
        /// JWT secret
        /// </summary>
        public String JwtSecret { get; private set; }

        /// <summary>
        /// Gets the issuer
        /// </summary>
        public String Issuer { get; private set; }

        /// <summary>
        /// Get the IDP description
        /// </summary>
        public IRestClientDescription GetIdpDescription()
        {
            return new OAuthClientDescription(this.AuthenticationEndpoint, this.AuthenticationRealm, null, SecurityScheme.None);
        }

        /// <summary>
        /// Convert this to a STS handler config
        /// </summary>
        public TokenValidationParameters ToConfigurationObject()
        {

            TokenValidationParameters retVal = new TokenValidationParameters();

            retVal.ValidIssuers = new String[] { this.Issuer };
            retVal.RequireExpirationTime = true;
            retVal.RequireSignedTokens = true;
            retVal.ValidateLifetime = true;
            retVal.ValidateIssuerSigningKey = true;
            retVal.ValidateIssuer = true;
            retVal.ValidateAudience = false;
            retVal.IssuerSigningToken = new BinarySecretSecurityToken(SHA256.Create().ComputeHash(Encoding.UTF8.GetBytes(this.JwtSecret)));
            retVal.IssuerSigningKeyValidator = (o) =>
            {
                ;
            };
            retVal.IssuerSigningKeys = new SecurityKey[] { new InMemorySymmetricSecurityKey(SHA256.Create().ComputeHash(Encoding.UTF8.GetBytes(this.JwtSecret))) };
            retVal.IssuerSigningKeyResolver = (s, securityToken, identifier, parameters) =>
            {
                return new InMemorySymmetricSecurityKey(SHA256.Create().ComputeHash(Encoding.UTF8.GetBytes(this.JwtSecret)));
            };
            return retVal;
        }
    }
}
