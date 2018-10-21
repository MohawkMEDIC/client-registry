﻿/**
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
using MARC.HI.EHRS.CR.Core.ComponentModel;
using MARC.HI.EHRS.CR.Notification.PixPdq.Configuration;

namespace MARC.HI.EHRS.CR.Notification.PixPdq
{
    /// <summary>
    /// Represents a notification work item for the wait thread poool
    /// </summary>
    public class NotificationQueueWorkItem
    {

        /// <summary>
        /// Create a new notification queue work item
        /// </summary>
        public NotificationQueueWorkItem(Core.ComponentModel.RegistrationEvent evt, Configuration.ActionType actionType)
        {
            // TODO: Complete member initialization
            this.Event = evt;
            this.Action = actionType;
        }

        /// <summary>
        /// Gets the event that triggered the action
        /// </summary>
        public RegistrationEvent Event { get; private set; }
        /// <summary>
        /// Gets the action performed on the Event
        /// </summary>
        public ActionType Action { get; private set; }

    }
}
