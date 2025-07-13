using HospitalManagement.Models;

namespace HospitalManagement.Repositories
{
    public interface IPatientRepository
    {
        Task<List<Patient>> SearchAsync(string? name, string? gender, int page, int pageSize);
        Task<int> CountAsync(string? name, string? gender);
        Task<Patient> GetByIdAsync(int id);
        Task<List<Patient>> GetOngoingPatients(int? doctordId);
        Task<List<Patient>> GetOngoingLabPatientsByRoom(int roomId);
        Task<int> CountActivePatientsAsync();
    }
}
