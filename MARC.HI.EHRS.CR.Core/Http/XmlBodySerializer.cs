/*
 * Copyright 2015-2018 Mohawk College of Applied Arts and Technology
 *
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
 * Date: 2017-9-1
 */
using SanteDB.Core.Http;
using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace MARC.HI.EHRS.CR.Core.Http
{
	/// <summary>
	/// Represents a body serializer that uses XmlSerializer
	/// </summary>
	internal class XmlBodySerializer : IBodySerializer
	{

        // Serializers
        private static Dictionary<Type, XmlSerializer> m_serializers = new Dictionary<Type, XmlSerializer>();

		// Serializer
		private XmlSerializer m_serializer;

		/// <summary>
		/// Creates a new body serializer
		/// </summary>
		public XmlBodySerializer(Type type)
		{
            if (!m_serializers.TryGetValue(type, out this.m_serializer))
            {
                this.m_serializer = new XmlSerializer(type);
                lock (m_serializers)
                    if (!m_serializers.ContainsKey(type))
                        m_serializers.Add(type, this.m_serializer);
            }
		}

		#region IBodySerializer implementation

		/// <summary>
		/// Serialize the object
		/// </summary>
		public void Serialize(System.IO.Stream s, object o)
		{
			this.m_serializer.Serialize(s, o);
		}

		/// <summary>
		/// Serialize the reply stream
		/// </summary>
		public object DeSerialize(System.IO.Stream s)
		{
			return this.m_serializer.Deserialize(s);
		}

		#endregion IBodySerializer implementation
	}
}