using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using ClientRegistryAdmin.Models;

namespace ClientRegistryAdmin.Controllers
{
    public class LogController : Controller
    {
        //
        // GET: /Log/

        public ActionResult Details(String id)
        {
            ViewLogModel model = new ViewLogModel() { Id = id };
            try
            {
                ClientRegistryAdminService.ClientRegistryAdminInterfaceClient client = new ClientRegistryAdminService.ClientRegistryAdminInterfaceClient();
                model.Log = client.GetLog(id.Replace("-", "_"));

            }
            catch
            {
                model.Log = String.Empty;
            }
            return View(model);
        }

    }
}
