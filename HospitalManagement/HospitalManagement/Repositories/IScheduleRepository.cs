using HospitalManagement.ViewModels;
namespace HospitalManagement.Repositories
{
    public interface IScheduleRepository
    {
        Task<List<RoomScheduleItemViewModel>> GetScheduleByRoomAndWeekAsync(int roomId, DateOnly weekStart);
        Task ChangeRoomForSchedulesAsync(List<int> scheduleIds, int newRoomId);
        Task<int?> GetCurrentWorkingRoomId(int doctorId);
        Task<List<ScheduleViewModel>> GetDoctorSchedulesInRangeAsync(int doctorId, DateOnly startDate, DateOnly endDate);
    }
}
