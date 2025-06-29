using System.Security.Claims;
using System.Text;
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
using Microsoft.EntityFrameworkCore.ValueGeneration.Internal;
using Newtonsoft.Json;
using X.PagedList.Extensions;
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
            ViewBag.DateFilter = DateTime.Now.ToString("dd/MM/yyyy");
            var slots = await _context.Slots.ToListAsync();
            ViewBag.SlotOptions = slots;

            return View(appointments);
        }

        [Authorize(Roles = "Patient, Sales, Doctor")]
        [HttpGet]
        public async Task<IActionResult> MyAppointments(string? SearchName, string? SlotFilter, string? DateFilter, string? StatusFilter, string? Type, int? page)
        {
            int pageSize = 12;
            int pageNumber = page ?? 1;

            // Chuẩn hóa tên
            SearchName = NormalizeName(SearchName);

            // Lấy role & ID người dùng
            var (roleKey, userId) = GetUserRoleAndId(User);
            if (userId == null) return RedirectToAction("Login", "Auth");

            // Trả lại giá trị cho Views
            ViewBag.SlotOptions = await _context.Slots.ToListAsync();
            ViewBag.SearchName = SearchName;
            ViewBag.SlotFilter = SlotFilter;
            ViewBag.DateFilter = DateFilter;
            ViewBag.StatusFilter = StatusFilter;
            ViewBag.FilterType = Type ?? "Today";

            // Truy vấn lọc
            var filteredList = await _appointmentRepository.Filter(roleKey, (int)userId, SearchName, SlotFilter, DateFilter, StatusFilter);

            // Lọc thêm theo filterType 
            var today = DateOnly.FromDateTime(DateTime.Now);
            var now = TimeOnly.FromDateTime(DateTime.Now);

            if (string.IsNullOrEmpty(Type))
                Type = "Today";
            if (!string.IsNullOrEmpty(Type))
            {
                switch (Type)
                {
                    case "Today":
                        filteredList = filteredList.Where(a => a.Date == today
                        && (a.Status == "Pending" || a.Status == "Confirmed")).
                        ToList();
                        break;

                    case "Ongoing":
                        filteredList = filteredList.Where(a =>  
                          a.Date > today && (a.Status == "Pending" || a.Status == "Confirmed"))
                         .ToList();
                        break;

                    case "Completed":
                        filteredList = filteredList.Where(a =>
                            a.Status == "Completed" || a.Status == "Rejected").ToList();
                        break;
                }
            }

            // Phân trang
            var pagedAppointments = filteredList
                .OrderByDescending(a => a.AppointmentId)
                .ToPagedList(pageNumber, pageSize);

            return View(pagedAppointments);
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

            var patientId = patient.PatientId;
            Doctor? doctor = null;
            if (model.SelectedDoctorId.HasValue)
            {
                doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.DoctorId == model.SelectedDoctorId);
            }

            Slot? slot = null;
            if (model.SelectedSlotId.HasValue)
            {
                slot = await _context.Slots.FirstOrDefaultAsync(d => d.SlotId == model.SelectedSlotId);
            }

            var service = await _context.Services.FirstOrDefaultAsync(d => d.ServiceId == model.SelectedServiceId);
            if (service == null)
            {
                model.ServiceOptions = await GetServiceListAsync();

                TempData["error"] = "Invalid doctor or service selection!";
                return View(model);
            }

            bool exists = false;
            if (doctor != null && slot != null)
            {
                exists = _context.Appointments.Any(a =>
                        a.DoctorId == model.SelectedDoctorId &&
                        a.PatientId == patientId &&
                        a.Date == model.AppointmentDate &&
                        a.SlotId == model.SelectedSlotId);
            }

            if (exists)
            {
                ModelState.Clear();
                model.ServiceOptions = await GetServiceListAsync();
                TempData["error"] = $"Đã có appointment rồi!";
                return View(model);
            }

            string code;
            do
            {
                code = GenerateUniqueAppointmentCode(patientId);
            }
            while (_context.Appointments.Any(a => a.AppointmentCode == code));
            var appointment = new Appointment
            {
                PatientId = patient.PatientId,
                DoctorId = model.SelectedDoctorId ?? null,
                Note = model.Note,
                SlotId = model.SelectedSlotId ?? null,
                Date = model.AppointmentDate,
                Status = "Pending",
                Doctor = doctor,
                ServiceId = model.SelectedServiceId,
                StaffId = StaffID,
                AppointmentCode = code
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
                var emailBodyBuilder = new StringBuilder();

                emailBodyBuilder.AppendLine("<h3>✅ New Appointment Successfully Booked!</h3>");
                emailBodyBuilder.AppendLine($"<p><strong>Appointment Code:</strong> {savedAppointment.AppointmentCode}</p>");
                emailBodyBuilder.AppendLine($"<p><strong>Patient:</strong> {savedAppointment.Patient.FullName}</p>");
                emailBodyBuilder.AppendLine($"<p><strong>Date:</strong> {savedAppointment.Date:dd/MM/yyyy}</p>");

                if (savedAppointment.Doctor != null)
                {
                    emailBodyBuilder.AppendLine($"<p><strong>Doctor:</strong> {savedAppointment.Doctor.FullName}</p>");
                    emailBodyBuilder.AppendLine($"<p><strong>Department:</strong> {savedAppointment.Doctor.DepartmentName}</p>");
                }

                if (savedAppointment.Slot != null)
                {
                    emailBodyBuilder.AppendLine($"<p><strong>Time:</strong> {savedAppointment.Slot.StartTime} - {savedAppointment.Slot.EndTime}</p>");
                }

                if (!string.IsNullOrWhiteSpace(savedAppointment.Note))
                {
                    emailBodyBuilder.AppendLine($"<p><strong>Note:</strong> {savedAppointment.Note}</p>");
                }

                if (savedAppointment.Staff != null)
                {
                    emailBodyBuilder.AppendLine($"<p><strong>Sales:</strong> {savedAppointment.Staff.FullName}</p>");
                }

                var emailBody = emailBodyBuilder.ToString();


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
                AppointmentDate = DateOnly.FromDateTime(DateTime.Today.AddDays(1))
            };
            return View(model);
        }
        public static string GenerateUniqueAppointmentCode(int userId)
        {
            // Ví dụ: APPT-20250617-00123-7F3A
            var random = new Random().Next(1000, 9999);
            return $"APPT-{userId:D5}-{random}";
        }

        [Authorize(Roles = "Patient")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Booking(BookingApointmentViewModel model)
        {
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
                TempData["error"] = "Thiếu các trường dữ liệu!";
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

            Doctor? doctor = null;
            if (model.SelectedDoctorId.HasValue)
            {
                doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.DoctorId == model.SelectedDoctorId);
            }

            Slot? slot = null;
            if (model.SelectedSlotId.HasValue)
            {
                slot = await _context.Slots.FirstOrDefaultAsync(d => d.SlotId == model.SelectedSlotId);
            }

            var service = await _context.Services.FirstOrDefaultAsync(d => d.ServiceId == model.SelectedServiceId);
            if (service == null)
            {
                model.ServiceOptions = await GetServiceListAsync();

                TempData["error"] = "Invalid doctor or service selection!";
                return View(model);
            }

            bool exists = false;
            if (doctor != null && slot != null)
            {
                exists = _context.Appointments.Any(a =>
                        a.DoctorId == model.SelectedDoctorId &&
                        a.PatientId == patientId &&
                        a.Date == model.AppointmentDate &&
                        a.SlotId == model.SelectedSlotId);
            }

            if (exists)
            {
                ModelState.Clear();
                model.ServiceOptions = await GetServiceListAsync();
                TempData["error"] = $"Đã có appointment rồi!";
                return View(model);
            }

            string code;
            do
            {
                code = GenerateUniqueAppointmentCode(patientId);
            }
            while (_context.Appointments.Any(a => a.AppointmentCode == code));
            var appointment = new Appointment
            {
                PatientId = patient.PatientId,
                DoctorId = model.SelectedDoctorId ?? null,
                Note = model.Note,
                SlotId = model.SelectedSlotId ?? null,
                Date = model.AppointmentDate,
                Status = "Pending",
                Doctor = doctor,
                ServiceId = model.SelectedServiceId,
                AppointmentCode = code
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
                var emailBodyBuilder = new StringBuilder();

                emailBodyBuilder.AppendLine("<h3>✅ New Appointment Successfully Booked!</h3>");
                emailBodyBuilder.AppendLine($"<p><strong>Appointment Code:</strong> {savedAppointment.AppointmentCode}</p>");
                emailBodyBuilder.AppendLine($"<p><strong>Patient:</strong> {savedAppointment.Patient.FullName}</p>");
                emailBodyBuilder.AppendLine($"<p><strong>Date:</strong> {savedAppointment.Date:dd/MM/yyyy}</p>");

                if (savedAppointment.Doctor != null)
                {
                    emailBodyBuilder.AppendLine($"<p><strong>Doctor:</strong> {savedAppointment.Doctor.FullName}</p>");
                    emailBodyBuilder.AppendLine($"<p><strong>Department:</strong> {savedAppointment.Doctor.DepartmentName}</p>");
                }

                if (savedAppointment.Slot != null)
                {
                    emailBodyBuilder.AppendLine($"<p><strong>Time:</strong> {savedAppointment.Slot.StartTime} - {savedAppointment.Slot.EndTime}</p>");
                }

                if (!string.IsNullOrWhiteSpace(savedAppointment.Note))
                {
                    emailBodyBuilder.AppendLine($"<p><strong>Note:</strong> {savedAppointment.Note}</p>");
                }

                if (savedAppointment.Staff != null)
                {
                    emailBodyBuilder.AppendLine($"<p><strong>Sales:</strong> {savedAppointment.Staff.FullName}</p>");
                }

                var emailBody = emailBodyBuilder.ToString();


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


        [Authorize(Roles = "Patient, Sales, Admin, Doctor")]
        public IActionResult Detail(int appId)
        {
            var appointment = _context.Appointments
                                .Include(a => a.Patient)
                                .Include(a => a.Doctor)
                                .Include(a => a.Staff)
                                .Include(a => a.Slot)
                                .FirstOrDefault(a => a.AppointmentId == appId);
            if (appointment == null)
            {
                TempData["error"] = "Trang không tồn tại";
                return NotFound();
            }


            if (User.IsInRole("Admin"))
            {
                return View(appointment);
            }

            // now, roleKey only Patient/Doctor/Staff

            var (roleKey, userId) = GetUserRoleAndId(User);
            if (userId == null)
            {
                TempData["error"] = "Bạn cần đăng nhập để thực hiện thao tác này";
                return RedirectToAction("Login", "Auth");
            }

            if (roleKey == "")
            {
                TempData["error"] = "Lỗi RoleKey không xác định";
                return NotFound();
            }

            if (roleKey == "PatientID" && appointment.Patient != null && appointment.Patient.PatientId != null && appointment.Patient.PatientId == userId)
            {
                return View(appointment);
            }
            if (roleKey == "DoctorID" && appointment.Doctor != null && appointment.Doctor.DoctorId != null && appointment.Doctor.DoctorId == userId)
            {
                return View(appointment);
            }
            if (roleKey == "StaffID" && appointment.Staff != null && appointment.Staff.StaffId != null && appointment.Staff.StaffId == userId)
            {
                return View(appointment);
            }

            TempData["error"] = "Bạn không có quyền truy cập";

            return RedirectToAction("Index", "Home");
        }
        public static string NormalizeName(string? input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            input = input.Trim();
            var words = input.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            return string.Join(" ", words);
        }
    }
}
