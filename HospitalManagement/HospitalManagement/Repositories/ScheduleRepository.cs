using HospitalManagement.ViewModels;
using Microsoft.EntityFrameworkCore;
using HospitalManagement.Data;

namespace HospitalManagement.Repositories
{
    public class ScheduleRepository : IScheduleRepository
    {
        private readonly HospitalManagementContext _context;
        public ScheduleRepository(HospitalManagementContext context)
        {
            _context = context;
        }

        public async Task ChangeRoomForSchedulesAsync(List<int> scheduleIds, int newRoomId)
        {
            // Lấy tất cả lịch có ID trong danh sách
            var schedules = await _context.Schedules
                .Where(s => scheduleIds.Contains(s.ScheduleId))
                .ToListAsync();

            // Cập nhật RoomId cho từng lịch
            foreach (var schedule in schedules)
            {
                schedule.RoomId = newRoomId;
            }

            // Lưu thay đổi vào database
            await _context.SaveChangesAsync();
        }

        public async Task<List<RoomScheduleItemViewModel>> GetScheduleByRoomAndWeekAsync(int roomId, DateOnly weekStart)
        {
            var weekEnd = weekStart.AddDays(6);

            var schedule = await _context.Schedules
                .Where(s => s.RoomId == roomId && s.Day >= weekStart && s.Day <= weekEnd)
                .Include(s => s.Doctor)
                .Include(s => s.Slot)
                .Select(s => new RoomScheduleItemViewModel
                {
                    SlotId = s.SlotId,
                    Day = s.Day,
                    StartTime = s.Slot.StartTime,
                    EndTime = s.Slot.EndTime,
                    DoctorId = s.DoctorId,
                    DoctorName = s.Doctor.FullName,
                    ScheduleId = s.ScheduleId
                })
                .ToListAsync();

            return schedule;
        }
    }
}
