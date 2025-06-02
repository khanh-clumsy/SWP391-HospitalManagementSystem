using HospitalManagement.Data;
using HospitalManagement.Models;
using HospitalManagement.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

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
        [Authorize(Roles = "Patient")]
        [HttpGet]
        public async Task<IActionResult> BookingAppointment(int? doctorId)
        {
            // Lấy PatientId từ Claims
            var patientIdClaim = User.FindFirst("PatientID")?.Value;
            if (patientIdClaim == null)
            {
                return RedirectToAction("Login", "Auth");
            }

            int patientId = int.Parse(patientIdClaim);

            // Lấy thông tin từ DB
            var context = new HospitalManagementContext();
            var user = context.Patients.FirstOrDefault(p => p.PatientId == patientId);
            if (user == null)
            {
                return RedirectToAction("Login", "Auth");
            }

            var model = new BookingApointment
            {
                Name = user.FullName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                DoctorOptions = await GetDoctorListAsync(),
                SlotOptions = await GetSlotListAsync(),
                ServiceOptions = await GetServiceListAsync(),
                AppointmentDate =DateOnly.FromDateTime(DateTime.Today),
            };
            return View(model);
        }

        [Authorize(Roles = "Patient")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BookingAppointment(BookingApointment model)
        {
            ModelState.Remove(nameof(model.DoctorOptions));
            ModelState.Remove(nameof(model.SlotOptions));
            ModelState.Remove(nameof(model.ServiceOptions));
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                foreach (var error in errors)
                {
                    // Ghi log các lỗi
                    Console.WriteLine(error);
                }
                // Nạp lại danh sách dropdown khi trả view để dropdown hiển thị đúng
                model.DoctorOptions = await GetDoctorListAsync();
                model.SlotOptions = await GetSlotListAsync();
                model.ServiceOptions = await GetServiceListAsync();
                return View(model);
            }
            // Lấy PatientId từ Claims
            var patientIdClaim = User.FindFirst("PatientID")?.Value;
            if (patientIdClaim == null)
            {
                return RedirectToAction("Login", "Auth");
            }

            int patientId = int.Parse(patientIdClaim);

            // Lấy thông tin từ DB
            var context = new HospitalManagementContext();
            var user = context.Patients.FirstOrDefault(p => p.PatientId == patientId);
            if (user == null)
            {
                return RedirectToAction("Login", "Auth");
            }

            var patient = _context.Patients.FirstOrDefault(p => p.PatientId == user.PatientId);

            if (patient == null)
            {
                return RedirectToAction("Login", "Auth");

            }
            var appointment = new Appointment
            {
                PatientId = patient.PatientId,
                DoctorId = model.SelectedDoctorId,
                Note = model.Note,
                SlotId = model.SelectedSlotId,
                ServiceId = model.SelectedServiceId,
                Date = model.AppointmentDate,
                Status = "Pending",
            };

            _context.Appointments.Add(appointment);
            _context.SaveChanges();
            return RedirectToAction("MyAppointments");
        }
    }
}
