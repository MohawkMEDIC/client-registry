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
using System.Security.Claims;
using System.Text;
using MARC.HI.EHRS.SVC.Core;
using MARC.HI.EHRS.SVC.Core.Services;

namespace MARC.HI.EHRS.CR.Security
{

    /// <summary>
    /// Shim for service provider extension
    /// </summary>
    public static class ServiceProviderExtensions
    {
        /// <summary>
        /// Get the strongly type service
        /// </summary>
        public static T GetService<T>(this IServiceProvider me) 
        {
            return (T)ApplicationContext.Current.GetService(typeof(T));
        }

        /// <summary>
        /// Claim value as date time
        /// </summary>
        public static DateTime AsDateTime(this Claim me)
        {
            DateTime value = DateTime.MinValue;
            if (!DateTime.TryParse(me.Value, out value))
            {
                int offset = 0;
                if (Int32.TryParse(me.Value, out offset))
                    value = new DateTime(1970, 1, 1).AddSeconds(offset).ToLocalTime();
                else
                    throw new ArgumentOutOfRangeException(nameof(Claim.Value));
            }
            return value;
        }
    }

    /// <summary>
    /// Handlers for application context
    /// </summary>
    public static class ApplicationContext
    {

        /// <summary>
        /// Host context backing field for singleton
        /// </summary>
        private static IServiceProvider s_hostContext;
        /// <summary>
        /// Localization service
        /// </summary>
        private static ILocalizationService s_localeService;
        /// <summary>
        /// Sync-lock
        /// </summary>
        private static Object s_syncLock = new object();
        /// <summary>
        /// System configuration service
        /// </summary>
        private static ISystemConfigurationService s_sysConfigService = null;

        /// <summary>
        /// Gets the configuration service
        /// </summary>
        public static ISystemConfigurationService ConfigurationService
        {
            get { return s_sysConfigService; }
        }

        /// <summary>
        /// Gets or sets the current host context
        /// </summary>
        public static IServiceProvider Current
        {
            get
            {
                return s_hostContext;
            }
            set
            {
                if (s_hostContext == null)
                    lock (s_syncLock)
                    {
                        s_hostContext = value;
                        s_sysConfigService = s_hostContext.GetService(typeof(ISystemConfigurationService)) as ISystemConfigurationService;
                    }
            }
        }

        /// <summary>
        /// Localization service
        /// </summary>
        public static ILocalizationService LocaleService
        {
            get
            {
                if (s_localeService == null)
                    s_localeService = Current.GetService(typeof(ILocalizationService)) as ILocalizationService;
                return s_localeService;
            }
        }
    }
}
