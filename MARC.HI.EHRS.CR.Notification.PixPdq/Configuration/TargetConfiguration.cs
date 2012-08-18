using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MARC.HI.EHRS.CR.Notification.PixPdq.Configuration
{
    /// <summary>
    /// Target node configuration
    /// </summary>
    public class TargetConfiguration
    {

        /// <summary>
        /// Creates a new target configuration
        /// </summary>
        public TargetConfiguration(string name, string connectionString)
        {
            this.Name = name;
            this.ConnectionString = connectionString;
            this.NotificationDomain = new List<NotificationDomainConfiguration>();
        }

        /// <summary>
        /// Gets or sets the name of the notification configuration
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Gets or sets the WcfServiceConnector connection string
        /// </summary>
        public string ConnectionString { get; private set; }

        /// <summary>
        /// Gets or sets the notification domains to be sent when an action is 
        /// created
        /// </summary>
        public List<NotificationDomainConfiguration> NotificationDomain { get; set; }
    }
}
