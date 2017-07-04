/**
 * Copyright 2012-2013 Mohawk College of Applied Arts and Technology
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
 * User: fyfej
 * Date: 13-8-2012
 */

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
            //foreach (var thd in this.m_listenerThreads)
            //    if(thd.IsAlive)
            //        thd.Abort();
            return true;
        }

        #endregion

        #region IUsesHostContext Members

        // Host context
        private IServiceProvider m_context;

        public event EventHandler Started;
        public event EventHandler Starting;
        public event EventHandler Stopped;
        public event EventHandler Stopping;

        /// <summary>
        /// Gets or sets the host context
        /// </summary>
        public IServiceProvider Context
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

        public bool IsRunning
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        #endregion
    }
}
