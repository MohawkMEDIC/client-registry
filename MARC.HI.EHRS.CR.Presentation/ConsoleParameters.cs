using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MohawkCollege.Util.Console.Parameters;
using System.ComponentModel;
using System.Collections.Specialized;

namespace MARC.HI.EHRS.SVC.Presentation
{
    /// <summary>
    /// Console parameters
    /// </summary>
    public class ConsoleParameters
    {

        /// <summary>
        /// Show help
        /// </summary>
        [Parameter("?")]
        [Parameter("help")]
        public bool Help { get; set; }

        /// <summary>
        /// Console mode
        /// </summary>
        [Parameter("console")]
        [Parameter("c")]
        [Description("Run the Client Registry in interactive mode")]
        public bool Interactive { get; set; }
    }
}
