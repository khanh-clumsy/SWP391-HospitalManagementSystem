
using HospitalManagement.Data;
using HospitalManagement.Models;
using HospitalManagement.ViewModels;
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

    public async Task<List<NewsViewModel>> GetAllAsync()
    {
        return await _context.News
       .Select(n => new NewsViewModel
       {
           NewsId = n.NewsId,
           Title = n.Title,
           Description = n.Description,
           CreatedAt = n.CreatedAt,
           Thumbnail = n.Thumbnail,
           AuthorName = n.Doctor != null
               ? n.Doctor.FullName
               : (n.Staff != null ? n.Staff.FullName : "Không xác định")
       })
       .AsNoTracking()
       .ToListAsync();
    }

    public async Task<List<NewsViewModel>> GetByDoctorIdAsync(int doctorId)
    {
        return await _context.News
            .Where(n => n.DoctorId == doctorId)
            .Select(n => new NewsViewModel
            {
                NewsId = n.NewsId,
                Title = n.Title,
                Description = n.Description,
                CreatedAt = n.CreatedAt,
                Thumbnail = n.Thumbnail,
                AuthorName = n.Doctor != null
                    ? n.Doctor.FullName
                    : (n.Staff != null ? n.Staff.FullName : "Không xác định"),
                DoctorId = n.DoctorId,
                StaffId = n.StaffId
            })
            .AsNoTracking()
            .OrderByDescending(n => n.CreatedAt)
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