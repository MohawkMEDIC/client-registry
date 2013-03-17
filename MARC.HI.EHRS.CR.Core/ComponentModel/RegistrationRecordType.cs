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
 * Date: 16-7-2012
 */

using System;

namespace MARC.HI.EHRS.CR.Core.ComponentModel
{
    /// <summary>
    /// Identifies a type of record
    /// </summary>
    [Flags]
    public enum RegistrationEventType
    {
        /// <summary>
        /// Any of the items
        /// </summary>
        Any = Register | Revise | Nullify,
        /// <summary>
        /// No service record
        /// </summary>
        None = 0,
        /// <summary>
        /// Marks an event as "Just a component" this prevents it from appearing in
        /// summary queries
        /// </summary>
        ComponentEvent = 0x01,
        /// <summary>
        /// Marks the event as a notification
        /// </summary>
        Notification = 0x02,
        /// <summary>
        /// Registration of a patient
        /// </summary>
        Register = 0x04,
        /// <summary>
        /// Revise of a patient
        /// </summary>
        Revise = 0x08,
        /// <summary>
        /// Nullify of a person
        /// </summary>
        Nullify = 0x10,
        /// <summary>
        /// Replacement 
        /// </summary>
        Replace = 0x20

    }
}
