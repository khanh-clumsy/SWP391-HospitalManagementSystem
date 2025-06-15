
using HospitalManagement.Data;
using HospitalManagement.Models;
using Microsoft.EntityFrameworkCore;

public class NewsRepository : INewsRepository
{
    private readonly HospitalManagementContext _context;

    public NewsRepository(HospitalManagementContext context)
    {
        _context = context;
    }
    public async Task<News> GetByIdAsync(int id)
    {
        return await _context.News
            .Include(a => a.Doctor)
            .Include(a => a.Staff)
            .FirstOrDefaultAsync(a => a.NewsId == id);
    }

    public async Task<List<News>> GetAllAsync()
    {
        return await _context.News
            .Include(n => n.Doctor)
            .Include(n => n.Staff)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<News>> GetByDoctorIdAsync(int DoctorID)
    {
        return await _context.News
            .Include(a => a.Staff)
            .Where(a => a.DoctorId == DoctorID)
            .ToListAsync();
    }

    public async Task CreateAsync(News news)
    {
        _context.News.Add(news);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(News news)
    {
        _context.News.Update(news);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var news = await _context.News.FindAsync(id);
        if (news != null)
        {
            _context.News.Remove(news);
            await _context.SaveChangesAsync();
        }
    }
}