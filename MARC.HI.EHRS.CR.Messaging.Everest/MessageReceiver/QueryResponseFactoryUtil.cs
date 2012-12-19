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
 * Date: 1-8-2012
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using MARC.Everest.Attributes;

namespace MARC.HI.EHRS.CR.Messaging.Everest.MessageReceiver
{
    /// <summary>
    /// Query response factory utility provides functions for dealing with Query Response Factories
    /// </summary>
    static class QueryResponseFactoryUtil
    {

        /// <summary>
        /// Static ctor for query response
        /// </summary>
        static QueryResponseFactoryUtil()
        {
            foreach (Type t in Array.FindAll<Type>(typeof(QueryResponseFactoryUtil).Assembly.GetTypes(), o => o.GetInterface(typeof(IQueryResponseFactory).FullName) != null))
            {
                ConstructorInfo ci = t.GetConstructor(Type.EmptyTypes);
                m_factories.Add(ci.Invoke(null) as IQueryResponseFactory);
            }
        }

        /// <summary>
        /// Factories
        /// </summary>
        private static List<IQueryResponseFactory> m_factories = new List<IQueryResponseFactory>();

        /// <summary>
        /// Get the appropriate response factory
        /// </summary>
        internal static IQueryResponseFactory GetResponseFactory(Type requestType)
        {
            // Determine the response type
            object[] resp = requestType.GetCustomAttributes(typeof(InteractionResponseAttribute), true);
            if (resp.Length == 0)
                return null;
            
            lock (m_factories)
                return m_factories.Find(o => o.CreateType.Name.Equals((resp[0] as InteractionResponseAttribute).Name));
        }
    }
}
