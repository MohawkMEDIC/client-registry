using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MARC.HI.EHRS.SVC.Core.Services;
using System.Threading;
using MARC.HI.EHRS.CR.Messaging.HL7.Configuration;
using System.Configuration;
using System.Diagnostics;

namespace MARC.HI.EHRS.CR.Messaging.HL7
{
    /// <summary>
    /// Message handler service
    /// </summary>
    public class HL7MessageHandler : IMessageHandlerService
    {
        #region IMessageHandlerService Members

        // Configuration 
        private HL7ConfigurationSection m_configuration;

        // Threads that are listening for messages
        private List<Thread> m_listenerThreads = new List<Thread>();

        /// <summary>
        /// Load configuration
        /// </summary>
        public HL7MessageHandler()
        {
            this.m_configuration = ConfigurationManager.GetSection("marc.hi.ehrs.cr.messaging.hl7") as HL7ConfigurationSection;
        }

        /// <summary>
        /// Start the v2 message handler
        /// </summary>
        public bool Start()
        {
            foreach (var sd in this.m_configuration.Services)
            {
                // Set contexts
                foreach (var hd in sd.Handlers)
                    hd.Handler.Context = this.Context;

                var sh = new ServiceHandler(sd);
                Thread thdSh = new Thread(sh.Run);
                thdSh.IsBackground = true;
                this.m_listenerThreads.Add(thdSh);
                Trace.TraceInformation("Starting HL7 Service '{0}'...", sd.Name);
                thdSh.Start();
            }
            return true;
        }

        /// <summary>
        /// Stop the v2 message handler
        /// </summary>
        public bool Stop()
        {
            foreach (var thd in this.m_listenerThreads)
                if(thd.IsAlive)
                    thd.Abort();
            return true;
        }

        #endregion

        #region IUsesHostContext Members

        // Host context
        private MARC.HI.EHRS.SVC.Core.HostContext m_context;

        /// <summary>
        /// Gets or sets the host context
        /// </summary>
        public SVC.Core.HostContext Context
        {
            get
            {
                return this.m_context;
            }
            set
            {
                this.m_context = value;
                ApplicationContext.CurrentContext = this.m_context;
            }
        }

        #endregion
    }
}
