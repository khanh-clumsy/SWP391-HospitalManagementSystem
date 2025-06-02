using HospitalManagement.Models;

namespace HospitalManagement.Repositories
{
    public interface IStaffRepository
    {
        Task<List<Staff>> SearchAsync(string? name, string? roleName, int page, int pageSize);
        Task<int> CountAsync(string? name, string? roleName);
        Task<List<string>> GetDistinctRole();
    }
}
