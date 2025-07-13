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

        // Async CRUD operations
        Task<Test?> GetByIdAsync(int id);
        Task<List<Test>> GetAllAsync();
        Task<Test> CreateAsync(Test test);
        Task<Test> UpdateAsync(Test test);
        Task DeleteAsync(int id);
        Task<bool> ExistsAsync(int id);

        // Test-Package relationships
        Task<List<Package>> GetPackagesByTestIdAsync(int testId);
        Task<List<Test>> GetTestsByPackageIdAsync(int packageId);

        // Test records
        Task<List<TestRecord>> GetTestRecordsByTestIdAsync(int testId);
        Task<List<TestRecord>> GetTestRecordsByAppointmentIdAsync(int appointmentId);
        Task<TestRecord?> GetTestRecordByIdAsync(int testRecordId);
        Task<TestRecord> CreateTestRecordAsync(TestRecord testRecord);
        Task<TestRecord> UpdateTestRecordAsync(TestRecord testRecord);
        Task DeleteTestRecordAsync(int testRecordId);

    }
}

