using ClientRegistryAdmin.ClientRegistryAdminService;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace ClientRegistryAdmin.Controllers
{
    public class OidController : Controller
    {
        //
        // GET: /Oid/
        public ActionResult Index(String id)
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
