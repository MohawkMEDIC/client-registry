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
