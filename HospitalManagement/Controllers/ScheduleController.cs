using HospitalManagement.Data;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Rendering;
using AspNetCoreGeneratedDocument;
using Microsoft.AspNetCore.Authorization;
using System.Threading.Tasks;
using HospitalManagement.Repositories;
using Newtonsoft.Json;

namespace HospitalManagement.Controllers
{
    [Authorize]
    public class ScheduleController : Controller
    {

        private readonly HospitalManagementContext _context;
        private readonly IDoctorRepository _doctorRepo;

        private readonly IRoomRepository _roomRepo;

        public ScheduleController(HospitalManagementContext context, IDoctorRepository doctorRepo, IRoomRepository roomRepo)
        {
            _context = context;
            _doctorRepo = doctorRepo;
            _roomRepo = roomRepo;

        }

        // ========================  view own  =======================

        [Authorize(Roles = "Doctor, TestDoctor")]
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
                    ScheduleId = s.ScheduleId,
                    Day = s.Day,
                    SlotId = s.SlotId,
                    StartTime = s.Slot.StartTime.ToString(@"hh\:mm"),
                    EndTime = s.Slot.EndTime.ToString(@"hh\:mm"),
                    RoomName = s.Room.RoomName,
                    RoomId = s.Room.RoomId,
                    Status = s.Status
                })
                .ToList();

            ViewBag.SelectedYear = selectedYear;
            ViewBag.SelectedWeekStart = selectedWeekStart;
            var slots = _context.Slots.ToList();
            ViewBag.SlotsPerDay = slots.Count();
            // TempData["success"] = $"Size: {schedules.Capacity}";
            //return Redirect("Home/NotFound");
            return View(schedules);
        }

        private DateOnly GetStartOfWeek(DateOnly date)
        {
            int diff = ((int)date.DayOfWeek + 6) % 7; // Monday = 0
            return date.AddDays(-diff);
        }

        [Authorize(Roles = "Doctor, TestDoctor")]
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
                    ScheduleId = s.ScheduleId,
                    SlotId = s.SlotId,
                    Day = s.Day,
                    StartTime = s.Slot.StartTime.ToString(@"hh\:mm"),
                    EndTime = s.Slot.EndTime.ToString(@"hh\:mm"),
                    RoomName = s.Room.RoomName,
                    RoomId = s.Room.RoomId,
                    Status = s.Status
                })
                .ToList();

            ViewBag.DaysInWeek = daysInWeek;
            var slots = _context.Slots.ToList();
            ViewBag.SlotsPerDay = slots.Count();
            ViewBag.SelectedYear = year;
            ViewBag.SelectedWeekStart = weekStart;

            // TempData["success"] = $"{weekStart} -> {ViewBag.SelectedWeekStart}";
            return PartialView("_ScheduleTablePartial", schedule);
        }

        [Authorize(Roles = "Doctor")]
        [HttpPost]
        public IActionResult UpdateScheduleStatus(int scheduleId, string status)
        {
            var user = HttpContext.User;
            string email = user.FindFirstValue(ClaimTypes.Email);
            if (email == null) Unauthorized();

            var doctor = _context.Doctors.FirstOrDefault(d => d.Email == email);
            if (doctor == null) return NotFound();

            var schedule = _context.Schedules.FirstOrDefault(s => s.ScheduleId == scheduleId);

            if (schedule == null)
                return Json(new { success = false, message = "Không tìm thấy lịch cần cập nhật." });

            schedule.Status = status;

            _context.SaveChanges();
            return Json(new { success = true });
        }


        // ========================  add  ========================

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
                    ViewBag.SlotsPerDay = slots.Count();
                    ViewBag.SelectedYear = selectedYear;
                    ViewBag.SelectedWeekStart = selectedWeekStart;
                    ViewBag.ListDep = new List<string> { doctor.DepartmentName };
                    ViewBag.ListRoom = await _roomRepo.GetAllActiveRoom();
                    var doctorList = await _doctorRepo.GetAllDoctorsWithDepartment(doctor.DepartmentName);
                    ViewBag.ListDoctor = doctorList
                                        .Where(d => d.IsActive)
                                        .Select(d => new ViewModels.CardViewModel
                                        {
                                            doctorId = d.DoctorId,
                                            fullName = d.FullName,
                                            departmentName = d.DepartmentName,
                                            doctorCode = d.GenerateDoctorCode()
                                        }).ToList();
                    // TempData["success"] = $"Size: {ViewBag.ListDoctor.Count}";
                    // return Redirect("Home/NotFound");
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
                ViewBag.SlotsPerDay = slots.Count();
                ViewBag.SelectedYear = selectedYear;
                ViewBag.SelectedWeekStart = selectedWeekStart;
                ViewBag.ListDep = await _doctorRepo.GetDistinctDepartment(true);

                ViewBag.ListRoom = await _roomRepo.GetAllActiveRoom();
                var doctorList = _context.Doctors.ToList();
                ViewBag.ListDoctor = doctorList
                                    .Where(d => d.IsActive)
                                    .ToList()
                                    .Select(d => new ViewModels.CardViewModel
                                    {
                                        doctorId = d.DoctorId,
                                        fullName = d.FullName,
                                        departmentName = d.DepartmentName,
                                        doctorCode = d.GenerateDoctorCode()
                                    }).ToList();
                // TempData["success"] = $"Size: {ViewBag.ListDoctor.Count}";
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
            ViewBag.SlotsPerDay = slots.Count();
            ViewBag.SelectedYear = year;
            ViewBag.SelectedWeekStart = weekStart;

            // TempData["success"] = $"{weekStart}";
            return PartialView("_ScheduleTablePartial2", slots);
        }

        [Authorize(Roles = "Admin, Doctor")]
        [HttpPost]
        public async Task<IActionResult> AddDoctorIntoSlot(Dictionary<string, string> SlotStates, string doctorId, string roomId, string departmentName,
                                                            int year, DateOnly weekStart)
        {

            var existingSuccessList = new List<string>();
            var existingFailList = new List<string>();

            var selectedSlots = SlotStates
                .Where(ss => ss.Value == "true")
                .Select(ss =>
                {
                    var parts = ss.Key.Split('_');
                    var date = DateOnly.Parse(parts[0]);
                    var slot = int.Parse(parts[1]);
                    return new { Date = date, Slot = slot };
                })
                .ToList();

            int parsedDoctorId = int.Parse(doctorId);
            int parsedRoomId = int.Parse(roomId);

            foreach (var item in selectedSlots)
            {
                bool exists = _context.Schedules.Any(s =>
                    s.DoctorId == parsedDoctorId &&
                    s.SlotId == item.Slot &&
                    s.Day == item.Date
                );
                var room = await _roomRepo.GetRoomById(parsedRoomId);

                string key = $"{item.Date:yyyy-MM-dd}_{item.Slot}";
                if (exists || room.Status != "Active")
                {
                    existingFailList.Add(key);
                }
                else
                {
                    _context.Schedules.Add(new Models.Schedule
                    {
                        DoctorId = parsedDoctorId,
                        SlotId = item.Slot,
                        Day = item.Date,
                        RoomId = parsedRoomId
                    });
                    _context.SaveChanges();
                    existingSuccessList.Add(key);
                }
            }

            // Chuẩn bị dữ liệu cho partial view
            weekStart = GetStartOfWeek(weekStart);
            var daysInWeek = Enumerable.Range(0, 7).Select(i => weekStart.AddDays(i)).ToList();
            var slots = _context.Slots.ToList();

            ViewBag.SelectedYear = year;
            ViewBag.SelectedWeekStart = weekStart;
            ViewBag.SlotsPerDay = slots.Count();
            ViewBag.SuccessList = existingSuccessList;
            ViewBag.FailList = existingFailList;

            return PartialView("_ScheduleTablePartial2", slots);
        }

        [Authorize(Roles = "Admin, Doctor")]
        [HttpPost]
        public IActionResult GetDoctorScheduleInWeek(string doctorId, string year, string weekStart)
        {
            var slots = _context.Slots.ToList();
            ViewBag.SlotsPerDay = slots.Count();
            int parsedYear = int.Parse(year);


            if (!DateOnly.TryParse(weekStart, out var startDate) || string.IsNullOrEmpty(doctorId))
            {

                DateOnly selectedWeekStart;
                selectedWeekStart = GetStartOfWeek(DateOnly.FromDateTime(DateTime.Today));

                ViewBag.SelectedYear = parsedYear;
                ViewBag.SelectedWeekStart = selectedWeekStart;

                return PartialView("_ScheduleTablePartial2", slots);
            }
            int parsedDoctorId = int.Parse(doctorId);

            var endDate = startDate.AddDays(6);

            var schedules = _context.Schedules
                .Where(s => s.DoctorId == parsedDoctorId && s.Day >= startDate && s.Day <= endDate)
                .Select(s => new DoctorScheduleViewModel.ScheduleItem
                {
                    ScheduleId = s.ScheduleId,
                    Day = s.Day,
                    SlotId = s.SlotId,
                    StartTime = s.Slot.StartTime.ToString(@"hh\:mm"),
                    EndTime = s.Slot.EndTime.ToString(@"hh\:mm"),
                    RoomName = s.Room.RoomName,
                    RoomId = s.Room.RoomId,
                    DoctorId = parsedDoctorId
                })
                .ToList();

            ViewBag.DoctorSchedules = schedules;


            ViewBag.SelectedYear = parsedYear;
            ViewBag.SelectedWeekStart = startDate;

            // TempData["success"] = "Đã tải lịch làm việc.";

            return PartialView("_ScheduleTablePartial2", slots);
        }



        // ========================  change  ========================

        [Authorize(Roles = "Admin, Doctor")]
        public async Task<IActionResult> ChangeSchedule(int? year, string? weekStart)
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
                    ViewBag.SlotsPerDay = slots.Count();
                    ViewBag.SelectedYear = selectedYear;
                    ViewBag.SelectedWeekStart = selectedWeekStart;
                    ViewBag.ListDep = new List<string> { doctor.DepartmentName };
                    ViewBag.ListRoom = await _roomRepo.GetAllActiveRoom();
                    var doctorList = await _doctorRepo.GetAllDoctorsWithDepartment(doctor.DepartmentName);
                    ViewBag.ListDoctor = doctorList
                                        .Where(d => d.IsActive)
                                        .Select(d => new ViewModels.CardViewModel
                                        {
                                            doctorId = d.DoctorId,
                                            fullName = d.FullName,
                                            departmentName = d.DepartmentName,
                                            doctorCode = d.GenerateDoctorCode()
                                        }).ToList();

                    // TempData["success"] = $"Size: {ViewBag.ListDoctor.Count}";
                    // return Redirect("Home/NotFound");
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
                ViewBag.SlotsPerDay = slots.Count();
                ViewBag.SelectedYear = selectedYear;
                ViewBag.SelectedWeekStart = selectedWeekStart;
                ViewBag.ListDep = await _doctorRepo.GetDistinctDepartment(true);
                ViewBag.ListRoom = await _roomRepo.GetAllActiveRoom();
                var doctorList = _context.Doctors.ToList();
                ViewBag.ListDoctor = doctorList
                                    .Where(d => d.IsActive)
                                    .ToList()
                                    .Select(d => new ViewModels.CardViewModel
                                    {
                                        doctorId = d.DoctorId,
                                        fullName = d.FullName,
                                        departmentName = d.DepartmentName,
                                        doctorCode = d.GenerateDoctorCode()
                                    }).ToList();
                // TempData["success"] = $"Size: {ViewBag.ListDoctor.Count}";
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
        public IActionResult GetScheduleTable3(int year, DateOnly weekStart)
        {
            var user = HttpContext.User;
            string email = user.FindFirstValue(ClaimTypes.Email);
            if (email == null) Unauthorized();

            // Tính toán các thông số như ViewSchedule
            weekStart = GetStartOfWeek(weekStart);
            var weekEnd = weekStart.AddDays(6);
            var daysInWeek = Enumerable.Range(0, 7).Select(i => weekStart.AddDays(i)).ToList();

            var slots = _context.Slots.ToList();

            ViewBag.ListRoom = _roomRepo.GetAllActiveRoom();
            ViewBag.DaysInWeek = daysInWeek;
            ViewBag.SlotsPerDay = slots.Count();
            ViewBag.SelectedYear = year;
            ViewBag.SelectedWeekStart = weekStart;
            ViewBag.DoctorId = 0;

            // TempData["success"] = $"{weekStart}";
            return PartialView("_ScheduleTablePartial3", slots);
        }

        [Authorize(Roles = "Admin, Doctor")]
        [HttpPost]
        public IActionResult GetDoctorScheduleInWeek2(string doctorId, string year, string weekStart)
        {
            var slots = _context.Slots.ToList();
            ViewBag.SlotsPerDay = slots.Count();
            int parsedYear = int.Parse(year);


            if (!DateOnly.TryParse(weekStart, out var startDate) || string.IsNullOrEmpty(doctorId))
            {

                DateOnly selectedWeekStart;
                selectedWeekStart = GetStartOfWeek(DateOnly.FromDateTime(DateTime.Today));

                ViewBag.SelectedYear = parsedYear;
                ViewBag.SelectedWeekStart = selectedWeekStart;

                return PartialView("_ScheduleTablePartial3", slots);
            }
            int parsedDoctorId = int.Parse(doctorId);

            var endDate = startDate.AddDays(6);

            var schedules = _context.Schedules
                .Where(s => s.DoctorId == parsedDoctorId && s.Day >= startDate && s.Day <= endDate)
                .Select(s => new DoctorScheduleViewModel.ScheduleItem
                {
                    ScheduleId = s.ScheduleId,
                    Day = s.Day,
                    SlotId = s.SlotId,
                    StartTime = s.Slot.StartTime.ToString(@"hh\:mm"),
                    EndTime = s.Slot.EndTime.ToString(@"hh\:mm"),
                    RoomName = s.Room.RoomName,
                    RoomId = s.Room.RoomId,
                    DoctorId = parsedDoctorId
                })
                .ToList();

            ViewBag.DoctorSchedules = schedules;


            ViewBag.SelectedYear = parsedYear;
            ViewBag.SelectedWeekStart = startDate;

            return PartialView("_ScheduleTablePartial3", slots);
        }

        [Authorize(Roles = "Admin, Doctor")]
        [HttpPost]
        public IActionResult DeleteSchedule(int scheduleId)
        {
            try
            {
                var schedule = _context.Schedules.FirstOrDefault(s => s.ScheduleId == scheduleId);

                if (schedule == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy lịch làm việc." });
                }

                _context.Schedules.Remove(schedule);
                _context.SaveChanges();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi: " + ex.Message });
            }
        }

        [Authorize(Roles = "Admin, Doctor")]
        [HttpPost]
        public IActionResult UpdateSchedule(int scheduleId, DateOnly day, int slotId, int doctorId, int roomId)
        {

            var schedule = _context.Schedules.FirstOrDefault(s => s.ScheduleId == scheduleId);

            if (schedule == null)
                return Json(new { success = false, message = "Không tìm thấy lịch cần cập nhật." });

            var isConflict = _context.Schedules.Any(s =>
                s.ScheduleId != scheduleId && // bỏ qua chính nó
                s.DoctorId == doctorId &&
                s.Day == day &&
                s.SlotId == slotId
            );

            if (isConflict)
            {
                return Json(new { success = false, message = "Bác sĩ đã có lịch trong khung giờ này." });
            }

            schedule.Day = day;
            schedule.SlotId = slotId;
            schedule.DoctorId = doctorId;
            schedule.RoomId = roomId;

            _context.SaveChanges();
            return Json(new { success = true });
        }

        // ========================  expired slots  ========================


        [Authorize(Roles = "Admin")]
        [HttpGet]
        public IActionResult UpdateExpiredSlots()
        {
            return View(new List<DoctorScheduleViewModel.ScheduleItem>());
        }

        [HttpPost]
        public IActionResult UpdateExpiredSlotsConfirmed()
        {
            var today = DateOnly.FromDateTime(DateTime.Today);

            var expiredSchedules = _context.Schedules
                .Where(s => s.Day < today && s.Status == "Not Yet")
                .Include(s => s.Room)
                .Include(s => s.Slot)
                .Include(s => s.Doctor)
                .ToList();

            foreach (var s in expiredSchedules)
            {
                s.Status = "Absent";
            }

            _context.SaveChanges();


            var expiredItems = expiredSchedules
                .Select(s => new DoctorScheduleViewModel.ScheduleItem
                {
                    ScheduleId = s.ScheduleId,
                    Day = s.Day,
                    SlotId = s.SlotId,
                    StartTime = s.Slot.StartTime.ToString(@"hh\:mm"),
                    EndTime = s.Slot.EndTime.ToString(@"hh\:mm"),
                    RoomId = s.Room.RoomId,
                    RoomName = s.Room.RoomName,
                    DoctorId = s.DoctorId,
                    DoctorName = s.Doctor.GenerateDoctorCode(),
                    Status = s.Status // absent
                })
                .OrderBy(s => s.Day)
                .ThenBy(s => s.SlotId)
                .ThenBy(s => s.DoctorId)
                .ToList();

            TempData["success"] = $"Cập nhật {expiredItems.Count} lịch làm việc quá hạn thành công";
            return View("UpdateExpiredSlots", expiredItems);
        }

    }
}
