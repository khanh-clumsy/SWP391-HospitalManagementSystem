// Repositories/INewsRepository.cs
using HospitalManagement.Models;

public interface INewsRepository
{
    Task<List<News>> GetAllAsync();
    Task<News> GetByIdAsync(int id);
    Task CreateAsync(News news);
    Task UpdateAsync(News news);
    Task DeleteAsync(int id);
}
