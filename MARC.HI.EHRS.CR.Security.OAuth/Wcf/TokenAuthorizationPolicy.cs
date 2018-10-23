﻿/*
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
using System.Collections.Generic;
using System.IdentityModel.Claims;
using System.IdentityModel.Policy;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MARC.HI.EHRS.CR.Security.OAuth.Wcf
{
    /// <summary>
    /// Set the authorization policy
    /// </summary>
    public class TokenAuthorizationPolicy : IAuthorizationPolicy
    {

        /// <summary>
        /// Gets the identifier of the policy
        /// </summary>
        public string Id
        {
            get
            {
                return nameof(TokenAuthorizationPolicy);
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
