using HospitalManagement.Data;
using HospitalManagement.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace HospitalManagement.Controllers
{
    [Authorize]
    public class AppointmentController : Controller
    {
        private readonly HospitalManagementContext _context;
        private readonly PasswordHasher<Patient> _passwordHasher;

        public AppointmentController(HospitalManagementContext context)
        {
            _context = context;
            _passwordHasher = new PasswordHasher<Patient>();

        }
        [Authorize(Roles = "Admin")]
        public IActionResult Index()
        {
            return View();
        }

        [Authorize(Roles = "Sales")]
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var model = new CreateAppointmentViewModel
            {
                DoctorOptions = await GetDoctorListAsync(),
                SlotOptions = await GetSlotListAsync(),
                ServiceOptions = await GetServiceListAsync()
            };
            return View(model);
        }
        [Authorize(Roles = "Sales")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateAppointmentViewModel model)
        {
            //Nếu không hợp lệ thì trả về View với các options luôn
            model.DoctorOptions = await GetDoctorListAsync();
            model.SlotOptions = await GetSlotListAsync();
            model.ServiceOptions = await GetServiceListAsync();
            if (!ModelState.IsValid)
            {
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
                };
                var fixedPassword = "A12345678";
                patient.PasswordHash = _passwordHasher.HashPassword(patient, fixedPassword);
                _context.Patients.Add(patient);
                await _context.SaveChangesAsync();
                TempData["success"] = $"Tạo tài khoản bệnh nhân thành công với email là {patient.Email}.";
            }
            else
            {
                var patientEmail = await _context.Patients
                    .Select(p => p.Email)
                    .ToListAsync();
                foreach (string email in patientEmail)
                {
                    if (patient.Email.Equals(email))
                    {
                        TempData["error"] = $"Đã có tài khoản bệnh nhân với email là {patient.Email}.";
                    }
                }
            }

            var isExistedAppointment = await _context.Appointments
            .AnyAsync(a => a.Date == model.AppointmentDate && a.PatientId == patient.PatientId);

            if (isExistedAppointment)
            {
                ViewBag.ErrorMessage = "Không thể tạo cuộc hẹn mới trong cùng 1 ngày!.";
                return View(model);
            }
            //Sau đó mới tạo 1 bản ghi cho appointment và add vào DB
            var newAppointment = new Appointment
            {
                PatientId = patient.PatientId,
                DoctorId = model.SelectedDoctorId,
                SlotId = model.SelectedSlotId,
                ServiceId = model.SelectedServiceId,
                Note = model.Note,
                Date = model.AppointmentDate,
                Status = "Pending"
            };
            _context.Appointments.Add(newAppointment);
            await _context.SaveChangesAsync();
            return RedirectToAction("ViewCompletedConsultations", "Sales");
        }
        //Lấy service cho vào SelectListItem để hiện ra ở form
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


        //Lấy slot cho vào SelectListItem để hiện ra ở form
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

        //Lấy doctor cho vào SelectListItem để hiện ra ở form
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
