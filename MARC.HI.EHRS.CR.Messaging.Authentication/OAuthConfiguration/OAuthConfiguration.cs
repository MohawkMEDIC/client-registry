using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MARC.HI.EHRS.CR.Messaging.Authentication.OAuthConfiguration
{
    class OAuthConfiguration
    {
        /// <summary>
        /// Security configuration
        /// </summary>
        public OAuthSecurityConfiguration Security { get; set; }

        /// <summary>
        /// Thread pool size
        /// </summary>
        public int ThreadPoolSize { get; set; }

        public string CertificateThumbprint { get; set; }

        public string URLEndpoint { get; set; }
    }
}
