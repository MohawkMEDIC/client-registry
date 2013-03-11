/**
 * Copyright 2012-2012 Mohawk College of Applied Arts and Technology
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
 * Date: 16-7-2012
 */
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
            ExitCode = ServiceUtil.Start(typeof(Program).GUID);
        }

        protected override void OnStop()
        {
            ServiceUtil.Stop();
        }
    }
}
