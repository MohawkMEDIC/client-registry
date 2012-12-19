/**
 * Copyright 2012-2012 Mohawk College of Applied Arts and Technology
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
    /// Identifies the type of actions
    /// </summary>
    public enum ActionType
    {
        /// <summary>
        /// Any action occurs. This is only used
        /// </summary>
        Any = Create | Update | DuplicatesResolved,
        /// <summary>
        /// Action occurs when a person is created
        /// </summary>
        Create = 0x1,
        /// <summary>
        /// Action occurs when a person is revised
        /// </summary>
        Update = 0x2,
        /// <summary>
        /// Action occurs when duplicates are resolved
        /// </summary>
        DuplicatesResolved = 0x4
    }

    /// <summary>
    /// Action configuration
    /// </summary>
    public class ActionConfiguration
    {

        /// <summary>
        /// Creates a new action configuration
        /// </summary>
        public ActionConfiguration(ActionType action)
        {
            this.Action = action;
        }

        /// <summary>
        /// Gets or sets the action type
        /// </summary>
        public ActionType Action { get; private set; }

    }
}
