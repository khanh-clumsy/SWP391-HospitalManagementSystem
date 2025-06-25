using HospitalManagement.ViewModels;
namespace HospitalManagement.Repositories
{
    public interface IScheduleRepository
    {
        Task<List<RoomScheduleItemViewModel>> GetScheduleByRoomAndWeekAsync(int roomId, DateOnly weekStart);
        Task ChangeRoomForSchedulesAsync(List<int> scheduleIds, int newRoomId);
    }
}
