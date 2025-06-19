using HospitalManagement.Data;
using HospitalManagement.ViewModels.Package;
using Microsoft.EntityFrameworkCore;
using X.PagedList;
using X.PagedList.Extensions;
using X.PagedList.EF;
using System.Threading.Tasks;

namespace HospitalManagement.Repositories
{
    public class PackageRepository : IPackageRepository
    {
        private readonly HospitalManagementContext _context;

        public PackageRepository(HospitalManagementContext context)
        {
            _context = context;
        }

        public async Task<IPagedList<PackageViewModel>> FilterPackagesAsync(string? categoryFilter, string? ageFilter, string? genderFilter, string? priceRangeFilter, int pageNumber, int pageSize)
        {
            var query = _context.Packages.AsQueryable();

            if (!string.IsNullOrEmpty(categoryFilter))
            {
                query = query.Where(p => p.PackageCategory.PackageCategoryId.ToString() == categoryFilter);
            }

            if (!string.IsNullOrEmpty(genderFilter) && (genderFilter != "A"))
            {
                query = query.Where(p => p.TargetGender == genderFilter);
            }

            if (!string.IsNullOrEmpty(ageFilter))
            {
                if (ageFilter.Contains("+") && int.TryParse(ageFilter.Replace("+", ""), out int minAge))
                {
                    query = query.Where(p => p.AgeFrom >= minAge); // Trên X tuổi
                }
                else
                {
                    var parts = ageFilter.Split('-');
                    if (parts.Length == 2 &&
                        int.TryParse(parts[0], out int fromAge) &&
                        int.TryParse(parts[1], out int toAge))
                    {
                        query = query.Where(p => p.AgeFrom == fromAge && p.AgeTo == toAge);
                    }
                }
            }

            if (!string.IsNullOrEmpty(priceRangeFilter))
            {
                if (priceRangeFilter.Contains("+") && decimal.TryParse(priceRangeFilter.Replace("+", ""), out decimal minPrice))
                {
                    query = query.Where(p => p.FinalPrice >= minPrice); // Trên X đồng
                }
                else
                {
                    var parts = priceRangeFilter.Split('-');
                    if (parts.Length == 2 &&
                        decimal.TryParse(parts[0], out decimal min) &&
                        decimal.TryParse(parts[1], out decimal max))
                    {
                        query = query.Where(p => p.FinalPrice >= min && p.FinalPrice <= max);
                    }
                }
            }

            return await query
                .OrderBy(p => p.PackageId)
                .Select(p => new PackageViewModel
                {
                    PackageId = p.PackageId,
                    PackageName = p.PackageName,
                    FinalPrice = p.FinalPrice,
                    OriginalPrice = p.OriginalPrice,
                    TargetGender = p.TargetGender,
                    AgeFrom = p.AgeFrom,
                    AgeTo = p.AgeTo,
                    Thumbnail = p.Thumbnail,
                    PackageCategory = new Models.PackageCategory
                    {
                        CategoryName = p.PackageCategory.CategoryName
                    }
                })
                .ToPagedListAsync(pageNumber, pageSize);
        }
    }
}
