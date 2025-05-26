using HospitalManagement.Data;
using HospitalManagement.Models;
using HospitalManagement.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;

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
        public async Task<IActionResult> BookingAppoinment()
        {
            var userJson = HttpContext.Session.GetString("UserSession");

            //if (string.IsNullOrEmpty(userJson))
            //{
            //    return RedirectToAction("Login", "Auth");
            //}

             var user = JsonConvert.DeserializeObject<Account>(userJson);
            //if (user == null)
            //{
            //    return RedirectToAction("Login", "Auth");
            //}
            var doctors = await _context.Doctors
            .Include(d => d.Account)
             
            .Select(d => new
            {
                Id = d.DoctorId,
                Name = d.Account.FullName
            })
            .ToListAsync();

            // Tạo SelectList cho dropdown
            ViewBag.DoctorList = new SelectList(doctors, "Id", "Name");
            var model = new BookingApointment
            {
                Name = user.FullName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult BookingAppointment(BookingApointment model)
        {
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

            var appointment = new Appointment
            {
                PatientId = patient.PatientId,
               
               
                Status = "Pending",
                ServiceId = model.SelectedServiceId
            };

            _context.Appointments.Add(appointment);
            _context.SaveChanges();

            return RedirectToAction("BookingSuccess");
        }



    }
}
