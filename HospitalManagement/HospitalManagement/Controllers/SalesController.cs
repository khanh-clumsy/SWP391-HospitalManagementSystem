using System.Threading.Tasks;
using HospitalManagement.Data;
using HospitalManagement.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace HospitalManagement.Controllers
{
    public class SalesController : Controller
    {
        private readonly HospitalManagementContext _context;
        public SalesController(HospitalManagementContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> CreateAppointment()
        {
            var model = new CreateAppointmentViewModel
            {
                DoctorOptions = await GetDoctorListAsync(),
                SlotOptions = await GetSlotListAsync(),
                ServiceOptions = await GetServiceListAsync()
            };
            return View(model);
        }
        [HttpPost]
        public async Task<IActionResult> CreateAppointment(CreateAppointmentViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.DoctorOptions = await GetDoctorListAsync();
                model.SlotOptions = await GetSlotListAsync();
                model.ServiceOptions = await GetServiceListAsync();
                return View(model);
            }

            var patient = await _context.Patients
                .FirstOrDefaultAsync(p => p.Email == model.Email);

            if (patient == null)
            {
                patient = new Patient
                {
                    FullName = model.Name ?? string.Empty, // Fix for CS8601: Ensure non-null assignment
                    Email = model.Email ?? string.Empty,  // Fix for CS8601: Ensure non-null assignment
                    Gender = "M"                          // Default value for Gender
                };
            }

            var appointment = new Appointment
            {
                // Additional code for appointment creation goes here
            };

            return View();
        }

        private async Task<List<SelectListItem>> GetServiceListAsync()
        {
            return await _context.Services
                .Select(s => new SelectListItem
                {
                    Value = s.ServiceId.ToString(),
                    Text = $"{s.ServiceType} - {s.ServicePrice.ToString("0")}k" 
                })
                .ToListAsync();
        }



        private async Task<List<SelectListItem>> GetSlotListAsync()
        {
            return await _context.Slots
                .Select(s => new SelectListItem
                {
                    Value = s.SlotId.ToString(),
                    Text = $"{s.StartTime:hh\\:mm} - {s.EndTime:hh\\:mm}"
                })
                .ToListAsync();
        }


        private async Task<List<SelectListItem>> GetDoctorListAsync()
        {
            return await _context.Doctors
                                .Where(d => d.IsActive)
                                .Select(d => new SelectListItem
                                {
                                    Value = d.DoctorId.ToString(),
                                    Text = d.FullName
                                })
                                .ToListAsync();
        }
    }
}
