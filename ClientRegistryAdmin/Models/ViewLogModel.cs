using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ClientRegistryAdmin.Models
{
    public class ViewLogModel
    {

        /// <summary>
        /// Id of the model
        /// </summary>
        public String Id { get; set; }
        /// <summary>
        /// Log contents
        /// </summary>
        public String Log { get; set; }
    }
}