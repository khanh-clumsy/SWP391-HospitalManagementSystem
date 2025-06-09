using System.Security.Claims;
using HospitalManagement.Data;
using HospitalManagement.Models;
using HospitalManagement.Repositories;
using HospitalManagement.Services;
using HospitalManagement.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace HospitalManagement.Controllers
{
    [Authorize]
    public class AppointmentController : Controller
    {
        private readonly HospitalManagementContext _context;
        private readonly PasswordHasher<Patient> _passwordHasher;
        private readonly IAppointmentRepository _appointmentRepository;
        private readonly EmailService _emailService;

        public AppointmentController(HospitalManagementContext context, IAppointmentRepository appointmentRepository, EmailService emailService)
        {
            _context = context;
            _passwordHasher = new PasswordHasher<Patient>();
            _appointmentRepository = appointmentRepository;
            _emailService = emailService;
        }
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Index(string? searchName, string? timeFilter, string? dateFilter, string? statusFilter)
        {
            var appointments = await _appointmentRepository.FilterForAdmin(searchName, timeFilter, dateFilter, statusFilter);

            var slots = await _context.Slots.ToListAsync();
            ViewBag.SlotOptions = slots;

            return View(appointments);
        }

        [Authorize(Roles = "Patient, Sales, Doctor")]
        [HttpGet]
        public async Task<IActionResult> MyAppointments()
        {
            //Lấy danh sách SlotOptions để hiển thị trong ViewBag
            var SlotOptions = await _context.Slots.ToListAsync();
            ViewBag.SlotOptions = SlotOptions;

            //Lấy role của người dùng hiện tại
            string role = "";
            if (User.IsInRole("Patient")) role = "Patient";
            else if (User.IsInRole("Sales")) role = "Sales";
            else if (User.IsInRole("Doctor")) role = "Doctor";

            //Hiển thị danh sách cuộc hẹn dựa trên role
            var appointment = new List<Appointment>();
            switch (role)
            {
                case "Patient":
                    var patientIdClaim = User.FindFirst("PatientID")?.Value;
                    if (patientIdClaim == null) return RedirectToAction("Login", "Auth");
                    int PatientID = int.Parse(patientIdClaim);
                    appointment = await _appointmentRepository.GetAppointmentByPatientIDAsync(PatientID);
                    return View(appointment);
                case "Sales":
                    var staffIdClaim = User.FindFirst("StaffID")?.Value;
                    if (staffIdClaim == null) return RedirectToAction("Login", "Auth");
                    int StaffID = int.Parse(staffIdClaim);
                    appointment = await _appointmentRepository.GetAppointmentBySalesIDAsync(StaffID);
                    return View(appointment);
                case "Doctor":
                    var doctorIdClaim = User.FindFirst("DoctorID")?.Value;
                    if (doctorIdClaim == null) return RedirectToAction("Login", "Auth");
                    int DoctorID = int.Parse(doctorIdClaim);
                    appointment = await _appointmentRepository.GetAppointmentByDoctorIDAsync(DoctorID);
                    return View(appointment);
                default:
                    break;
            }
            return View();
        }

        [Authorize(Roles = "Patient, Sales, Doctor")]
        [HttpGet]
        public async Task<IActionResult> Filter(string? SearchName, string? SlotFilter, string? DateFilter, string? StatusFilter)
        {
            //Lấy role và id hiện tại của người dùng
            var (roleKey, userId) = GetUserRoleAndId(User);
            if (userId == null) return RedirectToAction("Login", "Auth");

            //Lấy danh sách SlotOptions để hiển thị trong ViewBag
            var SlotOptions = await _context.Slots.ToListAsync();
            ViewBag.SlotOptions = SlotOptions;
            ViewBag.SearchName = SearchName;
            ViewBag.SlotFilter = SlotFilter;
            ViewBag.DateFilter = DateFilter;
            ViewBag.StatusFilter = StatusFilter;

            var result = await _appointmentRepository.Filter(roleKey, (int)userId, SearchName, SlotFilter, DateFilter, StatusFilter);
            return View("MyAppointments", result);
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
        private string GenerateRandomPassword(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
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
                var fixedPassword = GenerateRandomPassword(12);
                patient.PasswordHash = _passwordHasher.HashPassword(patient, fixedPassword);
                _context.Patients.Add(patient);
                await _context.SaveChangesAsync();

                try
                {
                    var emailBody = $@"
                    <h3>🔐 Thông tin tài khoản truy cập hệ thống</h3>
                    <p>Kính gửi <strong>{patient.FullName}</strong>,</p>
                    <p>Bạn đã được tạo tài khoản thành công trên hệ thống của chúng tôi với thông tin đăng nhập như sau:</p>
                    <ul>
                        <li><strong>Email:</strong> {patient.Email}</li>
                        <li><strong>Mật khẩu:</strong> {fixedPassword}</li>
                    </ul>
                    <p>Vui lòng đăng nhập và đổi mật khẩu ngay sau lần đăng nhập đầu tiên để đảm bảo bảo mật.</p>
                    <p>Trân trọng,</p>
                    <p>Đội ngũ hỗ trợ</p>";

                    await _emailService.SendEmailAsync(
                        toEmail: patient.Email,
                        subject: "✅ Fmec System - New Account",
                        body: emailBody
                    );

                    TempData["success"] = "✅ Appointment confirmation email sent successfully.";
                }
                catch (Exception ex)
                {
                    TempData["error"] = $"❌ Failed to send appointment confirmation email: {ex.Message}";
                }
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
            var staffIdClaim = User.FindFirst("StaffID")?.Value;
            if (staffIdClaim == null) return RedirectToAction("Login", "Auth");

            int StaffID = int.Parse(staffIdClaim);

            // Lấy thông tin từ DB
            var context = new HospitalManagementContext();
            var user = context.Staff.FirstOrDefault(p => p.StaffId == StaffID);
            if (user == null) return RedirectToAction("Login", "Auth");
            var newAppointment = new Appointment
            {
                PatientId = patient.PatientId,
                DoctorId = model.SelectedDoctorId,
                SlotId = model.SelectedSlotId,
                ServiceId = model.SelectedServiceId,
                Note = model.Note,
                Date = model.AppointmentDate,
                Status = "Pending",
                StaffId = StaffID,
            };
            _context.Appointments.Add(newAppointment);
            await _context.SaveChangesAsync();

            var savedAppointment = await _context.Appointments
                .Include(a => a.Doctor)
                .Include(a => a.Service)
                .Include(a => a.Staff)
                .Include(a => a.Patient)
                .Include(a => a.Slot)
                .FirstOrDefaultAsync(a => a.AppointmentId == newAppointment.AppointmentId);

            if (savedAppointment == null)
            {
                TempData["error"] = $"Error!";
                return View(model);
            }

            try
            {
                var emailBody = $@"
                <h3>✅ New Appointment Successfully Booked!</h3>
                <p><strong>Patient:</strong> {savedAppointment.Patient.FullName}</p>
                <p><strong>Date:</strong> {savedAppointment.Date:dd/MM/yyyy}</p>
                <p><strong>Doctor:</strong> {savedAppointment.Doctor.FullName}</p>
                <p><strong>Time:</strong> {savedAppointment.Slot.StartTime} - {savedAppointment.Slot.EndTime}</p>
                <p><strong>Department:</strong> {savedAppointment.Doctor.DepartmentName}</p>
                <p><strong>Service:</strong> {savedAppointment.Service.ServiceType}</p>
                <p><strong>Note:</strong> {savedAppointment.Note}</p>
                <p><strong>Sales:</strong> {savedAppointment.Staff?.FullName}</p>
                ";

                await _emailService.SendEmailAsync(
                    toEmail: patient.Email,
                    subject: "✅ Appointment Confirmation",
                    body: emailBody
                );

                TempData["success"] = "✅ Appointment confirmation email sent successfully.";
            }
            catch (Exception ex)
            {
                TempData["error"] = $"❌ Failed to send appointment confirmation email: {ex.Message}";
            }

            return RedirectToAction("MyAppointments", "Appointment");
        }

        [Authorize(Roles = "Patient")]
        [HttpGet]
        public async Task<IActionResult> Booking(int? doctorId)
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

            if (string.IsNullOrEmpty(user.PhoneNumber))
            {
                TempData["error"] = "Vui lòng cập nhật số điện thoại trước khi đặt cuộc hẹn!";
                return RedirectToAction("UpdateProfile", "Patient");
            }
            var doctor = _context.Doctors.FirstOrDefault(d => d.DoctorId == doctorId);
            var model = new BookingApointmentViewModel
            {
                Name = user.FullName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                SelectedDoctorId = doctorId ?? 0,
                Doctors = await _context.Doctors.ToListAsync(),
                ServiceOptions = await GetServiceListAsync(),
            };
            return View(model);
        }

        [Authorize(Roles = "Patient")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Booking(BookingApointmentViewModel model)
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
                model.ServiceOptions = await GetServiceListAsync(); 
                // Nạp lại danh sách dropdown khi trả view để dropdown hiển thị đúng
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

            var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.DoctorId == model.SelectedDoctorId);
            var service = await _context.Services.FirstOrDefaultAsync(d => d.ServiceId == model.SelectedServiceId);
            

            if (doctor == null || service == null) 
            {
                TempData["error"] = "Invalid doctor or service selection!";
                return View(model);
            }

            var appointment = new Appointment
            {
                PatientId = patient.PatientId,
                DoctorId = model.SelectedDoctorId,
                Note = model.Note,
                SlotId = model.SelectedSlotId,
                Date = model.AppointmentDate,
                Status = "Pending",
                Doctor = doctor,
                ServiceId = model.SelectedServiceId
            };

            _context.Appointments.Add(appointment);
            await _context.SaveChangesAsync();

            var savedAppointment = await _context.Appointments
                .Include(a => a.Doctor)
                .Include(a => a.Service)
                .Include(a => a.Staff)
                .Include(a => a.Patient)
                .Include(a => a.Slot)
                .FirstOrDefaultAsync(a => a.AppointmentId == appointment.AppointmentId);

            if (savedAppointment == null)
            {
                TempData["error"] = $"Error!";
                return View(model);
            }

            try
            {
                var emailBody = $@"
                <h3>✅ New Appointment Successfully Booked!</h3>
                <p><strong>Patient:</strong> {savedAppointment.Patient.FullName}</p>
                <p><strong>Date:</strong> {savedAppointment.Date:dd/MM/yyyy}</p>
                <p><strong>Doctor:</strong> {savedAppointment.Doctor.FullName}</p>
                <p><strong>Time:</strong> {savedAppointment.Slot.StartTime} - {savedAppointment.Slot.EndTime}</p>
                <p><strong>Department:</strong> {savedAppointment.Doctor.DepartmentName}</p>
                <p><strong>Note:</strong> {savedAppointment.Note}</p>
                <p><strong>Sales:</strong> {savedAppointment.Staff?.FullName}</p>
                ";

                await _emailService.SendEmailAsync(
                    toEmail: patient.Email,
                    subject: "✅ Appointment Confirmation",
                    body: emailBody
                );

                TempData["success"] = "✅ Appointment confirmation email sent successfully.";
            }
            catch (Exception ex)
            {
                TempData["error"] = $"❌ Failed to send confirmation email: {ex.Message}";
            }

            return RedirectToAction("MyAppointments");
        }

        [HttpGet]
        public async Task<IActionResult> GetDoctorsByDate(DateOnly date)
        {
            var doctors = await _context.Schedules
                                        .Where(s => s.Day == date)
                                        .Include(s => s.Doctor)
                                        .Select(s => new
                                        {
                                            s.DoctorId,
                                            DoctorName = s.Doctor.FullName,
                                            ProfileImage = s.Doctor.ProfileImage,
                                            DepartmentName = s.Doctor.DepartmentName
                                        })
                                        .Distinct()
                                        .ToListAsync();

            Console.WriteLine("Doctors: " + string.Join(", ", doctors.Select(d => d.DoctorName)));
            return Json(doctors);
        }

        [HttpGet]
        public async Task<IActionResult> GetSlotsByDoctorAndDate(DateOnly date, int doctorId)
        {
            var bookedSlotIds = await _context.Appointments
                    .Where(a => a.Date == date && a.DoctorId == doctorId)
                    .Select(a => a.SlotId)
                    .ToListAsync();

            var slots = await _context.Schedules
                            .Where(s => s.Day == date && s.DoctorId == doctorId)
                            .Select(s => new
                            {
                                s.SlotId,
                                SlotTime = $"{s.Slot.StartTime} - {s.Slot.EndTime}",
                                IsBooked = bookedSlotIds.Contains(s.SlotId)
                            })
                            .Distinct()
                            .ToListAsync();
            Console.WriteLine("Slots: " + string.Join(", ", slots.Select(s => s.SlotTime)));
            return Json(slots);
        }

        [HttpGet]
        public async Task<IActionResult> GetDoctorsBySlot(DateOnly date, Slot slot)
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int appointmentId)
        {
            var appointment = _context.Appointments.FirstOrDefault(a => a.AppointmentId == appointmentId);
            if (appointment == null)
            {
                TempData["error"] = $"Can not find appointment with ID = {appointmentId}!";
                return RedirectToAction("Index", "Appointment");
            }
            _context.Appointments.Remove(appointment);
            await _context.SaveChangesAsync();
            TempData["success"] = $"Delete successfully appointment with ID = {appointmentId}!";
            return RedirectToAction("Index", "Appointment");
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

        private (string RoleKey, int? UserId) GetUserRoleAndId(ClaimsPrincipal user)
        {
            if (user.IsInRole("Patient"))
                return ("PatientID", GetUserIdFromClaim(user, "PatientID"));
            if (user.IsInRole("Sales"))
                return ("StaffID", GetUserIdFromClaim(user, "StaffID"));
            if (user.IsInRole("Doctor"))
                return ("DoctorID", GetUserIdFromClaim(user, "DoctorID"));

            return default;
        }

        private int? GetUserIdFromClaim(ClaimsPrincipal user, string claimType)
        {
            var claim = user.FindFirst(claimType);
            if (claim == null) return null;

            return int.TryParse(claim.Value, out var id) ? id : null;
        }
    }
}
