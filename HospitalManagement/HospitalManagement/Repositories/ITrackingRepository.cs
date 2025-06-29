using HospitalManagement.Models;

namespace HospitalManagement.Repositories
{
    public interface ITrackingRepository
    {
        Task<List<Appointment>> GetAppointmentsAsync(string phone = null);
        Task StartAppointmentAsync(int appointmentId);
        Task<Tracking> GetAppointmentByIdAsync(int appointmentId);

        Task<IEnumerable<Appointment>> GetOngoingAppointmentsByDoctorIdAsync(int doctorId);

        Task<List<Tracking>> GetRoomByAppointmentIdAsync(int appointmentId);
        Task<List<Room>> GetTestRoomsAsync();
    }
}
