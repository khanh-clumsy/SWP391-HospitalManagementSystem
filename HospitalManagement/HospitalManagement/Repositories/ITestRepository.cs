using HospitalManagement.Models;

public interface ITestRepository
{
    Task<IEnumerable<Test>> GetAllAsync();
    Task<Test> GetByIdAsync(int id);
    Task AddAsync(Test test);
    Task UpdateAsync(Test test);
    Task DeleteAsync(int id);

    Task<IEnumerable<Test>> SearchAsync(string name, string sortOrder, decimal? minPrice, decimal? maxPrice);
}
