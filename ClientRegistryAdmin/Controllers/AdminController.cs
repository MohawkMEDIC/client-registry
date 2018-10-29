using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using ClientRegistryAdmin.Models;
using System.Diagnostics;

namespace ClientRegistryAdmin.Controllers
{
#if !DEBUG
     [Authorize(Roles = "Administrators,CR Administrators")]
#endif
    public class AdminController : Controller
    {
        //
        // GET: /Admin/
        public ActionResult Index()
        {
            RegistryStatusModel model = new RegistryStatusModel();

            try
            {
                // Client to the CR admin interface
                ClientRegistryAdminService.ClientRegistryAdminInterfaceClient client = new ClientRegistryAdminService.ClientRegistryAdminInterfaceClient();

                model.ServiceStats = client.GetServices();
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
