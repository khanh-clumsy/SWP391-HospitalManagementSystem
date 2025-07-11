using HospitalManagement.Models;
using HospitalManagement.ViewModels;

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
        Task<List<MonthlyAppointmentUsageDto>> GetMonthlyUsageStatsAsync(int year);
        Task<List<int>> GetAvailableYearsWithCompletedAppointmentsAsync();
        Task<(List<AppointmentDetailDto> Details, int TotalCount)> GetMonthlyAppointmentDetailsAsync(int year, int month, int page, int pageSize);
    }
}
