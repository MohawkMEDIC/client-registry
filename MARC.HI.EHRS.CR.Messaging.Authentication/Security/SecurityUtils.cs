using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography.X509Certificates;

namespace MARC.HI.EHRS.CR.Messaging.Authentication.Security
{
    public static class SecurityUtils
    {
        /// <summary>
        /// Find the specified certificate
        /// </summary>
        public static X509Certificate2 FindCertificate(
            String storeLocation, String storeName, String x509FindType, String findValue)
        {

            X509Store store = new X509Store((StoreName)Enum.Parse(typeof(StoreName), storeName ?? "My"),
                (StoreLocation)Enum.Parse(typeof(StoreLocation), storeLocation ?? "LocalMachine")
            );

            try
            {
                store.Open(OpenFlags.ReadOnly);
                // Now find the certificate
                var matches = store.Certificates.Find((X509FindType)Enum.Parse(typeof(X509FindType), x509FindType ?? "FindByThumbprint"), findValue, false);
                if (matches.Count > 1)
                    throw new Exception("More than one candidate certificate found");
                else if (matches.Count == 0)
                    throw new KeyNotFoundException("No matching certificates found");
                else
                    return matches[0];
            }
            finally
            {
                store.Close();
            }

        }
    }
}
