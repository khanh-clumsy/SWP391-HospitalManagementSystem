using System.Diagnostics;
using HospitalManagement.Models;
using Microsoft.AspNetCore.Mvc;

namespace HospitalFETemplate.Controllers
{
    public class HomeController : Controller
    {
        
        public IActionResult Index()
        {
            return View();
        }
        public IActionResult About()
        {
            return View();
        }

        public IActionResult Service()
        {
            return View();
        }

        public IActionResult NotFound()
        {
            return View();
        }
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}
