// Repositories/INewsRepository.cs
using HospitalManagement.Models;
using HospitalManagement.ViewModels;
public interface INewsRepository
{
    Task<List<NewsViewModel>> GetAllAsync();
    Task<List<NewsViewModel>> GetByDoctorIdAsync(int doctorId);

    Task<News> GetByIdAsync(int id);
    Task CreateAsync(News news);
    Task UpdateAsync(News news);
    Task DeleteAsync(int id);
}
