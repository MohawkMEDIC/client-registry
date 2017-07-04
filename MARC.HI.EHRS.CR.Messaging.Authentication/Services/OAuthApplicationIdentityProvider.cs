using MARC.HI.EHRS.CR.Messaging.Authentication.OAuthIdentityConfiguration;
using MARC.HI.EHRS.SVC.Core.Event;
using MARC.HI.EHRS.CR.Authentication.OAuth2.Wcf;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using System.Data.Linq;
using System.Net;
using System.Security;
using System.Security.Claims;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Net.Sockets;
using System.Net.Security;
using System.Web;

namespace MARC.HI.EHRS.CR.Messaging.Authentication.Services
{
    public class OAuthApplicationIdentityProvider : IApplicationIdentityProviderService
    {
        public event EventHandler<AuthenticatedEventArgs> Authenticated;
        public event EventHandler<AuthenticatingEventArgs> Authenticating;

        // Configuration from main OAuth
        private OAuthConfiguration.OAuthConfiguration m_configuration = ConfigurationManager.GetSection("marc.hi.ehrs.cr.messaging.authentication") as OAuthConfiguration.OAuthConfiguration;

        public IPrincipal Authenticate(string applicationId, string applicationSecret)
        {
            Trace.TraceInformation("Entering OAuth2: Authenticate User");

            Uri portalUri = new Uri(m_configuration.URLEndpoint + "/Account/Validate");
            
            X509Store store = new X509Store(StoreName.My, StoreLocation.LocalMachine);
            store.Open(OpenFlags.ReadOnly);
            
            // Attempts to locate the certificate by its thumbprint indicated in the configuration file
            var certificates = store.Certificates.Find(X509FindType.FindByThumbprint, m_configuration.CertificateThumbprint.ToUpper(), false);

            store.Close();

            // Throws an error if more than one certificate if found with the indicated thumbprint
            if (certificates.Count > 1)
            {
                Trace.TraceInformation("Found multiple certificates with the same fingerprint.");
                throw new Exception("Found multiple certificates with the same fingerprint.");
            }

            // Open a TCP connection with port 443 for an ssl connection
            //using (TcpClient client = new TcpClient(portalUri.Host, 44368))
            using (TcpClient client = new TcpClient(portalUri.Host, 443))
            {
                Trace.TraceInformation("OAuth2: Starting Secure Stream Request");

                try
                {
                    SslStream sslStream = null;

                    if (certificates[0] != null)
                    {
                        // Creates an ssl stream from the tcp client with a custom validation function
                        sslStream = new SslStream(client.GetStream(), false, ValidateServerCertificate, null);

                        var collection = new X509CertificateCollection
                        {
                            certificates[0]
                        };

                        Trace.TraceInformation("OAuth2 Authenticating Request as Client");

                        // Authenticates the user for making secure connection requests
                        sslStream.AuthenticateAsClient(portalUri.Host, collection, System.Security.Authentication.SslProtocols.Tls, true);
                    }

                    using (var stream = sslStream)
                    {
                        // Creates a web request to the MEMPI Portal validation endpoint
                        HttpWebRequest request = (HttpWebRequest)WebRequest.Create(portalUri);
                        request.KeepAlive = false;
                        request.ProtocolVersion = HttpVersion.Version10;
                        request.Method = "POST";
                        request.ContentType = "application/x-www-form-urlencoded";

                        ServicePointManager.ServerCertificateValidationCallback = ValidateServerCertificate;

                        // Adds the user's credentials to the post request data
                        var postData = "username=" + applicationId + "&password=" + applicationSecret;

                        byte[] postBytes = Encoding.ASCII.GetBytes(postData);

                        request.ContentLength = postBytes.Length;
                        
                        // Sends the request with the user's credentials
                        Stream requestStream = request.GetRequestStream();
                        requestStream.Write(postBytes, 0, postBytes.Length);
                        requestStream.Close();
                       
                        Trace.TraceInformation("OAuth2 Retrieving Response");

                        // gets the response from the authentication call to the MEMPI Portal
                        HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                        var result = new StreamReader(response.GetResponseStream()).ReadToEnd();

                        if (result == "True")
                        {
                            Trace.TraceInformation("OAuth2 Successful Credentials");

                            // Creates a new claims identity for the authenticated user
                            var principal = OAuthClaimsIdentity.Create(applicationId, applicationSecret).CreateClaimsPrincipal();
                            this.Authenticated?.Invoke(this, new AuthenticatedEventArgs(applicationId, principal, true));
                            return principal;
                        }
                        else
                        {
                            Trace.TraceInformation("OAuth2: User is not valid");
                            throw new Exception("User is not valid");
                        }
                    }
                }
                catch (Exception e)
                {
                    Trace.TraceInformation(e.ToString());
                    throw;
                }
            }
        }

        public IIdentity GetIdentity(string name)
        {
            return OAuthClaimsIdentity.Create(name);
        }

        public static bool ValidateServerCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            return true;
            if (sslPolicyErrors == SslPolicyErrors.None)
            {
                return true;
            }
            
            Dictionary<X509ChainStatusFlags, string> messages = new Dictionary<X509ChainStatusFlags, string>
            {
                { X509ChainStatusFlags.NotTimeValid, "An error occured in a date indicated on the certificate." },
                { X509ChainStatusFlags.Revoked, "The certificate being used has been revoked by the server." },
                { X509ChainStatusFlags.NotSignatureValid, "The certificate is not valid due to an invalid signature." },
                { X509ChainStatusFlags.NotValidForUsage, "The key usage is not valid." },
                { X509ChainStatusFlags.UntrustedRoot, "The request is not valid due to an untrusted certificate." },
                { X509ChainStatusFlags.RevocationStatusUnknown, "Revocation status of certificate unknown. Could be due to the certificate revocation list being offline or unavailable." },
                { X509ChainStatusFlags.Cyclic, "The certificate chain could not be built." },
                { X509ChainStatusFlags.InvalidExtension, "The certificate chain is invalid due to an invalid extension." },
                { X509ChainStatusFlags.InvalidPolicyConstraints, "The certificate chain is invalid due to invalid policy constraints." },
                { X509ChainStatusFlags.InvalidBasicConstraints, "The certificate chain is invalid due to invalid basic constraints." },
                { X509ChainStatusFlags.InvalidNameConstraints, "The certificate chain is invalid due to invalid name constraints." },
                { X509ChainStatusFlags.HasNotSupportedNameConstraint, "The certificate does not have supported name constraints or has a name constraint that is unsupported." },
                { X509ChainStatusFlags.HasNotDefinedNameConstraint, "The certificate has an undefined name constraint." },
                { X509ChainStatusFlags.HasNotPermittedNameConstraint, "The certificate has an impermissible name constraint." },
                { X509ChainStatusFlags.HasExcludedNameConstraint, "The certificate chain is invalid because a certificate has excluded a name constraint." },
                { X509ChainStatusFlags.PartialChain, "The certificate chain could not be built up to the root certificate." },
                { X509ChainStatusFlags.CtlNotTimeValid, "The certificate trust list (CTL) is not valid because of an invalid time value, such as one that indicates that the CTL has expired." },
                { X509ChainStatusFlags.CtlNotSignatureValid, "The certificate trust list (CTL) contains an invalid signature." },
                { X509ChainStatusFlags.CtlNotValidForUsage, "The certificate trust list (CTL) is not valid for this use." },
                { X509ChainStatusFlags.OfflineRevocation, "The online certificate revocation list (CRL) the X509 chain relies on is currently offline." },
                { X509ChainStatusFlags.NoIssuanceChainPolicy, "There is no certificate policy extension in the certificate. This error would occur if a group policy has specified that all certificates must have a certificate policy." }
            };
            
            List<X509ChainStatusFlags> acceptedMessages = new List<X509ChainStatusFlags>
            {
                X509ChainStatusFlags.NoError,
                X509ChainStatusFlags.RevocationStatusUnknown,
                X509ChainStatusFlags.OfflineRevocation
            };

            List<X509ChainStatusFlags> rejectedMessages = new List<X509ChainStatusFlags>
            {
                X509ChainStatusFlags.Revoked,
                X509ChainStatusFlags.UntrustedRoot,
                X509ChainStatusFlags.CtlNotSignatureValid
            };

            X509ChainStatus[] chainMessages = chain.ChainStatus;

            var accepted = chainMessages.Where(x => acceptedMessages.Contains(x.Status)).ToList();
            var rejected = chainMessages.Where(x => rejectedMessages.Contains(x.Status)).ToList();

            if (accepted.Count > 0)
            {
                if (rejected.Count > 0)
                {
                    return false;
                }

                return true;
            }

            return false;
        }
    }
}
