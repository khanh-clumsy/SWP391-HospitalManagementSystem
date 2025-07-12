using HospitalManagement.Data;
using HospitalManagement.Models;
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

        public async Task<Test> GetByIdAsync(int id)
        {
            return await _context.Tests.FindAsync(id);
        }

        public async Task AddAsync(Test test)
        {
            await _context.Tests.AddAsync(test);
        }

        public async Task UpdateAsync(Test test)
        {
            _context.Tests.Update(test);
            await Task.CompletedTask;
        }

        public async Task DeleteAsync(int id)
        {
            var test = await _context.Tests.FindAsync(id);
            if (test != null)
                _context.Tests.Remove(test);
        }

        public async Task SaveAsync()
        {
           await _context.SaveChangesAsync();
        }
        public async Task<IEnumerable<Test>> SearchAsync(string name, string sortOrder, decimal? minPrice, decimal? maxPrice, bool includeDeleted = false)
        {
            var query = _context.Tests.AsQueryable();

            // Only exclude deleted tests if not admin
            if (!includeDeleted)
            {
                query = query.Where(t => !t.IsDeleted);
            }

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

            return await query.ToListAsync();
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
    }
}
