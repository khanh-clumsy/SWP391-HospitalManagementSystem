using HospitalManagement.Data;
using HospitalManagement.Helpers;
using HospitalManagement.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace HospitalManagement.Repositories
{
    public class TestRepository : ITestRepository
    {
        private readonly HospitalManagementContext _context;

        public TestRepository(HospitalManagementContext context)
        {
            _context = context;
        }

        public IEnumerable<Test> GetAll()
        {
            return _context.Tests.ToList();
        }

        public Test GetById(int id)
        {
            return _context.Tests.Find(id);
        }

        public void Add(Test test)
        {
            _context.Tests.Add(test);
        }

        public void Update(Test test)
        {
            _context.Tests.Update(test);
        }

        public void Delete(int id)
        {
            var test = _context.Tests.Find(id);
            if (test != null)
                _context.Tests.Remove(test);
        }

        public void Save()
        {
            _context.SaveChanges();
        }

        public IEnumerable<Test> Search(string name, string sortOrder, decimal? minPrice, decimal? maxPrice)
        {
            var query = _context.Tests.AsQueryable();

            if (!string.IsNullOrEmpty(name))
            {
                name = string.Join(" ", name.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries));
                query = query.Where(t => t.Name.Contains(name));
            }

            if (minPrice.HasValue)
            {
                query = query.Where(t => t.Price >= minPrice.Value);
            }

            if (maxPrice.HasValue)
            {
                query = query.Where(t => t.Price <= maxPrice.Value);
            }

            if (sortOrder == "asc")
            {
                query = query.OrderBy(t => t.Price);
            }
            else if (sortOrder == "desc")
            {
                query = query.OrderByDescending(t => t.Price);
            }

            return query.ToList();
        }

        public async Task<List<Test>> GetAvailableTestsAsync()
        {
            return await _context.Tests
                .AsNoTracking()
                .Select(t => new Test { TestId = t.TestId, Name = t.Name })
                .ToListAsync();
        }

        public async Task<List<TestRecord>> GetUnassignedTestRecordsAsync(int appointmentId)
        {
            return await _context.TestRecords
                .Where(tr => tr.AppointmentId == appointmentId)
                .Where(tr =>
                    !_context.Trackings.Any(t => t.TestRecordId == tr.TestRecordId && t.AppointmentId == appointmentId)
                )
                .ToListAsync();
        }
        // Async CRUD operations
        public async Task<Test?> GetByIdAsync(int id)
        {
            return await _context.Tests.FirstOrDefaultAsync(t => t.TestId == id);
        }

        public async Task<List<Test>> GetAllAsync()
        {
            return await _context.Tests.ToListAsync();
        }

        public async Task<Test> CreateAsync(Test test)
        {
            _context.Tests.Add(test);
            await _context.SaveChangesAsync();
            return test;
        }

        public async Task<Test> UpdateAsync(Test test)
        {
            _context.Tests.Update(test);
            await _context.SaveChangesAsync();
            return test;
        }

        public async Task DeleteAsync(int id)
        {
            var test = await _context.Tests.FindAsync(id);
            if (test != null)
            {
                _context.Tests.Remove(test);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<bool> ExistsAsync(int id)
        {
            return await _context.Tests.AnyAsync(t => t.TestId == id);
        }

        // Test-Package relationships
        public async Task<List<Package>> GetPackagesByTestIdAsync(int testId)
        {
            return await _context.PackageTests
                .Where(pt => pt.TestId == testId)
                .Select(pt => pt.Package)
                .ToListAsync();
        }

        public async Task<List<Test>> GetTestsByPackageIdAsync(int packageId)
        {
            return await _context.PackageTests
                .Where(pt => pt.PackageId == packageId)
                .Select(pt => pt.Test)
                .ToListAsync();
        }

        // Test records
        public async Task<List<TestRecord>> GetTestRecordsByTestIdAsync(int testId)
        {
            return await _context.TestRecords
                .Where(tr => tr.TestId == testId)
                .ToListAsync();
        }

        public async Task<List<TestRecord>> GetTestRecordsByAppointmentIdAsync(int appointmentId)
        {
            return await _context.TestRecords
                .Where(tr => tr.AppointmentId == appointmentId)
                .ToListAsync();
        }

        public async Task<TestRecord?> GetTestRecordByIdAsync(int testRecordId)
        {
            return await _context.TestRecords.FindAsync(testRecordId);
        }

        public async Task<TestRecord> CreateTestRecordAsync(TestRecord testRecord)
        {
            _context.TestRecords.Add(testRecord);
            await _context.SaveChangesAsync();
            return testRecord;
        }

        public async Task<TestRecord> UpdateTestRecordAsync(TestRecord testRecord)
        {
            _context.TestRecords.Update(testRecord);
            await _context.SaveChangesAsync();
            return testRecord;
        }

        public async Task DeleteTestRecordAsync(int testRecordId)
        {
            var testRecord = await _context.TestRecords.FindAsync(testRecordId);
            if (testRecord != null)
            {
                _context.TestRecords.Remove(testRecord);
                await _context.SaveChangesAsync();
            }
        }


    }
}
