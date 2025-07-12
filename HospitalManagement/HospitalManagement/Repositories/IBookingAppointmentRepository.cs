using HospitalManagement.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace HospitalManagement.Repositories
{
    public interface IBookingAppointmentRepository
    {
        Task<List<SelectListItem>> GetDoctorSelectListAsync();
        Task<List<SelectListItem>> GetSlotSelectListAsync();
        Task<Patient?> GetPatientByPatientIdAsync(int accountId);
        Task AddAppointmentAsync(Appointment appointment);
        Task SaveChangesAsync();

    }
}
