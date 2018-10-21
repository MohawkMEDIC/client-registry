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
 * Date: 5-12-2012
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Reflection;
using System.IO;
using MARC.HI.EHRS.SVC.Core.Configuration;
using ServiceConfigurator;
using System.Threading;
using System.Security.Principal;
using System.Diagnostics;
using MohawkCollege.Util.Console.Parameters;
using System.Text;

namespace MARC.HI.EHRS.CR.Configurator
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(String[] args)
        {
            
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            
            // Attempt to open a restricted resource (see if we're administrator)
            frmSplash splash = new frmSplash();
            splash.Show();
            try
            {

                StringBuilder argString = new StringBuilder();

#if !DEBUG
                WindowsIdentity identity = WindowsIdentity.GetCurrent();
                WindowsPrincipal principal = new WindowsPrincipal(identity);
                if (Environment.OSVersion.Platform == PlatformID.Win32NT && 
                    !principal.IsInRole(WindowsBuiltInRole.Administrator))
                {
                    string cmdLine = Environment.CommandLine.Substring(Environment.CommandLine.IndexOf(".exe") + 4);
                    cmdLine = cmdLine.Contains(' ') ? cmdLine.Substring(cmdLine.IndexOf(" ")) : null;
                    ProcessStartInfo psi = new ProcessStartInfo(Assembly.GetEntryAssembly().Location, cmdLine);
                    psi.Verb = "runas";
                    Trace.TraceInformation("Not administrator!");
                    Process proc = Process.Start(psi);
                    Application.Exit();
                    return;
                }
#endif 

                // Scan for configuration options
                ScanAndLoadPluginFiles();
                splash.Close();
#if DEBUG
                ConfigurationApplicationContext.s_configFile = Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), "ClientRegistry.exe.config.test");
#else
                ConfigurationApplicationContext.s_configFile = Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), "ClientRegistry.exe.config");
#endif

                ParameterParser<ConsoleParameters> parser = new ParameterParser<ConsoleParameters>();

                var consoleParms = parser.Parse(args);

                if (consoleParms.ListDeploy)
                {
                    StringBuilder options = new StringBuilder("Available deployment modules: \r\n");
                    foreach (var pnl in ConfigurationApplicationContext.s_configurationPanels.FindAll(o => o is IAutoDeployConfigurationPanel))
                        options.AppendFormat("{0}\r\n", pnl.Name);
                    MessageBox.Show(options.ToString());
                }
                else if (consoleParms.Deploy != null && consoleParms.Deploy.Count > 0)
                    try
                    {
                        DeployUtil.Deploy(consoleParms.Deploy, consoleParms.Options);
                        ConfigurationApplicationContext_ConfigurationApplied(null, EventArgs.Empty);
                    }
                    catch (Exception e)
                    {
                        MessageBox.Show(String.Format("Could not deploy requested component : {0}", e), "Error Deploying");
                    }
                else
                {
                    ConfigurationApplicationContext.s_configurationPanels.Sort((a, b) => a.Name.CompareTo(b.Name));
                    ConfigurationApplicationContext.ConfigurationApplied += new EventHandler(ConfigurationApplicationContext_ConfigurationApplied);


                    // Configuration File exists?
                    if (!File.Exists(ConfigurationApplicationContext.s_configFile))
                    {
                        frmStartScreen start = new frmStartScreen();
                        if (start.ShowDialog() == DialogResult.Cancel)
                            return;
                    }

                    Application.Run(new frmMain());
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
            finally
            {
                splash.Dispose();
            }
        }

        /// <summary>
        /// Configuration has been applied
        /// </summary>
        static void ConfigurationApplicationContext_ConfigurationApplied(object sender, EventArgs e)
        {
            if (ServiceTools.ServiceInstaller.ServiceIsInstalled("Client Registry") && 
                ServiceTools.ServiceInstaller.GetServiceStatus("Client Registry") == ServiceTools.ServiceState.Starting &&
                MessageBox.Show("The Client Registry service must be restarted for these changes to take affect, restart it now?", "Confirm Service Restart", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                ServiceTools.ServiceInstaller.StopService("Client Registry");
                while (ServiceTools.ServiceInstaller.GetServiceStatus("Client Registry") == ServiceTools.ServiceState.Starting)
                    Thread.Sleep(200);
                ServiceTools.ServiceInstaller.StartService("Client Registry");
                while (ServiceTools.ServiceInstaller.GetServiceStatus("Client Registry") != ServiceTools.ServiceState.Starting)
                    Thread.Sleep(200);

            }
        }

        /// <summary>
        /// Scan and load plugin files for configuration
        /// </summary>
        private static void ScanAndLoadPluginFiles()
        {
            ConfigurationApplicationContext.s_configurationPanels.Add(new ClientRegistryAboutPanel());

            // Load DB providers
            foreach (var file in Directory.GetFiles(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), "*.dll"))
            {
                try
                {
                    Application.DoEvents();
                    Assembly asm = Assembly.LoadFrom(file);
                    // Scan assembly for database configurators
                    foreach (var typ in Array.FindAll<Type>(asm.GetTypes(), o => o.GetInterface(typeof(IDatabaseConfigurator).FullName) != null))
                    {
                        ConstructorInfo ci = typ.GetConstructor(Type.EmptyTypes);
                        if (ci != null)
                            DatabaseConfiguratorRegistrar.Configurators.Add(ci.Invoke(null) as IDatabaseConfigurator);
                    }
                }
                catch (Exception e) {
                    Console.WriteLine("ERROR: {0} : {1}", file, e.ToString());
                }
            }

            // Load Panels
            foreach (var file in Directory.GetFiles(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), "*.dll"))
            {
                try
                {
                    Application.DoEvents();
                    Assembly asm = Assembly.LoadFrom(file);
                    // Scan assembly for configuration panels
                    foreach (var typ in Array.FindAll<Type>(asm.GetTypes(), o => o.GetInterface(typeof(IConfigurationPanel).FullName) != null))
                    {
                        ConstructorInfo ci = typ.GetConstructor(Type.EmptyTypes);
                        if (ci != null)
                        {
                            try
                            {
                                var config = ci.Invoke(null);
                                Console.WriteLine("Adding panel {0}...", config.ToString());
                                ConfigurationApplicationContext.s_configurationPanels.Add(config as IConfigurationPanel);
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine("ERROR: {0} : {1}", file, e.ToString());
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("ERROR: {0} : {1}", file, e.ToString());
                }
            }
        }
    }
}
