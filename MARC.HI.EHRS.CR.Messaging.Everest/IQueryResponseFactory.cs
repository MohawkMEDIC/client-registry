/**
 * Copyright 2015-2015 Mohawk College of Applied Arts and Technology
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
 * User: Justin
 * Date: 12-7-2015
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MARC.Everest.Interfaces;
using MARC.HI.EHRS.CR.Core.ComponentModel;
using MARC.Everest.Connectors;
using MARC.HI.EHRS.SVC.Core.Services;
using MARC.HI.EHRS.SVC.Core.Issues;
using MARC.HI.EHRS.CR.Core.Services;

namespace MARC.HI.EHRS.CR.Messaging.Everest
{
    /// <summary>
    /// This class is used to create responses to queries
    /// </summary>
    interface IQueryResponseFactory : IUsesHostContext
    {

        /// <summary>
        /// Gets the type of message the response factory creates
        /// </summary>
        Type CreateType { get; }

        /// <summary>
        /// Create the HSR query data
        /// </summary>
        RegistryQueryRequest CreateFilterData(IInteraction request, List<IResultDetail> dtls);

        /// <summary>
        /// Create the response for the query
        /// </summary>
        IInteraction Create(IInteraction request, RegistryQueryResult results, List<IResultDetail> dtls);

    }
}
