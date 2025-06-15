// Repositories/INewsRepository.cs
using HospitalManagement.Models;

public interface INewsRepository
{
    Task<List<News>> GetAllAsync();
    Task<List<News>> GetByDoctorIdAsync(int doctorId);

    Task<News> GetByIdAsync(int id);
    Task CreateAsync(News news);
    Task UpdateAsync(News news);
    Task DeleteAsync(int id);
}
