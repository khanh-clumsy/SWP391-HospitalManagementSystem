using HospitalManagement.Models;
using HospitalManagement.ViewModels.Package;
using X.PagedList;

namespace HospitalManagement.Repositories
{
    public interface IPackageRepository
    {
        public Task<IPagedList<PackageViewModel>> FilterPackagesAsync(string? categoryFilter, string? ageFilter, string? genderFilter, string? priceRangeFilter, int pageNumber, int pageSize, bool includeDeleted);
    }
}
