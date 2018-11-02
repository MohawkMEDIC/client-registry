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
                    int page = 1;
                    if (Request.QueryString["page"] != null)
                        page = Int32.Parse(Request.QueryString["page"]);
                    var recent = CrUtil.GetRecentActivity(new TimeSpan(0, 1, 0, 0), (page - 1) * 10, 10);
                    model.Outcome = recent;
                    //model.Outcome = new List<PatientMatch>(tResults);
                    model.IsError = false;
                }
            }
            catch (Exception e)
            {
                model.IsError = true;
                Trace.TraceError(e.ToString());
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
                else if (model.FamilyName != null || model.GivenName != null || model.DateOfBirth != null || model.Identifier != null)
                {
                    int page = 1;
                    if (Request.QueryString["page"] != null)
                        page = Int32.Parse(Request.QueryString["page"]);

                    model.Outcome = CrUtil.Search(model.FamilyName, model.GivenName, model.DateOfBirth, model.Identifier, (page - 1) * 10, 10);
                    model.IsError = false;
                }
                else if(model.WasSubmitted)
                    ModelState.AddModelError(String.Empty, "Must provide at least one search parameter");
                model.WasSubmitted = true;
            }
            catch (Exception e)
            {
                model.IsError = true;
                Trace.TraceError(e.ToString());
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
                throw;
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
                int page = 1;
                if (Request.QueryString["page"] != null)
                    page = Int32.Parse(Request.QueryString["page"]);
                model.Patients = CrUtil.GetConflicts((page - 1) * 10, 10);
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

                if (Request.Form["action"] == "merge" && Request.Form["mrg"] != null)
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
