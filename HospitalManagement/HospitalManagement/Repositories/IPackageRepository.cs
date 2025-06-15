using HospitalManagement.Models;
using HospitalManagement.ViewModels.Package;

namespace HospitalManagement.Repositories
{
    public interface IPackageRepository
    {
        public Task<IEnumerable<PackageViewModel>> FilterPackagesAsync(string? categoryFilter, string? ageFilter, string? genderFilter, string? priceRangeFilter);
    }
}
