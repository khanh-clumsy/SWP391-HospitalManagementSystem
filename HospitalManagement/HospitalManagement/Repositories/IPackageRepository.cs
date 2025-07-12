using HospitalManagement.Models;
using HospitalManagement.ViewModels.Package;
using X.PagedList;

namespace HospitalManagement.Repositories
{
    public interface IPackageRepository
    {
        public Task<IPagedList<PackageViewModel>> FilterPackagesAsync(string? categoryFilter, string? ageFilter, string? genderFilter, string? priceRangeFilter, int pageNumber, int pageSize);
        
        // CRUD operations
        Task<Package?> GetByIdAsync(int id);
        Task<List<Package>> GetAllAsync();
        Task<Package> CreateAsync(Package package);
        Task<Package> UpdateAsync(Package package);
        Task DeleteAsync(int id);
        Task<bool> ExistsAsync(int id);
        
        // Package-Test relationships
        Task<List<Test>> GetTestsByPackageIdAsync(int packageId);
        Task AddTestToPackageAsync(int packageId, int testId);
        Task RemoveTestFromPackageAsync(int packageId, int testId);
        Task<List<Package>> GetPackagesByTestIdAsync(int testId);
        
        // Package categories
        Task<List<PackageCategory>> GetAllCategoriesAsync();
        Task<PackageCategory?> GetCategoryByIdAsync(int id);
    }
}
