using System.Security.Claims;
using System.Text;
using HospitalManagement.Data;
using HospitalManagement.Models;
using HospitalManagement.Repositories;
using HospitalManagement.Services;
using HospitalManagement.ViewModels;
using HospitalManagement.ViewModels.Booking;
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
        private readonly BookingQueueService _bookingQueue;


        public AppointmentController(HospitalManagementContext context, IAppointmentRepository appointmentRepository, EmailService emailService, BookingQueueService bookingQueue)
        {
            _context = context;
            _passwordHasher = new PasswordHasher<Patient>();
            _appointmentRepository = appointmentRepository;
            _emailService = emailService;
            _bookingQueue = bookingQueue;
        }

        public IActionResult BookingType()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> BookingByService(int? serviceId, int? packageId)
        {
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
            var model = new BookingByServiceViewModel
            {
                Name = user.FullName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                ServiceOptions = await GetServiceListAsync(),
                PackageOptions = await GetPackageListAsync(),
                AppointmentDate = DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
                SelectedServiceId = serviceId,
                SelectedPackageId = packageId
            };
            return View(model);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BookingByService(BookingByServiceViewModel model)
        {
            ModelState.Remove(nameof(model.ServiceOptions));
            ModelState.Remove(nameof(model.PackageOptions));
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
            var patient = _context.Patients.FirstOrDefault(p => p.PatientId == patientId);
            if (patient == null)
            {
                return RedirectToAction("Login", "Auth");

            }
            Slot? slot = null;
            if (model.SelectedSlotId.HasValue)
            {
                slot = await _context.Slots.FirstOrDefaultAsync(d => d.SlotId == model.SelectedSlotId);
            }

            //Kiểm tra xem người dùng đã chọn 1 trong 2 loại gói dịch vụ hay chưa
            var service = await _context.Services.FirstOrDefaultAsync(d => d.ServiceId == model.SelectedServiceId);
            var package = await _context.Packages.FirstOrDefaultAsync(d => d.PackageId == model.SelectedPackageId);
            if (service == null && package == null)
            {
                model.ServiceOptions = await GetServiceListAsync();
                model.PackageOptions = await GetPackageListAsync();
                TempData["error"] = "Chọn dịch vụ khám cơ bản hoặc gói khám chưa hợp lệ!";
                return View(model);
            }

            // Kiểm tra trùng appointment cùng ngày, cùng giờ (slot), và trạng thái là "Pending"
            bool exists = await _context.Appointments.AnyAsync(a =>
                a.PatientId == patientId &&
                a.Date == model.AppointmentDate &&
                a.SlotId == model.SelectedSlotId &&
                a.Status != "Rejected"
            );

            if (exists)
            {
                model.ServiceOptions = await GetServiceListAsync();
                model.PackageOptions = await GetPackageListAsync();
                TempData["error"] = $"Bạn đã có cuộc hẹn đang chờ duyệt trong cùng khung giờ này!";
                return View(model);
            }

            var appointment = new Appointment
            {
                PatientId = patient.PatientId,
                Note = model.Note,
                SlotId = model.SelectedSlotId ?? null,
                Date = model.AppointmentDate,
                Status = "Pending",
                ServiceId = model.SelectedServiceId,
                PackageId = model.SelectedPackageId
            };

            _context.Appointments.Add(appointment);
            await _context.SaveChangesAsync();

            var savedAppointment = await _context.Appointments
                .Include(a => a.Doctor)
                .Include(a => a.Service)
                .Include(a => a.Package)
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
                var emailBody = EmailBuilder.BuildPendingAppointmentEmail(savedAppointment);

                await _emailService.SendEmailAsync(
                    toEmail: patient.Email,
                    subject: "Đặt lịch hẹn thành công - Đang chờ duyệt",
                    body: emailBody
                );

                TempData["success"] = "Đặt lịch hẹn thành công!";
            }
            catch (Exception ex)
            {
                TempData["error"] = $"Đặt lịch không thành công: {ex.Message}";
            }

            return RedirectToAction("MyAppointments");
        }
        [HttpGet]
        public async Task<IActionResult> BookingByDoctor(int? doctorId, string? departmentName)
        {
            var patientIdClaim = User.FindFirst("PatientID")?.Value;
            if (patientIdClaim == null)
            {
                return RedirectToAction("Login", "Auth");
            }

            int patientId = int.Parse(patientIdClaim);

            // Lấy thông tin từ DB
            var user = _context.Patients.FirstOrDefault(p => p.PatientId == patientId);
            if (user == null)
            {
                return RedirectToAction("Login", "Auth");
            }

            if (string.IsNullOrEmpty(user.PhoneNumber))
            {
                TempData["error"] = "Vui lòng cập nhật số điện thoại trước khi đặt cuộc hẹn!";
                return RedirectToAction("UpdateProfile", "Patient");
            }
            int selectedYear = DateTime.Today.Year;
            DateOnly selectedWeekStart;
            selectedWeekStart = GetStartOfWeek(DateOnly.FromDateTime(DateTime.Today));
            DateOnly selectedWeekEnd = selectedWeekStart.AddDays(6);

            // 3. Lấy lịch làm việc của bác sĩ (nếu đã chọn bác sĩ)
            List<DoctorScheduleViewModel.ScheduleItem> schedules = new();
            if (doctorId != null)
            {
                schedules = await _context.Schedules
                    .Where(s => s.DoctorId == doctorId && s.Day >= selectedWeekStart && s.Day <= selectedWeekEnd)
                    .Select(s => new DoctorScheduleViewModel.ScheduleItem
                    {
                        Day = s.Day,
                        SlotIndex = s.SlotId,
                        StartTime = s.Slot.StartTime.ToString(@"hh\:mm"),
                        EndTime = s.Slot.EndTime.ToString(@"hh\:mm"),
                        RoomName = s.Room.RoomName
                    })
                    .ToListAsync();
            }
            ViewBag.SelectedYear = selectedYear;
            ViewBag.SelectedWeekStart = selectedWeekStart;
            ViewBag.DaysInWeek = Enumerable.Range(0, 7).Select(i => selectedWeekStart.AddDays(i)).ToList();
            ViewBag.SlotsPerDay = 6;
            var model = new BookingByDoctorViewModel
            {
                SelectedDepartmentId = departmentName,
                Name = user.FullName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                ServiceOptions = await GetServiceListAsync(),
                PackageOptions = await GetPackageListAsync(),
                AppointmentDate = DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
                SelectedDoctorId = doctorId,
                DepartmentOptions = await GetDepartmentListAsync(),
                WeeklySchedule = schedules
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BookingByDoctor(BookingByDoctorViewModel model)
        {
            if (!ModelState.IsValid)
            {
                // Nạp lại dropdown nếu có lỗi
                model.DepartmentOptions = await GetDepartmentListAsync();
                model.ServiceOptions = await GetServiceListAsync();
                model.PackageOptions = await GetPackageListAsync();
                return View(model);
            }

            // Kiểm tra xác thực người dùng là bệnh nhân
            var patientIdClaim = User.FindFirst("PatientID")?.Value;
            if (string.IsNullOrEmpty(patientIdClaim))
            {
                return RedirectToAction("Login", "Auth");
            }
            int patientId = int.Parse(patientIdClaim);
            var patient = _context.Patients.FirstOrDefault(p => p.PatientId == patientId);
            if (patient == null)
            {
                return RedirectToAction("Login", "Auth");
            }

            var isPatientConflict = await _context.Appointments.AnyAsync(a =>
                                                                a.PatientId == patientId &&
                                                                a.Date == model.AppointmentDate &&
                                                                a.SlotId == model.SelectedSlotId &&
                                                                a.Status != "Rejected");

            if (isPatientConflict)
            {
                TempData["error"] = "Bạn đã có một cuộc hẹn trong khung giờ này!";
                model.DepartmentOptions = await GetDepartmentListAsync();
                model.ServiceOptions = await GetServiceListAsync();
                model.PackageOptions = await GetPackageListAsync();
                return View(model);
            }
            // Đẩy request vào hàng đợi để xử lý bất đồng bộ
            await _bookingQueue.EnqueueAsync(new BookingRequest
            {
                Model = model,
                User = User
            });

            TempData["success"] = "Hệ thống đang xử lý đặt lịch của bạn. Vui lòng kiểm tra lịch hẹn sau vài phút.";
            return RedirectToAction("MyAppointments");
        }

        private DateOnly GetStartOfWeek(DateOnly date)
        {
            int diff = ((int)date.DayOfWeek + 6) % 7; // Monday = 0
            return date.AddDays(-diff);
        }

        [HttpGet]
        public async Task<IActionResult> GetDoctorScheduleTable(int doctorId, int? year, string? weekStart, bool includePending = false)
        {
            int selectedYear = year ?? DateTime.Today.Year;
            DateOnly selectedWeekStart;

            if (!string.IsNullOrEmpty(weekStart) && DateOnly.TryParseExact(weekStart, "yyyy-MM-dd", out var parsed))
            {
                selectedWeekStart = parsed;
            }
            else
            {
                selectedWeekStart = GetStartOfWeek(DateOnly.FromDateTime(DateTime.Today));
            }

            DateOnly selectedWeekEnd = selectedWeekStart.AddDays(6);

            var schedules = await _context.Schedules
                .Where(s => s.DoctorId == doctorId && s.Day >= selectedWeekStart && s.Day <= selectedWeekEnd)
                .Select(s => new DoctorScheduleViewModel.ScheduleItem
                {
                    Day = s.Day,
                    SlotIndex = s.SlotId,
                    StartTime = s.Slot.StartTime.ToString(@"hh\:mm"),
                    EndTime = s.Slot.EndTime.ToString(@"hh\:mm"),
                    RoomName = s.Room.RoomName
                })
                .ToListAsync();
            var statusList = includePending
                   ? new[] { "Confirmed", "Pending" }
                   : new[] { "Confirmed" };

            var bookedAppointments = await _context.Appointments
                            .Where(a => a.DoctorId == doctorId &&
                                        a.Date >= selectedWeekStart && a.Date <= selectedWeekEnd &&
                                        statusList.Contains(a.Status))
                            .ToListAsync();

            ViewBag.BookedAppointments = bookedAppointments;
            ViewBag.Today = DateTime.Today;
            ViewBag.SelectedYear = selectedYear;
            ViewBag.SelectedWeekStart = selectedWeekStart;
            ViewBag.DaysInWeek = Enumerable.Range(0, 7).Select(i => selectedWeekStart.AddDays(i)).ToList();
            ViewBag.SlotsPerDay = 6;

            return PartialView("~/Views/Appointment/_ScheduleTablePartial.cshtml", schedules);
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
        public async Task<IActionResult> MyAppointments(string? SearchName, string? SlotFilter, string? DateFilter, string? StatusFilter, int? page)
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

            // Truy vấn lọc
            var filteredList = await _appointmentRepository.Filter(roleKey, (int)userId, SearchName, SlotFilter, DateFilter, StatusFilter);

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
        [HttpGet]
        public async Task<IActionResult> Create(int? doctorId, int? year, string? weekStart)
        {
            int selectedYear = year ?? DateTime.Today.Year;
            DateOnly selectedWeekStart;
            if (!string.IsNullOrEmpty(weekStart) &&
                DateOnly.TryParseExact(weekStart, "yyyy-MM-dd", out var parsed))
            {
                selectedWeekStart = parsed;
            }
            else
            {
                selectedWeekStart = GetStartOfWeek(DateOnly.FromDateTime(DateTime.Today));
            }

            DateOnly selectedWeekEnd = selectedWeekStart.AddDays(6);

            // Lấy lịch làm việc của bác sĩ (nếu đã chọn bác sĩ)
            List<DoctorScheduleViewModel.ScheduleItem> schedules = new();
            if (doctorId != null)
            {
                schedules = await _context.Schedules
                    .Where(s => s.DoctorId == doctorId && s.Day >= selectedWeekStart && s.Day <= selectedWeekEnd)
                    .Select(s => new DoctorScheduleViewModel.ScheduleItem
                    {
                        Day = s.Day,
                        SlotIndex = s.SlotId,
                        StartTime = s.Slot.StartTime.ToString(@"hh\:mm"),
                        EndTime = s.Slot.EndTime.ToString(@"hh\:mm"),
                        RoomName = s.Room.RoomName
                    })
                    .ToListAsync();
            }

            ViewBag.SelectedYear = selectedYear;
            ViewBag.SelectedWeekStart = selectedWeekStart;
            ViewBag.DaysInWeek = Enumerable.Range(0, 7).Select(i => selectedWeekStart.AddDays(i)).ToList();
            ViewBag.SlotsPerDay = 6;

            var model = new CreateAppointmentViewModel
            {
                AppointmentDate = DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
                SelectedDoctorId = doctorId,
                ServiceOptions = await GetServiceListAsync(),
                PackageOptions = await GetPackageListAsync(),
                DepartmentOptions = await GetDepartmentListAsync(),
                WeeklySchedule = schedules
            };

            return View(model);
        }

        [Authorize(Roles = "Sales")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateAppointmentViewModel model)
        {
            if (!ModelState.IsValid)
            {
                // Nạp lại dropdown nếu có lỗi
                model.DepartmentOptions = await GetDepartmentListAsync();
                model.ServiceOptions = await GetServiceListAsync();
                model.PackageOptions = await GetPackageListAsync();
                model.WeeklySchedule = new List<DoctorScheduleViewModel.ScheduleItem>();
                return View(model);
            }

            // Kiểm tra xác thực người dùng là Sales
            var staffIdClaim = User.FindFirst("StaffID")?.Value;
            if (string.IsNullOrEmpty(staffIdClaim))
            {
                return RedirectToAction("Login", "Auth");
            }

            int staffId = int.Parse(staffIdClaim);

            // Kiểm tra xem bệnh nhân đã tồn tại chưa
            var patient = await _context.Patients.FirstOrDefaultAsync(p => p.Email == model.Email);
            if (patient == null)
            {
                // Tạo bệnh nhân mới
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
                    var emailBody = EmailBuilder.BuildAccountInfoEmail(patient.FullName, patient.Email, fixedPassword);

                    await _emailService.SendEmailAsync(
                        toEmail: patient.Email,
                        subject: "✅ Fmec System - Tài khoản mới",
                        body: emailBody
                    );
                }
                catch (Exception ex)
                {
                    TempData["error"] = $"Tạo tài khoản không thành công: {ex.Message}";
                }
            }
            //var isExistedAppointment = await _context.Appointments
            //    .AnyAsync(a => a.Date == model.AppointmentDate && a.PatientId == patient.PatientId);


            //if (isExistedAppointment)
            //{
            //    TempData["error"] = "Không thể tạo cuộc hẹn mới trong cùng 1 ngày!";
            //    model.DepartmentOptions = await GetDepartmentListAsync();
            //    model.ServiceOptions = await GetServiceListAsync();
            //    model.PackageOptions = await GetPackageListAsync();
            //    model.WeeklySchedule = new List<DoctorScheduleViewModel.ScheduleItem>();
            //    return View(model);
            //}

            // Tạo cuộc hẹn mới
            var appointment = new Appointment
            {
                PatientId = patient.PatientId,
                DoctorId = model.SelectedDoctorId,
                ServiceId = model.SelectedServiceId,
                PackageId = model.SelectedPackageId,
                SlotId = model.SelectedSlotId,
                Date = model.AppointmentDate,
                Status = "Confirmed",
                Note = model.Note,
                StaffId = staffId
            };

            _context.Appointments.Add(appointment);
            await _context.SaveChangesAsync();

            var confirmedAppointment = await _context.Appointments
                .Include(a => a.Doctor)
                .Include(a => a.Service)
                .Include(a => a.Package)
                .Include(a => a.Staff)
                .Include(a => a.Patient)
                .Include(a => a.Slot)
                .FirstOrDefaultAsync(a => a.AppointmentId == appointment.AppointmentId);

            if (confirmedAppointment == null)
            {
                TempData["error"] = $"Lỗi khi tạo lịch hẹn!";
                model.DepartmentOptions = await GetDepartmentListAsync();
                model.ServiceOptions = await GetServiceListAsync();
                model.PackageOptions = await GetPackageListAsync();
                model.WeeklySchedule = new List<DoctorScheduleViewModel.ScheduleItem>();
                return View(model);
            }

            try
            {
                var emailBody = EmailBuilder.BuildConfirmedAppointmentEmail(confirmedAppointment);

                await _emailService.SendEmailAsync(
                    toEmail: patient.Email,
                    subject: "Lịch hẹn đã được xác nhận",
                    body: emailBody
                );

                TempData["success"] = "Tạo lịch hẹn thành công!";
            }
            catch (Exception ex)
            {
                TempData["error"] = $"Tạo lịch hẹn không thành công: {ex.Message}";
            }

            return RedirectToAction("MyAppointments");
        }

        [Authorize(Roles = "Sales")]
        [HttpGet]
        public async Task<IActionResult> ApproveAppointment(string? statusFilter, string? searchName, string? timeFilter, string? dateFilter, int? page)
        {
            int pageSize = 10;
            int pageNumber = page ?? 1;

            // Chuẩn hóa tên
            searchName = NormalizeName(searchName);
            ViewBag.StatusFilter = statusFilter ?? "All";
            ViewBag.SlotOptions = await _context.Slots.ToListAsync();
            ViewBag.SearchName = searchName;
            ViewBag.SlotFilter = timeFilter;
            ViewBag.DateFilter = dateFilter;
            var filteredList = await _appointmentRepository.FilterApproveAppointment(statusFilter, searchName, timeFilter, dateFilter);

            // Phân trang
            var pagedAppointments = filteredList
                .OrderByDescending(a => a.AppointmentId)
                .ToPagedList(pageNumber, pageSize);

            return View(pagedAppointments);
        }

        [Authorize(Roles = "Sales")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignDoctor(AssignDoctorViewModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["error"] = "Dữ liệu không hợp lệ.";
                return RedirectToAction("ApproveAppointment");
            }
            var appointment = await _context.Appointments
                            .Include(a => a.Slot)
                            .FirstOrDefaultAsync(a => a.AppointmentId == model.AppointmentId);

            if (appointment == null)
            {
                TempData["error"] = "Không tìm thấy cuộc hẹn.";
                return RedirectToAction("ApproveAppointment");
            }

            var doctor = await _context.Doctors
                .Include(d => d.Schedules)
                .FirstOrDefaultAsync(d => d.DoctorId == model.SelectedDoctorId);

            if (doctor == null)
            {
                TempData["error"] = "Không tìm thấy bác sĩ.";
                return RedirectToAction("ApproveAppointment");
            }

            bool hasSchedule = doctor.Schedules.Any(s =>
                            s.Day == appointment.Date &&
                            s.SlotId == model.SlotId);

            if (!hasSchedule)
            {
                TempData["error"] = "Bác sĩ không có lịch làm việc trong khung giờ này.";
                return RedirectToAction("ApproveAppointment");
            }

            //Check xem bác sĩ đã bị trùng lịch hẹn hay chưa?
            bool hasConflict = await _context.Appointments.AnyAsync(a =>
                a.DoctorId == doctor.DoctorId &&
                a.Date == appointment.Date &&
                a.SlotId == model.SlotId &&
                a.AppointmentId != model.AppointmentId &&
                a.Status != "Rejected"
            );

            if (hasConflict)
            {
                TempData["error"] = "Bác sĩ đã có cuộc hẹn khác trong khung giờ này.";
                return RedirectToAction("ApproveAppointment");
            }

            appointment.DoctorId = model.SelectedDoctorId;
            appointment.Status = "Confirmed";
            _context.Update(appointment);
            await _context.SaveChangesAsync();

            var savedAppointment = await _context.Appointments
                .Include(a => a.Doctor)
                .Include(a => a.Service)
                .Include(a => a.Package)
                .Include(a => a.Staff)
                .Include(a => a.Patient)
                .Include(a => a.Slot)
                .FirstOrDefaultAsync(a => a.AppointmentId == appointment.AppointmentId);

            if (savedAppointment == null)
            {
                TempData["error"] = "Error!";
                return View(model);
            }

            try
            {
                var emailBody = EmailBuilder.BuildConfirmedAppointmentEmail(savedAppointment);
                await _emailService.SendEmailAsync(
                    toEmail: savedAppointment.Patient.Email,
                    subject: "Cuộc hẹn đã được duyệt!",
                    body: emailBody
                );
                TempData["success"] = "Duyệt cuộc hẹn thành công!";
            }
            catch (Exception ex)
            {
                TempData["error"] = $"Duyệt cuộc hẹn không thành công: {ex.Message}";
            }

            
            TempData["success"] = "Chỉ định bác sĩ thành công.";
            return RedirectToAction("ApproveAppointment");
        }

        [HttpPost]
        public async Task<IActionResult> Review(int id, string action)
        {
            var appointment = await _context.Appointments.FirstOrDefaultAsync(a => a.AppointmentId == id);
            if (appointment == null)
            {
                TempData["error"] = "Cuộc hẹn không tồn tại!";
                return RedirectToAction("ApproveAppointment");
            }

            if (appointment.DoctorId == null && action.Equals("Accept"))
            {
                TempData["error"] = "Cuộc hẹn chưa được chỉ định bác sĩ! Vui lòng chỉ định bác sĩ!";
                return RedirectToAction("ApproveAppointment");
            }

            switch (action)
            {
                case "Accept":
                    appointment.Status = "Confirmed";
                    TempData["success"] = "Cuộc hẹn đã được duyệt.";
                    break;

                case "Reject":
                    appointment.Status = "Rejected";
                    TempData["success"] = "Cuộc hẹn đã bị từ chối.";
                    break;

                default:
                    TempData["error"] = "Thao tác không hợp lệ.";
                    return RedirectToAction("ApproveAppointment");
            }
            _context.SaveChanges();
            return RedirectToAction("ApproveAppointment");
        }

        public IActionResult LoadAssignDoctorModal(int appointmentId, DateTime date)
        {
            var appointment = _context.Appointments
                .Include(a => a.Slot)
                .FirstOrDefault(a => a.AppointmentId == appointmentId);

            if (appointment == null || appointment.Slot == null)
            {
                TempData["error"] = "Không hợp lệ!";
                return RedirectToAction("ApproveAppointment");
            }

            var slotId = appointment.Slot.SlotId;
            var dateOnly = DateOnly.FromDateTime(date);

            // Lọc các bác sĩ:
            // - Có lịch trực trong khung giờ đó
            // - KHÔNG có lịch hẹn trùng giờ
            var doctors = _context.Doctors
                .Include(d => d.Schedules)
                .Where(d =>
                    d.Schedules.Any(s => s.Day == dateOnly && s.SlotId == slotId) &&
                    !_context.Appointments.Any(a =>
                        a.DoctorId == d.DoctorId &&
                        a.Date == dateOnly &&
                        a.SlotId == slotId &&
                        a.Status != "Rejected")
                )
                .ToList();

            var viewModel = new AssignDoctorViewModel
            {
                AppointmentId = appointmentId,
                AppointmentDate = dateOnly,
                SlotId = slotId,
                SlotTimeText = $"{appointment.Slot.StartTime:hh\\:mm} - {appointment.Slot.EndTime:hh\\:mm}",
                Doctors = doctors
            };

            return PartialView("_AssignDoctorModal", viewModel);
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
        public async Task<IActionResult> GetDoctorsByDepartment(string department)
        {
            if (string.IsNullOrEmpty(department))
            {
                return BadRequest("Department name is required.");
            }

            var doctors = await _context.Doctors
                                        .Where(d => d.DepartmentName == department)
                                        .Select(d => new
                                        {
                                            d.DoctorId,
                                            DoctorName = d.FullName,
                                            ProfileImage = d.ProfileImage,
                                            DepartmentName = d.DepartmentName
                                        })
                                        .ToListAsync();

            Console.WriteLine("Doctors: " + string.Join(", ", doctors.Select(d => d.DoctorName)));
            return Json(doctors);
        }
        [HttpGet]
        public async Task<IActionResult> GetSlotsSimple(DateOnly date)
        {
            var slots = await _context.Slots
                .OrderBy(s => s.SlotId)
                .Select(s => new
                {
                    s.SlotId,
                    SlotTime = $"{s.StartTime:hh\\:mm} - {s.EndTime:hh\\:mm}",
                    IsBooked = false // Không kiểm tra, luôn là false
                })
                .ToListAsync();

            return Json(slots);
        }

        [HttpGet]
        public async Task<IActionResult> GetSlotsBooked(DateOnly date, int? SelectedServiceId, int? SelectedPackageId)
        {
            // Lấy danh sách SlotId đã được đặt (Confirmed), đúng theo loại (Service hoặc Package)
            var bookedSlotIdsQuery = _context.Appointments
                .Where(a => a.Date == date && a.Status == "Confirmed");

            if (SelectedServiceId.HasValue)
            {
                bookedSlotIdsQuery = bookedSlotIdsQuery
                    .Where(a => a.ServiceId == SelectedServiceId.Value);
            }
            else if (SelectedPackageId.HasValue)
            {
                bookedSlotIdsQuery = bookedSlotIdsQuery
                    .Where(a => a.PackageId == SelectedPackageId.Value);
            }

            var bookedSlotIds = await bookedSlotIdsQuery
                .Select(a => a.SlotId)
                .ToListAsync();

            // Lấy toàn bộ slot trong bảng Slot (không cần theo doctor)
            var slots = await _context.Slots
                .Select(s => new
                {
                    s.SlotId,
                    SlotTime = $"{s.StartTime:hh\\:mm} - {s.EndTime:hh\\:mm}",
                    IsBooked = bookedSlotIds.Contains(s.SlotId)
                })
                .ToListAsync();
            Console.WriteLine("Slots: " + string.Join(", ", slots.Select(s => s.SlotTime)));
            return Json(slots);
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

        [HttpGet]
        public async Task<IActionResult> GetTestsByPackage(int packageId)
        {
            var tests = await _context.PackageTests
                                      .Where(pt => pt.PackageId == packageId)
                                      .Include(pt => pt.Test)
                                      .Select(pt => pt.Test)
                                      .ToListAsync();
            foreach (var test in tests)
            {
                Console.WriteLine($"Test: {test.TestId} - {test.Name} - {test.Price}");

            }
            ;
            return Json(tests);
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
        private async Task<List<SelectListItem>> GetDepartmentListAsync()
        {
            var depts = await _context.Doctors
                .Select(d => d.DepartmentName)
                .Where(name => !string.IsNullOrEmpty(name))
                .Distinct()
                .OrderBy(name => name)
                .ToListAsync();

            // Chuyển thành SelectListItem
            return depts
                .Select(name => new SelectListItem
                {
                    Value = name,
                    Text = name
                })
                .ToList();
        }

        private async Task<List<SelectListItem>> GetPackageListAsync()
        {
            return await _context.Packages
                .Select(s => new SelectListItem
                {
                    Value = s.PackageId.ToString(),
                    Text = $"{s.PackageName} - {s.FinalPrice.ToString("0")}k"
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


            if (User.IsInRole("Admin") || User.IsInRole("Sales"))
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
        [HttpGet]
        public async Task<IActionResult> SearchDoctors(string keyword, string? departmentName)
        {
            var query = _context.Doctors
                .Where(d => d.IsActive);

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var lowered = keyword.ToLower();
                query = query.Where(d =>
                    d.FullName.ToLower().Contains(lowered) ||
                    d.DepartmentName.ToLower().Contains(lowered));
            }

            if (!string.IsNullOrEmpty(departmentName))
            {
                query = query.Where(d => d.DepartmentName.Equals(departmentName));
            }

            var result = await query.Select(d => new
            {
                doctorId = d.DoctorId,
                fullName = d.FullName,
                departmentName = d.DepartmentName,
                profileImage = d.ProfileImage
            }).ToListAsync();
            return Json(result);
        }

    }

}
