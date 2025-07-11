using HospitalManagement.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HospitalManagement.Repositories
{
    public interface ITestRepository
    {
        IEnumerable<Test> GetAll();
        Test GetById(int id);
        void Add(Test test);
        void Update(Test test);
        void Delete(int id);
        void Save();
        IEnumerable<Test> Search(string name, string sortOrder, decimal? minPrice, decimal? maxPrice);
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

