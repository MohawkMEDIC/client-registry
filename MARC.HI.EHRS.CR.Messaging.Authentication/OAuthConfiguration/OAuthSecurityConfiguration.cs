using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MARC.HI.EHRS.CR.Messaging.Authentication.OAuthConfiguration
{
    class OAuthSecurityConfiguration
    {
        /// <summary>
        /// Basic authentication configuration
        /// </summary>
        public OAuthBasicAuthorization BasicAuth { get; set; }

        /// <summary>
        /// Gets or sets the claims auth
        /// </summary>
        public OAuthClaimsAuthorization ClaimsAuth { get; set; }
    }
}
