/**
 * Copyright 2012-2013 Mohawk College of Applied Arts and Technology
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
 * Date: 17-10-2012
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MARC.HI.EHRS.CR.Core.Configuration;
using MARC.HI.EHRS.CR.Core.ComponentModel;

namespace MARC.HI.EHRS.CR.Core.Services
{
    public interface IClientRegistryConfigurationService
    {

        /// <summary>
        /// Gets the client registry configuration
        /// </summary>
        ClientRegistryConfiguration Configuration { get; }

        /// <summary>
        /// True if strict identity rules should be imposed
        /// </summary>
        bool HasStrictIdentityRules { get; }

        /// <summary>
        /// Create a merge filter
        /// </summary>
        Person CreateMergeFilter(Person p);
    }
}
