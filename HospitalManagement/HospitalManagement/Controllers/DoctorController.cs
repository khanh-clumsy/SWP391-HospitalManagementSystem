using HospitalManagement.Data;
using Microsoft.AspNetCore.Mvc;
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
    }
}
