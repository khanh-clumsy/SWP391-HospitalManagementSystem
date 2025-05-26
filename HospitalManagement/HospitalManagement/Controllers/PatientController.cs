using HospitalManagement.Data;
using HospitalManagement.Models;
using HospitalManagement.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

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
        public IActionResult ViewBookingAppointment(string searchName, string timeFilter, DateTime? dateFilter, string statusFilter)
        {
            var appointments = _context.Appointments
            .Include(a => a.Patient).ThenInclude(p => p.Account)
            .Include(a => a.Slot)
            .AsQueryable();

            // Lọc theo thời gian slot

            if (!string.IsNullOrEmpty(timeFilter) && TimeOnly.TryParse(timeFilter, out var parsedTime))
            {
                appointments = appointments.Where(a => a.Slot.StartTime == parsedTime);
            }

            // Lọc theo ngày
            if (dateFilter.HasValue)
            {
                var filterDate = DateOnly.FromDateTime(dateFilter.Value);
                appointments = appointments.Where(a => a.Date == filterDate);
            }

            // Lọc theo trạng thái
            if (!string.IsNullOrEmpty(statusFilter))
            {
                appointments = appointments.Where(a => a.Status == statusFilter);
            }

            return View(appointments.ToList());
        }

    }
}

