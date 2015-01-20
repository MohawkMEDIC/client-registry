using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using ClientRegistryAdmin.Models;

namespace ClientRegistryAdmin.Controllers
{
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

                model.ServiceStats = client.GetServices();
                model.ClientRegistryLogs = client.GetLogFiles();

                model.ClientRegistryOnline = true;
            }
            catch
            {
                model.ClientRegistryOnline = false;
            }

            return View(model);
        }

    }
}
