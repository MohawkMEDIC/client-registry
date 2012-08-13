using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

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
            foreach(var asm in Array.FindAll(AppDomain.CurrentDomain.GetAssemblies(), a=>Array.Exists(a.GetTypes(), t=>t.GetInterface(typeof(ITransportProtocol).FullName) != null)))
                foreach (var typ in Array.FindAll(asm.GetTypes(), t => t.GetInterface(typeof(ITransportProtocol).FullName) != null))
                {
                    ConstructorInfo ci = typ.GetConstructor(Type.EmptyTypes);
                    if (ci == null)
                        throw new InvalidOperationException(String.Format("Cannot find parameterless constructor for type '{0}'", typ.AssemblyQualifiedName));
                    ITransportProtocol tp = ci.Invoke(null) as ITransportProtocol;
                    s_prots.Add(tp.ProtocolName, typ);
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
