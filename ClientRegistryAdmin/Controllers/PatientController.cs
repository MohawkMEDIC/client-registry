using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using ClientRegistryAdmin.Models;
using ClientRegistryAdmin.Util;

namespace ClientRegistryAdmin.Controllers
{
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
                else
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
    }
}
