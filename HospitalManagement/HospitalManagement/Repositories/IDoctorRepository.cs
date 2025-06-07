using HospitalManagement.Models;
namespace HospitalManagement.Repositories
{
    public interface IDoctorRepository
    {
        Task<List<Doctor>> GetAllAsync();
        Task<List<Doctor>> SearchAsync(string? name, string? department, int? exp, bool? isHead, string? sort, bool? isActive, int page, int pageSize);
        Task<Doctor> GetByIdAsync(int id);
        Task<int> CountAsync(string? name, string? department, int? exp, bool? isHead, bool? isActive);
        Task<List<Doctor>> GetAllDoctorsWithSpecialFirstAsync(int pageNumber, int pageSize);
        Task<int> CountAllActiveDoctorsAsync();
        Task<List<string>> GetDistinctDepartment();
    }
}
