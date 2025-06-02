using HospitalManagement.Models;

namespace HospitalManagement.Repositories
{
    public interface IAppointmentRepository
    {
        Task<List<Appointment>> GetAppointmentByPatientIDAsync(int PatientID);
        Task<List<Appointment>> GetAppointmentByDoctorIDAsync(int DoctorID);
        Task<List<Appointment>> GetAppointmentBySalesIDAsync(int SalesID);

        Task<List<Appointment>> Filter(string RoleKey, int UserID, string? Name, string? Slot, string? Date, string? Status);
    }
}
