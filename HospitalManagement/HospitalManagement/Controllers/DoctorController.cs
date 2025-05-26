using HospitalManagement.Data;
using HospitalManagement.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace HospitalManagement.Controllers
{
    public class DoctorController : Controller
    {
        private readonly HospitalManagementContext _context;

        public DoctorController(HospitalManagementContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            return View();
        }

        public async Task<IActionResult> AppointmentList(string searchName, string statusFilter, string timeFilter, DateTime? dateFilter)
        {
            var query = _context.Appointments
                .Include(a => a.Patient)
                    .ThenInclude(p => p.Account)
               .Include(a => a.Slot)
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchName))
            {
                query = query.Where(a => EF.Functions.Like(a.Patient.Account.FullName, $"{searchName}%"));

            }

            if (!string.IsNullOrEmpty(statusFilter))
            {
                query = query.Where(a => a.Status == statusFilter);
            }

            if (!string.IsNullOrEmpty(timeFilter) && TimeOnly.TryParse(timeFilter, out var parsedTime))
            {
                query = query.Where(a => a.Slot.StartTime == parsedTime);
            }
            var appointments = await query.ToListAsync();
            return View(appointments);
        }
        public async Task<IActionResult> UpdateStatus(int appointmentId, string newStatus)
        {
            var appointment = await _context.Appointments.FindAsync(appointmentId);
            if (appointment == null)
            {
                return NotFound();
            }

            appointment.Status = newStatus;
            _context.Appointments.Update(appointment);
            await _context.SaveChangesAsync();

            return RedirectToAction("AppointmentList");
        }

        [HttpGet]
        public IActionResult ConsultationList(string searchName, string statusFilter, string timeFilter, DateTime? dateFilter)
        {
            var statusOptions = new List<SelectListItem>
            {
                new SelectListItem { Value = "", Text = "All Status" },
                new SelectListItem { Value = "Confirmed", Text = "Confirmed" },
                new SelectListItem { Value = "Pending", Text = "Pending" },
                new SelectListItem { Value = "Cancelled", Text = "Cancelled" }
            };

            ViewBag.StatusOptions = new SelectList(statusOptions, "Value", "Text", statusFilter);
            var query = _context.Consultants
            .Include(c => c.Patient).ThenInclude(p => p.Account)
            .Include(c => c.Doctor).ThenInclude(d => d.Account)
            .Include(c => c.Service)
            .AsQueryable();

            if (dateFilter.HasValue)
            {
                var dateOnlyFilter = DateOnly.FromDateTime(dateFilter.Value);

                query = query.Where(c => c.RequestedDate.HasValue && c.RequestedDate.Value == dateOnlyFilter);
            }

            if (!string.IsNullOrEmpty(statusFilter))
            {
                query = query.Where(c => c.Status == statusFilter);
            }

            var model = new ViewConsultationsViewModel
            {
                DateFilter = dateFilter,
                StatusFilter = statusFilter,
                Consultants = query.ToList()
            };
            return View(model);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatusConsultation(int consultantId, string newStatus)
        {
            var consultant = await _context.Consultants.FindAsync(consultantId);
            if (consultant == null)
            {
                TempData["ErrorMessage"] = "Consultation not found.";
                return RedirectToAction("ConsultationList");
            }

            consultant.Status = newStatus;
            _context.Consultants.Update(consultant);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Update successfully!";
            return RedirectToAction("ConsultationList");
        }

    }
}