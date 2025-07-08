using HospitalManagement.Models;
using HospitalManagement.ViewModels;
namespace HospitalManagement.Repositories
{
    public interface IScheduleRepository
    {
        Task<List<RoomScheduleItemViewModel>> GetScheduleByRoomAndWeekAsync(int roomId, DateOnly weekStart);
        Task ChangeRoomForSchedulesAsync(List<int> scheduleIds, int newRoomId);
        Task<int?> GetCurrentWorkingRoomId(int doctorId);
        Task<List<ScheduleViewModel>> GetDoctorSchedulesInRangeAsync(int doctorId, DateOnly startDate, DateOnly endDate);
        Task<int?> GetRoomIdByDoctorSlotAndDayAsync(int doctorId, int slotId, DateOnly day);
        Task<Schedule?> GetScheduleWithRoomAsync(int doctorId, int slotId, DateOnly day);
    }
}
