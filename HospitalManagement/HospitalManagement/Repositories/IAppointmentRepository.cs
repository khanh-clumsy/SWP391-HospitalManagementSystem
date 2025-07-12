using HospitalManagement.Models;

namespace HospitalManagement.Repositories
{
    public interface IAppointmentRepository
    {
        IQueryable<Appointment> GetAppointmentByPatientID(int PatientID);
        IQueryable<Appointment> GetAppointmentByDoctorID(int DoctorID);
        IQueryable<Appointment> GetAppointmentBySalesID(int SalesID);
        Task<Appointment> GetByIdAsync(int id);
        Task DeleteAsync(Appointment appointment);
        Task<List<Appointment>> Filter(string RoleKey, int UserID, string? Name, string? Slot, string? Date, string? Status);
        Task<List<Appointment>> FilterForAdmin(string? Name, string? slotId, string? Date, string? Status);

        Task<List<Appointment>> FilterApproveAppointment(string? statusFilter, string? searchName, string? timeFilter, string? dateFilter);
        Task<bool> HasAppointmentAsync(int doctorId, int slotId, DateOnly day);
        Task<List<Appointment>> GetAppointmentsAsync(string phone);
        Task StartAppointmentAsync(int appointmentId);
        Task<List<Appointment>> GetTodayAppointmentsAsync(string? phone);
        Task<List<Appointment>> GetOngoingAppointmentsByDoctorIdAsync(int doctorId);
        Task<Appointment> GetAppointmentByIdAsync(int appointmentId);
    }
}
