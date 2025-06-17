using HospitalManagement.Data;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Rendering;
using AspNetCoreGeneratedDocument;
using Microsoft.AspNetCore.Authorization;

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
        public async Task<IActionResult> ViewSchedule(DateTime? weekStart)
        {
            var user = HttpContext.User;
            string email = user.FindFirstValue(ClaimTypes.Email);
            if (email == null) return Unauthorized();

            var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.Email == email);
            if (doctor == null) return NotFound();

            // Tính tuần đang được chọn (mặc định là tuần hiện tại)
            DateTime startOfWeek = weekStart ?? DateTime.Today.AddDays(-(int)DateTime.Today.DayOfWeek + 1); // Thứ 2 đầu tuần
            DateTime endOfWeek = startOfWeek.AddDays(6);

            var schedules = await _context.Schedules
                .Where(s => s.DoctorId == doctor.DoctorId && s.Day >= DateOnly.FromDateTime(startOfWeek) && s.Day <= DateOnly.FromDateTime(endOfWeek))
                .Include(s => s.Slot)
                .Include(s => s.Room)
                .ToListAsync();

            var result = schedules.Select(s => new DoctorScheduleViewModel.ScheduleItem
            {
                Day = s.Day.ToDateTime(TimeOnly.MinValue),
                StartTime = s.Slot.StartTime.ToString(@"hh\:mm"),
                EndTime = s.Slot.EndTime.ToString(@"hh\:mm"),
                SlotIndex = s.Slot.SlotId,
                RoomName = s.Room.RoomName
            }).ToList();

            return View(result);
        }

    }
}
