using HospitalManagement.Models;

namespace HospitalManagement.Repositories
{
    public interface IAppointmentRepository
    {
        Task<List<Appointment>> GetAppointmentByPatientIDAsync(int PatientID);
        Task<List<Appointment>> GetAppointmentByDoctorIDAsync(int DoctorID);
        Task<List<Appointment>> GetAppointmentBySalesIDAsync(int SalesID);
        Task<Appointment> GetByIdAsync(int id);
        Task DeleteAsync(Appointment appointment);
        Task<List<Appointment>> Filter(string RoleKey, int UserID, string? Name, string? Slot, string? Date, string? Status);
        Task<List<Appointment>> FilterForAdmin(string? Name, string? slotId, string? Date, string? Status);

    }
}
