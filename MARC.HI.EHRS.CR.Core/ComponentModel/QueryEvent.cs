/**
 * Copyright 2012-2015 Mohawk College of Applied Arts and Technology
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
 * User: Justin
 * Date: 30-7-2015
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace MARC.HI.EHRS.CR.Core.ComponentModel
{
    /// <summary>
    /// Query event
    /// </summary>
    [Serializable]
    [XmlType("QueryEvent", Namespace = "urn:marc-hi:svc:componentModel")]
    [XmlRoot("QueryEvent", Namespace = "urn:marc-hi:svc:componentModel")]
    public class QueryEvent : CrHealthServiceRecordContainer
    {
    }
}
