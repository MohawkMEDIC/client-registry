using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Reflection;
using System.IO;
using MARC.HI.EHRS.SVC.Core.Configuration;
using ServiceConfigurator;

namespace MARC.HI.EHRS.CR.Configurator
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            
            frmSplash splash = new frmSplash();
            splash.Show();
            try
            {
                // Scan for configuration options
                ScanAndLoadPluginFiles();
                splash.Close();
                ConfigurationApplicationContext.s_configFile = Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), "ClientRegistry.exe.config.test");
                // Configuration File exists?
                if (!File.Exists(ConfigurationApplicationContext.s_configFile))
                {
                    frmStartScreen start = new frmStartScreen();
                    if (start.ShowDialog() == DialogResult.Cancel)
                        return;
                }
                ConfigurationApplicationContext.s_configurationPanels.Sort((a, b) => a.Name.CompareTo(b.Name));
                Application.Run(new frmMain());
            }
            finally
            {
                splash.Dispose();
            }
        }

        /// <summary>
        /// Scan and load plugin files for configuration
        /// </summary>
        private static void ScanAndLoadPluginFiles()
        {
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
                            ConfigurationApplicationContext.s_configurationPanels.Add(ci.Invoke(null) as IConfigurationPanel);
                    }
                    // Scan assembly for database configurators
                    foreach (var typ in Array.FindAll<Type>(asm.GetTypes(), o => o.GetInterface(typeof(IDatabaseConfigurator).FullName) != null))
                    {
                        ConstructorInfo ci = typ.GetConstructor(Type.EmptyTypes);
                        if (ci != null)
                            DatabaseConfiguratorRegistrar.Configurators.Add(ci.Invoke(null) as IDatabaseConfigurator);
                    }
                }
                catch { }
            }
        }
    }
}
