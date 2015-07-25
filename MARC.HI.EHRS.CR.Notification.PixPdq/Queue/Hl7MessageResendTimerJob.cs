/**
 * Copyright 2012-2015 Mohawk College of Applied Arts and Technology
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
 * User: Justin
 * Date: 12-7-2015
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MARC.HI.EHRS.SVC.Core.Timer;
using System.Diagnostics;
using MARC.HI.EHRS.SVC.Core.Services;

namespace MARC.HI.EHRS.CR.Notification.PixPdq.Queue
{
    /// <summary>
    /// Represents a dead message queue timer job
    /// </summary>
    public class Hl7MessageResendTimerJob : ITimerJob
    {

        /// <summary>
        /// Localization service 
        /// </summary>
        private ILocalizationService m_localeService; 

        /// <summary>
        /// Restore the queue of work items
        /// </summary>
        public Hl7MessageResendTimerJob()
        {
            Hl7MessageQueue.Current.Restore();
        }
        
        /// <summary>
        /// Timer has elapsed
        /// </summary>
        public void Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            // Try to send the work items again
            var msi = Hl7MessageQueue.Current.DequeueMessageItem();
            if (msi == null) return; // nothing to send

            // Try to re-send the message
            if (!msi.TrySend() && msi.FailCount < 10)
                Hl7MessageQueue.Current.EnqueueMessageItem(msi);
            else
            {
                if (this.m_localeService == null)
                    this.m_localeService = Context.GetService(typeof(ILocalizationService)) as ILocalizationService;
                Trace.TraceError(this.m_localeService.GetString("NTFW006"));
            }
        }

        /// <summary>
        /// Context for the timer job
        /// </summary>
        public IServiceProvider Context
        {
            get;
            set;
        }
    }
}
