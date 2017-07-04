using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MARC.HI.EHRS.CR.Messaging.Authentication.OAuthConfiguration
{
    class OAuthBasicAuthorization
    {
        /// <summary>
        /// Require client authentication.
        /// </summary>
        public bool RequireClientAuth { get; set; }

        /// <summary>
        /// Allowed claims 
        /// </summary>
        public List<string> AllowedClientClaims { get; set; }

        /// <summary>
        /// Realm of basic auth
        /// </summary>
        public string Realm { get; internal set; }
    }
}
