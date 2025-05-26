using HospitalManagement.Data;
using HospitalManagement.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace HospitalManagement.Controllers
{
    public class PatientController : Controller
    {
        private readonly HospitalManagementContext _context;

        public PatientController(HospitalManagementContext context)
        {
            _context = context;
        }

        public IActionResult ViewDoctors()
        {
            return View();
        }

        public IActionResult DoctorDetail()
        {
            return View();
        }

        [HttpGet]
        public IActionResult RequestConsultant()
        {
            var userJson = HttpContext.Session.GetString("UserSession");

            if (!string.IsNullOrEmpty(userJson))
            {
                var user = JsonConvert.DeserializeObject<Account>(userJson);
                if (user == null)
                {
                    return RedirectToAction("Login", "Auth");
                }
                var model = new RequestConsultantViewModel
                {
                    Name = user.FullName,
                    Email = user.Email,
                    PhoneNumber = user.PhoneNumber
                };
                return View(model);
            }
            return View(new RequestConsultantViewModel());
        }

        [HttpPost]
        public IActionResult RequestConsultant(RequestConsultantViewModel model)
        {
            return View();
        }

        public IActionResult Appointment()

        {
            return View();
        }
        public IActionResult AppointmentList()
        {
            return View();
        }
    }
    
}
