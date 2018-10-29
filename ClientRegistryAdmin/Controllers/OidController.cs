using ClientRegistryAdmin.ClientRegistryAdminService;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using ClientRegistryAdmin.Models;

namespace ClientRegistryAdmin.Controllers
{
#if !DEBUG
     [Authorize(Roles = "Administrators,CR Administrators")]
#endif
    public class OidController : Controller
    {
        /// <summary>
        /// Index
        /// </summary>
        public ActionResult Index()
        {
            RegistryStatusModel model = new RegistryStatusModel();

            try
            {
                // Client to the CR admin interface
                ClientRegistryAdminService.ClientRegistryAdminInterfaceClient client = new ClientRegistryAdminService.ClientRegistryAdminInterfaceClient();
                model.Oids = client.GetOids();
                model.ClientRegistryOnline = true;
            }
            catch (Exception e)
            {
                Trace.TraceError(e.ToString());
                model.ClientRegistryOnline = false;
            }

            return View(model);
        }

        //
        // GET: /Oid/
        public ActionResult View(String id)
        {
            OidInfo model = new OidInfo();

            try
            {
                // Client to the CR admin interface
                ClientRegistryAdminService.ClientRegistryAdminInterfaceClient client = new ClientRegistryAdminService.ClientRegistryAdminInterfaceClient();

                model = client.GetOids().Where(o=>o.oid == id).First();
                
            }
            catch (Exception e)
            {
                Trace.TraceError(e.ToString());
            }

            return View(model);
        }

    }
}
