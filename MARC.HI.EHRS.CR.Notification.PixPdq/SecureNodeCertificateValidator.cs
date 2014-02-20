using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IdentityModel.Selectors;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography;
using System.Diagnostics;
using System.IdentityModel.Tokens;

namespace MARC.HI.EHRS.CR.Notification.PixPdq
{
    /// <summary>
    /// Secure node certificate validator
    /// </summary>
    public class SecureNodeCertificateValidator : X509CertificateValidator
    {

        public override void Validate(System.Security.Cryptography.X509Certificates.X509Certificate2 certificate)
        {
            bool overallValid = false;
            // First Validate the chain
            foreach (var t in PixNotifier.s_configuration.Targets)
            {
                X509Chain chain = new X509Chain(t.TrustedIssuerCertLocation == StoreLocation.LocalMachine);
                chain.ChainPolicy.ApplicationPolicy.Add(new Oid("1.3.6.1.5.5.7.3.2"));
                chain.ChainPolicy.RevocationMode = X509RevocationMode.Online;
                chain.Build(certificate);

                if (certificate == null || chain == null)
                    throw new SecurityTokenValidationException("Failed to build chain from certificate");
                else
                {

                    bool isValid = false;
                    foreach (var cer in chain.ChainElements)
                        if (cer.Certificate.Thumbprint == t.TrustedIssuerCertificate.Thumbprint)
                            isValid = true;
                    if (!isValid)
                        Trace.TraceError("Certification authority from the supplied certificate doesn't match the expected thumbprint");
                    foreach (var stat in chain.ChainStatus)
                        Trace.TraceWarning("Certificate chain validation error: {0}", stat.StatusInformation);
                    isValid &= chain.ChainStatus.Length == 0;
                    overallValid |= isValid;
                }
            }

            // overall failed?
            if(!overallValid)
                throw new SecurityTokenValidationException("Certificate provided by service was not issued by any specified issuer");

        }
    }
}
