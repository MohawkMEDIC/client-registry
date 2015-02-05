using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ClientRegistryAdmin.Models
{
    /// <summary>
    /// Registry status model
    /// </summary>
    public class RegistryStatusModel
    {

        /// <summary>
        /// Client registry is online?
        /// </summary>
        public bool ClientRegistryOnline { get; set; }

        /// <summary>
        /// Service status
        /// </summary>
        public ClientRegistryAdminService.ServiceStatus[] ServiceStats { get; set; }

        /// <summary>
        /// Log files
        /// </summary>
        public ClientRegistryAdminService.LogInfo[] ClientRegistryLogs { get; set; }


        public ClientRegistryAdminService.OidInfo[] Oids { get; set; }
    }
}