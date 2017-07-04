using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MARC.HI.EHRS.CR.Messaging.Authentication.OAuthIdentityConfiguration
{
    class OAuthIdentityConfiguration
    {
        /// <summary>
        /// Read/write connection string
        /// </summary>
        public String ReadWriteConnectionString { get; set; }

        /// <summary>
        /// Readonly connection string
        /// </summary>
        public String ReadonlyConnectionString { get; set; }

        /// <summary>
        /// Maximum cache size of an object
        /// </summary>
        public int MaxCacheSize { get; set; }

        /// <summary>
        /// Trace SQL enabled
        /// </summary>
        public bool TraceSql { get; set; }

        /// <summary>
        /// When true, indicates that inserts can allow keyed inserts
        /// </summary>
        public bool AutoUpdateExisting { get; set; }
    }
}
