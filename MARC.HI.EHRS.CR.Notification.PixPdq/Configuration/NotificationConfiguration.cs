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
