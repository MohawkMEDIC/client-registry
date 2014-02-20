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
 * Date: 13-8-2012
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Diagnostics;

namespace MARC.HI.EHRS.CR.Messaging.HL7.TransportProtocol
{
    /// <summary>
    /// Transport utilities
    /// </summary>
    internal static class TransportUtil
    {

        /// <summary>
        /// Transport protocols
        /// </summary>
        private static Dictionary<String, Type> s_prots = new Dictionary<string, Type>();

        /// <summary>
        /// Static ctor, construct protocol types
        /// </summary>
        static TransportUtil()
        {

            // Get all assemblies which have a transport protocol
            var asm = typeof(TransportUtil).Assembly;
            try
            {
                foreach (var typ in Array.FindAll(asm.GetTypes(), t => t.GetInterface(typeof(ITransportProtocol).FullName) != null))
                {
                    ConstructorInfo ci = typ.GetConstructor(Type.EmptyTypes);
                    if (ci == null)
                        throw new InvalidOperationException(String.Format("Cannot find parameterless constructor for type '{0}'", typ.AssemblyQualifiedName));
                    ITransportProtocol tp = ci.Invoke(null) as ITransportProtocol;
                    s_prots.Add(tp.ProtocolName, typ);
                }
            }
            catch (Exception e)
            {
                Trace.TraceError(e.ToString());
            }
        }


        /// <summary>
        /// Create transport for the specified protocoltype
        /// </summary>
        internal static ITransportProtocol CreateTransport(string protocolType)
        {
            Type pType = null;
            if (!s_prots.TryGetValue(protocolType, out pType))
                throw new InvalidOperationException(String.Format("Cannot find protocol handler for '{0}'", protocolType));

            ConstructorInfo ci = pType.GetConstructor(Type.EmptyTypes);
            if (ci == null)
                throw new InvalidOperationException(String.Format("Cannot find parameterless constructor for type '{0}'", pType.AssemblyQualifiedName));
            return ci.Invoke(null) as ITransportProtocol;
            
        }

    }
}
