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

namespace HospitalFETemplate.Controllers
{

    public class HomeController : Controller
    {
        private readonly IDoctorRepository _doctorRepo;
        private readonly HospitalManagementContext _context;
        public HomeController(IDoctorRepository doctorRepo, HospitalManagementContext context)
        {
            _doctorRepo = doctorRepo;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            // Nếu là bệnh nhân, lấy phòng cần tới
            if (User.Identity.IsAuthenticated && User.IsInRole("Patient"))
            {
                var patientIdClaim = User.FindFirst("PatientID")?.Value;
                if (int.TryParse(patientIdClaim, out int patientId))
                {
                    // Lấy duy nhất 1 appointment Ongoing của bệnh nhân
                    var appointment = await _context.Appointments
                        .Where(a => a.PatientId == patientId && a.Status == "Ongoing")
                        .FirstOrDefaultAsync();
                    if (appointment != null)
                    {
                        // Lấy tracking mới nhất của appointment này (có phòng)
                        var tracking = await _context.Trackings
                            .Include(t => t.Room)
                            .Where(t => t.AppointmentId == appointment.AppointmentId)
                            .OrderByDescending(t => t.Time)
                            .FirstOrDefaultAsync();
                        if (tracking?.Room != null)
                        {
                            ViewBag.PatientCurrentRoom = $"Bạn đang có cuộc hẹn cần tới phòng: {tracking.Room.RoomName}";
                            ViewBag.PatientCurrentAppointmentId = appointment.AppointmentId;
                        }
                        else{
                            Console.WriteLine("Tracking is null!");
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
