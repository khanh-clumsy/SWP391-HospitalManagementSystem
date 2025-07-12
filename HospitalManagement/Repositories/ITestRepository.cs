using HospitalManagement.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HospitalManagement.Repositories
{
    public interface ITestRepository
    {
        IEnumerable<Test> GetAll();
        Task<Test> GetByIdAsync(int id);
        Task AddAsync(Test test);
        Task UpdateAsync(Test test);
        Task DeleteAsync(int id);
        Task SaveAsync();
        Task<IEnumerable<Test>> SearchAsync(string name, string sortOrder, decimal? minPrice, decimal? maxPrice, bool includeDeleted = false);
        /// <summary>
        /// Lấy các TestRecord chưa được chỉ định phòng cho một appointment
        /// </summary>
        /// <returns>List các test đang khả dụng trong hệ thống</returns>
        Task<List<Test>> GetAvailableTestsAsync();
        /// <summary>
        /// Lấy các TestRecord chưa được chỉ định phòng cho một appointment
        /// </summary>
        /// <param name="appointmentId">ID cuộc hẹn</param>
        /// <returns>List TestRecord chưa có tracking</returns>
        Task<List<TestRecord>> GetUnassignedTestRecordsAsync(int appointmentId);
    }
}

