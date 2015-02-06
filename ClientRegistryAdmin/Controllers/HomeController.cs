using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using ClientRegistryAdmin.Models;
using System.Diagnostics;

namespace ClientRegistryAdmin.Controllers
{
    [AllowAnonymous]
    public class HomeController : Controller
    {
        //
        // GET: /Home/

        public ActionResult Index()
        {

            RegistryStatusModel model = new RegistryStatusModel();

            try
            {
                // Client to the CR admin interface
                ClientRegistryAdminService.ClientRegistryAdminInterfaceClient client = new ClientRegistryAdminService.ClientRegistryAdminInterfaceClient();

                model.ClientRegistryOnline = true;
            }
            catch(Exception e)
            {
                Trace.TraceError(e.ToString());
                model.ClientRegistryOnline = false;
            }

            return View(model);
        }

    }
}
