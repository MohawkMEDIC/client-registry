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
using SanteDB.Core.Diagnostics;
using SanteDB.Core.Http;
using SanteDB.Core.Model.Query;
using System;
using System.IO;
using System.Reflection;

namespace MARC.HI.EHRS.CR.Core.Http
{
	/// <summary>
	/// Form element attribute.
	/// </summary>
	[AttributeUsage(AttributeTargets.Property)]
	public class FormElementAttribute : Attribute
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="SanteDB.Core.Http.FormElementAttribute"/> class.
		/// </summary>
		/// <param name="name">Name.</param>
		public FormElementAttribute(String name)
		{
			this.Name = name;
		}

		/// <summary>
		/// Gets or sets the name.
		/// </summary>
		/// <value>The name.</value>
		public String Name
		{
			get;
			set;
		}
	}

	/// <summary>
	/// Form body serializer.
	/// </summary>
	public class FormBodySerializer : IBodySerializer
	{
		private Tracer m_tracer = Tracer.GetTracer(typeof(FormBodySerializer));

		#region IBodySerializer implementation

		/// <summary>
		/// Serialize the specified object
		/// </summary>
		public void Serialize(System.IO.Stream s, object o)
		{
			// Get runtime properties
			bool first = true;
			using (StreamWriter sw = new StreamWriter(s))
			{
				foreach (var pi in o.GetType().GetRuntimeProperties())
				{
					// Use XML Attribute
					FormElementAttribute fatt = pi.GetCustomAttribute<FormElementAttribute>();
					if (fatt == null)
						continue;

					// Write
					String value = pi.GetValue(o)?.ToString();
					if (String.IsNullOrEmpty(value))
						continue;

					if (!first)
						sw.Write("&");
					sw.Write("{0}={1}", fatt.Name, value);
					first = false;
				}
			}
		}

		/// <summary>
		/// De-serialize
		/// </summary>
		/// <returns>The serialize.</returns>
		/// <param name="s">S.</param>
		public object DeSerialize(System.IO.Stream s)
		{
            using (StreamReader sr = new StreamReader(s))
            {
                return NameValueCollection.ParseQueryString(sr.ReadToEnd());
            }
 		}

		#endregion IBodySerializer implementation
	}
}