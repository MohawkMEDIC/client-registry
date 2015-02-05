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
