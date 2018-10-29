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
    public class LogController : Controller
    {
        //
        // GET: /Log/
        public ActionResult Index()
        {
            RegistryStatusModel model = new RegistryStatusModel();

            try
            {
                // Client to the CR admin interface
                ClientRegistryAdminService.ClientRegistryAdminInterfaceClient client = new ClientRegistryAdminService.ClientRegistryAdminInterfaceClient();
                model.ClientRegistryLogs = client.GetLogFiles();
                model.ClientRegistryOnline = true;
            }
            catch (Exception e)
            {
                Trace.TraceError(e.ToString());
                model.ClientRegistryOnline = false;
            }

            return View(model);
        }

        public ActionResult Details(String id)
        {
            ViewLogModel model = new ViewLogModel() { Id = id };
            try
            {
                ClientRegistryAdminService.ClientRegistryAdminInterfaceClient client = new ClientRegistryAdminService.ClientRegistryAdminInterfaceClient();
                model.Log = client.GetLog(id.Replace("-", "_"));
                Trace.TraceInformation("Got a log of {0} bytes", model.Log.Length);

            }
            catch
            {
                model.Log = String.Empty;
            }
            return View(model);
        }

        /// <summary>
        /// Download the log file
        /// </summary>
        public ActionResult Download(String id)
        {
            ViewLogModel model = new ViewLogModel() { Id = id };
            try
            {
                ClientRegistryAdminService.ClientRegistryAdminInterfaceClient client = new ClientRegistryAdminService.ClientRegistryAdminInterfaceClient();
                model.Log = client.GetLog(id.Replace("-", "_"));
                return new ContentResult()
                {
                    Content = model.Log,
                    ContentEncoding = System.Text.Encoding.UTF8,
                    ContentType = "text/plain"
                };
            }
            catch
            {
                model.Log = String.Empty;
            }
            return View("Details", model);
        }
    }
}
