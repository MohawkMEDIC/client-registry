/**
 * Copyright 2012-2015 Mohawk College of Applied Arts and Technology
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

namespace MARC.HI.EHRS.CR.Notification.PixPdq.Configuration
{
    /// <summary>
    /// Represents notification domain configuration
    /// </summary>
    public class NotificationDomainConfiguration
    {

        /// <summary>
        /// Creates a new notification domain configuration
        /// </summary>
        public NotificationDomainConfiguration(string domain)
        {
            this.Domain = domain;
            this.Actions = new List<ActionConfiguration>();
        }

        /// <summary>
        /// Gets the domain of the notification configuration
        /// </summary>
        public string Domain { get; private set; }

        /// <summary>
        /// Gets or sets a list of actions that trigger notifications within a 
        /// domain
        /// </summary>
        public List<ActionConfiguration> Actions { get; private set; }

        /// <summary>
        /// Returns true if the notification domain should be applied for
        /// the specified <paramref name="action"/>
        /// </summary>
        public bool IsApplicableFor(ActionType action)
        {
            return this.Actions.Exists(a => a.Action == action);
        }
    }
}
