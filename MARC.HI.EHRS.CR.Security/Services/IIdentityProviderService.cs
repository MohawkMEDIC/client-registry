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
using System.Security.Principal;

namespace MARC.HI.EHRS.CR.Security.Services
{
    
	/// <summary>
	/// Represents an identity provider
	/// </summary>
	public interface IIdentityProviderService
	{
        
		/// <summary>
		/// Authenticate the user
		/// </summary>
		IPrincipal Authenticate(string userName, string password);

		/// <summary>
		/// Authenticate the specified principal with the password
		/// </summary>
		/// <param name="principal">Principal.</param>
		/// <param name="password">Password.</param>
		IPrincipal Authenticate(IPrincipal principal, string password);

		/// <summary>
		/// Gets an un-authenticated identity
		/// </summary>
		IIdentity GetIdentity(string userName);

		/// <summary>
		/// Authenticate the user using a TwoFactorAuthentication secret
		/// </summary>
		IPrincipal Authenticate(string userName, string password, string tfaSecret);

		/// <summary>
		/// Change the user's password
		/// </summary>
		void ChangePassword(string userName, string newPassword, IPrincipal principal);

        /// <summary>
        /// Changes the user's password
        /// </summary>
        void ChangePassword(string userName, string password);

        /// <summary>
        /// Creates the specified user
        /// </summary>
        IIdentity CreateIdentity(string userName, string password);

        /// <summary>
        /// Create an identity with the specified data
        /// </summary>
        IIdentity CreateIdentity(Guid sid, String userName, String password);

        /// <summary>
        /// Locks the user account out
        /// </summary>
        void SetLockout(string userName, bool v);

        /// <summary>
        /// Deletes the specified identity
        /// </summary>
        /// <param name="userName"></param>
        void DeleteIdentity(string userName);
    }

    /// <summary>
    /// Represents an offline identity provider service
    /// </summary>
    public interface IOfflineIdentityProviderService : IIdentityProviderService
    {
        /// <summary>
        /// Create a local offline identity
        /// </summary>
        IIdentity CreateIdentity(Guid sid, string username, string password, IPrincipal principal);
    }
}

