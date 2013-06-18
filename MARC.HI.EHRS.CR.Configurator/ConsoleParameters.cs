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
 * Date: 24-5-2013
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.Specialized;
using MohawkCollege.Util.Console.Parameters;

namespace MARC.HI.EHRS.CR.Configurator
{
    /// <summary>
    /// Console parameters
    /// </summary>
    public class ConsoleParameters
    {

        /// <summary>
        /// List deployment options
        /// </summary>
        [Parameter("l")]
        [Parameter("list")]
        public bool ListDeploy { get; set; }

        /// <summary>
        /// Deployment scripts to run
        /// </summary>
        [Parameter("d")]
        [Parameter("deploy")]
        public StringCollection Deploy { get; set; }


        /// <summary>
        /// Deployment options
        /// </summary>
        [ParameterExtension()]
        public Dictionary<String, StringCollection> Options { get; set; }
    }
}
