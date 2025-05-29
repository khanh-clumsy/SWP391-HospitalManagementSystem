using System.Security.Claims;
using System.Threading.Tasks;
using HospitalManagement.Data;
using HospitalManagement.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace HospitalManagement.Controllers
{
    public class SalesController : Controller
    {
        private readonly HospitalManagementContext _context;
        private readonly IPasswordHasher<Patient> _passwordHasher;

        public SalesController(HospitalManagementContext context, IPasswordHasher<Patient> passwordHasher)
        {
            _context = context;
            _passwordHasher = passwordHasher;
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
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateAppointment(CreateAppointmentViewModel model)
        {
            //Nếu không hợp lệ thì trả về View với các options luôn
            if (!ModelState.IsValid)
            {
                model.DoctorOptions = await GetDoctorListAsync();
                model.SlotOptions = await GetSlotListAsync();
                model.ServiceOptions = await GetServiceListAsync();
                return View(model);
            }

            // Kiểm tra xem bệnh nhân đã tồn tại trong hệ thống chưa
            var patient = await _context.Patients
                .FirstOrDefaultAsync(p => p.Email == model.Email);

            // Nếu bệnh nhân chưa tồn tại, tạo mới một đối tượng Patient
            if (patient == null)
            {
                patient = new Patient
                {
                    FullName = model.Name ?? string.Empty,
                    Email = model.Email ?? string.Empty,
                    PhoneNumber = model.PhoneNumber,
                    IsActive = true,
                    PasswordHash = ""
                };
                _context.Patients.Add(patient);
                await _context.SaveChangesAsync(); 
            }

            //Sau đó mới tạo 1 bản ghi cho appointment và add vào DB
            var appointment = new Appointment
            {
                PatientId = patient.PatientId,
                DoctorId =  model.SelectedDoctorId,
                SlotId = model.SelectedSlotId,
                ServiceId = model.SelectedServiceId,
                Note = model.Note,
                Date = DateOnly.FromDateTime(model.AppointmentDate ?? DateTime.Now),
                Status = "Pending"
            };
            _context.Appointments.Add(appointment);
            await _context.SaveChangesAsync(); 
            return RedirectToAction("Index", "Home");
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
