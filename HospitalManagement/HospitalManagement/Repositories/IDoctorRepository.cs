using HospitalManagement.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
namespace HospitalManagement.Repositories
{
    public interface IDoctorRepository
    {
        Task<List<Doctor>> GetAllAsync();
        Task<List<Doctor>> SearchAsync(string? name, string? department, int? exp, bool? isHead, string? sort, bool? isActive,bool? containTestDoc , int page, int pageSize);
        Task<Doctor> GetByIdAsync(int id);
        Task<int> CountAsync(string? name, string? department, int? exp, bool? isHead, bool? isActive, bool? containTestDoc);
        Task<List<Doctor>> GetAllDoctorsWithDepartment(string dep);
        Task<List<Doctor>> GetAllDoctorsWithSpecialFirstAsync(int pageNumber, int pageSize);
        Task<int> CountAllActiveDoctorsAsync();
        Task<List<string>> GetDistinctDepartment(bool? containTestDoc);
        Task<List<Doctor>> GetDoctorsBySchedule(List<int> ids);
        Task<List<SelectListItem>> GetAvailableDoctorsAsync(string departmentName, int slotId, DateOnly day, int excludeDoctorId);
        Task<(int xetNghiemCount, int otherCount)> CountDoctorsByDepartmentAsync();
    }
}
