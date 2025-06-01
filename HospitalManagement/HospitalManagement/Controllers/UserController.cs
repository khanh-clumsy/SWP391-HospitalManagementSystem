using HospitalManagement.Data;
using HospitalManagement.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HospitalManagement.ViewModels;

namespace HospitalManagement.Controllers
{
    public class UserController : Controller
    {
        private HospitalManagementContext _context;
        public UserController(HospitalManagementContext context)
        {
            _context = context;
        }
        public IActionResult ManageAccount(string type = "Patient")
        {
            var vm = new AccountListViewModel();
            vm.Patients = _context.Patients.ToList();
            vm.Doctors = _context.Doctors.ToList();
            vm.Staffs = _context.Staff.ToList();
            vm.AccountType = type;
            return View(vm);
        }

        [HttpPost]
        public IActionResult UpdatePatientStatus(List<Patient> patients)
        {
            foreach (var updated in patients)
            {
                var existing = _context.Patients.FirstOrDefault(p => p.PatientId == updated.PatientId);
                if (existing != null)
                {
                    existing.IsActive = updated.IsActive;
                }
            }

            _context.SaveChanges();
            TempData["success"] = "Status updated successfully.";
            return RedirectToAction("ManageAccount", new { type = "Patient" });
        }

        [HttpPost]
        public IActionResult UpdateDoctorStatus(List<Doctor> doctors)
        {
            foreach (var updated in doctors)
            {
                var existing = _context.Doctors.FirstOrDefault(d => d.DoctorId == updated.DoctorId);
                if (existing != null)
                {
                    existing.IsActive = updated.IsActive;
                }
            }

            _context.SaveChanges();
            TempData["success"] = "Status updated successfully.";
            return RedirectToAction("ManageAccount", new { type = "Doctor" });
        }

        [HttpPost]
        public IActionResult UpdateStaffStatus(List<Staff> staff)
        {
            foreach (var updated in staff)
            {
                var existing = _context.Staff.FirstOrDefault(s => s.StaffId == updated.StaffId);
                if (existing != null)
                {
                    existing.IsActive = updated.IsActive;
                }
            }

            _context.SaveChanges();
            TempData["success"] = "Status updated successfully.";
            return RedirectToAction("ManageAccount", new { type = "Staff" });
        }

    }
}
