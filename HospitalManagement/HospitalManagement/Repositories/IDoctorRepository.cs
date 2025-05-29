using HospitalManagement.Models;
namespace HospitalManagement.Repositories
{
    public interface IDoctorRepository
    {
        Task<IEnumerable<Doctor>> GetAllAsync();
        Task<IEnumerable<Doctor>> SearchAsync(string? name, string? department, int? exp,bool? isHead, string? sort, int page,int pageSize);
        Task<Doctor> GetByIdAsync(int id);
        Task<int> CountAsync(string? name, string? department, int? exp, bool? isHead);
        Task<IEnumerable<string>> GetDistinctDepartment();
    }
}
