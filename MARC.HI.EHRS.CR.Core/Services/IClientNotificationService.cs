using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MARC.HI.EHRS.SVC.Core.Services;
using MARC.HI.EHRS.CR.Core.ComponentModel;

namespace MARC.HI.EHRS.CR.Core.Services
{
    /// <summary>
    /// Client notification service
    /// </summary>
    public interface IClientNotificationService : IUsesHostContext
    {

        /// <summary>
        /// Notify that an update has occurred
        /// </summary>
        /// <remarks>Contains an older version of the record that was updated</remarks>
        void NotifyUpdate(RegistrationEvent evt);

        /// <summary>
        /// Notify that a registration has occurred
        /// </summary>
        /// <remarks>Contains no older version of the registration</remarks>
        void NotifyRegister(RegistrationEvent evt);

        /// <summary>
        /// Notify that a reconcilation is required
        /// </summary>
        void NotifyReconciliationRequired(RegistrationEvent[] candidates);
    }
}
