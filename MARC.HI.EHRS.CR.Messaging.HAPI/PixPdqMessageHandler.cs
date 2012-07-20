using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MARC.HI.EHRS.SVC.Core.Services;

namespace MARC.HI.EHRS.CR.Messaging.PixPdqv2
{
    /// <summary>
    /// Message handler service
    /// </summary>
    public class PixPdqMessageHandler : IMessageHandlerService
    {
        #region IMessageHandlerService Members

        /// <summary>
        /// Start the v2 message handler
        /// </summary>
        public bool Start()
        {
            
            throw new NotImplementedException();
        }

        /// <summary>
        /// Stop the v2 message handler
        /// </summary>
        public bool Stop()
        {
            throw new NotImplementedException();
        }

        #endregion

        #region IUsesHostContext Members

        /// <summary>
        /// Gets or sets the host context
        /// </summary>
        public SVC.Core.HostContext Context
        {
            get;
            set;
        }

        #endregion
    }
}
