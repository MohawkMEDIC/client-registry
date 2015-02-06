using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using ClientRegistryAdmin.Models;
using ClientRegistryAdmin.Util;
using System.Diagnostics;
using ClientRegistryAdmin.ClientRegistryAdminService;

namespace ClientRegistryAdmin.Controllers
{
#if !DEBUG
     [Authorize(Roles = "Administrators,CR Administrators")]
     [RequireHttps]
#endif
    public class PatientController : Controller
    {
        //
        // GET: /Patient/
        public ActionResult Index()
        {
            PatientSearchModel model = new PatientSearchModel();
            try
            {
                if (!ModelState.IsValid)
                    model.IsError = true;
                else
                {
                    model.Outcome = CrUtil.GetRecentActivity(new TimeSpan(1, 0, 0, 0));
                    model.IsError = model.Outcome == null;
                }
            }
            catch (Exception e)
            {
                model.IsError = true;

            }
            return View(model);
        }

        /// <summary>
        /// /Patient/Search
        /// </summary>
        public ActionResult Search(PatientSearchModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                    model.IsError = true;
                else if(model.FamilyName != null || model.GivenName != null || model.DateOfBirth != null)
                {
                    model.Outcome = CrUtil.Search(model.FamilyName, model.GivenName, model.DateOfBirth);
                    model.IsError = model.Outcome == null;
                }
            }
            catch (Exception e)
            {
                model.IsError = true;
                
            }
            return View(model);
        }

        /// <summary>
        /// View a patient
        /// </summary>
        public ActionResult View(Decimal id)
        {
            PatientMatch model = null;
            try
            {
                model = CrUtil.Get(id);
            }
            catch (Exception e)
            {
                
            }
            return View("View", model);
        }

        /// <summary>
        /// Conflict list
        /// </summary>
        public ActionResult Conflict(ConflictListModel model)
        {
            try
            {
                model.Patients = CrUtil.GetConflicts();
                model.IsError = false;
            }
            catch
            {
                model.IsError = true;
            }
            return View(model);
        }

        /// <summary>
        /// Show the resolution screen
        /// </summary>
        public ActionResult Resolve(Decimal? id)
        {
            ConflictPatientMatch model = new ConflictPatientMatch();
            try
            {
                if(id.HasValue)
                    model = CrUtil.GetConflict(id.Value);
            }
            catch
            {
            }
            return View(model);

        }

        /// <summary>
        /// Do the resolution
        /// </summary>
        [ValidateAntiForgeryToken]
        [HttpPost]
        public ActionResult Resolve()
        {

            ConflictPatientMatch model = new ConflictPatientMatch();
            try
            {

                if (Request.Form["action"] == "cancel")
                    return RedirectToAction("Conflict");

                ClientRegistryAdminInterfaceClient client = new ClientRegistryAdminInterfaceClient();
                decimal survivor = Decimal.Parse(Request.Form["id"]);
                if (Request.Form["mrg"] != null)
                {
                    decimal[] ids = Request.Form["mrg"].Split(',').Select(o=>Decimal.Parse(o)).ToArray();
                    client.Merge(ids, survivor);
                }
                client.Resolve(survivor);
                return RedirectToAction("View", new { id = survivor });
                
            }
            catch(Exception e)
            {
                Trace.TraceError(e.ToString());
                model = new ConflictPatientMatch();
            }
            return View(model);

        }

    }
}
