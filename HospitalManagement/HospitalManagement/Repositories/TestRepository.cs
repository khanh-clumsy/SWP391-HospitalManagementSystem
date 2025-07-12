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
    }
}
