using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using MARC.HI.EHRS.CR.Presentation.Console;
using MARC.HI.EHRS.SVC.Core.Services;
using MARC.HI.EHRS.SVC.Core;

namespace MARC.HI.EHRS.SVC.Presentation.Console
{
    partial class SharedHealthRecord : ServiceBase
    {

        // Start the message handler service
        IMessageHandlerService m_messageHandlerService = null;

        public SharedHealthRecord()
        {
            // Service Name
            this.ServiceName = "Client Registry";
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            Trace.CorrelationManager.ActivityId = typeof(Program).GUID;
            Trace.TraceInformation("Starting host context on Console Presentation System at {0}", DateTime.Now);

            // Detect platform
            if (System.Environment.OSVersion.Platform != PlatformID.Win32NT)
                Trace.TraceWarning("Not running on WindowsNT, some features may not function correctly");

            // Do this because loading stuff is tricky ;)
            AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(Program.CurrentDomain_AssemblyResolve);

            try
            {
                // Initialize 
                HostContext context = new HostContext();

                Trace.TraceInformation("Getting default message handler service.");
                m_messageHandlerService = context.GetService(typeof(IMessageHandlerService)) as IMessageHandlerService;

                if (m_messageHandlerService == null)
                    Trace.TraceError("PANIC! Can't find a default message handler service: {0}", "No IMessageHandlerService classes are registered with this host context");
                else
                {
                    Trace.TraceInformation("Starting message handler service {0}", m_messageHandlerService);
                    if (m_messageHandlerService.Start())
                    {
                        Trace.TraceInformation("Service Started Successfully");
                        ExitCode = 0;
                    }
                    else
                    {
                        Trace.TraceError("No message handler service started. Terminating program");
                        ExitCode = 1911;
                        Stop();
                    }
                }
            }
            catch (Exception e)
            {
                Trace.TraceError("Fatal exception occurred: {0}", e.ToString());
                ExitCode = 1064;
                Stop();
            }
            finally
            {
            }
            

        }

        protected override void OnStop()
        {
            if (m_messageHandlerService != null)
            {
                Trace.TraceInformation("Stopping message handler service {0}", m_messageHandlerService);
                m_messageHandlerService.Stop();
            }

        }
    }
}
