using HospitalManagement.Models;
using HospitalManagement.Repositories;
using HospitalManagement.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.Diagnostics;
using System.Security.Claims;
using X.PagedList;
using Microsoft.EntityFrameworkCore;
using HospitalManagement.Data;
using HospitalManagement.Helpers;

namespace HospitalFETemplate.Controllers
{

    public class HomeController : Controller
    {
        private readonly IDoctorRepository _doctorRepo;
        private readonly HospitalManagementContext _context;
        private readonly IPatientRepository _patientRepo;
        private readonly IFeedbackRepository _feedbackRepo;
        public HomeController(IDoctorRepository doctorRepo, HospitalManagementContext context, IPatientRepository patientRepo, IFeedbackRepository feedbackRepo)
        {
            _doctorRepo = doctorRepo;
            _context = context;
            _patientRepo = patientRepo;
            _feedbackRepo = feedbackRepo;
        }

        public async Task<IActionResult> Index()
        {
            var (xetNghiem, doctors) = await _doctorRepo.CountDoctorsByDepartmentAsync();
            ViewBag.XetNghiemCount = xetNghiem;
            ViewBag.OtherCount = doctors;
            var patientCount = await _patientRepo.CountActivePatientsAsync();
            ViewBag.PatientCount = patientCount;
            var specialFeedbacks = await _feedbackRepo.GetSpecialFeedbacksAsync();
            ViewBag.SpecialFeedbacks = specialFeedbacks;


            // Nếu là bệnh nhân
            if (User.Identity.IsAuthenticated && User.IsInRole("Patient"))
            {
                var patientIdClaim = User.FindFirst("PatientID")?.Value;
                if (int.TryParse(patientIdClaim, out int patientId))
                {
                    Console.WriteLine($"patientId: {patientId}");
                    // Tìm appointment đang diễn ra
                    var appointment = await _context.Appointments
                        .Where(a => a.PatientId == patientId && a.Status == AppConstants.AppointmentStatus.Ongoing)
                        .Include(a => a.Trackings)
                            .ThenInclude(t => t.Room)
                        .Include(a => a.Trackings)
                            .ThenInclude(t => t.TestRecord)
                        .FirstOrDefaultAsync();

                    Console.WriteLine($"appointment: {(appointment != null ? appointment.AppointmentId.ToString() : "null")}");

                    if (appointment != null)
                    {
                        var allTrackings = appointment.Trackings
                            .OrderBy(t => t.Time)
                            .ToList();
                        Console.WriteLine($"allTrackings.Count: {allTrackings.Count}");

                        // 1. Phòng khám đầu tiên
                        var clinic = allTrackings
                            .Where(t => t.Room?.RoomType == AppConstants.RoomTypes.Clinic)
                            .OrderByDescending(t => t.Time)
                            .FirstOrDefault();
                        Console.WriteLine($"clinic: {(clinic != null ? clinic.Room?.RoomName : "null")}, TrackingBatch: {(clinic != null ? clinic.TrackingBatch.ToString() : "null")}");

                        // 2. Gom theo batch
                        var groupedByBatch = allTrackings
                            .Where(t => t.Room?.RoomType != AppConstants.RoomTypes.Clinic)
                            .GroupBy(t => t.TrackingBatch)
                            .OrderBy(g => g.Key)
                            .ToList();
                        Console.WriteLine($"groupedByBatch.Count: {groupedByBatch.Count}");

                        var sortedTrackings = new List<Tracking>();
                        if (clinic != null)
                            sortedTrackings.Add(clinic);

                        foreach (var batch in groupedByBatch)
                        {
                            var unpaid = batch
                                .Where(t => t.TestRecord?.TestStatus == AppConstants.TestStatus.WaitingForPayment)
                                .ToList();

                            var ongoingTests = batch
                                .Where(t => t.TestRecord != null &&
                                           t.TestRecord.TestStatus != AppConstants.TestStatus.Completed &&
                                           t.TestRecord.TestStatus != AppConstants.TestStatus.WaitingForPayment)
                                .OrderBy(t => t.Time)
                                .ToList();

                            var completedTests = batch
                                .Where(t => t.TestRecord != null &&
                                           t.TestRecord.TestStatus == AppConstants.TestStatus.Completed)
                                .ToList();

                            // Thêm theo thứ tự: unpaid -> ongoing -> completed
                            sortedTrackings.AddRange(unpaid);
                            sortedTrackings.AddRange(ongoingTests);
                            sortedTrackings.AddRange(completedTests);
                        }
                        Console.WriteLine("sortedTrackings:");
                        foreach (var t in sortedTrackings)
                        {
                            Console.WriteLine($"  Room: {t.Room?.RoomName}, RoomType: {t.Room?.RoomType}, Batch: {t.TrackingBatch}, TestStatus: {t.TestRecord?.TestStatus}");
                        }
                        bool isClinicDone = clinic != null && groupedByBatch.Any(g =>
                                g.Key == clinic.TrackingBatch &&
                                g.Any(t => t.TestRecord != null)
                            );
                        Console.WriteLine($"isClinicDone: {isClinicDone}");

                        // 3. Tìm tracking kế tiếp (chưa hoàn thành)
                        Tracking nextTracking = null;

                        if (clinic != null && !isClinicDone)
                        {
                            // Bệnh nhân chưa đi khám
                            nextTracking = clinic;
                        }
                        else
                        {
                            // Sau khi khám xong
                            foreach (var batch in groupedByBatch)
                            {
                                var unpaid = batch
                                    .Where(t => t.TestRecord?.TestStatus == AppConstants.TestStatus.WaitingForPayment)
                                    .ToList();

                                var ongoingTests = batch
                                    .Where(t => t.TestRecord != null &&
                                               t.TestRecord.TestStatus != AppConstants.TestStatus.Completed &&
                                               t.TestRecord.TestStatus != AppConstants.TestStatus.WaitingForPayment)
                                    .OrderBy(t => t.Time)
                                    .ToList();

                                // Nếu có test cần thanh toán, hướng dẫn đến lễ tân
                                if (unpaid.Any())
                                {
                                    ViewBag.PatientCurrentRoom = "Bạn hãy tới lễ tân để thanh toán";
                                    ViewBag.PatientCurrentAppointmentId = appointment.AppointmentId;
                                    return View();
                                }

                                // Nếu có test đang diễn ra, hướng dẫn đến phòng test đầu tiên
                                if (ongoingTests.Any())
                                {
                                    nextTracking = ongoingTests.First();
                                    break;
                                }
                            }
                        }
                        Console.WriteLine($"nextTracking: {(nextTracking != null ? nextTracking.Room?.RoomName : "null")}, RoomType: {(nextTracking != null ? nextTracking.Room?.RoomType : "null")}");

                        if (nextTracking?.Room != null)
                        {
                            ViewBag.PatientCurrentRoom = $"Bạn đang có cuộc hẹn cần tới phòng: {nextTracking.Room.RoomName}";
                            ViewBag.PatientCurrentAppointmentId = appointment.AppointmentId;
                        }
                        else
                        {
                            ViewBag.PatientCurrentRoom = "";
                        }
                    }
                }
            }
       
            return View();
        }

        public IActionResult About()
        {
            return View();
            
        }

        public IActionResult Service()
        {
            return View();
        }

        public IActionResult NotFound()
        {
            return View();
        }
        public IActionResult AccessDenied()
        {
            return View();
        }
        public IActionResult TooMuchAttempt()
        {
            return View();
        }

        /**
         * Controller for ViewDoctors page, get name, department, exp year, isHead,
         * sort type, to filter out doctor, handle pagination
         */
        public async Task<IActionResult> ViewDoctors(int? page, string? name, string? department, int? exp, bool? isHead, string? sort)
        {
            name = HomeController.NormalizeName(name);
            int pageSize = 8;
            int pageNumber = page ?? 1;

            bool isDefaultView = string.IsNullOrEmpty(name)
                                 && string.IsNullOrEmpty(department)
                                 && !exp.HasValue
                                 && !isHead.HasValue
                                 && string.IsNullOrEmpty(sort);

            List<Doctor> doctors;
            int totalDoctors;

            if (isDefaultView)
            {
                // Lấy tất cả bác sĩ, ưu tiên isSpecial = 1 lên trước
                doctors = await _doctorRepo.GetAllDoctorsWithSpecialFirstAsync(pageNumber, pageSize);
                totalDoctors = await _doctorRepo.CountAllActiveDoctorsAsync();
            }
            else
            {
                doctors = await _doctorRepo.SearchAsync(name, department, exp, isHead, sort, true, null, pageNumber, pageSize);
                totalDoctors = await _doctorRepo.CountAsync(name, department, exp, isHead, true, null);
            }

            var pagedDoctors = new StaticPagedList<Doctor>(doctors, pageNumber, pageSize, totalDoctors);
            var departments = await _doctorRepo.GetDistinctDepartment(null);

            // Truyền lại filter cho view
            ViewBag.Name = name;
            ViewBag.Department = department;
            ViewBag.Experience = exp;
            ViewBag.Type = isHead;
            ViewBag.Sort = sort;
            ViewBag.Departments = departments;

            return View(pagedDoctors);
        }

        public async Task<IActionResult> DoctorDetail(int id)
        {
            var doctor = await _doctorRepo.GetByIdAsync(id);
            if (doctor == null)
            {
                return RedirectToAction("NotFound", "Home");
            }
            return View(doctor);
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
