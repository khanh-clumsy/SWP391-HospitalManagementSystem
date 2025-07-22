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
using HospitalManagement.Helpers;

namespace HospitalManagement.Controllers
{
    [Authorize]
    public class AppointmentController : BaseController
    {
        private readonly HospitalManagementContext _context;
        private readonly PasswordHasher<Patient> _passwordHasher;
        private readonly IAppointmentRepository _appointmentRepository;
        private readonly EmailService _emailService;
        private readonly BookingQueueService _bookingQueue;
        private readonly InvoiceService _invoiceService;

        public AppointmentController(HospitalManagementContext context, IAppointmentRepository appointmentRepository, EmailService emailService, BookingQueueService bookingQueue, InvoiceService invoiceService)
        {
            _context = context;
            _passwordHasher = new PasswordHasher<Patient>();
            _appointmentRepository = appointmentRepository;
            _emailService = emailService;
            _bookingQueue = bookingQueue;
            _invoiceService = invoiceService;
        }

        public IActionResult BookingType()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> BookingByService(int? serviceId, int? packageId)
        {
            var patientIdClaim = User.FindFirst(AppConstants.ClaimTypes.PatientId)?.Value;
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
                TempData["error"] = AppConstants.Messages.User.PhoneRequired;
                TempData["ReturnUrl"] = Url.Action("BookingByService", new { serviceId, packageId });
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
                TempData["error"] = AppConstants.Messages.General.ModelStateInvalid;
                return View(model);
            }
            // Lấy PatientId từ Claims
            var patientIdClaim = User.FindFirst(AppConstants.ClaimTypes.PatientId)?.Value;
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
                TempData["error"] = AppConstants.Messages.Appointment.InvalidServiceOrPackage;
                return View(model);
            }

            var excludedStatuses = new[] {
                AppConstants.AppointmentStatus.Rejected,
                AppConstants.AppointmentStatus.Failed,
                AppConstants.AppointmentStatus.Expired
            };

            // Kiểm tra trùng appointment cùng ngày, cùng giờ (slot), và trạng thái là "Pending"
            bool exists = await _context.Appointments.AnyAsync(a =>
                a.PatientId == patientId &&
                a.Date == model.AppointmentDate &&
                a.SlotId == model.SelectedSlotId &&
                !excludedStatuses.Contains(a.Status)

            );

            if (exists)
            {
                model.ServiceOptions = await GetServiceListAsync();
                model.PackageOptions = await GetPackageListAsync();
                TempData["error"] = AppConstants.Messages.Appointment.AlreadyExists;
                return View(model);
            }

            var appointment = new Appointment
            {
                PatientId = patient.PatientId,
                Note = model.Note,
                SlotId = model.SelectedSlotId ?? null,
                Date = model.AppointmentDate,
                Status = AppConstants.AppointmentStatus.Pending,
                ServiceId = model.SelectedServiceId,
                PackageId = model.SelectedPackageId
            };

            _context.Appointments.Add(appointment);
            await _context.SaveChangesAsync();

            var savedAppointment = await _context.Appointments
                .Include(a => a.Doctor)
                .Include(a => a.Service)
                .Include(a => a.Package)
                .Include(a => a.CreatedByStaff)
                .Include(a => a.Patient)
                .Include(a => a.Slot)
                .FirstOrDefaultAsync(a => a.AppointmentId == appointment.AppointmentId);

            if (savedAppointment == null)
            {
                TempData["error"] = AppConstants.Messages.Appointment.NotFound;
                return View(model);
            }

            try
            {
                var emailBody = EmailBuilder.BuildPendingAppointmentEmail(savedAppointment);

                await _emailService.SendEmailAsync(
                    toEmail: patient.Email,
                    subject: AppConstants.Messages.Appointment.PendingEmailSubject,
                    body: emailBody
                );

                TempData["success"] = AppConstants.Messages.Appointment.CreateSuccess;
            }
            catch (Exception ex)
            {
                TempData["error"] = $"{AppConstants.Messages.Appointment.CreateFail}: {ex.Message}";
            }

            return RedirectToAction("MyAppointments");
        }

        [HttpGet]
        public async Task<IActionResult> BookingByDoctor(int? doctorId, string? departmentName)
        {
            var patientIdClaim = User.FindFirst(AppConstants.ClaimTypes.PatientId)?.Value;
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
                TempData["error"] = AppConstants.Messages.User.PhoneRequired;
                TempData["ReturnUrl"] = Url.Action("BookingByDoctor", new { doctorId, departmentName });
                return RedirectToAction("UpdateProfile", "Patient");
            }

            int selectedYear = DateTime.Today.Year;
            DateOnly selectedWeekStart;
            selectedWeekStart = GetStartOfWeek(DateOnly.FromDateTime(DateTime.Today));
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
                        SlotId = s.SlotId,
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
                DepartmentOptions = await GetDepartmentListAsync(false),
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
                model.DepartmentOptions = await GetDepartmentListAsync(false);
                model.ServiceOptions = await GetServiceListAsync();
                model.PackageOptions = await GetPackageListAsync();
                TempData["error"] = AppConstants.Messages.General.ModelStateInvalid;
                return View(model);
            }

            // Kiểm tra xác thực người dùng là bệnh nhân
            var patientIdClaim = User.FindFirst(AppConstants.ClaimTypes.PatientId)?.Value;
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
            var excludedStatuses = new[] {
                AppConstants.AppointmentStatus.Rejected,
                AppConstants.AppointmentStatus.Failed,
                AppConstants.AppointmentStatus.Expired
            };
            var isPatientConflict = await _context.Appointments.AnyAsync(a =>
                                                                a.PatientId == patientId &&
                                                                a.Date == model.AppointmentDate &&
                                                                a.SlotId == model.SelectedSlotId &&
                                                                !excludedStatuses.Contains(a.Status));

            if (isPatientConflict)
            {
                TempData["error"] = AppConstants.Messages.Appointment.AlreadyExists;
                model.DepartmentOptions = await GetDepartmentListAsync(false);
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

            TempData["success"] = AppConstants.Messages.Appointment.Processing;
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
                    SlotId = s.SlotId,
                    StartTime = s.Slot.StartTime.ToString(@"hh\:mm"),
                    EndTime = s.Slot.EndTime.ToString(@"hh\:mm"),
                    RoomName = s.Room.RoomName
                })
                .ToListAsync();
            var statusList = includePending
                   ? new[] { AppConstants.AppointmentStatus.Confirmed, AppConstants.AppointmentStatus.Pending }
                   : new[] { AppConstants.AppointmentStatus.Confirmed };

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
        [Authorize(Roles = AppConstants.Roles.Admin)]
        public async Task<IActionResult> Index(string? searchName, string? timeFilter, string? dateFilter, string? statusFilter)
        {
            var appointments = await _appointmentRepository.FilterForAdmin(searchName, timeFilter, dateFilter, statusFilter);

            var slots = await _context.Slots.ToListAsync();
            ViewBag.SlotOptions = slots;

            return View(appointments);
        }

        [Authorize(Roles = AppConstants.Roles.Patient + "," + AppConstants.Roles.Sales + "," + AppConstants.Roles.Doctor)]
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

            // Truy vấn lọc
            var filteredList = await _appointmentRepository.Filter(roleKey, (int)userId, SearchName, SlotFilter, DateFilter, StatusFilter);

            // Lọc thêm theo filterType 
            var today = DateOnly.FromDateTime(DateTime.Now);
            var now = TimeOnly.FromDateTime(DateTime.Now);

            if (string.IsNullOrEmpty(Type))
                Type = AppConstants.FilterTypes.All;
            ViewBag.Type = Type;
            ViewBag.FilterType = Type;
            if (!string.IsNullOrEmpty(Type))
            {
                switch (Type)
                {
                    case AppConstants.FilterTypes.Today:
                        filteredList = filteredList.Where(a => a.Date == today).ToList();
                        break;

                    case AppConstants.FilterTypes.Ongoing:
                        filteredList = filteredList.Where(a => a.Date > today).ToList();
                        break;

                    case AppConstants.FilterTypes.Completed:
                        filteredList = filteredList.Where(a => a.Status == AppConstants.AppointmentStatus.Completed).ToList();
                        break;

                    case AppConstants.FilterTypes.All:
                    default:
                        // Không lọc thêm gì nữa
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

        [Authorize(Roles = AppConstants.Roles.Sales)]
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
                        SlotId = s.SlotId,
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
                DepartmentOptions = await GetDepartmentListAsync(false),
                WeeklySchedule = schedules
            };

            return View(model);
        }

        [Authorize(Roles = AppConstants.Roles.Sales)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateAppointmentViewModel model)
        {
            if (!ModelState.IsValid)
            {
                // Nạp lại dropdown nếu có lỗi
                model.DepartmentOptions = await GetDepartmentListAsync(false);
                model.ServiceOptions = await GetServiceListAsync();
                model.PackageOptions = await GetPackageListAsync();
                model.WeeklySchedule = new List<DoctorScheduleViewModel.ScheduleItem>();
                TempData["error"] = AppConstants.Messages.General.ModelStateInvalid;
                return View(model);
            }

            // Kiểm tra xác thực người dùng là Sales
            var staffIdClaim = User.FindFirst(AppConstants.ClaimTypes.StaffId)?.Value;
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
                        subject: AppConstants.Messages.User.NewAccountEmailSubject,
                        body: emailBody
                    );
                }
                catch (Exception ex)
                {
                    TempData["error"] = $"{AppConstants.Messages.User.CreateFail}: {ex.Message}";
                }
            }

            // Tạo cuộc hẹn mới
            var appointment = new Appointment
            {
                PatientId = patient.PatientId,
                DoctorId = model.SelectedDoctorId,
                ServiceId = model.SelectedServiceId,
                PackageId = model.SelectedPackageId,
                SlotId = model.SelectedSlotId,
                Date = model.AppointmentDate,
                Status = AppConstants.AppointmentStatus.Confirmed,
                Note = model.Note,
                CreatedByStaffId = staffId
            };

            _context.Appointments.Add(appointment);
            await _context.SaveChangesAsync();

            var confirmedAppointment = await _context.Appointments
                .Include(a => a.Doctor)
                .Include(a => a.Service)
                .Include(a => a.Package)
                .Include(a => a.CreatedByStaff)
                .Include(a => a.Patient)
                .Include(a => a.Slot)
                .FirstOrDefaultAsync(a => a.AppointmentId == appointment.AppointmentId);

            if (confirmedAppointment == null)
            {
                TempData["error"] = AppConstants.Messages.Appointment.NotFound;
                model.DepartmentOptions = await GetDepartmentListAsync(false);
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
                    subject: AppConstants.Messages.Appointment.ConfirmedEmailSubject,
                    body: emailBody
                );

                TempData["success"] = AppConstants.Messages.Appointment.CreateSuccess;
            }
            catch (Exception ex)
            {
                TempData["error"] = $"{AppConstants.Messages.Appointment.CreateFail}: {ex.Message}";
            }

            return RedirectToAction("MyAppointments");
        }

        [Authorize(Roles = AppConstants.Roles.Admin + "," + AppConstants.Roles.Sales)]
        [HttpGet]
        public async Task<IActionResult> ApproveAppointment(string? statusFilter, string? searchName, string? timeFilter, string? dateFilter, int? page)
        {
            int pageSize = 10;
            int pageNumber = page ?? 1;

            // Chuẩn hóa tên
            searchName = NormalizeName(searchName);
            ViewBag.StatusFilter = statusFilter ?? AppConstants.FilterTypes.All;
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

        [Authorize(Roles = AppConstants.Roles.Admin + "," + AppConstants.Roles.Sales)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignDoctor(AssignDoctorViewModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["error"] = AppConstants.Messages.General.ModelStateInvalid;
                return RedirectToAction("ApproveAppointment");
            }
            var appointment = await _context.Appointments
                            .Include(a => a.Slot)
                            .FirstOrDefaultAsync(a => a.AppointmentId == model.AppointmentId);

            if (appointment == null)
            {
                TempData["error"] = AppConstants.Messages.Appointment.NotFound;
                return RedirectToAction("ApproveAppointment");
            }

            var doctor = await _context.Doctors
                .Include(d => d.Schedules)
                .FirstOrDefaultAsync(d => d.DoctorId == model.SelectedDoctorId);

            if (doctor == null)
            {
                TempData["error"] = AppConstants.Messages.Doctor.NotFound;
                return RedirectToAction("ApproveAppointment");
            }

            bool hasSchedule = doctor.Schedules.Any(s =>
                            s.Day == appointment.Date &&
                            s.SlotId == model.SlotId);

            if (!hasSchedule)
            {
                TempData["error"] = AppConstants.Messages.Doctor.DoctorScheduleNotFound;
                return RedirectToAction("ApproveAppointment");
            }
            var excludedStatuses = new[] {
                AppConstants.AppointmentStatus.Rejected,
                AppConstants.AppointmentStatus.Failed,
                AppConstants.AppointmentStatus.Expired
            };
            //Check xem bác sĩ đã bị trùng lịch hẹn hay chưa?
            bool hasConflict = await _context.Appointments.AnyAsync(a =>
                a.DoctorId == doctor.DoctorId &&
                a.Date == appointment.Date &&
                a.SlotId == model.SlotId &&
                a.AppointmentId != model.AppointmentId &&
                !excludedStatuses.Contains(a.Status)
            );

            if (hasConflict)
            {
                TempData["error"] = AppConstants.Messages.Doctor.DoctorAlreadyHasAppointment;
                return RedirectToAction("ApproveAppointment");
            }
            appointment.DoctorId = model.SelectedDoctorId;
            appointment.Status = AppConstants.AppointmentStatus.Confirmed;
            _context.Update(appointment);
            await _context.SaveChangesAsync();

            var savedAppointment = await _context.Appointments
                .Include(a => a.Doctor)
                .Include(a => a.Service)
                .Include(a => a.Package)
                .Include(a => a.CreatedByStaff)
                .Include(a => a.Patient)
                .Include(a => a.Slot)
                .FirstOrDefaultAsync(a => a.AppointmentId == appointment.AppointmentId);

            if (savedAppointment == null)
            {
                TempData["error"] = AppConstants.Messages.Appointment.NotFound;
                return View(model);
            }

            //Tạo ra hóa đơn tương ứng
            await _invoiceService.CreateInvoiceForAppointmentAsync(savedAppointment!);
            if (savedAppointment.PackageId != null)
            {
                var package = await _context.Packages
                    .Include(p => p.PackageTests).ThenInclude(pt => pt.Test)
                    .FirstOrDefaultAsync(p => p.PackageId == savedAppointment.PackageId);
                if (package == null)
                {
                    TempData["error"] = AppConstants.Messages.Package.NotFound;
                    return RedirectToAction("ApproveAppointment");
                }

                if (package.PackageTests != null)
                {
                    foreach (var test in package.PackageTests)
                    {
                        var testRecord = new TestRecord
                        {
                            AppointmentId = savedAppointment.AppointmentId,
                            TestId = test.TestId,
                            TestStatus = AppConstants.TestStatus.Pending
                        };
                        _context.TestRecords.Add(testRecord);

                    }
                }
            }

            try
            {
                var emailBody = EmailBuilder.BuildConfirmedAppointmentEmail(savedAppointment);
                await _emailService.SendEmailAsync(
                    toEmail: savedAppointment.Patient.Email,
                    subject: AppConstants.Messages.Appointment.ConfirmedEmailSubject,
                    body: emailBody
                );
                TempData["success"] = AppConstants.Messages.Appointment.ApproveSuccess;
            }
            catch (Exception ex)
            {
                TempData["error"] = $"{AppConstants.Messages.Appointment.ApproveFail}: {ex.Message}";
            }

            TempData["success"] = AppConstants.Messages.Doctor.AssignSuccess;
            return RedirectToAction("ApproveAppointment");
        }

        [Authorize(Roles = AppConstants.Roles.Admin + "," + AppConstants.Roles.Sales)]
        [HttpPost]
        public async Task<IActionResult> Review(int id, string action)
        {
            var appointment = await _context.Appointments
                .Include(a => a.Patient)
                .FirstOrDefaultAsync(a => a.AppointmentId == id);
            if (appointment == null)
            {
                TempData["error"] = AppConstants.Messages.Appointment.NotFound;
                return RedirectToAction("ApproveAppointment");
            }

            if (appointment.DoctorId == null && action.Equals(AppConstants.AppointmentActions.Accept))
            {
                TempData["error"] = AppConstants.Messages.Appointment.NoDoctorAssigned;
                return RedirectToAction("ApproveAppointment");
            }

            switch (action)
            {
                case AppConstants.AppointmentActions.Accept:
                    appointment.Status = AppConstants.AppointmentStatus.Confirmed;
                    await _invoiceService.CreateInvoiceForAppointmentAsync(appointment);
                    if (appointment.PackageId != null)
                    {
                        var package = await _context.Packages
                            .Include(p => p.PackageTests).ThenInclude(pt => pt.Test)
                            .FirstOrDefaultAsync(p => p.PackageId == appointment.PackageId);
                        if (package == null)
                        {
                            TempData["error"] = AppConstants.Messages.Package.NotFound;
                            return RedirectToAction("ApproveAppointment");
                        }

                        if (package.PackageTests != null)
                        {
                            foreach (var test in package.PackageTests)
                            {
                                var testRecord = new TestRecord
                                {
                                    AppointmentId = appointment.AppointmentId,
                                    TestId = test.TestId,
                                    TestStatus = AppConstants.TestStatus.Pending
                                };
                                _context.TestRecords.Add(testRecord);

                            }
                        }
                    }
                    try
                    {
                        var emailBody = EmailBuilder.BuildConfirmedAppointmentEmail(appointment);
                        await _emailService.SendEmailAsync(
                            toEmail: appointment.Patient.Email,
                            subject: AppConstants.Messages.Appointment.ConfirmedEmailSubject,
                            body: emailBody
                        );
                        TempData["success"] = AppConstants.Messages.Appointment.ApproveSuccess;
                    }
                    catch (Exception ex)
                    {
                        TempData["error"] = $"{AppConstants.Messages.Appointment.ApproveFail}: {ex.Message}";
                    }
                    await _context.SaveChangesAsync();
                    TempData["success"] = AppConstants.Messages.Appointment.AlreadyApproved;
                    break;

                case AppConstants.AppointmentActions.Reject:
                    appointment.Status = AppConstants.AppointmentStatus.Rejected;
                    try
                    {
                        var emailBody = EmailBuilder.BuildRequestAppointmentFailed(appointment);
                        await _emailService.SendEmailAsync(
                            toEmail: appointment.Patient.Email,
                            subject: AppConstants.Messages.Appointment.RejectedEmailSubject,
                            body: emailBody
                        );
                        TempData["success"] = AppConstants.Messages.Appointment.ApproveSuccess;
                    }
                    catch (Exception ex)
                    {
                        TempData["error"] = $"{AppConstants.Messages.Appointment.ApproveFail}: {ex.Message}";
                    }
                    TempData["success"] = AppConstants.Messages.Appointment.Rejected;
                    break;

                default:
                    TempData["error"] = AppConstants.Messages.Appointment.InvalidAction;
                    return RedirectToAction("ApproveAppointment");
            }
            await _context.SaveChangesAsync();
            return RedirectToAction("ApproveAppointment");
        }

        public IActionResult LoadAssignDoctorModal(int appointmentId, DateTime date)
        {
            var appointment = _context.Appointments
                .Include(a => a.Slot)
                .FirstOrDefault(a => a.AppointmentId == appointmentId);

            if (appointment == null || appointment.Slot == null)
            {
                TempData["error"] = AppConstants.Messages.General.InvalidData;
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
                        a.Status != AppConstants.AppointmentStatus.Rejected &&
                        a.Status != AppConstants.AppointmentStatus.Expired)
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
                return BadRequest(AppConstants.Messages.General.DepartmentRequired);
            }

            var doctors = await _context.Doctors
                                        .Where(d => d.DepartmentName == department && d.IsActive)
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
                .Where(a => a.Date == date && a.Status == AppConstants.AppointmentStatus.Confirmed);

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
            return Json(tests);
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int appointmentId)
        {
            var appointment = _context.Appointments.FirstOrDefault(a => a.AppointmentId == appointmentId);
            if (appointment == null)
            {
                TempData["error"] = AppConstants.Messages.Appointment.NotFound;
                return RedirectToAction("Index", "Appointment");
            }
            _context.Appointments.Remove(appointment);
            await _context.SaveChangesAsync();
            TempData["success"] = AppConstants.Messages.General.SuccessDelete;
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

        private async Task<List<SelectListItem>> GetDepartmentListAsync(bool? containTestDoc)
        {
            var query = _context.Doctors.AsQueryable();

            if (!(containTestDoc ?? false))
            {
                query = query.Where(d =>
                    d.DepartmentName != AppConstants.RoomTypes.Lab &&
                    d.DepartmentName != AppConstants.RoomTypes.Imaging);
            }

            var departmentNames = await query
                .Where(d => !string.IsNullOrEmpty(d.DepartmentName))
                .Select(d => d.DepartmentName)
                .Distinct()
                .OrderBy(name => name)
                .ToListAsync();

            return departmentNames
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
            if (user.IsInRole(AppConstants.Roles.Patient))
                return (AppConstants.ClaimTypes.PatientId, GetUserIdFromClaim(user, AppConstants.ClaimTypes.PatientId));
            if (user.IsInRole(AppConstants.Roles.Sales))
                return (AppConstants.ClaimTypes.StaffId, GetUserIdFromClaim(user, AppConstants.ClaimTypes.StaffId));
            if (user.IsInRole(AppConstants.Roles.Doctor))
                return (AppConstants.ClaimTypes.DoctorId, GetUserIdFromClaim(user, AppConstants.ClaimTypes.DoctorId));
            return default;
        }

        private int? GetUserIdFromClaim(ClaimsPrincipal user, string claimType)
        {
            var claim = user.FindFirst(claimType);
            if (claim == null) return null;

            return int.TryParse(claim.Value, out var id) ? id : null;
        }

        [Authorize(Roles = AppConstants.Roles.Patient + "," + AppConstants.Roles.Sales + "," + AppConstants.Roles.Admin + "," + AppConstants.Roles.Doctor + "," + AppConstants.Roles.Receptionist)]
        public async Task<IActionResult> Detail(int appId, string returnUrl = null)
        {
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                TempData["PreviousUrl"] = returnUrl;
            }
            var appointment = await _context.Appointments
                                .Include(a => a.Patient)
                                .Include(a => a.Doctor)
                                .Include(a => a.CreatedByStaff)
                                .Include(a => a.Slot)
                                .Include(a => a.Service)
                                .Include(a => a.Package)
                                .Include(a => a.InvoiceDetails)
                                .Include(a => a.TestRecords).ThenInclude(tr => tr.Test)
                                .Include(a => a.Trackings).ThenInclude(t => t.Room)
                                .FirstOrDefaultAsync(a => a.AppointmentId == appId);
            if (appointment == null)
            {
                TempData["error"] = AppConstants.Messages.Appointment.NotFound;
                return NotFound();
            }

            ViewBag.BackUrl = GetSafeBackUrl();

            if (User.IsInRole(AppConstants.Roles.Admin) || User.IsInRole(AppConstants.Roles.Sales) || User.IsInRole(AppConstants.Roles.Receptionist))
            {
                return View(appointment);
            }

            // now, roleKey only Patient/Doctor/Staff
            var (roleKey, userId) = GetUserRoleAndId(User);
            if (userId == null)
            {
                TempData["error"] = AppConstants.Messages.Auth.SessionExpired;
                return RedirectToAction("Login", "Auth");
            }

            // Lấy toàn bộ trackings
            var allTrackings = appointment.Trackings?
                .OrderBy(t => t.Time)
                .ToList() ?? new List<Tracking>();

            // 1. Phòng khám
            var clinicTrackings = allTrackings
                 .Where(t => t.Room?.RoomType == AppConstants.RoomTypes.Clinic)
                 .OrderBy(t => t.Time)
                 .ToList();

            // 2. Gom theo batch (nhóm các tracking theo đợt)
            var groupedByBatch = allTrackings
                .Where(t => t.Room?.RoomType != AppConstants.RoomTypes.Clinic)
                .GroupBy(t => t.TrackingBatch)
                .OrderBy(g => g.Key)
                .ToList();

            var finalOrderedTrackings = new List<Tracking>();
            var firstClinic = allTrackings
                .Where(t => t.Room?.RoomType == AppConstants.RoomTypes.Clinic)
                .OrderBy(t => t.Time)
                .FirstOrDefault();

            if (firstClinic != null)
            {
                finalOrderedTrackings.Add(firstClinic);
            }

            // 4. Duyệt từng batch
            foreach (var batchGroup in groupedByBatch)
            {
                var unpaidTests = batchGroup
                    .Where(t => t.TestRecord != null && t.TestRecord.TestStatus == AppConstants.TestStatus.WaitingForPayment)
                    .ToList();

                var others = batchGroup
                    .Except(unpaidTests)
                    .Where(t => t.Room?.RoomType != AppConstants.RoomTypes.Cashier)
                    .ToList();

                finalOrderedTrackings.AddRange(unpaidTests);
                finalOrderedTrackings.AddRange(others);

                // Sau batch, thêm tracking clinic nếu có (sau thời gian cuối của batch)
                var lastTime = batchGroup.Max(t => t.Time);
                var nextClinic = clinicTrackings.FirstOrDefault(c => c.Time > lastTime);
                if (nextClinic != null)
                {
                    finalOrderedTrackings.Add(nextClinic);
                }
            }

            // Gán vào ViewBag để view hiển thị
            ViewBag.SortedTrackings = finalOrderedTrackings;
            if (roleKey == "")
            {
                TempData["error"] = AppConstants.Messages.General.Undefined;
                return NotFound();
            }
            if (roleKey == AppConstants.ClaimTypes.PatientId && appointment.Patient != null && appointment.Patient.PatientId != null && appointment.Patient.PatientId == userId)
            {
                return View(appointment);
            }
            if (roleKey == AppConstants.ClaimTypes.DoctorId && appointment.Doctor != null && appointment.Doctor.DoctorId != null && appointment.Doctor.DoctorId == userId)
            {
                return View(appointment);
            }
            if (roleKey == AppConstants.ClaimTypes.StaffId)
            {
                // Nếu bạn muốn tất cả staff được xem
                return View(appointment);
            }

            TempData["error"] = AppConstants.Messages.General.NoPermission;
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

            keyword = NormalizeName(keyword);
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