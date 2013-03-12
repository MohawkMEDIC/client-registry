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
 * Date: 21-8-2012
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MARC.HI.EHRS.CR.Notification.PixPdq.Configuration
{
    /// <summary>
    /// Notification configuration
    /// </summary>
    public class NotificationConfiguration
    {

        /// <summary>
        /// Creates a new instance of the NotificationConfiguration
        /// </summary>
        public NotificationConfiguration(int concurrencyLevel)
        {
            this.Targets = new List<TargetConfiguration>();
            this.ConcurrencyLevel = concurrencyLevel;
        }


        /// <summary>
        /// Gets or sets the list of targets that are configured as part of this notification service
        /// </summary>
        public List<TargetConfiguration> Targets { get; private set; }

        /// <summary>
        /// Gets the concurrency level
        /// </summary>
        public int ConcurrencyLevel { get; private set; }
    }
}
