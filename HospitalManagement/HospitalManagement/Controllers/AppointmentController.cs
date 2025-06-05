using System.Security.Claims;
using HospitalManagement.Data;
using HospitalManagement.Models;
using HospitalManagement.Repositories;
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
        public AppointmentController(HospitalManagementContext context, IAppointmentRepository appointmentRepository)
        {
            _context = context;
            _passwordHasher = new PasswordHasher<Patient>();
            _appointmentRepository = appointmentRepository;

        }
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Index(string? searchName, string? timeFilter, string? dateFilter, string? statusFilter)
        {
            var appointments = await _appointmentRepository.FilterForAdmin(searchName, timeFilter, dateFilter, statusFilter);
            if (appointments == null)
            {
                appointments = new List<Appointment>();
            }
            var slots = await _context.Slots.ToListAsync();
            ViewBag.slots = slots;
            // Lưu lại các giá trị filter hiện tại vào ViewBag
            ViewBag.SearchName = searchName;
            ViewBag.TimeFilter = timeFilter;
            ViewBag.DateFilter = dateFilter;
            ViewBag.StatusFilter = statusFilter;

            return View(appointments);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAppointment(int id)
        {
            var appointment = await _context.Appointments.FindAsync(id);
            if (appointment == null)
            {
                return NotFound();
            }
            await _appointmentRepository.DeleteAsync(appointment);
            return RedirectToAction(nameof(Index));
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
                StaffId = StaffID
            };
            _context.Appointments.Add(newAppointment);
            await _context.SaveChangesAsync();
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
                DoctorOptions = await GetDoctorListAsync(),
                SlotOptions = await GetSlotListAsync(),
                ServiceOptions = await GetServiceListAsync(),
                AppointmentDate = DateOnly.FromDateTime(DateTime.Today),
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