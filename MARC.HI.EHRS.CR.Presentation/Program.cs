/**
 * Copyright 2013-2013 Mohawk College of Applied Arts and Technology
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
 * Date: 12-3-2013
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using console = System.Console;
using System.Reflection;
using System.Diagnostics;
using MARC.HI.EHRS.SVC.Core;
using MARC.HI.EHRS.SVC.Core.Services;
using System.Runtime.InteropServices;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Xml.Serialization;
using MARC.HI.EHRS.SVC.Presentation.Console;
using MARC.HI.EHRS.SVC.Presentation;
using System.Collections.Specialized;
using System.ComponentModel;

namespace MARC.HI.EHRS.CR.Presentation.Console
{
    [GuidAttribute("1D5016E2-F3E7-4a33-B263-B9CDBAC20F9C")]
    class Program
    {
        /// <summary>
        /// Entry point for the console presentation layer
        /// </summary>
        static void Main(string[] args)
        {


            // Keep track of console access so we don't throw any wonky service exceptions
            bool hasConsole = true;

            // Parser for the parameters
            MohawkCollege.Util.Console.Parameters.ParameterParser<ConsoleParameters> parser = new MohawkCollege.Util.Console.Parameters.ParameterParser<ConsoleParameters>();
            try
            {
                var parameters = parser.Parse(args);

                // Help?
                if (parameters.Help)
                    parser.WriteHelp(System.Console.Out);
                else if (parameters.Interactive)
                {
                    ShowCopyright();
                    ServiceUtil.Start(typeof(Program).GUID);
                    console.WriteLine("Press any key to stop...");

                    console.ReadKey();
                    ServiceUtil.Stop();
                }
                else
                {
                    hasConsole = false;
                    System.ServiceProcess.ServiceBase[] ServicesToRun;
                    ServicesToRun = new System.ServiceProcess.ServiceBase[] { new SharedHealthRecord() };
                    System.ServiceProcess.ServiceBase.Run(ServicesToRun);
                }
            }
            catch (Exception e)
            {
                if (hasConsole)
                    console.Write(e.ToString());
                else
                    Trace.TraceError(e.ToString());
            }

        }


        /// <summary>
        /// Show copyright information on screen
        /// </summary>
        private static void ShowCopyright()
        {
            console.WriteLine("MARC-HI Service Host Console v{0}", Assembly.GetEntryAssembly().GetName().Version);
            console.WriteLine("Copyright (C) 2010, Mohawk College of Applied Arts and Technology");
        }
    }
}
