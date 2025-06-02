using HospitalManagement.Models;
namespace HospitalManagement.Repositories
{
    public interface IDoctorRepository
    {
        Task<List<Doctor>> GetAllAsync();
        Task<List<Doctor>> SearchAsync(string? name, string? department, int? exp,bool? isHead, string? sort, int page,int pageSize);
        Task<Doctor> GetByIdAsync(int id);
        Task<int> CountAsync(string? name, string? department, int? exp, bool? isHead);
        Task<List<Doctor>> GetAllDoctorsWithSpecialFirstAsync(int pageNumber, int pageSize);
        Task<int> CountAllDoctorsAsync();
        Task<List<string>> GetDistinctDepartment();
    }
}
