using HospitalManagement.Data;
using HospitalManagement.Models;
using HospitalManagement.Repositories;
using Microsoft.EntityFrameworkCore;

public class TestRepository : ITestRepository
{
    private readonly HospitalManagementContext _context;

    public TestRepository(HospitalManagementContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Test>> GetAllAsync()
    {
        return await _context.Tests.ToListAsync();
    }

    public async Task<Test> GetByIdAsync(int id)
    {
        return await _context.Tests.FindAsync(id);
    }

    public async Task AddAsync(Test test)
    {
        await _context.Tests.AddAsync(test);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Test test)
    {
        _context.Tests.Update(test);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Test test)
    {
       
            _context.Tests.Remove(test);
            await _context.SaveChangesAsync();
    }

    public async Task<IEnumerable<Test>> SearchAsync(string name, string sortOrder, decimal? minPrice, decimal? maxPrice)
    {
        var query = _context.Tests.AsQueryable();

        if (!string.IsNullOrEmpty(name))
        {

            name = name.Trim();
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
}
