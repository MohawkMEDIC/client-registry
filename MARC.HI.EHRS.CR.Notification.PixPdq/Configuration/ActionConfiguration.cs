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
