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
        void NotifyUpdate(RegistrationEvent evt);

    }
}
