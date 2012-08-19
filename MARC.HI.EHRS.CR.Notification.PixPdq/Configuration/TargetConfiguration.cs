using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MARC.HI.EHRS.CR.Notification.PixPdq.Configuration
{

    /// <summary>
    /// The actor types
    /// </summary>
    public enum TargetActorType
    {
        PAT_IDENTITY_SRC,
        PAT_IDENTITY_X_REF_MGR
    }

    /// <summary>
    /// Target node configuration
    /// </summary>
    public class TargetConfiguration
    {

        /// <summary>
        /// Creates a new target configuration
        /// </summary>
        public TargetConfiguration(string name, string connectionString, TargetActorType actAs, string deviceId)
        {
            this.Name = name;
            this.ConnectionString = connectionString;
            this.ActAs = actAs;
            this.DeviceIdentifier = deviceId;
            this.NotificationDomain = new List<NotificationDomainConfiguration>();

        }

        
        /// <summary>
        /// Gets the value that indicates which IHE actor this tool is acting as 
        /// when communicating with the notification target
        /// </summary>
        public TargetActorType ActAs { get; set; }

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

        /// <summary>
        /// Configuration device identifier
        /// </summary>
        public string DeviceIdentifier { get; private set; }
    }
}
