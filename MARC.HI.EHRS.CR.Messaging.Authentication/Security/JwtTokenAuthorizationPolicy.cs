using System;
using System.Collections.Generic;
using System.IdentityModel.Claims;
using System.IdentityModel.Policy;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MARC.HI.EHRS.CR.Messaging.Authentication.Security
{
    public class JwtTokenAuthorizationPolicy : IAuthorizationPolicy
    {
        /// <summary>
        /// Gets the identifier of the policy
        /// </summary>
        public string Id
        {
            get
            {
                return nameof(JwtTokenAuthorizationPolicy);
            }
        }

        /// <summary>
        /// Issuer
        /// </summary>
        public ClaimSet Issuer
        {
            get
            {
                return ClaimSet.System;
            }
        }

        /// <summary>
        /// Evaluate the context
        /// </summary>
        public bool Evaluate(EvaluationContext evaluationContext, ref object state)
        {
            if (AuthenticationContext.Current.Principal == AuthenticationContext.AnonymousPrincipal)
                return false;
            evaluationContext.Properties["Principal"] = AuthenticationContext.Current.Principal;
            return true;
        }
    }
}
