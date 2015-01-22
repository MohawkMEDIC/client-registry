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
            return View(new PatientSearchModel());
        }

        /// <summary>
        /// /Patient/Search
        /// </summary>
        [HttpPost]
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
            return View("Index", model);
        }
    }
}
