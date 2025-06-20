using HospitalManagement.Data;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Rendering;
using AspNetCoreGeneratedDocument;
using Microsoft.AspNetCore.Authorization;
using System.Threading.Tasks;

namespace HospitalManagement.Controllers
{
    [Authorize]
    public class ScheduleController : Controller
    {

        private readonly HospitalManagementContext _context;

        public ScheduleController(HospitalManagementContext context)
        {
            _context = context;
        }

        [Authorize(Roles = "Cashier, Sales, Doctor")]
        public async Task<IActionResult> ViewSchedule(int? year, string? weekStart)
        {
            var user = HttpContext.User;
            string email = user.FindFirstValue(ClaimTypes.Email);
            if (email == null) Unauthorized();

            var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.Email == email);
            if (doctor == null) return NotFound();



            // Nếu không truyền gì, dùng tuần hiện tại
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

            var schedules = _context.Schedules
                .Where(s => s.DoctorId == doctor.DoctorId && s.Day >= selectedWeekStart && s.Day <= selectedWeekEnd)
                .Select(s => new DoctorScheduleViewModel.ScheduleItem
                {
                    Day = s.Day,
                    SlotIndex = s.SlotId,
                    StartTime = s.Slot.StartTime.ToString(@"hh\:mm"),
                    EndTime = s.Slot.EndTime.ToString(@"hh\:mm"),
                    RoomName = s.Room.RoomName
                })
                .ToList();

            ViewBag.SelectedYear = selectedYear;
            ViewBag.SelectedWeekStart = selectedWeekStart;
            // TempData["success"] = $"Size: {schedules.Capacity}";
            //return Redirect("Home/NotFound");
            return View(schedules);
        }

        private DateOnly GetStartOfWeek(DateOnly date)
        {
            int diff = ((int)date.DayOfWeek + 6) % 7; // Monday = 0
            return date.AddDays(-diff);
        }

        [Authorize(Roles = "Cashier, Sales, Doctor")]
        [HttpGet]
        public IActionResult GetScheduleTable(int year, DateOnly weekStart)
        {
            var user = HttpContext.User;
            string email = user.FindFirstValue(ClaimTypes.Email);
            if (email == null) Unauthorized();

            var doctor = _context.Doctors.FirstOrDefault(d => d.Email == email);
            if (doctor == null) return NotFound();

            // Tính toán các thông số như ViewSchedule
            weekStart = GetStartOfWeek(weekStart);
            var weekEnd = weekStart.AddDays(6);
            var daysInWeek = Enumerable.Range(0, 7).Select(i => weekStart.AddDays(i)).ToList();

            var schedule = _context.Schedules
                .Where(s => s.DoctorId == doctor.DoctorId && s.Day >= weekStart && s.Day <= weekEnd)
                .Select(s => new DoctorScheduleViewModel.ScheduleItem
                {
                    SlotIndex = s.SlotId,
                    Day = s.Day,
                    StartTime = s.Slot.StartTime.ToString(@"hh\:mm"),
                    EndTime = s.Slot.EndTime.ToString(@"hh\:mm"),
                    RoomName = s.Room.RoomName

                })
                .ToList();

            ViewBag.DaysInWeek = daysInWeek;
            ViewBag.SlotsPerDay = 6;
            ViewBag.SelectedYear = year;
            ViewBag.SelectedWeekStart = weekStart;

            // TempData["success"] = $"{weekStart} -> {ViewBag.SelectedWeekStart}";
            return PartialView("_ScheduleTablePartial", schedule);
        }


        [Authorize(Roles = "Admin, Doctor")]
        public async Task<IActionResult> ManageSchedule(int? year, string? weekStart)
        {
            var user = HttpContext.User;
            string email = user.FindFirstValue(ClaimTypes.Email);
            if (email == null) Unauthorized();

            if (User.IsInRole("Doctor"))
            {
                if (User.HasClaim(c => c.Type == "IsDepartmentHead") &&
                                User.FindFirst("IsDepartmentHead")?.Value == "True")
                {
                    var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.Email == email);
                    if (doctor == null) return NotFound();

                    // Nếu không truyền gì, dùng tuần hiện tại
                    int selectedYear = year ?? DateTime.Today.Year;

                    DateOnly selectedWeekStart;
                    if (!string.IsNullOrEmpty(weekStart) &&
                        DateOnly.TryParseExact(weekStart, "yyyy-MM-dd", out var parsed))
                    {
                        selectedWeekStart = GetStartOfWeek(parsed);
                    }
                    else
                    {
                        selectedWeekStart = GetStartOfWeek(DateOnly.FromDateTime(DateTime.Today));
                    }

                    DateOnly selectedWeekEnd = selectedWeekStart.AddDays(6);

                    var slots = _context.Slots.ToList();

                    ViewBag.SelectedYear = selectedYear;
                    ViewBag.SelectedWeekStart = selectedWeekStart;
                    // TempData["success"] = $"Size: {schedules.Capacity}";
                    //return Redirect("Home/NotFound");
                    return View(slots);
                }
                else
                {
                    TempData["error"] = "Bạn không có quyền truy cập";
                    return Redirect("Home/AccessDenied");
                }
            }
            else if (user.IsInRole("Admin"))
            {
                var staff = await _context.Staff.FirstOrDefaultAsync(d => d.Email == email);
                if (staff == null) return NotFound();

                // Nếu không truyền gì, dùng tuần hiện tại
                int selectedYear = year ?? DateTime.Today.Year;

                DateOnly selectedWeekStart;
                if (!string.IsNullOrEmpty(weekStart) &&
                    DateOnly.TryParseExact(weekStart, "yyyy-MM-dd", out var parsed))
                {
                    selectedWeekStart = GetStartOfWeek(parsed);
                }
                else
                {
                    selectedWeekStart = GetStartOfWeek(DateOnly.FromDateTime(DateTime.Today));
                }

                DateOnly selectedWeekEnd = selectedWeekStart.AddDays(6);

                var slots = _context.Slots.ToList();

                ViewBag.SelectedYear = selectedYear;
                ViewBag.SelectedWeekStart = selectedWeekStart;
                // TempData["success"] = $"Size: {schedules.Capacity}";
                //return Redirect("Home/NotFound");
                
                return View(slots);
            }
            else
            {
                TempData["error"] = "Bạn không có quyền truy cập";
                return Redirect("Home/AccessDenied");
            }

        }

        [Authorize(Roles = "Admin, Doctor")]
        [HttpGet]
        public IActionResult GetScheduleTable2(int year, DateOnly weekStart)
        {
            var user = HttpContext.User;
            string email = user.FindFirstValue(ClaimTypes.Email);
            if (email == null) Unauthorized();

            // Tính toán các thông số như ViewSchedule
            weekStart = GetStartOfWeek(weekStart);
            var weekEnd = weekStart.AddDays(6);
            var daysInWeek = Enumerable.Range(0, 7).Select(i => weekStart.AddDays(i)).ToList();

            var slots = _context.Slots.ToList();


            ViewBag.DaysInWeek = daysInWeek;
            ViewBag.SlotsPerDay = 6;
            ViewBag.SelectedYear = year;
            ViewBag.SelectedWeekStart = weekStart;

            // TempData["success"] = $"{weekStart}";
            return PartialView("_ScheduleTablePartial2", slots);
        }
    }
}
