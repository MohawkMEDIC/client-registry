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
 * Date: 5-12-2012
 */
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using MARC.HI.EHRS.SVC.Messaging.Everest.Configuration;
using MARC.Everest.Attributes;

namespace MARC.HI.EHRS.SVC.Config.Messaging.Forms
{
    public partial class frmAddCachedInteraction : Form
    {

        /// <summary>
        /// Interaction information
        /// </summary>
        private struct InteractionInformation
        {
            /// <summary>
            /// Get or set the type of interaction
            /// </summary>
            public Type Type { get; set; }

            /// <summary>
            /// Name of the interaction
            /// </summary>
            public string Name
            {
                get
                {
                    object[] inAtt = Type.GetCustomAttributes(typeof(StructureAttribute), false);
                    return inAtt.Length > 0 ? (inAtt[0] as StructureAttribute).Name : "Unknown";
                }
            }

            /// <summary>
            /// Response interactions
            /// </summary>
            public List<Type> Responses
            {
                get
                {
                    List<Type> retVal = new List<Type>();
                    object[] intRAtt = Type.GetCustomAttributes(typeof(InteractionResponseAttribute), false);
                    foreach (object intr in intRAtt)
                        retVal.Add(Type.Assembly.GetType(String.Format("{0}.{1}", Type.Namespace, (intr as InteractionResponseAttribute).Name)));
                    return retVal;
                }
            }
            
            /// <summary>
            /// Description of the interaction
            /// </summary>
            public string Description
            {
                get
                {
                    object[] deAtt = Type.GetCustomAttributes(typeof(DescriptionAttribute), false);
                    return deAtt.Length > 0 ? (deAtt[0] as DescriptionAttribute).Description : "Unknown";
                }
            }

            /// <summary>
            /// Represent this object as a string
            /// </summary>
            public override string ToString()
            {
                return String.Format("{0} ({1})", Name, Description);
            }
        }

        /// <summary>
        /// Configuration section
        /// </summary>
        private RevisionConfiguration m_revisionConfiguration;

        /// <summary>
        /// Gets a list of selected types
        /// </summary>
        public List<Type> SelectedTypes
        {
            get;
            private set;
        }

        /// <summary>
        /// Configuration
        /// </summary>
        public RevisionConfiguration RevisionConfiguration
        {
            get
            {
                return m_revisionConfiguration;
            }
            set
            {
                m_revisionConfiguration = value;

                lstCachedItems.Items.Clear();
                foreach (var type in Array.FindAll<Type>(m_revisionConfiguration.Assembly.GetTypes(), o => o.GetCustomAttributes(typeof(InteractionAttribute), false).Length > 0))
                    lstCachedItems.Items.Add(new InteractionInformation() { Type = type });
            }
        }

        public frmAddCachedInteraction()
        {
            InitializeComponent();
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            lstCachedItems.SelectedItems.Clear();
            lstCachedItems.SelectedIndex = lstCachedItems.FindString(textBox1.Text);

        }

        private void btnOk_Click(object sender, EventArgs e)
        {

            this.SelectedTypes = new List<Type>();
            foreach (InteractionInformation ii in lstCachedItems.SelectedItems)
            {
                SelectedTypes.Add(ii.Type);
                if (chkIncludeResponse.Checked)
                    SelectedTypes.AddRange(ii.Responses);
            }
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }
    }
}
