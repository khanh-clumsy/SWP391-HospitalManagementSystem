using System.Security.Claims;
using System.Threading.Tasks;
using HospitalManagement.Data;
using HospitalManagement.Models;
using Microsoft.AspNetCore.Authentication;
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

        //// Hiển thị trang tạo lịch hẹn, lấy các danh sách bác sĩ, slot và dịch vụ để hiển thị trong form
        //[HttpGet]
        //public async Task<IActionResult> CreateAppointment()
        //{
        //    var model = new CreateAppointmentViewModel
        //    {
        //        DoctorOptions = await GetDoctorListAsync(),
        //        SlotOptions = await GetSlotListAsync(),
        //        ServiceOptions = await GetServiceListAsync()
        //    };
        //    return View(model);
        //}
        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> CreateAppointment(CreateAppointmentViewModel model)
        //{
        //    //Nếu không hợp lệ thì trả về View với các options luôn
        //    model.DoctorOptions = await GetDoctorListAsync();
        //    model.SlotOptions = await GetSlotListAsync();
        //    model.ServiceOptions = await GetServiceListAsync();
        //    if (!ModelState.IsValid)
        //    {
        //        return View(model);
        //    }

        //    // Kiểm tra xem bệnh nhân đã tồn tại trong hệ thống chưa
        //    var patient = await _context.Patients
        //        .FirstOrDefaultAsync(p => p.Email == model.Email);

        //    // Nếu bệnh nhân chưa tồn tại, tạo mới một đối tượng Patient
        //    if (patient == null)
        //    {
        //        patient = new Patient
        //        {
        //            FullName = model.Name ?? string.Empty,
        //            Email = model.Email ?? string.Empty,
        //            PhoneNumber = model.PhoneNumber,
        //            IsActive = true,
        //        };
        //        var fixedPassword = "A12345678";
        //        patient.PasswordHash = _passwordHasher.HashPassword(patient, fixedPassword);
        //        _context.Patients.Add(patient);
        //        await _context.SaveChangesAsync();
        //        TempData["success"] = $"Tạo tài khoản bệnh nhân thành công với email là {patient.Email}.";
        //    }
        //    else
        //    {
        //        var patientEmail = await _context.Patients
        //            .Select(p => p.Email)
        //            .ToListAsync();
        //        foreach (string email in patientEmail)
        //        {
        //            if (patient.Email.Equals(email))
        //            {
        //                TempData["error"] = $"Đã có tài khoản bệnh nhân với email là {patient.Email}.";
        //            }
        //        }
        //    }

        //    var isExistedAppointment = await _context.Appointments
        //    .AnyAsync(a => a.Date == model.AppointmentDate && a.PatientId == patient.PatientId);

        //    if (isExistedAppointment)
        //    {
        //        ViewBag.ErrorMessage = "Không thể tạo cuộc hẹn mới trong cùng 1 ngày!.";
        //        return View(model);
        //    }
        //    //Sau đó mới tạo 1 bản ghi cho appointment và add vào DB
        //    var newAppointment = new Appointment
        //    {
        //        PatientId = patient.PatientId,
        //        DoctorId = model.SelectedDoctorId,
        //        SlotId = model.SelectedSlotId,
        //        ServiceId = model.SelectedServiceId,
        //        Note = model.Note,
        //        Date = model.AppointmentDate,
        //        Status = "Pending"
        //    };
        //    _context.Appointments.Add(newAppointment);
        //    await _context.SaveChangesAsync();
        //    return RedirectToAction("ViewCompletedConsultations", "Sales");
        //}

        ////Lấy service cho vào SelectListItem để hiện ra ở form
        //private async Task<List<SelectListItem>> GetServiceListAsync()
        //{
        //    return await _context.Services
        //        .Select(s => new SelectListItem
        //        {
        //            Value = s.ServiceId.ToString(),
        //            Text = $"{s.ServiceType} - {s.ServicePrice.ToString("0")}k"
        //        })
        //        .ToListAsync();
        //}


        ////Lấy slot cho vào SelectListItem để hiện ra ở form
        //private async Task<List<SelectListItem>> GetSlotListAsync()
        //{
        //    return await _context.Slots
        //        .Select(s => new SelectListItem
        //        {
        //            Value = s.SlotId.ToString(),
        //            Text = $"{s.StartTime:hh\\:mm} - {s.EndTime:hh\\:mm}"
        //        })
        //        .ToListAsync();
        //}

        ////Lấy doctor cho vào SelectListItem để hiện ra ở form
        //private async Task<List<SelectListItem>> GetDoctorListAsync()
        //{
        //    return await _context.Doctors
        //                        .Where(d => d.IsActive)
        //                        .Select(d => new SelectListItem
        //                        {
        //                            Value = d.DoctorId.ToString(),
        //                            Text = d.FullName
        //                        })
        //                        .ToListAsync();
        //}

        //[HttpGet]
        //public async Task<IActionResult> ViewCompletedConsultations()
        //{
        //    var appointments = await _context.Appointments
        //        .Include(a => a.Patient)
        //        .Include(a => a.Doctor)
        //        .Include(a => a.Slot)
        //        .Include(a => a.Service)
        //        .ToListAsync();
        //    return View(appointments);
        //}

        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            // Đăng xuất người dùng khỏi Identity (cookie authentication)
            await HttpContext.SignOutAsync();

            TempData["success"] = "Logout successful!";
            return RedirectToAction("Index", "Home");
        }
    }
}
