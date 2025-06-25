using HospitalManagement.Models;
using HospitalManagement.ViewModels;
namespace HospitalManagement.Repositories
{
    public interface IRoomRepository
    {
        Task<List<RoomWithDoctorDtoViewModel>> SearchAsync(string? name, string? building, string? floor, string? status, string? roomType, int page, int pageSize);
        Task<int> CountAsync(string? name, string? building, string? floor, string? status, string? roomType);
        Task<Room> GetByIdAsync(int id);
        Task<List<string>> GetAllDistinctBuildings();
        Task<List<string>> GetAllDistinctFloors();
        Task<List<string>> GetAllDistinctRoomTypes();
        Task<List<Room>> GetAvailableRoomsForSchedulesAsync(List<int> selectedScheduleIds);

    }
}
