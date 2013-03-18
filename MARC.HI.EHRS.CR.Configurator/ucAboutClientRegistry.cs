using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Reflection;

namespace MARC.HI.EHRS.CR.Configurator
{
    public partial class ucAboutClientRegistry : UserControl
    {
        public ucAboutClientRegistry()
        {
            InitializeComponent();

            // Populate Form
            PopulateForm();

        }

        /// <summary>
        /// Populate the form
        /// </summary>
        private void PopulateForm()
        {
            string exeFile = Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), "ClientRegistry.exe");
            if (!File.Exists(exeFile))
                lblVersion.Text = rtfLicense.Text = lblCopyright.Text = "Missing ClientRegistry.exe";
            var asm = Assembly.LoadFile(exeFile);

            // Get asm attributes
            lblVersion.Text = asm.GetName().Version.ToString();
            lblCopyright.Text = (asm.GetCustomAttributes(typeof(AssemblyCopyrightAttribute), false)[0] as AssemblyCopyrightAttribute).Copyright;

            // Get the manifest license
            var licenseName = Array.Find(asm.GetManifestResourceNames(), o=>o.Contains("License.rtf"));
            rtfLicense.LoadFile(asm.GetManifestResourceStream(licenseName), RichTextBoxStreamType.RichText);

            
        }

        private void tmrService_Tick(object sender, EventArgs e)
        {
            try
            {
                svcController.Refresh();
                lblStatus.Text = svcController.Status.ToString();
                if (svcController.Status == System.ServiceProcess.ServiceControllerStatus.Running)
                {
                    btnStartStop.Text = "(Stop)";
                    btnStartStop.Enabled = true;
                }
                else if (svcController.Status == System.ServiceProcess.ServiceControllerStatus.Stopped)
                {
                    btnStartStop.Text = "(Start)";
                    btnStartStop.Enabled = true;
                }
                else
                    btnStartStop.Enabled = false;
            }
            catch
            {
                lblStatus.Text = "Not Installed";
            }
        }

        private void btnStartStop_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            try
            {
                if (svcController.Status == System.ServiceProcess.ServiceControllerStatus.Stopped)
                    svcController.Start();
                else if (svcController.Status == System.ServiceProcess.ServiceControllerStatus.Running)
                    svcController.Stop();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
    }
}
