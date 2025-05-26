using HospitalManagement.Data;
using HospitalManagement.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
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

        private List<SelectListItem> GetConsultantTypeList()
        {
            return new List<SelectListItem>
    {
        new SelectListItem { Value = "doctor", Text = "Doctor" },
        new SelectListItem { Value = "department_head", Text = "Department Head" }
    };
        }

        [HttpGet]
        public IActionResult RequestConsultant()
        {
            var userJson = HttpContext.Session.GetString("UserSession");

            if (string.IsNullOrEmpty(userJson))
            {
                return RedirectToAction("Login", "Auth");
            }

            var user = JsonConvert.DeserializeObject<Account>(userJson);
            if (user == null)
            {
                return RedirectToAction("Login", "Auth");
            }

            var model = new RequestConsultantViewModel
            {
                ConsultantTypes = GetConsultantTypeList(),
                Name = user.FullName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber
            };

            return View(model);
        }



        [HttpPost]
        public IActionResult RequestConsultant(RequestConsultantViewModel model)
        {

            model.ConsultantTypes = GetConsultantTypeList();
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            var userJson = HttpContext.Session.GetString("UserSession");
            if (string.IsNullOrEmpty(userJson))
            {
                return RedirectToAction("Login", "Auth");
            }

            var user = JsonConvert.DeserializeObject<Account>(userJson);
            if (user == null)
            {
                return RedirectToAction("Login", "Auth");
            }

            var patient = _context.Patients.FirstOrDefault(p => p.AccountId == user.AccountId);
            Console.WriteLine(patient.PatientId);
            var consultant = new Consultant
            {
                PatientId = patient.PatientId,
                Description = model.Note,
                RequestedDate = DateOnly.FromDateTime(DateTime.Now),
                Status = "Pending",
                ServiceId = model.SelectedServiceId
            };
            _context.Consultants.Add(consultant);
            _context.SaveChanges();
            int affected = _context.SaveChanges();
            Console.WriteLine($"Rows affected: {affected}");
            TempData["SuccessMessage"] = "Request sent successfully!";
            return RedirectToAction("ViewDoctors");
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
