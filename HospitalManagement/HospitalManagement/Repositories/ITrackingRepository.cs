using HospitalManagement.Models;

namespace HospitalManagement.Repositories
{
    public interface ITrackingRepository
    {
        Task<List<Tracking>> GetRoomByAppointmentIdAsync(int appointmentId);
        Task<List<Room>> GetTestRoomsAsync();
        Task<List<Tracking>> GetTrackingsByAppointmentIdWithDetailsAsync(int appointmentId);
    }
}
