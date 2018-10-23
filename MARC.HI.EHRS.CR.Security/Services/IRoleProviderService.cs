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
using System;
using System.Collections.Generic;
using System.Security.Principal;

namespace MARC.HI.EHRS.CR.Security.Services
{
	/// <summary>
	/// Represents a service which is capableof retrieving roles
	/// </summary>
	public interface IRoleProviderService
	{

		/// <summary>
		/// Find all users in a role
		/// </summary>
		string[] FindUsersInRole(string role);

		/// <summary>
		/// Get all roles
		/// </summary>
		/// <returns></returns>
		string[] GetAllRoles();

		/// <summary>
		/// Get all rolesfor user
		/// </summary>
		/// <returns></returns>
		string[] GetAllRoles(String userName);

		/// <summary>
		/// Determine if the user is in the specified role
		/// </summary>
		bool IsUserInRole(IPrincipal principal, string roleName);

		/// <summary>
		/// Determine if user is in role
		/// </summary>
		bool IsUserInRole(string userName, string roleName);

        /// <summary>
        /// Adds the specified users to the specified roles
        /// </summary>
        void AddUsersToRoles(string[] userNames, string[] roleNames, IPrincipal principal = null);

    }
    
}

